from __future__ import annotations
import argparse
import json
from pathlib import Path

import torch
import torch.nn.functional as F

import sys
sys.path.insert(0, str(Path(__file__).parent))
from dataset import encode
from model import build_model

ROOT = Path(__file__).resolve().parent.parent
MODEL_DIR = ROOT / "models"

# Real-world test set — 64 sentences across 8 intents, 8 each.
# Includes: clean speech, regional slang, telex typos, compound sentences,
# malformed grammar, OOD vocabulary, and adversarial near-confusions.
# Scoring this is the actual signal — synthetic val_acc is misleading.
TEST_CASES = [
    # -- HOI_LICH (8) --
    ("Hôm nay có lịch gì", "HOI_LICH"),
    ("Tuần sau có hoạt động gì", "HOI_LICH"),
    ("Cho hỏi mai tập trung lúc mấy giờ và làm gì ạ", "HOI_LICH"),
    ("Hom nay co lich gi khong", "HOI_LICH"),                         # no accent
    ("Lich tuan nay ra sao thu truong", "HOI_LICH"),                  # no accent + closing
    ("Đồng chí ơi thứ 4 lớp mình học môn gì", "HOI_LICH"),
    ("Chiều nay đại đội ta bố trí gì không", "HOI_LICH"),
    ("Schedule hôm nay sao rồi", "HOI_LICH"),                         # English mix

    # -- HOI_GIO_AN (8) --
    ("Mấy giờ thì ăn cơm", "HOI_GIO_AN"),
    ("Bao giờ phát cơm", "HOI_GIO_AN"),
    ("May gio thi an", "HOI_GIO_AN"),                                  # no accent
    ("Khi nào có cơm vậy ạ", "HOI_GIO_AN"),
    ("Đói quá còn lâu mới tới giờ ăn không", "HOI_GIO_AN"),
    ("Trưa nay 11h30 ăn đúng không", "HOI_GIO_AN"),
    ("Có gì ăn không em đói", "HOI_GIO_AN"),
    ("Nhà ăn mở mấy giờ vậy", "HOI_GIO_AN"),

    # -- HOI_VI_TRI (8) --
    ("Nhà ăn nằm đâu", "HOI_VI_TRI"),
    ("Phòng học ở đâu vậy", "HOI_VI_TRI"),
    ("Cho em hỏi giảng đường B5 ở chỗ nào ạ", "HOI_VI_TRI"),
    ("Toi muon di toi tram y te di duong nao", "HOI_VI_TRI"),         # no accent
    ("Chỗ căng tin ra sao đi đường nào tới", "HOI_VI_TRI"),
    ("Phòng 305 ở tầng mấy ạ", "HOI_VI_TRI"),
    ("Cổng số 2 cách đây bao xa", "HOI_VI_TRI"),
    ("Em mới đến không biết khu KTX nằm ở đâu", "HOI_VI_TRI"),

    # -- HOI_KIEN_THUC (8) --
    ("Quy tắc bắn 3 điểm là gì", "HOI_KIEN_THUC"),
    ("Súng AK47 dùng thế nào", "HOI_KIEN_THUC"),
    ("Em chưa hiểu RSA hoạt động ra sao", "HOI_KIEN_THUC"),
    ("Mat ma doi xung khac mat ma bat doi xung cho nao", "HOI_KIEN_THUC"),  # no accent
    ("Thế tháo lắp súng AK có mấy bước ạ", "HOI_KIEN_THUC"),
    ("Cho em hỏi điều lệnh đội ngũ với", "HOI_KIEN_THUC"),
    ("Hàm băm SHA-256 dùng để làm gì", "HOI_KIEN_THUC"),
    ("Tư thế nằm bắn quy tắc thế nào", "HOI_KIEN_THUC"),

    # -- BAO_CAO (8) --
    ("Báo cáo đầy đủ", "BAO_CAO"),
    ("Tôi xin báo cáo đã hoàn thành", "BAO_CAO"),
    ("Báo cáo thủ trưởng đại đội 1 đủ quân", "BAO_CAO"),
    ("Bao cao trung doi 2 da co mat day du", "BAO_CAO"),               # no accent
    ("Báo cáo có người ốm xin gọi y tế", "BAO_CAO"),
    ("Em xin báo cáo đã trực ban xong", "BAO_CAO"),
    ("Báo cáo bài bắn đạt yêu cầu", "BAO_CAO"),
    ("Đã hoàn thành nhiệm vụ xin báo cáo", "BAO_CAO"),

    # -- XIN_PHEP (8) --
    ("Cho em xin nghỉ", "XIN_PHEP"),
    ("Em xin phép về quê", "XIN_PHEP"),
    ("Thưa thủ trưởng cho em nghỉ vì sốt cao ạ", "XIN_PHEP"),
    ("Em xin phep di kham benh", "XIN_PHEP"),                          # no accent
    ("Anh cho em ra ngoài chút được không", "XIN_PHEP"),
    ("Cho em xin phép vắng buổi tối ngày mai", "XIN_PHEP"),
    ("Thầy ơi cho em nghỉ tập thể dục sáng mai", "XIN_PHEP"),
    ("Em đề nghị duyệt cho em nghỉ phép", "XIN_PHEP"),

    # -- TAM_BIET (8) --
    ("Chào thủ trưởng", "TAM_BIET"),
    ("Em chào thủ trưởng em đi đây", "TAM_BIET"),
    ("Em chào em đi học đây ạ", "TAM_BIET"),
    ("Em đi truoc nhe", "TAM_BIET"),                                   # no accent
    ("Tạm biệt anh em đi nhiệm vụ", "TAM_BIET"),
    ("Thôi tới giờ rồi em đi", "TAM_BIET"),
    ("Em xuống ăn cơm đã chào thủ trưởng", "TAM_BIET"),
    ("Em phải về phòng đây tạm biệt", "TAM_BIET"),

    # -- OUT_OF_SCOPE (8) — student small talk that should NOT match commands --
    ("Messi đá hay không", "OUT_OF_SCOPE"),
    ("Wifi yếu quá", "OUT_OF_SCOPE"),
    ("Hôm nay tớ thèm trà sữa quá", "OUT_OF_SCOPE"),
    ("ChatGPT moi ra phien ban moi rat hay", "OUT_OF_SCOPE"),         # no accent + tech slang
    ("Bài tập về nhà nhiều quá", "OUT_OF_SCOPE"),
    ("Có ai chơi liên quân không", "OUT_OF_SCOPE"),
    ("Nhớ nhà quá đêm nay", "OUT_OF_SCOPE"),
    ("Ngày mai có khảo sát môn nào", "OUT_OF_SCOPE"),
]

