from __future__ import annotations
import argparse
import random
import re
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
SEED_PATH = ROOT / "data" / "intents.csv"
OUT_PATH = ROOT / "data" / "intents_v2.csv"

# Word pools — feel free to extend

TIMES = [
    "hôm nay", "hôm qua", "ngày mai", "ngày kia", "chiều nay", "tối nay", "sáng nay",
    "sáng mai", "chiều mai", "tối mai", "tuần này", "tuần sau", "tuần tới", "cuối tuần",
    "đầu tuần", "thứ 2", "thứ 3", "thứ 4", "thứ 5", "thứ 6", "thứ 7", "chủ nhật",
    "buổi sáng", "buổi trưa", "buổi chiều", "buổi tối", "đêm nay",
    "5 giờ sáng", "6 giờ sáng", "7 giờ", "8 giờ", "10 giờ", "11 giờ", "12 giờ",
    "1 giờ chiều", "3 giờ chiều", "5 giờ chiều", "6 giờ", "7 giờ tối", "9 giờ tối",
    "lúc nào", "khi nào",
]

MEALS = ["ăn cơm", "ăn trưa", "ăn tối", "ăn sáng", "phát cơm", "có cơm",
         "nấu cơm", "dọn cơm", "có bữa", "cơm chiều", "cơm trưa", "cơm sáng"]

PLACES = [
    "nhà ăn", "phòng tập", "sân tập", "lớp học", "phòng y tế", "doanh trại",
    "nhà kho", "bãi tập", "kho súng", "đại đội", "trung đội", "tiểu đội",
    "phòng học", "phòng thí nghiệm", "thư viện", "ký túc xá", "phòng A1",
    "khu B", "tòa B5", "phòng 305", "phòng giảng đường", "căng tin",
    "sân điều lệnh", "bãi bắn", "giảng đường lớn", "phòng máy tính",
    "trung tâm thể thao", "cổng chính", "cổng phụ", "nhà sinh hoạt chung",
    "khu hành chính", "phòng họp", "nhà chỉ huy",
]

KNOWLEDGE = [
    "súng AK", "súng AK47", "lựu đạn", "chào điều lệnh", "đội hình hàng dọc",
    "đội hình hàng ngang", "quy tắc bắn", "quân hàm", "kỷ luật quân đội",
    "10 lời thề", "12 điều", "điều lệnh đội ngũ", "đi đều bước",
    "tháo lắp súng", "tư thế quỳ bắn", "tư thế nằm bắn", "bài tập thể lực",
    "chạy 3000m", "vượt vật cản", "đi nghiêm", "đi nghỉ", "quay phải", "quay trái",
    "xếp hàng", "kiểm tra quân tư trang", "trực ban", "trực nhật", "lễ chào cờ",
    "kỹ thuật chiến đấu", "kỹ thuật bộ binh", "mật mã quân sự",
    "quy chế học viện", "an toàn thông tin", "mật mã đối xứng", "RSA",
]

REPORT_OBJ = [
    "đại đội", "trung đội", "tiểu đội", "đội hình", "lớp", "tổ trực ban",
    "tổ trực nhật", "đội mẫu", "nhóm 1", "nhóm 2", "phân đội",
]

REASONS = [
    "ốm", "bị sốt", "đau bụng", "có việc gia đình", "đi khám bệnh",
    "có giỗ", "việc đột xuất", "đi viện", "mệt", "đau đầu", "bị thương nhẹ",
    "có lịch khám", "phải về quê", "có việc gấp",
]

GREETINGS_PRE = [
    "", "", "",  # weight to often-empty
    "thưa thủ trưởng ", "báo cáo thủ trưởng ", "đồng chí ơi ",
    "anh ơi ", "em hỏi với ", "thầy ơi ", "thưa cán bộ ",
    "xin phép thủ trưởng ", "xin hỏi ", "cho em hỏi ", "cho hỏi ",
]

PARTICLES_END = [
    "", "", "", "",  # bias toward empty
    " ạ", " nhỉ", " thế", " vậy", " đi", " đấy", " chứ",
    " được không", " được không ạ", " thưa thủ trưởng",
    "?", " ?",
]

# Templates — 20+ per intent

