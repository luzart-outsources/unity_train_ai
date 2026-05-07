from __future__ import annotations
import argparse
import random
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
SEED_PATH = ROOT / "data" / "intents.csv"
OUT_PATH = ROOT / "data" / "intents_expanded.csv"

# Slot vocab (Quyen co the bo sung)
TIMES = ["sáng nay", "chiều nay", "tối nay", "ngày mai", "tuần này", "thứ 2", "thứ 3", "thứ 4", "thứ 5", "thứ 6", "cuối tuần", "8 giờ", "10 giờ", "trưa", "chiều"]
MEALS = ["ăn cơm", "ăn trưa", "ăn tối", "ăn sáng", "phát cơm", "có cơm"]
PLACES = ["nhà ăn", "phòng tập", "sân tập", "lớp học", "phòng y tế", "doanh trại", "nhà kho", "bãi tập", "kho súng", "đại đội"]
KNOWLEDGE = ["súng AK", "lựu đạn", "chào điều lệnh", "đội hình hàng dọc", "đội hình hàng ngang", "quy tắc bắn", "quân hàm", "kỷ luật quân đội", "10 lời thề", "12 điều"]

TEMPLATES = {
    "HOI_LICH": [
        "{time} có lịch gì",
        "lịch {time} thế nào",
        "{time} phải làm gì",
        "cho tôi xem lịch {time}",
        "{time} có hoạt động gì",
        "thời khoá biểu {time} ra sao",
    ],
    "HOI_GIO_AN": [
        "mấy giờ {meal}",
        "khi nào {meal}",
        "bao giờ {meal}",
        "{time} mấy giờ {meal}",
        "giờ {meal} là khi nào",
        "lúc nào {meal} vậy",
    ],
    "HOI_VI_TRI": [
        "{place} ở đâu",
        "{place} ở chỗ nào",
        "cho tôi đường tới {place}",
        "{place} ở khu nào",
        "đi tới {place} thế nào",
        "{place} hướng nào",
    ],
    "HOI_KIEN_THUC": [
        "{topic} là gì",
        "cách thực hiện {topic} ra sao",
        "{topic} có quy tắc gì",
        "thủ trưởng dạy về {topic} đi",
        "giải thích {topic} cho tôi",
        "{topic} dùng thế nào",
    ],
    "BAO_CAO": [
        "báo cáo {place} đã có mặt đầy đủ",
        "báo cáo đã hoàn thành nhiệm vụ",
        "báo cáo thủ trưởng {place} đủ quân",
        "tôi xin báo cáo {time} đã chuẩn bị xong",
        "báo cáo trung đội đầy đủ",
        "báo cáo tiểu đội đã sẵn sàng",
    ],
    "XIN_PHEP": [
        "cho tôi xin phép ra ngoài",
        "tôi xin phép nghỉ {time}",
        "cho tôi nghỉ buổi {time}",
        "xin phép thủ trưởng cho ra cổng",
        "cho tôi xin phép về thăm nhà",
        "xin phép vắng mặt buổi {time}",
    ],
    "TAM_BIET": [
        "chào thủ trưởng tôi đi đây",
        "em chào sĩ quan",
        "tạm biệt thủ trưởng",
        "hẹn gặp lại",
        "chào tạm biệt",
        "em xin phép đi trước",
    ],
    "OUT_OF_SCOPE": [
        "hôm nay trời đẹp nhỉ",
        "bạn có biết bóng đá không",
        "kể chuyện cười đi",
        "bạn ăn gì chưa",
        "thích uống cafe không",
        "phim mới nào hay",
        "{time} trời mưa to",
        "tôi buồn quá",
    ],
}

def fill(template: str) -> str:
    return (
        template
        .replace("{time}", random.choice(TIMES))
        .replace("{meal}", random.choice(MEALS))
        .replace("{place}", random.choice(PLACES))
        .replace("{topic}", random.choice(KNOWLEDGE))
    )

def cap_first(s: str) -> str:
    return s[0].upper() + s[1:] if s else s

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--target", type=int, default=120, help="target samples per intent")
    p.add_argument("--seed", type=int, default=42)
    args = p.parse_args()

    random.seed(args.seed)
    seed_df = pd.read_csv(SEED_PATH)
    rows: list[dict] = seed_df.to_dict("records")

    have = seed_df.groupby("intent").size().to_dict()
    for intent, templates in TEMPLATES.items():
        need = max(0, args.target - have.get(intent, 0))
        for _ in range(need):
            t = random.choice(templates)
            text = fill(t)
            if random.random() < 0.4:
                text = cap_first(text)
            if random.random() < 0.15:
                text = text + "?"
            rows.append({"text": text, "intent": intent})

    out = pd.DataFrame(rows).drop_duplicates(subset=["text"]).reset_index(drop=True)
    out.to_csv(OUT_PATH, index=False, encoding="utf-8")
    print(f"[expand] wrote {len(out)} rows to {OUT_PATH}")
    print(f"[expand] per intent:\n{out['intent'].value_counts()}")

if __name__ == "__main__":
    main()