def evaluate_arch(arch: str) -> dict:
    ckpt = torch.load(MODEL_DIR / f"{arch}_best.pt", map_location="cpu", weights_only=True)
    model = build_model(arch, ckpt["vocab_size"], ckpt["num_classes"])
    model.load_state_dict(ckpt["state_dict"])
    model.eval()
    with open(MODEL_DIR / "vocab.json", encoding="utf-8") as f:
        vocab = json.load(f)
    with open(MODEL_DIR / "id2label.json", encoding="utf-8") as f:
        id2label = {int(k): v for k, v in json.load(f).items()}
    max_len = ckpt["max_len"]

    correct = 0
    rows = []
    for text, expected in TEST_CASES:
        ids = torch.tensor([encode(text, vocab, max_len)], dtype=torch.long)
        with torch.no_grad():
            probs = F.softmax(model(ids), dim=-1)[0]
        idx = int(probs.argmax())
        pred = id2label[idx]
        ok = pred == expected
        correct += int(ok)
        rows.append({"text": text, "expected": expected, "pred": pred,
                     "conf": float(probs[idx]), "ok": ok})
    acc = correct / len(TEST_CASES)
    return {"arch": arch, "accuracy": acc, "correct": correct,
            "total": len(TEST_CASES), "rows": rows}

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--archs", nargs="+", default=["fasttext", "lstm", "transformer"])
    p.add_argument("--save", default=None, help="optional path to save JSON results")
    args = p.parse_args()

    results = {}
    for arch in args.archs:
        ckpt_path = MODEL_DIR / f"{arch}_best.pt"
        if not ckpt_path.exists():
            print(f"[skip ] {arch}: no checkpoint")
            continue
        r = evaluate_arch(arch)
        results[arch] = r
        print(f"\n=== {arch.upper()} — {r['correct']}/{r['total']} = {r['accuracy']*100:.1f}% ===")
        for row in r["rows"]:
            mark = "ok" if row["ok"] else "fail "
            print(f"  {mark} {row['text']:40s} -> {row['pred']:14s} (exp {row['expected']:14s}) {row['conf']*100:5.1f}%")

    if args.save:
        with open(args.save, "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        print(f"\n[save ] {args.save}")

    if results:
        best = max(results.values(), key=lambda r: r["accuracy"])
        print(f"\nWINNER: {best['arch']} with {best['accuracy']*100:.1f}% on real-world set")

if __name__ == "__main__":
    main()