TEMPLATES = {
    "HOI_LICH": [
        "{time} có lịch gì",
        "{time} có hoạt động gì",
        "lịch {time} thế nào",
        "lịch {time} ra sao",
        "{time} phải làm gì",
        "{time} làm gì",
        "{time} có gì đặc biệt",
        "{time} học gì",
        "{time} tập gì",
        "{time} có buổi gì",
        "cho tôi xem lịch {time}",
        "cho em xem thời khoá biểu {time}",
        "thời khoá biểu {time}",
        "lịch trình {time}",
        "kế hoạch {time}",
        "chương trình {time}",
        "{time} có lịch không",
        "{time} đại đội có lịch gì",
        "{time} trung đội làm gì",
        "{time} sinh viên có lịch không",
        "lịch tuần",
        "lịch hôm nay",
        "lịch của lớp {time}",
        "{time} giảng đường có lịch gì",
        "tôi muốn biết lịch",
    ],
    "HOI_GIO_AN": [
        "mấy giờ {meal}",
        "khi nào {meal}",
        "bao giờ {meal}",
        "{time} mấy giờ {meal}",
        "giờ {meal} là khi nào",
        "lúc nào {meal}",
        "{meal} mấy giờ",
        "tôi đói rồi {meal} chưa",
        "đến giờ {meal} chưa",
        "{meal} lúc mấy giờ",
        "{meal} giờ nào",
        "giờ {meal}",
        "bao giờ có cơm",
        "cơm {time} mấy giờ",
        "khi nào nhà ăn mở",
        "nhà ăn mở mấy giờ",
        "có gì ăn không",
        "đến bữa chưa",
        "khi nào ăn",
        "đói quá khi nào ăn",
        "bao giờ tới giờ cơm",
        "trưa nay mấy giờ ăn",
        "tối nay ăn lúc mấy giờ",
    ],
    "HOI_VI_TRI": [
        "{place} ở đâu",
        "{place} ở chỗ nào",
        "cho tôi đường tới {place}",
        "{place} ở khu nào",
        "đi tới {place} thế nào",
        "{place} hướng nào",
        "{place} nằm ở đâu",
        "{place} đi lối nào",
        "đường tới {place} đi sao",
        "làm sao tới {place}",
        "{place} chỗ nào",
        "tìm {place} ở đâu",
        "tôi không biết {place} ở đâu",
        "chỉ giúp tôi {place}",
        "đến {place} đi đường nào",
        "{place} có xa không",
        "muốn đi {place} thì đi đâu",
        "ai biết {place} ở đâu không",
        "vị trí của {place}",
        "{place} ở khu B hay khu A",
        "có ai biết {place} không",
        "đi đến {place} mất bao lâu",
        "{place} cách đây bao xa",
    ],
    "HOI_KIEN_THUC": [
        "{topic} là gì",
        "cách thực hiện {topic} ra sao",
        "{topic} có quy tắc gì",
        "thủ trưởng dạy về {topic}",
        "giải thích {topic} cho tôi",
        "{topic} dùng thế nào",
        "{topic} làm thế nào",
        "tôi muốn học về {topic}",
        "{topic} hoạt động ra sao",
        "nguyên lý của {topic}",
        "{topic} có những bước gì",
        "tài liệu về {topic} ở đâu",
        "{topic} khó không",
        "ai có thể dạy {topic}",
        "{topic} áp dụng khi nào",
        "kể về {topic}",
        "phân tích {topic}",
        "ý nghĩa của {topic}",
        "tìm hiểu về {topic}",
        "lịch sử của {topic}",
        "đặc điểm của {topic}",
        "{topic} dùng để làm gì",
        "khi nào dùng {topic}",
        "{topic} có nguy hiểm không",
    ],
    "BAO_CAO": [
        "báo cáo {report} đã có mặt đầy đủ",
        "báo cáo {report} đủ quân",
        "báo cáo đã hoàn thành nhiệm vụ",
        "báo cáo thủ trưởng {report} đã sẵn sàng",
        "tôi xin báo cáo {time} đã chuẩn bị xong",
        "báo cáo {report} đã tập hợp",
        "báo cáo {report} đã đến vị trí",
        "báo cáo đầy đủ",
        "xin báo cáo đã xong",
        "báo cáo hoàn tất nhiệm vụ",
        "tôi báo cáo đã hoàn thành",
        "báo cáo {report} đã ăn cơm xong",
        "báo cáo {report} đã trực ban xong",
        "{report} báo cáo đầy đủ",
        "tổ trực báo cáo có mặt",
        "báo cáo điểm danh xong",
        "báo cáo {report} thiếu một người",
        "báo cáo {report} vắng hai người",
        "báo cáo có người ốm",
        "tôi báo cáo lên cấp trên",
        "{report} đã làm xong nhiệm vụ",
        "báo cáo công việc",
        "đã thực hiện xong xin báo cáo",
        "đã hoàn thành xin báo cáo thủ trưởng",
    ],
    "XIN_PHEP": [
        "cho tôi xin phép ra ngoài",
        "tôi xin phép nghỉ {time}",
        "cho tôi nghỉ buổi {time}",
        "xin phép thủ trưởng cho ra cổng",
        "cho tôi xin phép về thăm nhà",
        "xin phép vắng mặt buổi {time}",
        "xin phép nghỉ học {time}",
        "xin phép đi khám bệnh",
        "tôi xin phép vì {reason}",
        "cho tôi nghỉ vì {reason}",
        "thủ trưởng cho em xin nghỉ {reason}",
        "em xin phép vắng vì {reason}",
        "cho em xin nghỉ {time}",
        "em xin phép ra ngoài một chút",
        "tôi xin phép đi vệ sinh",
        "xin phép xuống nhà ăn trước",
        "cho em xin phép về sớm",
        "xin nghỉ tập",
        "xin phép không tham gia buổi {time}",
        "xin phép đi viện",
        "em xin phép đi gặp gia đình",
        "anh cho em ra ngoài chút",
        "cho em xin nghỉ buổi sáng nay",
        "em muốn xin phép vắng",
        "cho em xin phép thưa thủ trưởng",
    ],
    "TAM_BIET": [
        "chào thủ trưởng tôi đi đây",
        "em chào sĩ quan",
        "tạm biệt thủ trưởng",
        "hẹn gặp lại",
        "chào tạm biệt",
        "em xin phép đi trước",
        "em chào",
        "em đi đây",
        "tạm biệt nhé",
        "chào nhé",
        "hẹn gặp lại sau",
        "thôi em xin phép",
        "em chào anh",
        "chào anh em đi đây",
        "đến đây thôi",
        "em đi nha",
        "tới giờ rồi em đi",
        "em phải đi đây",
        "thôi tạm biệt",
        "chào",
        "tạm biệt anh em",
        "em chào các đồng chí",
        "em chào thủ trưởng em đi học",
        "chào thủ trưởng em xuống dưới",
    ],
    "OUT_OF_SCOPE": [
        # Real student-life small talk so model knows "not a command"
        "hôm nay trời đẹp nhỉ",
        "trời nóng quá",
        "trời lạnh thế",
        "mưa to quá",
        "bạn có biết bóng đá không",
        "Messi với Ronaldo ai giỏi hơn",
        "đội tuyển Việt Nam đá thế nào",
        "kể chuyện cười đi",
        "bạn ăn gì chưa",
        "thích uống cafe không",
        "phim mới nào hay",
        "tôi vừa xem phim hay lắm",
        "tôi buồn quá",
        "tôi đang vui",
        "máy tính của tôi bị lag",
        "wifi yếu quá",
        "hôm nay tôi mệt mỏi",
        "có ai chơi liên quân không",
        "PUBG tối nay không",
        "dota mới nào hay",
        "đội mình thắng rồi",
        "lập trình C khó quá",
        "thuật toán Dijkstra hơi khó",
        "thầy môn toán dạy vui ghê",
        "đề thi học kỳ ra dễ",
        "điểm thi sao rồi nhỉ",
        "nhớ nhà quá",
        "đói bụng nhưng chưa tới giờ",
        "bài tập về nhà nhiều quá",
        "ngày mai có khảo sát",
        "tài liệu môn an toàn thông tin",
        "cuối kỳ có nhiều bài lắm",
        "giảng viên chấm điểm khắt khe",
        "Học viện kỹ thuật mật mã có nhiều môn hay",
        "tôi đang thèm trà sữa",
        "đi chơi cuối tuần không",
        "nhóm chat lớp im quá",
        "ai có note môn toán cho mượn",
        "hôm qua tôi thức khuya quá",
        "ngủ chưa đủ giấc",
        "máy quay được không",
        "tôi vừa xem TikTok hay lắm",
        "có deal sale gì không",
    ],
}

