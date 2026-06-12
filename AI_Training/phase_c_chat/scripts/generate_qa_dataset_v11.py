"""
Phase C ITER 7 — v11 dataset: v9 + explicit math/calc/general-knowledge OOS.

User-reported regression: "1+1 bằng mấy" tokenises into ['1','1','bằng',
'mấy'] and aligns with NPC-name training data ('đồng chí 01', 'ban 01')
at cosine 0.91 — above the 0.88 production threshold. Same shape for any
math/calc/general-knowledge query.

This patch adds ~800 explicit (math/general/calc) -> OUT_OF_SCOPE pairs
so the student learns these inputs collapse onto the OOS cluster
instead of the NPC-name attractor.

Output: data/qa_pairs_v11.jsonl
"""
import json
import random
import unicodedata
from pathlib import Path

random.seed(2026)
SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
V9_PATH    = DATA_DIR / "qa_pairs_v9.jsonl"
OUT_PATH   = DATA_DIR / "qa_pairs_v11.jsonl"


def no_accent(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    out = "".join(c for c in nfkd if not unicodedata.combining(c))
    return out.replace("Đ", "D").replace("đ", "d")


# Numbers and operators that show up in math questions
NUMS = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "100", "1000",
        "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười"]

MATH_TEMPLATES = [
    "{a}+{b} bằng mấy", "{a} cộng {b} bằng mấy", "{a} + {b} = ?",
    "{a}-{b} bằng mấy", "{a} trừ {b} bằng bao nhiêu",
    "{a}*{b} bằng mấy", "{a} nhân {b}", "{a} x {b} = ?",
    "{a}/{b} bằng mấy", "{a} chia {b}",
    "Đồng chí có biết {a}+{b} bằng mấy không",
    "Đồng chí có biết {a} cộng {b} bằng bao nhiêu",
    "Anh ơi {a}+{b} bằng mấy", "Anh biết {a}*{b} không",
    "Cho hỏi {a}+{b}", "Em hỏi {a} cộng {b} bằng mấy",
    "Báo cáo, {a}+{b} bằng mấy ạ",
    "Có biết tính {a}+{b} không",
    "{a} + {b} là số nào",
    "Anh dạy em {a}+{b} với", "Em không biết {a} cộng {b}",
]

GENERAL_OOS_Q = [
    # Math / calc generic
    "Tính giúp em", "Giải toán giúp em", "Toán khó quá",
    "Em không hiểu môn toán", "Đồng chí biết toán không",
    "Phép tính này thế nào", "Bao nhiêu nhân bao nhiêu",
    "Cộng trừ nhân chia", "Tính diện tích hình vuông",
    "Một cộng một bằng hai à", "Hai nhân hai bằng bốn",
    "Số pi là gì", "Số nguyên tố là gì",

    # General knowledge / science
    "Trái đất quay quanh mặt trời à", "Mặt trăng là gì",
    "Nước sôi ở bao nhiêu độ", "Trọng lực là gì",
    "Ánh sáng đi nhanh thế nào", "Khoa học là gì",
    "Vũ trụ rộng bao nhiêu", "Bao giờ có người sao Hỏa",

    # Random celebrity / pop culture
    "Sơn Tùng là ai", "Mỹ Tâm là ai",
    "BTS là gì", "BlackPink là ai",
    "Hari Won là ai", "Ai là người giàu nhất Việt Nam",
    "Elon Musk làm gì", "Bill Gates là ai",
    "Tổng thống Mỹ là ai", "Việt Nam có bao nhiêu tỉnh",
    "Thủ đô Việt Nam là gì",

    # Tech / brands
    "iPhone 15 giá bao nhiêu", "Samsung S24",
    "Macbook Pro M3", "AI là gì", "ChatGPT là gì",
    "Google là gì", "Facebook là gì",
    "Tiktok làm sao kiếm tiền",

    # Sports
    "Bóng đá là gì", "Messi đá hay không", "Ronaldo là ai",
    "World Cup 2022 ai vô địch", "Việt Nam vô địch SEA Games chưa",

    # Daily life / random
    "Hôm nay trời mưa không", "Ngày mai thời tiết ra sao",
    "Em đói quá", "Em khát nước",
    "Đói bụng quá đồng chí", "Em buồn ngủ rồi",
    "Anh có người yêu chưa", "Anh đẹp trai không",
    "Lương đồng chí bao nhiêu", "Quê đồng chí ở đâu",
    "Đồng chí thích ăn gì", "Đồng chí mấy tuổi",

    # Cheating / hacks
    "Cách hack game này", "Cheat code", "Mod menu",
    "Auto-clicker", "Bypass tutorial", "Skip cutscene",

    # Politics / sensitive
    "Bầu cử là gì", "Quốc hội làm gì",

    # Random nonsense / probes
    "abc xyz", "qwerty", "asdfgh", "test test test",
    "blah blah", "lorem ipsum",
    "????", "123456",
]

