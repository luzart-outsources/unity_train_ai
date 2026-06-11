"""
Phase C ITER 4 — v10 dataset: v9 + AGGRESSIVE telex saturation.

Goal: fix the 2/3 TELEX_TYPOS failures still present in v3 / v4.

Strategy: for each canonical real-area query and meal time query,
generate 50+ heavy-telex variants and 30+ no-accent + telex combo
variants. Saturate the vocab + train signal so student learns
"Saan vaan ddoongg" maps to the same vector as "Sân vận động".
"""
import json
import random
import re
import unicodedata
from pathlib import Path

random.seed(2026)

SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
V9_PATH    = DATA_DIR / "qa_pairs_v9.jsonl"
OUT_PATH   = DATA_DIR / "qa_pairs_v10.jsonl"


VOWELS = "aeiouôơêăâAEIOUÔƠÊĂÂ"


def heavy_telex(text: str, rate: float) -> str:
    out = []
    for ch in text:
        out.append(ch)
        if ch in VOWELS and random.random() < rate:
            out.append(ch)
    return "".join(out)


def no_accent(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    out = "".join(c for c in nfkd if not unicodedata.combining(c))
    return out.replace("Đ", "D").replace("đ", "d")


# Canonical queries + answers
CANON = {
    "SanVanDong": {
        "queries": [
            "Sân vận động ở đâu", "Sân vận động ở chỗ nào",
            "Khu thể dục ở đâu", "Sân tập thể dục chỗ nào",
            "Sân chạy ở đâu", "Sân thể thao ở chỗ nào",
        ],
        "answer": "Sân vận động nằm ở phía đông doanh trại, đi thẳng khoảng 98m là tới.",
    },
    "NhaAn_Door": {
        "queries": [
            "Nhà ăn ở đâu", "Nhà ăn ở chỗ nào",
            "Phòng ăn ở đâu", "Khu ẩm thực ở chỗ nào",
            "Chỗ ăn cơm ở đâu", "Căng tin ở đâu",
        ],
        "answer": "Nhà ăn ở trung tâm doanh trại, đi 65m theo lối nội bộ.",
    },
    "LopHoc_Door": {
        "queries": [
            "Lớp học ở đâu", "Phòng học ở đâu",
            "Giảng đường ở đâu", "Lớp học ở chỗ nào",
        ],
        "answer": "Lớp học ở phía bắc doanh trại, đi 89m là tới.",
    },
    "KTX_Door": {
        "queries": [
            "Ký túc xá ở đâu", "Phòng ngủ ở đâu",
            "Doanh trại ở đâu", "Nhà ở ở đâu", "Chỗ ngủ ở đâu",
        ],
        "answer": "Ký túc xá ở phía nam doanh trại, đi 72m.",
    },
    "FreeArea": {
        "queries": [
            "Khu tự do ở đâu", "Khu giải trí ở đâu",
            "Chỗ nghỉ ngơi ở đâu", "Khu thư giãn ở đâu",
        ],
        "answer": "Khu tự do ở phía đông nam doanh trại, đi 61m.",
    },
    "DonVeSinh": {
        "queries": [
            "Khu dọn vệ sinh ở đâu", "Khu vệ sinh ở đâu",
            "Khu tăng gia ở đâu", "Khu lao động ở đâu",
        ],
        "answer": "Khu dọn vệ sinh ở phía tây nam doanh trại, đi 102m.",
    },
}

MEAL_CANON = {
    "slot_3": {
        "queries": [
            "Mấy giờ ăn sáng", "Ăn sáng mấy giờ", "Bao giờ ăn sáng",
            "Bữa sáng mấy giờ",
        ],
        "answer": "Ăn sáng từ 07:00 đến 07:30, đồng chí đến đúng giờ nhé.",
    },
    "slot_5": {
        "queries": [
            "Mấy giờ ăn trưa", "Ăn trưa mấy giờ", "Bao giờ ăn trưa",
            "Mấy giờ ăn cơm trưa", "Nghỉ trưa mấy giờ",
            "Mấy giờ ăn cơm",  # important — fixes v1 bug
        ],
        "answer": "Nghỉ trưa và ăn trưa từ 11:30 đến 14:00, có 2.5 tiếng cả ăn lẫn nghỉ.",
    },
    "slot_8": {
        "queries": [
            "Mấy giờ đi ngủ", "Đi ngủ mấy giờ", "Bao giờ đi ngủ",
            "Khi nào đi ngủ",
        ],
        "answer": "Đi ngủ từ 18:30, lên giường đúng giờ giữ sức khỏe.",
    },
    "slot_1": {
        "queries": [
            "Mấy giờ tập thể dục", "Tập thể dục mấy giờ", "Khi nào tập thể dục",
        ],
        "answer": "Tập thể dục lúc 05:00 đến 05:15. Đồng chí đến đúng giờ nhé.",
    },
}


def main():
    print("[v10] loading v9 base...")
    records = []
    with open(V9_PATH, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            records.append(json.loads(line))
    print(f"[v10] v9 records = {len(records):,}")

    aug_count = 0
    rate_ladder = [0.15, 0.25, 0.35, 0.45, 0.55]   # increasing severity

    # 1. Heavy telex for area canonicals
    for entity_id, info in CANON.items():
        for q in info["queries"]:
            for rate in rate_ladder:
                for _ in range(6):                 # 6 variants per rate per query
                    var = heavy_telex(q, rate)
                    if var != q:
                        records.append({
                            "intent": "ASK_LOCATION",
                            "question": var,
                            "answer": info["answer"],
                            "answer_template": "TELEX_SAT",
                            "entityId": entity_id,
                            "entityType": "area",
                            "needsGameState": False,
                            "augmentation": f"htelex_{rate:.2f}",
                        })
                        aug_count += 1
            # No-accent + telex combo
            na = no_accent(q)
            for rate in rate_ladder:
                for _ in range(3):
                    var = heavy_telex(na, rate)
                    records.append({
                        "intent": "ASK_LOCATION",
                        "question": var,
                        "answer": info["answer"],
                        "answer_template": "TELEX_SAT_NA",
                        "entityId": entity_id,
                        "entityType": "area",
                        "needsGameState": False,
                        "augmentation": f"htelex_na_{rate:.2f}",
                    })
                    aug_count += 1

    # 2. Heavy telex for meal canonicals
    for slot_id, info in MEAL_CANON.items():
        for q in info["queries"]:
            for rate in rate_ladder:
                for _ in range(6):
                    var = heavy_telex(q, rate)
                    if var != q:
                        records.append({
                            "intent": "ASK_TIME",
                            "question": var,
                            "answer": info["answer"],
                            "answer_template": "TELEX_MEAL_SAT",
                            "entityId": slot_id,
                            "entityType": "schedule_slot",
                            "needsGameState": False,
                            "augmentation": f"htelex_{rate:.2f}",
                        })
                        aug_count += 1
            na = no_accent(q)
            for rate in rate_ladder:
                for _ in range(3):
                    var = heavy_telex(na, rate)
                    records.append({
                        "intent": "ASK_TIME",
                        "question": var,
                        "answer": info["answer"],
                        "answer_template": "TELEX_MEAL_NA",
                        "entityId": slot_id,
                        "entityType": "schedule_slot",
                        "needsGameState": False,
                        "augmentation": f"htelex_na_{rate:.2f}",
                    })
                    aug_count += 1
    print(f"[v10] aggressive telex aug added = {aug_count:,}")

    # Dedup
    seen = set()
    out = []
    for r in records:
        key = (r["intent"], r["question"].lower().strip(), r.get("entityId", ""))
        if key in seen: continue
        seen.add(key)
        out.append(r)
    print(f"[v10] post-dedup = {len(out):,}")

    for i, r in enumerate(out):
        r["id"] = f"qa10_{i:07d}"

    with open(OUT_PATH, "w", encoding="utf-8") as f:
        for r in out:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"[v10] wrote {OUT_PATH}  ({OUT_PATH.stat().st_size/1024/1024:.2f} MiB)")


if __name__ == "__main__":
    main()