# Augmentations

ACCENT_MAP = str.maketrans(
    "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ"
    "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ",
    "aaaaaaaaaaaaaaaaaeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyyd"
    "AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD",
)

def drop_accent(s: str) -> str:
    return s.translate(ACCENT_MAP)

def random_typo(s: str) -> str:
    """Drop a random char (small chance) — simulates fast typing."""
    if len(s) < 6:
        return s
    i = random.randrange(len(s))
    if s[i] == " ":
        return s
    return s[:i] + s[i + 1:]

def drop_filler_word(s: str) -> str:
    """Drop one filler word like 'thì', 'là', 'à', 'ơi'."""
    fillers = {"thì", "là", "à", "ơi", "ạ", "vậy", "thế", "đó"}
    words = s.split()
    keep = [w for w in words if w not in fillers]
    if 0 < len(keep) < len(words):
        return " ".join(keep)
    return s

def cap_first(s: str) -> str:
    return s[0].upper() + s[1:] if s else s

# Generation

def fill(template: str) -> str:
    return (
        template
        .replace("{time}", random.choice(TIMES))
        .replace("{meal}", random.choice(MEALS))
        .replace("{place}", random.choice(PLACES))
        .replace("{topic}", random.choice(KNOWLEDGE))
        .replace("{report}", random.choice(REPORT_OBJ))
        .replace("{reason}", random.choice(REASONS))
    )