OOS_ANSWERS = [
    "Cái đó không thuộc phạm vi tôi nắm được. Đồng chí hỏi việc trong đại đội đi.",
    "Tôi là chỉ huy đại đội, mấy việc đó tôi không rõ. Có việc gì trong học viện không?",
    "Câu này tôi không trả lời được. Hỏi gì về lịch, vị trí, môn học - tôi biết.",
    "Ngoài chuyên môn rồi đồng chí. Tập trung vào học viện đi.",
    "Tôi không nắm được vấn đề đó, đồng chí hỏi cái khác đi.",
    "Việc đó không phải nhiệm vụ của chỉ huy đại đội. Hỏi về sinh hoạt đại đội đi đồng chí.",
    "Tôi chỉ trả lời được việc trong học viện thôi đồng chí.",
    "Cái này tôi không biết. Hỏi việc khác đi.",
]


def main():
    print(f"[v11] copying v9 base...")
    records = []
    with open(V9_PATH, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            records.append(json.loads(line))
    print(f"[v11] v9 records = {len(records):,}")

    # 1. Math OOS — explicit a+b style
    n_math = 0
    for tpl in MATH_TEMPLATES:
        for a in NUMS:
            for b in NUMS:
                q = tpl.format(a=a, b=b)
                ans = random.choice(OOS_ANSWERS)
                records.append({
                    "id": f"qa11_math_{n_math:05d}",
                    "intent": "OUT_OF_SCOPE",
                    "question": q,
                    "answer": ans,
                    "answer_template": "MATH_OOS",
                    "entityId": "math",
                    "entityType": "negative",
                    "needsGameState": False,
                    "augmentation": "none",
                })
                n_math += 1
                # also no_accent variant
                q_na = no_accent(q)
                if q_na != q:
                    records.append({
                        "id": f"qa11_math_{n_math:05d}",
                        "intent": "OUT_OF_SCOPE",
                        "question": q_na,
                        "answer": ans,
                        "answer_template": "MATH_OOS",
                        "entityId": "math",
                        "entityType": "negative",
                        "needsGameState": False,
                        "augmentation": "no_accent",
                    })
                    n_math += 1
    print(f"[v11] math OOS records = {n_math:,}")

    # 2. General OOS — celebrity / tech / random / sport / etc
    n_gen = 0
    for q in GENERAL_OOS_Q:
        for _ in range(3):  # 3 different answer choices per question
            ans = random.choice(OOS_ANSWERS)
            records.append({
                "id": f"qa11_gen_{n_gen:05d}",
                "intent": "OUT_OF_SCOPE",
                "question": q,
                "answer": ans,
                "answer_template": "GEN_OOS",
                "entityId": "general",
                "entityType": "negative",
                "needsGameState": False,
                "augmentation": "none",
            })
            n_gen += 1
        # no_accent
        q_na = no_accent(q)
        if q_na != q:
            records.append({
                "id": f"qa11_gen_{n_gen:05d}",
                "intent": "OUT_OF_SCOPE",
                "question": q_na,
                "answer": random.choice(OOS_ANSWERS),
                "answer_template": "GEN_OOS",
                "entityId": "general",
                "entityType": "negative",
                "needsGameState": False,
                "augmentation": "no_accent",
            })
            n_gen += 1
    print(f"[v11] general OOS records = {n_gen:,}")

    # Dedup
    seen = set()
    out = []
    for r in records:
        key = (r["intent"], r["question"].lower().strip(), r.get("entityId", ""))
        if key in seen: continue
        seen.add(key)
        out.append(r)
    print(f"[v11] post-dedup = {len(out):,}")

    for i, r in enumerate(out):
        r["id"] = f"qa11_{i:07d}"

    with open(OUT_PATH, "w", encoding="utf-8") as f:
        for r in out:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"[v11] wrote {OUT_PATH}  ({OUT_PATH.stat().st_size/1024/1024:.2f} MiB)")

    from collections import Counter
    by_intent = Counter(r["intent"] for r in out)
    print()
    for k, v in sorted(by_intent.items(), key=lambda x: -x[1]):
        print(f"  {k:25s} {v}")


if __name__ == "__main__":
    main()
