"""
Phase C — Hard retrieval eval set focused on the specific bug scenarios
the user reported, plus general robustness tests.

Each case has:
  query:        what the user types
  expected_intent: required intent (must match top-1 hit's intent)
  expected_entity_in: optional list — top-1 entityId must be in this set
  expected_phrase_in: optional list — top-1 answer must contain one of these substrings (case-insensitive)
  cat:          difficulty category for per-category breakdown
"""
import json
import sys
from pathlib import Path
from collections import defaultdict
import numpy as np
from sentence_transformers import SentenceTransformer

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
EMB_PATH   = MODELS_DIR / "embeddings.npy"
META_PATH  = MODELS_DIR / "qa_metadata.json"
INFO_PATH  = MODELS_DIR / "index_info.json"
FT_DIR     = MODELS_DIR / "minilm_ft"

EMB  = np.load(EMB_PATH)
META = json.loads(META_PATH.read_text(encoding="utf-8"))
INFO = json.loads(INFO_PATH.read_text(encoding="utf-8"))

USE_FT = FT_DIR.exists() and "--base" not in sys.argv
MODEL_NAME = str(FT_DIR) if USE_FT else INFO["modelName"]
print(f"[eval] using model: {MODEL_NAME}")
encoder = SentenceTransformer(MODEL_NAME)


