"""
Phase C — Local HTTP chat server.

Architecture (RAG = Retrieval-Augmented Generation):
  1. /retrieve  – embed query, cosine vs Q&A bank, return top-K hits.
  2. /chat      – run /retrieve, then EITHER:
                  - if top-1 score >= HIGH_TH  → return template answer directly
                  - if score >= LOW_TH         → forward top-K as RAG context to Groq LLM
                  - else                       → polite "I don't know" with suggestions

The Unity client only needs HTTP - no SentencePiece port, no ONNX integration on day-1.

Start:
  python chat_server.py
  (binds 127.0.0.1:8765)

Endpoints:
  GET  /healthz
  POST /retrieve   {"query": "...", "k": 5}
  POST /chat       {"query": "...", "gameState": {...}, "useGroq": true|false}
"""
import json
import os
import time
from pathlib import Path
from typing import Optional

import numpy as np
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import httpx

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
DATA_DIR   = ROOT / "data"

# Prefer the highest-versioned fine-tuned model if any exist.
# finetune_embedding.py saves to minilm_ft_v2, minilm_ft_v3, ... so the
# newest checkpoint always wins.
def _latest_ft_dir(base: Path):
    cands = sorted(base.glob("minilm_ft_v*"), reverse=True)
    if cands:
        return cands[0]
    legacy = base / "minilm_ft"
    return legacy if legacy.exists() else None

FT_DIR = _latest_ft_dir(MODELS_DIR) or (MODELS_DIR / "minilm_ft")
INFO   = json.loads((MODELS_DIR / "index_info.json").read_text(encoding="utf-8"))
META   = json.loads((MODELS_DIR / "qa_metadata.json").read_text(encoding="utf-8"))
EMB    = np.load(MODELS_DIR / "embeddings.npy")
GAME   = json.loads((DATA_DIR / "game_entities.json").read_text(encoding="utf-8"))

# Confidence thresholds — tuned from smoke_test_server.py results.
HIGH_TH = 0.75   # confident exact answer -> use template
LOW_TH  = 0.55   # weak match -> still RAG-feed to LLM, otherwise OOS

# Groq settings (read from env if present, fall back to project default).
GROQ_KEY    = os.getenv("GROQ_API_KEY", "")
GROQ_URL    = "https://api.groq.com/openai/v1/chat/completions"
GROQ_MODEL  = os.getenv("GROQ_MODEL", "llama3-8b-8192")

# Load model after constants so a startup error is visible.
print("[server] loading sentence encoder...")
if FT_DIR.exists():
    print(f"[server] using fine-tuned model: {FT_DIR}")
    encoder = SentenceTransformer(str(FT_DIR))
else:
    print(f"[server] using base model: {INFO['modelName']}")
    encoder = SentenceTransformer(INFO["modelName"])


# Re-embed bank if model fingerprint mismatches.
def maybe_reembed():
    global EMB
    fingerprint_path = MODELS_DIR / "_bank_fp.json"
    current_name = str(FT_DIR if FT_DIR.exists() else INFO["modelName"])
    fp = {}
    if fingerprint_path.exists():
        fp = json.loads(fingerprint_path.read_text(encoding="utf-8"))
    if fp.get("model") == current_name and fp.get("count") == int(EMB.shape[0]):
        return
    print(f"[server] re-embedding bank with {current_name} ...")
    questions = [m["question"] for m in META]
    EMB = encoder.encode(
        questions,
        batch_size=64,
        show_progress_bar=False,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float32)
    np.save(MODELS_DIR / "embeddings.npy", EMB)
    fingerprint_path.write_text(
        json.dumps({"model": current_name, "count": int(EMB.shape[0])}, ensure_ascii=False),
        encoding="utf-8",
    )
    print(f"[server] re-embedded {EMB.shape[0]} records")


maybe_reembed()

app = FastAPI(title="Phase C Academy NPC Chat", version="0.1")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


# ---------- Request/response models ----------
class RetrieveReq(BaseModel):
    query: str
    k:     int = 5


class ChatReq(BaseModel):
    query:     str
    gameState: Optional[dict] = None   # {currentDay, currentTime, currentArea, ...}
    useGroq:   bool = True
    k:         int = 5


# ---------- Game-state helpers ----------
def resolve_schedule_today(game_state: Optional[dict]) -> str:
    day = (game_state or {}).get("currentDay", 1)
    routine = GAME["routine"]
    lines = []
    for slot_str in sorted(routine.keys(), key=int):
        info = routine[slot_str]
        area = GAME["areas"].get(info["areaId"], {}).get("displayName", info["areaId"] or "?")
        subj = ""
        if info.get("subjects"):
            idx = (day - 1 + (int(slot_str) - 4)) % len(info["subjects"]) if info["subjects"] else 0
            subj_id = info["subjects"][idx] if info["subjects"] else None
            if subj_id:
                subj = " (" + GAME["subjects"][subj_id]["displayName"] + ")"
        lines.append(f"  {info['startTime']}-{info['endTime']}: {info['displayVi']}{subj} @ {area}")
    return f"Lịch ngày {day}:\n" + "\n".join(lines)


def resolve_placeholders(answer: str, game_state: Optional[dict]) -> str:
    if "{__SCHEDULE_TODAY__}" in answer:
        return answer.replace("{__SCHEDULE_TODAY__}", resolve_schedule_today(game_state))
    return answer


