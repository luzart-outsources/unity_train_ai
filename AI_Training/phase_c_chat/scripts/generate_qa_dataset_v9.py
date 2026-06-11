"""
Phase C ITER 2 — v9 dataset: v8 + negative samples for non-existent areas
+ extreme telex augmentation.

Adds:
  1. Negative location samples — "khu A1/A2/A3/A4/B/B1/B2/C/D1/.. ở đâu"
     mapped to OUT_OF_SCOPE so student learns these are NOT real entities.
     Helps fix v2's regression where it confidently picked random real
     areas for non-existent ones.
  2. Extreme telex training samples for the canonical area names so the
     model can decode "Saan vaan ddoongg" -> SanVanDong.
  3. Synonyms for common area-asking phrasings.

Outputs:
  data/qa_pairs_v9.jsonl
"""
import json
import random
import unicodedata
from pathlib import Path

random.seed(2026)

SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
GAME_PATH  = DATA_DIR / "game_entities.json"
V8_PATH    = DATA_DIR / "qa_pairs_v8.jsonl"
OUT_PATH   = DATA_DIR / "qa_pairs_v9.jsonl"

# All fake area names that PLAYER might type but DON'T exist in game.
FAKE_AREAS = [
    "khu A", "khu A1", "khu A2", "khu A3", "khu A4",
    "khu B", "khu B1", "khu B2", "khu B3",
    "khu C", "khu C1", "khu C2",
    "khu D", "khu D1", "khu E",
    "khu F", "phòng A1", "phòng A2", "phòng B1", "phòng B2",
    "phòng C1", "phòng D1", "phòng E", "phòng X",
    "tầng 1", "tầng 2", "tầng 3", "tầng 4",
    "block A", "block B", "block C",
    "tòa A", "tòa B", "tòa nhà X", "tòa C",
    "khu vực 1", "khu vực 2", "khu vực 3",
    "tòa 1", "tòa 2", "tòa 3",
]

NEG_LOCATION_Q = [
    "{X} ở đâu", "{X} ở chỗ nào", "{X} đâu",
    "{X} nằm ở đâu", "Cho hỏi {X} ở đâu",
    "{X} ở khu nào", "Em chưa biết {X}",
    "{X} hướng nào", "{X} đi lối nào",
    "Tới {X} đi đường nào", "{X} chỗ nào nhỉ",
    "Báo cáo, {X} ở đâu ạ", "Em mới tới, {X} ở đâu",
    "Em không tìm thấy {X}",
]

NEG_ANSWERS = [
    "Trong doanh trại không có khu vực {X}. Đồng chí muốn hỏi sân vận động, nhà ăn, lớp học, ký túc xá, khu tự do hay khu dọn vệ sinh?",
    "Đại đội ta chỉ có 6 khu vực: sân vận động, nhà ăn, lớp học, ký túc xá, khu tự do, khu dọn vệ sinh. {X} không có.",
    "Tôi không biết {X} - chắc đồng chí nhầm. Hỏi về 6 khu vực doanh trại đi.",
    "{X} không tồn tại trong doanh trại của chúng ta. Đồng chí xem lại tên khu.",
]


def heavy_telex_variants(text: str, n: int = 5) -> list[str]:
    """Generate n variants with heavy random vowel duplication."""
    VOWELS = "aeiouôơêăâ"
    out = set()
    for _ in range(n * 3):
        chars = []
        for ch in text:
            chars.append(ch)
            if ch.lower() in VOWELS and random.random() < 0.40:
                chars.append(ch)
        v = "".join(chars)
        if v != text:
            out.add(v)
        if len(out) >= n:
            break
    return list(out)


