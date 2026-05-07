from __future__ import annotations
import io
import json
import sys as _sys
from pathlib import Path

if hasattr(_sys.stdout, "buffer"):
    _sys.stdout = io.TextIOWrapper(_sys.stdout.buffer, encoding="utf-8")

import torch
import torch.nn.functional as F

import sys
sys.path.insert(0, str(Path(__file__).parent))
from dataset import encode
from model import build_model

ROOT = Path(__file__).resolve().parent.parent
MODEL_DIR = ROOT / "models"
UNITY_ROOT = ROOT.parent.parent
SLOT_VOCAB_PATH = UNITY_ROOT / "Assets" / "AI" / "Resources" / "slot_vocab.json"

def load_v4():
    ckpt = torch.load(MODEL_DIR / "lstm_v4_best.pt", map_location="cpu", weights_only=True)
    model = build_model("lstm", ckpt["vocab_size"], ckpt["num_classes"])
    model.load_state_dict(ckpt["state_dict"])
    model.eval()
    with open(MODEL_DIR / "vocab_v4.json", encoding="utf-8") as f:
        vocab = json.load(f)
    with open(MODEL_DIR / "id2label_v4.json", encoding="utf-8") as f:
        id2label = {int(k): v for k, v in json.load(f).items()}
    return model, vocab, id2label, ckpt["max_len"]

def load_slots() -> dict[str, list[str]]:
    with open(SLOT_VOCAB_PATH, encoding="utf-8") as f:
        data = json.load(f)
    return {k: v for k, v in data.items() if not k.startswith("_")}

def normalize(text: str) -> str:
    out = []
    for c in text.lower():
        if c.isalnum() or c.isspace():
            out.append(c)
        else:
            out.append(" ")
    s = " " + " ".join("".join(out).split()) + " "
    return s

def extract(text: str, slot_vocab: dict[str, list[str]]) -> dict[str, str]:
    """Mirror EntityExtractor.cs Extract logic."""
    norm = normalize(text)
    result = {}
    for slot, phrases in slot_vocab.items():
        for phrase in phrases:
            padded = " " + phrase.lower() + " "
            if padded in norm:
                result[slot] = phrase
                break
    return result

def classify(model, vocab, id2label, max_len, text: str) -> tuple[str, float]:
    ids = torch.tensor([encode(text, vocab, max_len)], dtype=torch.long)
    with torch.no_grad():
        probs = F.softmax(model(ids), dim=-1)[0]
    idx = int(probs.argmax())
    return id2label[idx], float(probs[idx])

def main():
    model, vocab, id2label, max_len = load_v4()
    slots_db = load_slots()
    print(f"[load] v4 LSTM ready - vocab={len(vocab)} classes={len(id2label)} max_len={max_len}")
    print(f"[load] slot vocab:" + ", ".join(f"{k}={len(v)}" for k, v in slots_db.items()))

    # User pain-point cases + adversarial mix
    cases = [
        ("Khu A ở đâu",                      "HOI_VI_TRI"),
        ("Khu C nằm chỗ nào",                "HOI_VI_TRI"),
        ("Phòng 305 ở tầng mấy",             "HOI_VI_TRI"),
        ("Khu B5 đi đường nào",              "HOI_VI_TRI"),
        ("Mai ăn cơm mấy giờ",               "HOI_GIO_AN"),
        ("Tối nay đại đội ăn lúc mấy giờ",   "HOI_GIO_AN"),
        ("RSA hoạt động ra sao",             "HOI_KIEN_THUC"),
        ("Em xin nghỉ vì sốt cao",           "XIN_PHEP"),
        ("Báo cáo trung đội 2 đủ quân",      "BAO_CAO"),
        ("Em chào thầy em đi học đây",       "TAM_BIET"),
        ("ngày mai có gì",                   "HOI_LICH"),
        ("z hôm nay làm gì",                 "HOI_LICH"),
        ("đói lắm rồi",                      "HOI_GIO_AN"),
        ("lạc đường rồi",                    "HOI_VI_TRI"),
        ("schedule mai sao",                 "HOI_LICH"),
        ("Wifi yếu quá",                     "OUT_OF_SCOPE"),
    ]
    pass_count = 0
    print("\nv4 + slot extractor sanity")
    for text, expected in cases:
        intent, conf = classify(model, vocab, id2label, max_len, text)
        ok = intent == expected
        if ok: pass_count += 1
        slot_dict = extract(text, slots_db)
        slot_str = "  ".join(f"{k}=\"{v}\"" for k, v in slot_dict.items()) or "(no slots)"
        mark = "OK " if ok else "X  "
        print(f"{mark} \"{text:<35s}\" -> {intent:<14s} ({conf*100:5.1f}%) [exp {expected:<14s}]  slots: {slot_str}")
    print(f"\nScore: {pass_count}/{len(cases)} = {pass_count/len(cases)*100:.1f}%")

if __name__ == "__main__":
    main()