CASES = [
    # ===== CLEAN — straightforward Vietnamese =====
    ("Sân vận động ở đâu",          "ASK_LOCATION",     ["SanVanDong"],  ["sân vận động"], "CLEAN"),
    ("Nhà ăn ở đâu",                "ASK_LOCATION",     ["NhaAn_Door"],  ["nhà ăn"],       "CLEAN"),
    ("Lớp học ở đâu",               "ASK_LOCATION",     ["LopHoc_Door"], ["lớp học"],      "CLEAN"),
    ("Ký túc xá ở đâu",             "ASK_LOCATION",     ["KTX_Door"],    ["ký túc xá"],    "CLEAN"),
    ("Khu tự do ở đâu",             "ASK_LOCATION",     ["FreeArea"],    ["tự do"],        "CLEAN"),
    ("Khu dọn vệ sinh ở đâu",       "ASK_LOCATION",     ["DonVeSinh"],   ["dọn vệ sinh"],  "CLEAN"),
    ("Mấy giờ ăn cơm",              "ASK_TIME",         None,            None,             "CLEAN"),
    ("Mấy giờ tập thể dục",         "ASK_TIME",         ["slot_1"],      None,             "CLEAN"),
    ("Tập thể dục mấy giờ",         "ASK_TIME",         ["slot_1"],      None,             "CLEAN"),
    ("Hôm nay học môn gì",          "ASK_SCHEDULE_TODAY", None,          None,             "CLEAN"),
    ("Hôm nay có gì",               "ASK_SCHEDULE_TODAY", None,          None,             "CLEAN"),
    ("Lịch hôm nay sao",            "ASK_SCHEDULE_TODAY", None,          None,             "CLEAN"),
    ("Đại đội trưởng là ai",        "ASK_NPC",          ["DaiDoiTruong"], ["chỉ huy"],     "CLEAN"),
    ("Lịch sử là môn gì",           "ASK_SUBJECT_INFO", ["LichSu"],      ["Lịch sử"],      "CLEAN"),
    ("Chính trị là môn gì",         "ASK_SUBJECT_INFO", ["ChinhTri"],    ["Chính trị"],    "CLEAN"),
    ("GDQP là môn gì",              "ASK_SUBJECT_INFO", ["GDQuocPhong"], ["Quốc phòng"],   "CLEAN"),
    ("Em chào thủ trưởng",          "GREETING",         None,            None,             "CLEAN"),
    ("Em xin phép",                 "GOODBYE",          None,            None,             "CLEAN"),
    ("Em cảm ơn",                   "THANKS",           None,            None,             "CLEAN"),

    # ===== NO_ACCENT — mất dấu =====
    ("san van dong o dau",          "ASK_LOCATION",     ["SanVanDong"],  None,             "NO_ACCENT"),
    ("nha an o dau",                "ASK_LOCATION",     ["NhaAn_Door"],  None,             "NO_ACCENT"),
    ("lop hoc o dau",               "ASK_LOCATION",     ["LopHoc_Door"], None,             "NO_ACCENT"),
    ("ky tuc xa o dau",             "ASK_LOCATION",     ["KTX_Door"],    None,             "NO_ACCENT"),
    ("may gio an com",              "ASK_TIME",         None,            None,             "NO_ACCENT"),
    ("hom nay hoc gi",              "ASK_SCHEDULE_TODAY", None,          None,             "NO_ACCENT"),
    ("dai doi truong la ai",        "ASK_NPC",          ["DaiDoiTruong"], None,            "NO_ACCENT"),
    ("don ve sinh o dau",           "ASK_LOCATION",     ["DonVeSinh"],   None,             "NO_ACCENT"),

    # ===== TELEX_TYPOS — gõ telex dư ký tự =====
    ("Saan vaan ddoongg o ddaau",   "ASK_LOCATION",     ["SanVanDong"],  None,             "TELEX_TYPOS"),
    ("Nhaa aan o ddaau",            "ASK_LOCATION",     ["NhaAn_Door"],  None,             "TELEX_TYPOS"),
    ("Maay giio aan coom",          "ASK_TIME",         None,            None,             "TELEX_TYPOS"),

    # ===== CODE_MIX — chèn English =====
    ("Where is sân vận động",       "ASK_LOCATION",     ["SanVanDong"],  None,             "CODE_MIX"),
    ("Where is canteen",            "ASK_LOCATION",     ["NhaAn_Door"],  None,             "CODE_MIX"),
    ("Where is classroom",          "ASK_LOCATION",     ["LopHoc_Door"], None,             "CODE_MIX"),
    ("Where is dormitory",          "ASK_LOCATION",     ["KTX_Door"],    None,             "CODE_MIX"),
    ("Today học gì",                "ASK_SCHEDULE_TODAY", None,          None,             "CODE_MIX"),
    ("Schedule hôm nay",            "ASK_SCHEDULE_TODAY", None,          None,             "CODE_MIX"),
    ("What time ăn cơm",            "ASK_TIME",         None,            None,             "CODE_MIX"),

    # ===== ELLIPSIS — câu cụt =====
    ("nhà ăn?",                     "ASK_LOCATION",     ["NhaAn_Door"],  None,             "ELLIPSIS"),
    ("lớp học?",                    "ASK_LOCATION",     ["LopHoc_Door"], None,             "ELLIPSIS"),
    ("KTX đâu",                     "ASK_LOCATION",     ["KTX_Door"],    None,             "ELLIPSIS"),
    ("Đói",                         None,               None,            None,             "ELLIPSIS"),  # ambiguous

    # ===== SLANG — viết tắt / từ lóng =====
    ("svd đâu v",                   "ASK_LOCATION",     ["SanVanDong"],  None,             "SLANG"),
    ("ktx đâu nhỉ",                 "ASK_LOCATION",     ["KTX_Door"],    None,             "SLANG"),
    ("ddt la ai v",                 "ASK_NPC",          ["DaiDoiTruong"], None,            "SLANG"),
    ("gdqp hok cai gi",             "ASK_SUBJECT_INFO", ["GDQuocPhong"], None,             "SLANG"),

    # ===== AMBIGUOUS / OOS =====
    ("Crypto giảm sốc",             "OUT_OF_SCOPE",     None,            None,             "OOS"),
    ("iPhone giá bao nhiêu",        "OUT_OF_SCOPE",     None,            None,             "OOS"),
    ("Messi đá hay không",          "OUT_OF_SCOPE",     None,            None,             "OOS"),
    ("Wifi password",               "OUT_OF_SCOPE",     None,            None,             "OOS"),

    # ===== ADVERSARIAL — confusable phrasings =====
    ("Em đói",                      None,               None,            None,             "ADVERSARIAL"),  # could be smalltalk or ASK_TIME meal
    ("Em buồn ngủ",                 None,               None,            None,             "ADVERSARIAL"),
    ("Mệt quá",                     "SMALL_TALK",       None,            None,             "ADVERSARIAL"),

    # ===== THE ORIGINAL BUG SCENARIO =====
    # User typed "khu A1/A2/A3/B" - these are NOT real game areas.
    # Test that we either say "I don't know" (low conf) or accept it as ambiguous
    # but NOT mode-collapse to the same answer.
    ("khu A1 ở đâu",                None,               None,            None,             "ORIGINAL_BUG"),
    ("khu A2 ở đâu",                None,               None,            None,             "ORIGINAL_BUG"),
    ("khu A3 ở đâu",                None,               None,            None,             "ORIGINAL_BUG"),
    ("khu B ở đâu",                 None,               None,            None,             "ORIGINAL_BUG"),
]