def no_accent(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    out = "".join(c for c in nfkd if not unicodedata.combining(c))
    return out.replace("Đ", "D").replace("đ", "d")


def main():
    # 1. Copy all v8 records
    print(f"[v9] copying v8 base ({V8_PATH.name})...")
    out_records = []
    with open(V8_PATH, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            out_records.append(json.loads(line))
    print(f"[v9] v8 records = {len(out_records):,}")

    # 2. Negative location samples for non-existent areas
    neg_count = 0
    for fake in FAKE_AREAS:
        for q_tpl in NEG_LOCATION_Q:
            q = q_tpl.format(X=fake)
            a = random.choice(NEG_ANSWERS).format(X=fake)
            out_records.append({
                "id": f"qa9_neg_{neg_count:05d}",
                "intent": "OUT_OF_SCOPE",
                "question": q,
                "answer": a,
                "answer_template": "FAKE_AREA_NEG",
                "entityId": "fake_area",
                "entityType": "negative",
                "needsGameState": False,
                "augmentation": "none",
            })
            neg_count += 1
            # Also generate no_accent variant
            q_na = no_accent(q)
            if q_na != q:
                out_records.append({
                    "id": f"qa9_neg_{neg_count:05d}",
                    "intent": "OUT_OF_SCOPE",
                    "question": q_na,
                    "answer": a,
                    "answer_template": "FAKE_AREA_NEG",
                    "entityId": "fake_area",
                    "entityType": "negative",
                    "needsGameState": False,
                    "augmentation": "no_accent",
                })
                neg_count += 1
    print(f"[v9] negative location samples added = {neg_count:,}")

    # 3. Extreme telex variants for the 6 REAL areas
    real_area_canon = {
        "SanVanDong": "Sân vận động ở đâu",
        "NhaAn_Door": "Nhà ăn ở đâu",
        "LopHoc_Door": "Lớp học ở đâu",
        "KTX_Door": "Ký túc xá ở đâu",
        "FreeArea": "Khu tự do ở đâu",
        "DonVeSinh": "Khu dọn vệ sinh ở đâu",
    }
    real_canonical_answers = {
        "SanVanDong":  "Sân vận động nằm ở phía đông doanh trại, đi thẳng 98m là tới.",
        "NhaAn_Door":  "Nhà ăn ở trung tâm doanh trại, đi 65m theo lối nội bộ.",
        "LopHoc_Door": "Lớp học ở phía bắc doanh trại, đi 89m là tới.",
        "KTX_Door":    "Ký túc xá ở phía nam doanh trại, đi 72m.",
        "FreeArea":    "Khu tự do ở phía đông nam doanh trại, đi 61m.",
        "DonVeSinh":   "Khu dọn vệ sinh ở phía tây nam doanh trại, đi 102m.",
    }
    telex_count = 0
    for area_id, canon_q in real_area_canon.items():
        ans = real_canonical_answers[area_id]
        # Heavy telex per canonical question — 10 variants each, 5 base variants
        bases = [canon_q]
        # Add more variants for the same area
        if "Sân vận động" in canon_q:
            bases += ["Sân tập thể dục ở đâu", "Khu thể dục ở đâu", "Sân chạy ở đâu"]
        elif "Nhà ăn" in canon_q:
            bases += ["Phòng ăn ở đâu", "Chỗ ăn cơm ở đâu", "Khu ẩm thực ở đâu"]
        elif "Lớp học" in canon_q:
            bases += ["Phòng học ở đâu", "Giảng đường ở đâu"]
        elif "Ký túc xá" in canon_q:
            bases += ["Phòng ngủ ở đâu", "Chỗ ngủ ở đâu", "Nhà ở ở đâu", "Doanh trại ở đâu"]
        elif "Khu tự do" in canon_q:
            bases += ["Khu giải trí ở đâu", "Chỗ nghỉ ngơi ở đâu"]
        elif "Khu dọn vệ sinh" in canon_q:
            bases += ["Khu vệ sinh ở đâu", "Khu tăng gia ở đâu"]

        for base in bases:
            variants = heavy_telex_variants(base, n=8)
            for v in variants:
                out_records.append({
                    "id": f"qa9_telex_{telex_count:05d}",
                    "intent": "ASK_LOCATION",
                    "question": v,
                    "answer": ans,
                    "answer_template": "TELEX_CANON",
                    "entityId": area_id,
                    "entityType": "area",
                    "needsGameState": False,
                    "augmentation": "extreme_telex",
                })
                telex_count += 1
    print(f"[v9] extreme telex samples added = {telex_count:,}")

    # De-dup by (question.lower, intent)
    seen = set()
    deduped = []
    for r in out_records:
        key = (r["intent"], r["question"].lower().strip(), r.get("entityId", ""))
        if key in seen: continue
        seen.add(key)
        deduped.append(r)
    print(f"[v9] post-dedup = {len(deduped):,}")

    # Re-id everything
    for i, r in enumerate(deduped):
        r["id"] = f"qa9_{i:07d}"

    with open(OUT_PATH, "w", encoding="utf-8") as f:
        for r in deduped:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"[v9] wrote {OUT_PATH}  ({OUT_PATH.stat().st_size/1024/1024:.2f} MiB)")

    from collections import Counter
    by_intent = Counter(r["intent"] for r in deduped)
    print()
    for k, v in sorted(by_intent.items(), key=lambda x: -x[1]):
        print(f"  {k:25s} {v}")


if __name__ == "__main__":
    main()
