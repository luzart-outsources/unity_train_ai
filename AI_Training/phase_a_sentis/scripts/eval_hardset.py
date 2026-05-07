from __future__ import annotations
import argparse
import io
import json
import sys as _sys
from pathlib import Path

# Force utf-8 stdout for Vietnamese output on Windows cp1252 consoles.
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

# 200+ hard test cases. Format: (text, expected_intent, category).
TEST_CASES = [
    # -- HOI_LICH (28) --
    ("Hôm nay có lịch gì", "HOI_LICH", "CLEAN"),
    ("Tuần sau có hoạt động gì", "HOI_LICH", "CLEAN"),
    ("Chiều mai đại đội tập trung không", "HOI_LICH", "CLEAN"),
    ("Lich tuan nay ra sao thu truong", "HOI_LICH", "NO_ACCENT"),
    ("Hom nay co lich gi khong", "HOI_LICH", "NO_ACCENT"),
    ("Ngay mai dai doi minh lam gi", "HOI_LICH", "NO_ACCENT"),
    ("Đoonng chí chiều mai có gì", "HOI_LICH", "TELEX_TYPOS"),
    ("Thu trưởng oơi maaii có lịch gì", "HOI_LICH", "TELEX_TYPOS"),
    ("Schedule hôm nay sao rồi", "HOI_LICH", "CODE_MIX"),
    ("Agenda ngày mai có gì hot", "HOI_LICH", "CODE_MIX"),
    ("Plan tuần này thế nào", "HOI_LICH", "CODE_MIX"),
    ("Thầy ơi mai có gì làm và sáng có học không", "HOI_LICH", "COMPOUND"),
    ("Chiều nay đại đội bố trí gì không và mai có thay đổi không", "HOI_LICH", "COMPOUND"),
    ("Còn mai thì sao", "HOI_LICH", "ELLIPSIS"),
    ("Thế còn tuần sau", "HOI_LICH", "ELLIPSIS"),
    ("Lịch", "HOI_LICH", "ELLIPSIS"),
    ("Mai sao đây", "HOI_LICH", "ELLIPSIS"),
    ("Chương trình ngày mai bao gồm gì", "HOI_LICH", "SYNONYM"),
    ("Kế hoạch tuần tới thế nào", "HOI_LICH", "SYNONYM"),
    ("Bố trí công việc mai có gì", "HOI_LICH", "SYNONYM"),
    ("Hôm nay rảnh không nhỉ", "HOI_LICH", "COMPLAINT"),
    ("Tuần này bận hay rảnh thế", "HOI_LICH", "COMPLAINT"),
    ("z hôm nay làm gì z", "HOI_LICH", "SLANG"),
    ("mai lớp mình học môn gì zậy", "HOI_LICH", "SLANG"),
    ("nay lịch sao thầy", "HOI_LICH", "SLANG"),
    ("Cho hỏi mai tập trung lúc mấy giờ và làm gì ạ", "HOI_LICH", "CLEAN"),
    ("Đồng chí ơi thứ 4 lớp mình học môn gì", "HOI_LICH", "CLEAN"),
    ("Mai đi đâu vậy thầy", "HOI_LICH", "SLANG"),

    # -- HOI_GIO_AN (26) --
    ("Mấy giờ thì ăn cơm", "HOI_GIO_AN", "CLEAN"),
    ("Bao giờ phát cơm", "HOI_GIO_AN", "CLEAN"),
    ("Trưa nay ăn lúc mấy giờ", "HOI_GIO_AN", "CLEAN"),
    ("May gio thi an", "HOI_GIO_AN", "NO_ACCENT"),
    ("Toi nay an luc may gio", "HOI_GIO_AN", "NO_ACCENT"),
    ("May gio phat com em doi qua", "HOI_GIO_AN", "NO_ACCENT"),
    ("Mấyy giờờ ăn cooom", "HOI_GIO_AN", "TELEX_TYPOS"),
    ("Khi nào có cơm vây ạ", "HOI_GIO_AN", "TELEX_TYPOS"),
    ("Lunch mấy giờ", "HOI_GIO_AN", "CODE_MIX"),
    ("Dinner time là khi nào", "HOI_GIO_AN", "CODE_MIX"),
    ("Breakfast lúc nào nhỉ", "HOI_GIO_AN", "CODE_MIX"),
    ("Mai mấy giờ ăn sáng và trưa nay ăn lúc nào", "HOI_GIO_AN", "COMPOUND"),
    ("Đói quá còn lâu mới tới giờ ăn không", "HOI_GIO_AN", "COMPOUND"),
    ("Đói quá", "HOI_GIO_AN", "COMPLAINT"),
    ("Bụng tôi cồn cào", "HOI_GIO_AN", "COMPLAINT"),
    ("Đói lắm rồi", "HOI_GIO_AN", "COMPLAINT"),
    ("Có gì ăn không em đói", "HOI_GIO_AN", "COMPLAINT"),
    ("Tới bữa chưa", "HOI_GIO_AN", "ELLIPSIS"),
    ("Cơm chưa", "HOI_GIO_AN", "ELLIPSIS"),
    ("Ăn", "HOI_GIO_AN", "ELLIPSIS"),
    ("Khi nào xơi cơm", "HOI_GIO_AN", "SYNONYM"),
    ("Bữa tối mấy giờ chén", "HOI_GIO_AN", "SYNONYM"),
    ("Nhà ăn mở mấy giờ vậy", "HOI_GIO_AN", "CLEAN"),
    ("Trưa nay 11h30 ăn đúng không", "HOI_GIO_AN", "CLEAN"),
    ("ăn cơm chưa thầy ơi đói lắm", "HOI_GIO_AN", "SLANG"),
    ("nay đói meo khi nào ăn z", "HOI_GIO_AN", "SLANG"),

    # -- HOI_VI_TRI (28) --
    ("Nhà ăn nằm đâu", "HOI_VI_TRI", "CLEAN"),
    ("Phòng học ở đâu vậy", "HOI_VI_TRI", "CLEAN"),
    ("Cho em hỏi giảng đường B5 ở chỗ nào ạ", "HOI_VI_TRI", "CLEAN"),
    ("Khu A ở đâu", "HOI_VI_TRI", "CLEAN"),
    ("Khu C nằm chỗ nào", "HOI_VI_TRI", "CLEAN"),
    ("Phòng 305 ở tầng mấy ạ", "HOI_VI_TRI", "CLEAN"),
    ("Toi muon di toi tram y te di duong nao", "HOI_VI_TRI", "NO_ACCENT"),
    ("Em moi den khong biet khu KTX nam o dau", "HOI_VI_TRI", "NO_ACCENT"),
    ("Cho em hoi sang van dong o dau", "HOI_VI_TRI", "NO_ACCENT"),
    ("Phoongg học oo đâu", "HOI_VI_TRI", "TELEX_TYPOS"),
    ("Where is the dining hall", "HOI_VI_TRI", "CODE_MIX"),
    ("Location của phòng 305", "HOI_VI_TRI", "CODE_MIX"),
    ("Library ở đâu thầy", "HOI_VI_TRI", "CODE_MIX"),
    ("Phòng học ở đâu và mở mấy giờ", "HOI_VI_TRI", "COMPOUND"),
    ("Nhà ăn cách đây bao xa và đi đường nào", "HOI_VI_TRI", "COMPOUND"),
    ("Lạc đường rồi", "HOI_VI_TRI", "COMPLAINT"),
    ("Tôi đi lạc rồi nhà ăn ở đâu", "HOI_VI_TRI", "COMPLAINT"),
    ("Mới đến không biết đường", "HOI_VI_TRI", "COMPLAINT"),
    ("Em mới chuyển đến chưa biết phòng học", "HOI_VI_TRI", "COMPLAINT"),
    ("Thư viện đâu", "HOI_VI_TRI", "ELLIPSIS"),
    ("Cổng số 2 đâu", "HOI_VI_TRI", "ELLIPSIS"),
    ("Cantin", "HOI_VI_TRI", "ELLIPSIS"),
    ("Tọa độ thư viện", "HOI_VI_TRI", "SYNONYM"),
    ("Vị trí của trạm y tế", "HOI_VI_TRI", "SYNONYM"),
    ("Sân bóng đá ở phía nào", "HOI_VI_TRI", "CLEAN"),
    ("Bể bơi học viện cách đây bao xa", "HOI_VI_TRI", "CLEAN"),
    ("nhà ăn đâu z", "HOI_VI_TRI", "SLANG"),
    ("phòng 401 chỗ nào v", "HOI_VI_TRI", "SLANG"),

    # -- HOI_KIEN_THUC (26) --
    ("Quy tắc bắn 3 điểm là gì", "HOI_KIEN_THUC", "CLEAN"),
    ("Súng AK47 dùng thế nào", "HOI_KIEN_THUC", "CLEAN"),
    ("Hàm băm SHA-256 dùng để làm gì", "HOI_KIEN_THUC", "CLEAN"),
    ("Em chưa hiểu RSA hoạt động ra sao", "HOI_KIEN_THUC", "CLEAN"),
    ("Mat ma doi xung khac mat ma bat doi xung cho nao", "HOI_KIEN_THUC", "NO_ACCENT"),
    ("Quy tac ban 3 diem la gi", "HOI_KIEN_THUC", "NO_ACCENT"),
    ("Sung AK thao lap may buoc", "HOI_KIEN_THUC", "NO_ACCENT"),
    ("Mạtt mãã đối xứng hoạtt động sao", "HOI_KIEN_THUC", "TELEX_TYPOS"),
    ("AES với DES khác nhau ở đâu", "HOI_KIEN_THUC", "CODE_MIX"),
    ("What is RSA encryption", "HOI_KIEN_THUC", "CODE_MIX"),
    ("Public key cryptography là gì", "HOI_KIEN_THUC", "CODE_MIX"),
    ("Cho em hỏi RSA và AES khác sao", "HOI_KIEN_THUC", "COMPOUND"),
    ("Tháo lắp súng AK có mấy bước và mất bao lâu", "HOI_KIEN_THUC", "COMPOUND"),
    ("Em không hiểu gì về thuật toán Dijkstra", "HOI_KIEN_THUC", "COMPLAINT"),
    ("Đọc tài liệu mãi không hiểu cây AVL", "HOI_KIEN_THUC", "COMPLAINT"),
    ("RSA", "HOI_KIEN_THUC", "ELLIPSIS"),
    ("Giảng AES nha", "HOI_KIEN_THUC", "ELLIPSIS"),
    ("Cho ví dụ chữ ký số", "HOI_KIEN_THUC", "ELLIPSIS"),
    ("Định nghĩa của entropy là gì", "HOI_KIEN_THUC", "SYNONYM"),
    ("Cơ chế hoạt động của firewall", "HOI_KIEN_THUC", "SYNONYM"),
    ("Cho em hỏi điều lệnh đội ngũ với", "HOI_KIEN_THUC", "CLEAN"),
    ("Tư thế nằm bắn quy tắc thế nào", "HOI_KIEN_THUC", "CLEAN"),
    ("Em không hiểu mật mã đối xứng", "HOI_KIEN_THUC", "CLEAN"),
    ("Thế tháo lắp súng AK có mấy bước ạ", "HOI_KIEN_THUC", "CLEAN"),
    ("RSA hoạt động ra sao", "HOI_KIEN_THUC", "CLEAN"),
    ("dijkstra hoạt động sao thầy", "HOI_KIEN_THUC", "SLANG"),

    # -- BAO_CAO (26) --
    ("Báo cáo đầy đủ", "BAO_CAO", "CLEAN"),
    ("Tôi xin báo cáo đã hoàn thành", "BAO_CAO", "CLEAN"),
    ("Báo cáo thủ trưởng đại đội 1 đủ quân", "BAO_CAO", "CLEAN"),
    ("Báo cáo bài bắn đạt yêu cầu", "BAO_CAO", "CLEAN"),
    ("Em xin báo cáo đã trực ban xong", "BAO_CAO", "CLEAN"),
    ("Bao cao trung doi 2 da co mat day du", "BAO_CAO", "NO_ACCENT"),
    ("Bao cao xong nhiem vu", "BAO_CAO", "NO_ACCENT"),
    ("Em bao cao da hoan thanh", "BAO_CAO", "NO_ACCENT"),
    ("Bááoo cáo đủù quânnn", "BAO_CAO", "TELEX_TYPOS"),
    ("Báo cáoo đãã xongg", "BAO_CAO", "TELEX_TYPOS"),
    ("Report đại đội đủ quân", "BAO_CAO", "CODE_MIX"),
    ("Task done thưa thủ trưởng", "BAO_CAO", "CODE_MIX"),
    ("Báo cáo đã hoàn thành nhiệm vụ và tổ trực có mặt đủ", "BAO_CAO", "COMPOUND"),
    ("Trung đội 1 đủ quân và bài bắn đã đạt", "BAO_CAO", "COMPOUND"),
    ("Báo cáo có người ốm xin gọi y tế", "BAO_CAO", "COMPLAINT"),
    ("Có người vắng báo cáo lên", "BAO_CAO", "COMPLAINT"),
    ("Đại đội ta thiếu một người", "BAO_CAO", "COMPLAINT"),
    ("Đã xong", "BAO_CAO", "ELLIPSIS"),
    ("Hoàn thành rồi", "BAO_CAO", "ELLIPSIS"),
    ("Đại đội đầy đủ", "BAO_CAO", "ELLIPSIS"),
    ("Em xin trình báo công việc đã hoàn tất", "BAO_CAO", "SYNONYM"),
    ("Tổ đã thực hiện xong nhiệm vụ", "BAO_CAO", "SYNONYM"),
    ("Đã hoàn thành nhiệm vụ xin báo cáo", "BAO_CAO", "CLEAN"),
    ("Báo cáo thủ trưởng tổ trực đã có mặt", "BAO_CAO", "CLEAN"),
    ("xong nhiệm vụ rồi anh ơi", "BAO_CAO", "SLANG"),
    ("báo cáo team 1 done", "BAO_CAO", "CODE_MIX"),

    # -- XIN_PHEP (28) --
    ("Cho em xin nghỉ", "XIN_PHEP", "CLEAN"),
    ("Em xin phép về quê", "XIN_PHEP", "CLEAN"),
    ("Thưa thủ trưởng cho em nghỉ vì sốt cao ạ", "XIN_PHEP", "CLEAN"),
    ("Anh cho em ra ngoài chút được không", "XIN_PHEP", "CLEAN"),
    ("Cho em xin phép vắng buổi tối ngày mai", "XIN_PHEP", "CLEAN"),
    ("Em đề nghị duyệt cho em nghỉ phép", "XIN_PHEP", "CLEAN"),
    ("Em xin phep di kham benh", "XIN_PHEP", "NO_ACCENT"),
    ("Cho em xin nghi", "XIN_PHEP", "NO_ACCENT"),
    ("Em xin phep ve que vi co viec gia dinh", "XIN_PHEP", "NO_ACCENT"),
    ("Em xinn phép về quêee", "XIN_PHEP", "TELEX_TYPOS"),
    ("Cho emm xin nghỉỉ", "XIN_PHEP", "TELEX_TYPOS"),
    ("Permission xin nghỉ ngày mai", "XIN_PHEP", "CODE_MIX"),
    ("Request leave ngày 15", "XIN_PHEP", "CODE_MIX"),
    ("Ask for leave thầy ơi", "XIN_PHEP", "CODE_MIX"),
    ("Em xin nghỉ vì sốt và đau bụng", "XIN_PHEP", "COMPOUND"),
    ("Cho em ra ngoài chút và buổi chiều xin nghỉ luôn", "XIN_PHEP", "COMPOUND"),
    ("Em đang sốt 39 độ", "XIN_PHEP", "COMPLAINT"),
    ("Em đau bụng quá", "XIN_PHEP", "COMPLAINT"),
    ("Bố em ốm phải về quê", "XIN_PHEP", "COMPLAINT"),
    ("Nhà có việc gấp", "XIN_PHEP", "COMPLAINT"),
    ("Xin nghỉ", "XIN_PHEP", "ELLIPSIS"),
    ("Xin phép", "XIN_PHEP", "ELLIPSIS"),
    ("Em vắng được không", "XIN_PHEP", "ELLIPSIS"),
    ("Em đề nghị được vắng mặt", "XIN_PHEP", "SYNONYM"),
    ("Tôi mong muốn xin nghỉ phép một buổi", "XIN_PHEP", "SYNONYM"),
    ("Thầy ơi cho em nghỉ tập thể dục sáng mai", "XIN_PHEP", "CLEAN"),
    ("anh ơi em xin off chiều nay được hum", "XIN_PHEP", "SLANG"),
    ("cho em nghỉ học mai ạ", "XIN_PHEP", "SLANG"),

    # -- TAM_BIET (26) --
    ("Chào thủ trưởng", "TAM_BIET", "CLEAN"),
    ("Em chào thủ trưởng em đi đây", "TAM_BIET", "CLEAN"),
    ("Em chào em đi học đây ạ", "TAM_BIET", "CLEAN"),
    ("Tạm biệt anh em đi nhiệm vụ", "TAM_BIET", "CLEAN"),
    ("Thôi tới giờ rồi em đi", "TAM_BIET", "CLEAN"),
    ("Em xuống ăn cơm đã chào thủ trưởng", "TAM_BIET", "CLEAN"),
    ("Em phải về phòng đây tạm biệt", "TAM_BIET", "CLEAN"),
    ("Em đi truoc nhe", "TAM_BIET", "NO_ACCENT"),
    ("Em chao thu truong em di hoc", "TAM_BIET", "NO_ACCENT"),
    ("Tam biet a", "TAM_BIET", "NO_ACCENT"),
    ("Emm chào thủủ trưởng emm đii", "TAM_BIET", "TELEX_TYPOS"),
    ("Bye thủ trưởng", "TAM_BIET", "CODE_MIX"),
    ("See you tomorrow ạ", "TAM_BIET", "CODE_MIX"),
    ("Bye bye thầy", "TAM_BIET", "CODE_MIX"),
    ("Em chào em đi và mai gặp lại", "TAM_BIET", "COMPOUND"),
    ("Tạm biệt anh em phải đi học", "TAM_BIET", "COMPOUND"),
    ("Tới giờ rồi em đi", "TAM_BIET", "ELLIPSIS"),
    ("Bye", "TAM_BIET", "ELLIPSIS"),
    ("Em chạy đây", "TAM_BIET", "ELLIPSIS"),
    ("Hết giờ rồi em đi", "TAM_BIET", "SYNONYM"),
    ("Sắp muộn em phải đi", "TAM_BIET", "SYNONYM"),
    ("Em xin từ biệt thủ trưởng", "TAM_BIET", "SYNONYM"),
    ("em đi nha thầy", "TAM_BIET", "SLANG"),
    ("thôi em đi z", "TAM_BIET", "SLANG"),
    ("ok em đi đây", "TAM_BIET", "SLANG"),
    ("hẹn mai gặp lại nha", "TAM_BIET", "CLEAN"),

    # -- OUT_OF_SCOPE (28) - student small talk that should NOT match commands --
    ("Messi đá hay không", "OUT_OF_SCOPE", "CLEAN"),
    ("Wifi yếu quá", "OUT_OF_SCOPE", "CLEAN"),
    ("Hôm nay tớ thèm trà sữa quá", "OUT_OF_SCOPE", "CLEAN"),
    ("Bài tập về nhà nhiều quá", "OUT_OF_SCOPE", "CLEAN"),
    ("Có ai chơi liên quân không", "OUT_OF_SCOPE", "CLEAN"),
    ("Nhớ nhà quá đêm nay", "OUT_OF_SCOPE", "CLEAN"),
    ("ChatGPT moi ra phien ban moi rat hay", "OUT_OF_SCOPE", "NO_ACCENT"),
    ("Sai Gon nong qua", "OUT_OF_SCOPE", "NO_ACCENT"),
    ("Bua nay nhom chat im qua", "OUT_OF_SCOPE", "NO_ACCENT"),
    ("Nhớớ nhàà quá đêmm nay", "OUT_OF_SCOPE", "TELEX_TYPOS"),
    ("Tomorrow tớ đi chơi với bạn", "OUT_OF_SCOPE", "CODE_MIX"),
    ("Game LoL có patch mới hot phết", "OUT_OF_SCOPE", "CODE_MIX"),
    ("ChatGPT 4 với Claude ai mạnh hơn", "OUT_OF_SCOPE", "CODE_MIX"),
    ("Hôm nay trời đẹp và tớ đói", "OUT_OF_SCOPE", "COMPOUND"),
    ("Trời nóng và Wifi lag", "OUT_OF_SCOPE", "COMPOUND"),
    ("Tớ buồn quá", "OUT_OF_SCOPE", "COMPLAINT"),
    ("Stress vãi", "OUT_OF_SCOPE", "COMPLAINT"),
    ("Đề thi khó vãi", "OUT_OF_SCOPE", "COMPLAINT"),
    ("Buồn ngủ quá", "OUT_OF_SCOPE", "COMPLAINT"),
    ("Trời đẹp", "OUT_OF_SCOPE", "ELLIPSIS"),
    ("Cuối tuần", "OUT_OF_SCOPE", "ELLIPSIS"),
    ("Stress", "OUT_OF_SCOPE", "ELLIPSIS"),
    ("Vũ Hán đang dịch", "OUT_OF_SCOPE", "SYNONYM"),
    ("Tâm trạng tớ không tốt", "OUT_OF_SCOPE", "SYNONYM"),
    # Adversarial - might confuse with HOI_LICH
    ("Tớ vừa xem phim hay lắm hôm nay", "OUT_OF_SCOPE", "ADVERSARIAL"),
    ("Hôm nay tớ vừa xem TikTok hay lắm", "OUT_OF_SCOPE", "ADVERSARIAL"),
    # Adversarial - might confuse with HOI_KIEN_THUC
    ("Lập trình C khó quá", "OUT_OF_SCOPE", "ADVERSARIAL"),
    ("Python dễ ghê", "OUT_OF_SCOPE", "ADVERSARIAL"),
]