def decorate(text: str) -> str:
    """Add greeting prefix + ending particle stochastically."""
    pre = random.choice(GREETINGS_PRE)
    post = random.choice(PARTICLES_END)
    s = pre + text + post
    return re.sub(r"\s+", " ", s).strip()

def augment(text: str) -> str:
    """Apply 0–2 augmentations randomly."""
    r = random.random()
    if r < 0.10:
        text = drop_accent(text)
    elif r < 0.18:
        text = random_typo(text)
    elif r < 0.26:
        text = drop_filler_word(text)
    if random.random() < 0.30:
        text = cap_first(text)
    return text

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--per_intent", type=int, default=250,
                   help="target samples per intent after dedup")
    p.add_argument("--seed", type=int, default=42)
    args = p.parse_args()

    random.seed(args.seed)

    rows: list[dict] = []
    # Always include the seed sentences (handwritten — high quality)
    if SEED_PATH.exists():
        rows.extend(pd.read_csv(SEED_PATH).to_dict("records"))

    # Generate per-intent
    target_attempts = args.per_intent * 4  # over-generate to allow dedup
    for intent, templates in TEMPLATES.items():
        seen = set(r["text"] for r in rows if r["intent"] == intent)
        attempts = 0
        while len([r for r in rows if r["intent"] == intent]) < args.per_intent and attempts < target_attempts:
            attempts += 1
            t = random.choice(templates)
            text = fill(t)
            text = decorate(text)
            text = augment(text)
            text = re.sub(r"\s+", " ", text).strip()
            if not text or text in seen:
                continue
            seen.add(text)
            rows.append({"text": text, "intent": intent})

    out = pd.DataFrame(rows).drop_duplicates(subset=["text"]).reset_index(drop=True)
    out.to_csv(OUT_PATH, index=False, encoding="utf-8")

    counts = out["intent"].value_counts()
    print(f"[gen v2] wrote {len(out)} rows to {OUT_PATH}")
    print(f"[gen v2] per intent:")
    for intent, n in counts.items():
        print(f"{intent:<14} {n}")

    # Quick vocab estimate
    from collections import Counter
    c = Counter()
    for t in out["text"]:
        c.update(t.lower().split())
    print(f"[gen v2] approx vocab (whitespace, no underthesea): {len(c)} unique tokens")

if __name__ == "__main__":
    main()