def encode_query(text: str):
    return encoder.encode(
        [text],
        normalize_embeddings=True,
        convert_to_numpy=True,
        show_progress_bar=False,
    ).astype(np.float32)[0]


def retrieve(query: str, k: int = 5):
    q = encode_query(query)
    sims = EMB @ q
    idx = np.argsort(-sims)[:k]
    return [(float(sims[i]), META[i]) for i in idx]


def case_passes(top_score: float, top: dict, expected_intent, expected_entity_in, expected_phrase_in) -> bool:
    if expected_intent is not None and top["intent"] != expected_intent:
        return False
    if expected_entity_in is not None and top["entityId"] not in expected_entity_in:
        return False
    if expected_phrase_in is not None:
        ans = top["answer"].lower()
        if not any(p.lower() in ans for p in expected_phrase_in):
            return False
    return True


def main():
    results = []
    by_cat = defaultdict(list)
    print(f"\n[eval] running {len(CASES)} cases ...\n")
    for i, (q, ei, ee, ep, cat) in enumerate(CASES, 1):
        hits = retrieve(q, k=3)
        top_score, top = hits[0]
        ok = case_passes(top_score, top, ei, ee, ep) if ei is not None else True
        results.append({
            "case_num":  i,
            "query":     q,
            "category":  cat,
            "expected_intent":  ei,
            "expected_entity":  ee,
            "expected_phrase":  ep,
            "top_intent":       top["intent"],
            "top_entity":       top["entityId"],
            "top_score":        top_score,
            "top_answer":       top["answer"],
            "passed":           ok,
        })
        by_cat[cat].append(ok)
        flag = "OK" if ok else "FAIL"
        print(f"  [{flag:4s}] {cat:14s} score={top_score:.2f} | {top['intent']:18s} | {top['entityId']:15s} | Q: {q}")
        if not ok:
            print(f"          expected intent={ei} entity={ee} phrase={ep}")
            print(f"          got      intent={top['intent']} entity={top['entityId']}")
            print(f"          answer  : {top['answer']}")

    n_total = sum(1 for _, ei, *_ in CASES if ei is not None)
    n_passed = sum(1 for r in results if r["passed"] and r["expected_intent"] is not None)
    print(f"\n[eval] OVERALL: {n_passed}/{n_total} = {n_passed/n_total*100:.1f}%")
    print(f"\n[eval] By category:")
    for cat, oks in sorted(by_cat.items()):
        n = sum(1 for ok in oks if ok)
        total = len(oks)
        print(f"  {cat:14s} {n}/{total} = {n/total*100:.1f}%")

    out_path = MODELS_DIR / ("eval_hardset_ft.json" if USE_FT else "eval_hardset_base.json")
    out_path.write_text(
        json.dumps({
            "model":     MODEL_NAME,
            "useFt":     USE_FT,
            "overall":   {"passed": n_passed, "total": n_total, "acc": n_passed/n_total},
            "byCategory": {cat: {"passed": sum(1 for ok in oks if ok), "total": len(oks)} for cat, oks in by_cat.items()},
            "results":   results,
        }, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"\n[eval] wrote {out_path}")


if __name__ == "__main__":
    main()