# ---------- Retrieval ----------
def encode_query(text: str) -> np.ndarray:
    v = encoder.encode(
        [text],
        normalize_embeddings=True,
        convert_to_numpy=True,
        show_progress_bar=False,
    ).astype(np.float32)[0]
    return v


def retrieve(query: str, k: int) -> list[dict]:
    q = encode_query(query)
    sims = EMB @ q
    idx = np.argsort(-sims)[:k]
    return [
        {
            "score":      float(sims[i]),
            "id":         META[i]["id"],
            "question":   META[i]["question"],
            "answer":     META[i]["answer"],
            "intent":     META[i]["intent"],
            "entityId":   META[i]["entityId"],
            "entityType": META[i]["entityType"],
            "needsGameState": META[i].get("needsGameState", False),
        }
        for i in idx
    ]


# ---------- Groq call ----------
def groq_chat(user_q: str, context_hits: list[dict], game_state: Optional[dict]) -> str:
    if not GROQ_KEY:
        return "[Groq fallback chưa cấu hình API key. Set env GROQ_API_KEY.]"
    sys_lines = [
        "Bạn là Đại đội trưởng đại đội học viên - chỉ huy quân sự nghiêm khắc nhưng quan tâm.",
        "Trả lời ngắn gọn, dứt khoát, dùng xưng hô 'đồng chí'. Chỉ trả lời 1-2 câu.",
        "Nếu câu hỏi nằm ngoài hoạt động đại đội, từ chối lịch sự và gợi ý các chủ đề bạn biết.",
        "",
        "Thông tin đại đội:",
        f"- 6 khu: " + ", ".join(a["displayName"] for a in GAME["areas"].values()),
        f"- 3 môn học: " + ", ".join(s["displayName"] for s in GAME["subjects"].values()),
        f"- 8 hoạt động hằng ngày: " + ", ".join(GAME["routine"][s]["displayVi"] for s in sorted(GAME["routine"].keys(), key=int)),
    ]
    if game_state:
        if "currentDay" in game_state:
            sys_lines.append(f"- Hôm nay là ngày {game_state['currentDay']}/30")
        if "currentTime" in game_state:
            sys_lines.append(f"- Bây giờ là {game_state['currentTime']}")
        if "currentArea" in game_state:
            sys_lines.append(f"- Đồng chí đang ở {game_state['currentArea']}")
    sys_lines.append("")
    sys_lines.append("Tham khảo Q&A từ ngân hàng tri thức (chọn ý gần nhất, paraphrase lại tự nhiên):")
    for h in context_hits[:3]:
        sys_lines.append(f"- Q: {h['question']!r} | A: {h['answer']}")
    system_prompt = "\n".join(sys_lines)
    body = {
        "model":       GROQ_MODEL,
        "messages":    [
            {"role": "system", "content": system_prompt},
            {"role": "user",   "content": user_q},
        ],
        "temperature": 0.4,
        "max_tokens":  180,
    }
    headers = {"Authorization": f"Bearer {GROQ_KEY}", "Content-Type": "application/json"}
    try:
        with httpx.Client(timeout=15.0) as cli:
            r = cli.post(GROQ_URL, headers=headers, json=body)
            r.raise_for_status()
            return r.json()["choices"][0]["message"]["content"].strip()
    except Exception as exc:
        return f"[Groq error: {exc}]"


# ---------- Endpoints ----------
@app.get("/healthz")
def healthz():
    return {
        "ok": True,
        "modelName":    str(FT_DIR) if FT_DIR.exists() else INFO["modelName"],
        "bankSize":     int(EMB.shape[0]),
        "dim":          int(EMB.shape[1]),
        "highTh":       HIGH_TH,
        "lowTh":        LOW_TH,
        "groqEnabled":  bool(GROQ_KEY),
        "groqModel":    GROQ_MODEL,
    }


@app.post("/retrieve")
def endpoint_retrieve(req: RetrieveReq):
    t0 = time.time()
    hits = retrieve(req.query, req.k)
    return {"hits": hits, "latencyMs": (time.time() - t0) * 1000}


@app.post("/chat")
def endpoint_chat(req: ChatReq):
    t0 = time.time()
    hits = retrieve(req.query, req.k)
    top = hits[0]
    score = top["score"]
    route = "unknown"
    answer = ""

    if score >= HIGH_TH:
        route  = "template"
        answer = resolve_placeholders(top["answer"], req.gameState)
    elif score >= LOW_TH and req.useGroq:
        route  = "groq_rag"
        answer = groq_chat(req.query, hits, req.gameState)
    else:
        route  = "fallback"
        answer = (
            "Tôi chưa hiểu rõ câu hỏi. Đồng chí có thể hỏi về: "
            + "lịch hôm nay, giờ ăn, vị trí (sân vận động / nhà ăn / lớp học / ký túc xá / khu dọn vệ sinh / khu tự do), "
            + "ba môn học, hoặc thông tin Đại đội trưởng."
        )
    return {
        "answer":    answer,
        "route":     route,
        "score":     score,
        "topIntent": top["intent"],
        "topEntity": top["entityId"],
        "hits":      hits[:3],
        "latencyMs": (time.time() - t0) * 1000,
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8765, log_level="info")