def evaluate_arch(arch: str, tag: str = "", vocab_path: Path = None, label_path: Path = None) -> dict:
    """Eval an arch checkpoint on the hard test set.

    Args:
        arch: model arch name (must match a build_model option)
        tag: optional suffix (e.g. 'v4' loads lstm_v4_best.pt + vocab_v4.json)
        vocab_path / label_path: explicit overrides (win over tag-based defaults)
    """
    suffix = ("_" + tag) if tag else ""
    ckpt_path = MODEL_DIR / f"{arch}{suffix}_best.pt"
    if not ckpt_path.exists():
        return {"arch": arch, "tag": tag, "error": f"checkpoint missing: {ckpt_path}"}

    ckpt = torch.load(ckpt_path, map_location="cpu", weights_only=True)
    model = build_model(arch, ckpt["vocab_size"], ckpt["num_classes"])
    model.load_state_dict(ckpt["state_dict"])
    model.eval()

    vp = vocab_path or (MODEL_DIR / f"vocab{suffix}.json")
    lp = label_path or (MODEL_DIR / f"id2label{suffix}.json")
    if not vp.exists():
        vp = MODEL_DIR / "vocab.json"  # fallback to default
    if not lp.exists():
        lp = MODEL_DIR / "id2label.json"
    with open(vp, encoding="utf-8") as f:
        vocab = json.load(f)
    with open(lp, encoding="utf-8") as f:
        id2label = {int(k): v for k, v in json.load(f).items()}
    max_len = ckpt["max_len"]

    correct = 0
    by_cat: dict[str, dict] = {}
    rows = []
    for text, expected, cat in TEST_CASES:
        ids = torch.tensor([encode(text, vocab, max_len)], dtype=torch.long)
        with torch.no_grad():
            probs = F.softmax(model(ids), dim=-1)[0]
        idx = int(probs.argmax())
        pred = id2label[idx]
        ok = pred == expected
        correct += int(ok)
        cat_stats = by_cat.setdefault(cat, {"correct": 0, "total": 0})
        cat_stats["total"] += 1
        if ok:
            cat_stats["correct"] += 1
        rows.append({"text": text, "expected": expected, "pred": pred,
                     "conf": float(probs[idx]), "cat": cat, "ok": ok})
    acc = correct / len(TEST_CASES)
    return {"arch": arch, "tag": tag, "accuracy": acc, "correct": correct,
            "total": len(TEST_CASES), "rows": rows, "by_cat": by_cat}

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--archs", nargs="+", default=["fasttext", "lstm", "transformer"])
    p.add_argument("--tag", default="", help="suffix for ckpt/vocab files (e.g. 'v4')")
    p.add_argument("--vocab", default=None, help="optional override vocab.json")
    p.add_argument("--labels", default=None, help="optional override id2label.json")
    p.add_argument("--save", default=None, help="optional path to save JSON results")
    p.add_argument("--print_misses", action="store_true", help="print only misclassified")
    args = p.parse_args()

    vp = Path(args.vocab) if args.vocab else None
    lp = Path(args.labels) if args.labels else None

    results = {}
    for arch in args.archs:
        r = evaluate_arch(arch, args.tag, vp, lp)
        if "error" in r:
            print(f"[skip ] {arch}: {r['error']}")
            continue
        results[arch] = r
        print(f"\n=== {arch.upper()} - {r['correct']}/{r['total']} = {r['accuracy']*100:.1f}% ===")
        # By category
        print("[by category]")
        for cat in sorted(r["by_cat"].keys()):
            s = r["by_cat"][cat]
            print(f"  {cat:<14} {s['correct']:>3}/{s['total']:<3} = {s['correct']/s['total']*100:5.1f}%")
        # Detail
        if args.print_misses:
            print("[misses]")
            for row in r["rows"]:
                if row["ok"]:
                    continue
                print(f"  [{row['cat']:<12}] \"{row['text']:50s}\" -> {row['pred']:14s} (exp {row['expected']:14s}) {row['conf']*100:5.1f}%")
        else:
            for row in r["rows"]:
                mark = "OK " if row["ok"] else "X  "
                print(f"  {mark}[{row['cat']:<12}] \"{row['text']:50s}\" -> {row['pred']:14s} (exp {row['expected']:14s}) {row['conf']*100:5.1f}%")

    if args.save:
        with open(args.save, "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        print(f"\n[save ] {args.save}")

    if results:
        best = max(results.values(), key=lambda r: r["accuracy"])
        print(f"\nWINNER: {best['arch']} with {best['accuracy']*100:.1f}% on hard test set ({best['total']} Q)")

if __name__ == "__main__":
    main()
