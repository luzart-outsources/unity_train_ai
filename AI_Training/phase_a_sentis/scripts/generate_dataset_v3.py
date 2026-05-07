from __future__ import annotations
import argparse
import random
import re
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
SEED_PATH = ROOT / "data" / "intents.csv"
OUT_PATH = ROOT / "data" / "intents_v3.csv"

# Massive word pools
TIMES = [
    # Day-relative
    "hôm nay", "hôm qua", "hôm kia", "ngày mai", "ngày kia", "ngày mốt",
    "tuần này", "tuần sau", "tuần tới", "tuần trước", "cuối tuần", "đầu tuần",
    "tháng này", "tháng sau", "đầu tháng", "cuối tháng",
    # Weekday
    "thứ 2", "thứ 3", "thứ 4", "thứ 5", "thứ 6", "thứ 7", "chủ nhật",
    "thứ hai", "thứ ba", "thứ tư", "thứ năm", "thứ sáu", "thứ bảy", "chủ nhật tuần này",
    # Day-part
    "buổi sáng", "buổi trưa", "buổi chiều", "buổi tối", "đêm nay", "khuya nay",
    "sáng nay", "trưa nay", "chiều nay", "tối nay", "sáng mai", "chiều mai",
    "tối mai", "đầu giờ sáng", "cuối giờ chiều",
    # Clock
    "5 giờ", "5h", "5 giờ sáng", "6 giờ", "6h", "6 giờ sáng", "7 giờ", "7h",
    "7 giờ sáng", "7 rưỡi", "8 giờ", "8h", "8 rưỡi", "9 giờ", "9h", "10 giờ",
    "10h", "10 giờ rưỡi", "11 giờ", "11h", "11 rưỡi", "12 giờ trưa",
    "1 giờ chiều", "1h", "2 giờ", "2 giờ chiều", "3 giờ chiều", "3h chiều",
    "4 giờ chiều", "5 giờ chiều", "5h chiều", "6 giờ tối", "6h tối", "7 giờ tối",
    "8 giờ tối", "9 giờ tối", "10 giờ tối", "21 giờ", "22 giờ", "đúng giờ",
    # Vague
    "lát nữa", "tý nữa", "chốc nữa", "lúc nãy", "ban nãy", "vừa rồi",
    "sắp tới", "sắp", "hồi nãy", "khi nãy",
]

MEALS = [
    "ăn cơm", "ăn trưa", "ăn tối", "ăn sáng", "phát cơm", "có cơm",
    "có bữa", "cơm chiều", "cơm trưa", "cơm sáng", "cơm tối", "ăn vặt",
    "uống nước", "uống sữa", "ăn xế", "phát đồ ăn", "phát suất ăn",
    "lấy cơm", "lĩnh cơm", "lĩnh phần ăn", "có suất", "có khẩu phần",
]

PLACES = [
    # Military quarters
    "nhà ăn", "nhà bếp", "phòng tập", "sân tập", "lớp học", "phòng y tế",
    "trạm y tế", "doanh trại", "nhà kho", "bãi tập", "kho súng", "kho đạn",
    "đại đội", "trung đội", "tiểu đội", "phân đội", "ban chỉ huy", "phòng chỉ huy",
    "phòng họp", "phòng họp đại đội", "nhà chỉ huy", "khu hành chính",
    "phòng trực ban", "phòng trực", "phòng văn hóa", "phòng truyền thống",
    "sân điều lệnh", "bãi bắn", "trường bắn", "vườn rau", "vườn tăng gia",
    "khu tăng gia", "chỗ tăng gia", "khu nuôi quân", "chuồng heo", "chuồng gà",
    # Academic (KTMM)
    "phòng học", "phòng học A1", "phòng học B2", "phòng thí nghiệm",
    "phòng lab", "phòng máy tính", "phòng thực hành", "thư viện",
    "ký túc xá", "ktx", "khu KTX", "phòng 305", "phòng 306", "phòng 401",
    "phòng giảng đường", "giảng đường lớn", "giảng đường nhỏ", "giảng đường A",
    "giảng đường B", "căng tin", "căn tin", "quán nước", "khu B", "khu B5",
    "tòa B5", "tòa A", "khu hành chính", "phòng phòng đào tạo",
    "phòng quản lý sinh viên", "phòng đoàn", "phòng hội trường",
    "trung tâm thể thao", "sân vận động", "sân bóng", "sân bóng đá",
    "sân bóng rổ", "sân tennis", "bể bơi", "bể bơi học viện",
    # Commute
    "cổng chính", "cổng phụ", "cổng số 1", "cổng số 2", "khu để xe",
    "bãi gửi xe", "trạm xe buýt", "đường nội bộ", "đường ra cổng",
]

KNOWLEDGE = [
    # Combat
    "súng AK", "súng AK47", "súng AKM", "súng K54", "súng K59", "súng RPG",
    "súng B40", "súng B41", "lựu đạn", "lựu đạn M67", "mìn", "mìn DH-10",
    "thủ pháo", "đạn", "đạn 7.62", "viên đạn", "ngòi nổ",
    # Tactical drill
    "chào điều lệnh", "đi đều bước", "đi đứng", "tư thế nghiêm", "tư thế nghỉ",
    "đội hình hàng dọc", "đội hình hàng ngang", "đội hình chữ V",
    "quy tắc bắn", "ngắm bắn", "tư thế quỳ bắn", "tư thế nằm bắn",
    "tư thế đứng bắn", "tháo lắp súng", "lau chùi súng", "bảo dưỡng súng",
    "vệ sinh vũ khí", "kiểm tra vũ khí", "kỹ thuật chiến đấu",
    "kỹ thuật bộ binh", "vận động chiến thuật", "ẩn nấp", "ngụy trang",
    "vượt vật cản", "đu dây", "leo tường", "vượt rào",
    # Discipline
    "10 lời thề", "12 điều", "10 lời thề danh dự", "kỷ luật quân đội",
    "điều lệnh đội ngũ", "điều lệnh quản lý bộ đội", "quân hàm", "cấp bậc",
    "trực ban", "trực nhật", "lễ chào cờ", "tăng gia sản xuất",
    "tự quản đại đội", "tự quản tiểu đội", "an toàn doanh trại",
    "an toàn cháy nổ", "phòng cháy chữa cháy",
    # Physical
    "bài tập thể lực", "chạy 3000m", "chạy 1500m", "chống đẩy", "hít xà",
    "bật xa", "kéo xà đơn", "vượt xà kép", "xà đôi", "tiêu chuẩn thể lực",
    # KTMM-specific
    "mật mã đối xứng", "mật mã bất đối xứng", "AES", "DES", "RSA",
    "ECC", "hàm băm", "SHA-256", "MD5", "chữ ký số", "chứng chỉ số",
    "PKI", "SSL", "TLS", "HTTPS", "an toàn thông tin", "an ninh mạng",
    "xâm nhập mạng", "tường lửa", "phát hiện xâm nhập", "IDS", "IPS",
    "mã hóa dữ liệu", "mã hóa file", "Linux quân sự", "quy chế học viện",
    "an toàn mạng", "lập trình C", "lập trình Python", "thuật toán Dijkstra",
    "cấu trúc dữ liệu", "cây nhị phân", "đồ thị", "phân tích thuật toán",
    "lý thuyết thông tin", "entropy", "mã sửa lỗi", "Reed-Solomon",
    # Training subject
    "lý luận chính trị", "tư tưởng Hồ Chí Minh", "lịch sử Đảng",
    "kỹ thuật quân sự", "y tế cấp cứu", "băng bó vết thương",
]

REPORT_OBJ = [
    "đại đội", "đại đội 1", "đại đội 2", "đại đội 3", "đại đội 4",
    "trung đội", "trung đội 1", "trung đội 2", "trung đội 3",
    "tiểu đội", "tiểu đội 1", "tiểu đội 2", "tiểu đội 3", "tiểu đội 4",
    "đội hình", "đội hình đại đội", "đội hình trung đội",
    "phân đội", "lớp", "lớp CT06", "lớp ATM06", "tổ trực ban",
    "tổ trực nhật", "đội mẫu", "đội mẫu đại đội", "nhóm 1", "nhóm 2",
    "nhóm A", "nhóm B", "tổ", "tổ 1", "tổ 2", "tổ 3", "ban chỉ huy",
    "đại đội tự quản", "đội canh gác", "tổ canh gác",
]

REASONS = [
    # Health
    "ốm", "bị ốm", "bị sốt", "sốt cao", "sốt 39 độ", "đau bụng",
    "đau đầu", "đau lưng", "đau dạ dày", "viêm họng", "ho nhiều",
    "cảm cúm", "cúm A", "covid", "đau răng", "viêm xoang", "tiêu chảy",
    "khó thở", "tức ngực", "chóng mặt", "buồn nôn", "say nắng",
    "trật cổ chân", "bong gân", "bị thương nhẹ", "vết thương cũ",
    "bị thương chiến thuật", "đau khớp gối",
    # Family
    "có việc gia đình", "gia đình có việc", "có việc gấp ở nhà",
    "bố ốm", "mẹ ốm", "ông bà ốm", "có giỗ", "có giỗ ông", "có giỗ bà",
    "anh chị về thăm", "có lễ gia đình", "phải về quê",
    "phải về thăm gia đình", "nhà có chuyện",
    # Procedural
    "đi khám bệnh", "có lịch khám", "đi viện", "có hẹn bác sĩ",
    "đi tái khám", "có lịch hẹn", "việc đột xuất", "có việc đột xuất",
    "có việc gấp", "có lịch học bù", "có lịch thi", "có lịch phỏng vấn",
    "đi xin học bổng", "có lịch ở học viện", "có buổi báo cáo",
    "có buổi seminar", "có cuộc thi tin học",
]

PRE_GREETINGS = [
    "", "", "", "",  # bias to empty
    "thưa thủ trưởng ", "báo cáo thủ trưởng ", "đồng chí ơi ",
    "anh ơi ", "em hỏi với ", "thầy ơi ", "thưa cán bộ ",
    "xin phép thủ trưởng ", "xin hỏi ", "cho em hỏi ", "cho hỏi ",
    "thưa anh ", "thưa thầy ", "thưa các anh ", "đồng chí cho hỏi ",
    "xin lỗi đồng chí ", "đồng chí cho em hỏi ", "anh cho em hỏi ",
    "em báo cáo ", "thủ trưởng ơi ", "chỉ huy ơi ", "trực ban ơi ",
    "cán bộ ơi ", "anh trực ban ", "anh ơi cho em hỏi ",
]

POST_PARTICLES = [
    "", "", "", "", "",  # heavy empty bias
    " ạ", " nhỉ", " thế", " vậy", " đi", " đấy", " chứ",
    " được không", " được không ạ", " thưa thủ trưởng",
    "?", " ?", "...", " nha", " nhé", " nghe", " á",
    " thì sao", " thì làm sao", " làm sao", " phải không",
    " đúng không", " mà", " hả", " hả thủ trưởng",
]

CONNECTORS = [
    "với lại", "rồi", "và", "nhân tiện", "à mà", "thế còn",
    "rồi còn", "vả lại", "à với cả", "hôm nay luôn",
]

# OOC small talk pool — much bigger
OOC = [
    # Weather
    "hôm nay trời đẹp nhỉ", "trời nóng quá", "trời lạnh thế",
    "mưa to quá", "mưa rả rích cả ngày", "nắng quá tôi không chịu nổi",
    "gió mạnh quá", "trời âm u", "trời sắp mưa", "không khí mát mẻ",
    "có dự báo bão không", "có lụt không nhỉ", "miền bắc lạnh ghê",
    # Sports
    "bạn có biết bóng đá không", "Messi với Ronaldo ai giỏi hơn",
    "đội tuyển Việt Nam đá thế nào", "U23 mình mạnh không",
    "Hà Nội FC vs Hoàng Anh Gia Lai trận nào hay", "bóng rổ NBA",
    "Lakers thắng đêm qua", "F1 Verstappen lại win", "tennis Federer",
    "đội tuyển bóng chuyền nữ", "có ai chơi pickleball không",
    # Games
    "có ai chơi liên quân không", "PUBG tối nay không",
    "dota mới nào hay", "đội mình thắng rồi", "tôi vừa rank lên cao thủ",
    "Genshin Impact map mới ra", "Valorant skin hot",
    "đội mình thua hổ", "ranked queue lâu quá",
    # Movies / shows
    "phim mới nào hay", "tôi vừa xem phim hay lắm", "phim Mai dở",
    "Avatar 2 hay không", "anime mùa này nào hot", "Naruto ending",
    "One Piece chap mới", "Marvel ra phim mới", "phim Hàn này cảm động",
    # Food / drink
    "thèm trà sữa quá", "đang thèm phở", "muốn ăn bún bò Huế",
    "trà đào TocoToco ngon không", "Highlands cafe đắt vãi",
    "cà phê đen đá thôi", "thèm bánh mì pa-tê", "có ai mua đồ ăn không",
    "Phúc Long mới mở cửa hàng", "trân châu đen với trắng",
    # Mood / life
    "tôi buồn quá", "tôi đang vui", "stress quá",
    "deadline dí sát rồi", "burnout luôn rồi đây",
    "nhớ nhà quá", "nhớ bạn gái", "nhớ bạn thân",
    "muốn về nhà ngủ thôi", "không muốn dậy nữa",
    "đói bụng nhưng chưa tới giờ", "muốn ăn vặt",
    # Tech / KTMM small talk
    "máy tính của tôi bị lag", "wifi yếu quá", "wifi rớt",
    "máy laptop sạc chậm", "ChatGPT mới ra phiên bản mới",
    "Github Copilot ngon ghê", "Visual Studio Code crash", "VS Code update",
    "Linux Ubuntu 24 mới", "tôi mới install Arch", "có ai dùng Macbook không",
    "AI Claude với GPT ai ngon hơn", "DeepSeek viết code OK",
    "Cursor IDE thử chưa", "thuật toán Dijkstra hơi khó",
    "lập trình C khó quá", "Python dễ ghê", "JavaScript ức chế",
    # Class / school
    "thầy môn toán dạy vui ghê", "đề thi học kỳ ra dễ", "đề khó vãi",
    "điểm thi sao rồi nhỉ", "có học bù không", "lớp mình ai trượt",
    "hôm qua tôi thức khuya quá", "ngủ chưa đủ giấc",
    "ngày mai có khảo sát", "tài liệu môn an toàn thông tin",
    "cuối kỳ có nhiều bài lắm", "giảng viên chấm điểm khắt khe",
    "Học viện kỹ thuật mật mã có nhiều môn hay",
    "lớp mình xếp hạng tệ", "kỳ này trúng môn khó",
    "tớ lại thiếu chuyên cần", "deadline đồ án TN dí sát",
    # Misc
    "đi chơi cuối tuần không", "Hà Nội cuối tuần đi đâu",
    "nhóm chat lớp im quá", "ai có note môn toán cho mượn",
    "có ai mất điện không", "có ai rảnh rủ đi cà phê",
    "máy quay được không", "tôi vừa xem TikTok hay lắm",
    "có deal sale gì không", "Shopee 9/9 có voucher gì",
    "Tiki giao hàng nhanh không", "Lazada free ship",
    "tớ vừa nhận lương", "tháng này tiêu hết tiền",
    "cuối năm có thưởng không", "hôm nay có gì đặc biệt",
]

# Templates — 50+ per intent (compound forms included)
TEMPLATES = {
    "HOI_LICH": [
        "{time} có lịch gì",
        "{time} có hoạt động gì",
        "{time} có việc gì",
        "{time} có gì làm không",
        "lịch {time} thế nào",
        "lịch {time} ra sao",
        "lịch {time} ra làm sao",
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
        "kế hoạch của đại đội {time}",
        "chương trình {time}",
        "{time} có lịch không",
        "{time} đại đội có lịch gì",
        "{time} trung đội làm gì",
        "{time} sinh viên có lịch không",
        "lịch tuần",
        "lịch tuần này",
        "lịch hôm nay",
        "lịch trong ngày",
        "lịch của lớp {time}",
        "{time} giảng đường có lịch gì",
        "tôi muốn biết lịch",
        "lịch của lớp ta {time}",
        "có lịch gì đặc biệt {time} không",
        "lịch {time} có thay đổi gì không",
        "có hoạt động bất thường nào {time} không",
        "có ai biết lịch {time} không",
        "kế hoạch huấn luyện {time}",
        "cho em hỏi lịch {time}",
        "cho hỏi lịch huấn luyện {time}",
        "thông báo lịch {time}",
        "đại đội ta {time} làm gì",
        "đơn vị {time} có lịch gì",
        "có biết lịch {time} không",
        "{time} có chương trình gì",
        "lịch học môn nào {time}",
        "{time} mình học môn gì",
        "{time} có đổi lịch không",
        "lịch huấn luyện {time}",
        "nhiệm vụ {time} là gì",
        "công việc {time} có gì",
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
        "sáng mai ăn mấy giờ",
        "cơm tối {time}",
        "{time} có cơm không",
        "ăn xong rồi à",
        "tới giờ ăn chưa",
        "còn bao lâu nữa ăn",
        "còn mấy phút nữa ăn",
        "ăn cơm cùng đại đội mấy giờ",
        "giờ ăn của đơn vị",
        "thời gian ăn cơm",
        "thời gian ăn trưa",
        "thời gian ăn tối",
        "thời gian ăn sáng",
        "lịch ăn của đại đội",
        "đến giờ phát cơm chưa",
        "cơm trưa hôm nay {time} chưa",
        "phát cơm lúc nào",
        "cơm tối nay có gì",
        "nhà ăn đóng cửa mấy giờ",
        "căn tin mở mấy giờ",
        "có ai biết giờ ăn không",
        "có ai biết bữa nay mấy giờ không",
        "đói quá đến lúc ăn chưa",
        "mấy giờ thì có suất",
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
        "{place} đi mất bao lâu",
        "từ đây tới {place} bao xa",
        "đường ngắn nhất tới {place}",
        "có cách nào tới {place} nhanh không",
        "tới {place} đi đường nào tiện nhất",
        "{place} ở phía nào của doanh trại",
        "{place} nằm trong khu nào",
        "{place} có gần cổng không",
        "tôi đi lạc rồi {place} ở đâu",
        "chỉ đường tới {place}",
        "hướng dẫn tôi đến {place}",
        "{place} ở tầng mấy",
        "{place} ở dãy nào",
        "{place} có biển chỉ đường không",
        "{place} có ai trực không",
        "ai trực ở {place}",
        "{place} mở cửa lúc nào",
        "{place} đóng cửa chưa",
        "{place} nhìn từ đây thấy không",
        "ra {place} bằng đường nào",
        "{place} cách doanh trại bao xa",
        "có cần xe đi {place} không",
        "đến {place} có cần đi xe không",
        "đi bộ đến {place} có xa không",
        "tôi mới đến không biết {place}",
        "tôi là sinh viên mới {place} ở đâu",
        "đồng chí biết {place} không",
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
        "{topic} có an toàn không",
        "ưu điểm của {topic}",
        "nhược điểm của {topic}",
        "{topic} có ưu nhược điểm gì",
        "so sánh {topic} với cái khác",
        "{topic} có mấy loại",
        "phân loại {topic}",
        "ứng dụng của {topic}",
        "ví dụ về {topic}",
        "cho tôi ví dụ {topic}",
        "kỹ thuật {topic} là gì",
        "kiến thức về {topic}",
        "câu hỏi về {topic}",
        "tôi không hiểu {topic}",
        "ai biết về {topic} không",
        "thầy đã giảng về {topic} chưa",
        "{topic} có trong giáo trình không",
        "trong tài liệu nói gì về {topic}",
        "{topic} có quan trọng không",
        "thi có ra {topic} không",
        "{topic} là chương mấy",
        "đọc {topic} ở đâu",
        "{topic} có khó học không",
        "muốn ôn {topic}",
        "muốn tự học {topic}",
        "{topic} dùng cho gì trong quân đội",
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
        "báo cáo {report} hoàn tất bài tập",
        "báo cáo trực ban {time}",
        "báo cáo gác đã xong",
        "báo cáo gác đêm xong",
        "tổ canh gác báo cáo",
        "báo cáo công tác hành chính xong",
        "báo cáo huấn luyện xong",
        "báo cáo bài tập thể lực xong",
        "báo cáo đã bắn xong",
        "báo cáo bài bắn đạt",
        "báo cáo điểm bài bắn",
        "đã hoàn thành mục tiêu",
        "đã hoàn thành chỉ tiêu",
        "đã đạt yêu cầu báo cáo lên",
        "thủ trưởng tôi báo cáo {report} đầy đủ",
        "báo cáo các đồng chí đã có mặt",
        "báo cáo có chuyện đột xuất",
        "báo cáo {report} có người mất tích",
        "báo cáo có sự cố",
        "báo cáo có tình huống",
        "báo cáo tình huống đã xử lý",
        "báo cáo lên ban chỉ huy",
        "báo cáo nhanh nhiệm vụ tuần",
        "báo cáo tuần này hoàn thành",
        "thưa thủ trưởng {report} đầy đủ",
        "đại đội xin báo cáo",
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
        "em xin nghỉ chiều nay vì {reason}",
        "em xin phép vắng buổi tối {reason}",
        "tôi xin nghỉ học hôm nay",
        "em xin phép nghỉ tập thể dục",
        "thưa thủ trưởng em xin phép ra cổng",
        "em xin phép đi đến trạm y tế",
        "em xin phép tới phòng y tế",
        "em xin phép đi mua thuốc",
        "em xin nghỉ buổi điều lệnh sáng nay",
        "em xin nghỉ buổi học tối",
        "em xin phép đi tới ban chỉ huy",
        "em xin phép có việc đột xuất",
        "em xin nghỉ vì sốt cao",
        "thủ trưởng cho phép em vắng",
        "em xin phép đi gặp giáo viên",
        "em xin phép đi đăng ký môn",
        "em xin phép đi nộp tài liệu",
        "em xin phép đi gặp bạn cũ",
        "em xin nghỉ phép {time}",
        "tôi xin nghỉ phép {time}",
        "đề nghị duyệt cho em nghỉ",
        "đề nghị cho em xin nghỉ",
        "có thể cho em nghỉ {time} được không",
        "em xin phép vắng buổi sáng",
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
        "thôi em đi",
        "em đi trước nhé",
        "tạm biệt mọi người",
        "em đi học đây ạ",
        "em đi lên lớp đây",
        "em xuống ăn cơm đã",
        "em phải về phòng đây",
        "em về ktx đây",
        "em chào thầy em đi",
        "tạm biệt thủ trưởng em đi nhiệm vụ",
        "tạm biệt em đi gác",
        "em đi trực đây",
        "em chào em đi tập",
        "em chào em xuống sân",
        "em đi đến giảng đường đây",
        "em đi nộp bài đây",
        "thôi em đi sang đại đội khác",
        "em đi lên ban chỉ huy đây",
        "em đi giao báo cáo đây",
        "ok thôi em chào",
        "ok em đi",
        "rồi em đi đây thưa thủ trưởng",
        "em chào tạm biệt thưa thủ trưởng",
        "tới giờ rồi em đi nha",
        "em đi thôi tạm biệt",
        "em đi nha thưa thủ trưởng",
    ],
    "OUT_OF_SCOPE": OOC,
}

# Augmentation — Vietnamese-specific
ACCENT_MAP = str.maketrans(
    "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ"
    "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ",
    "aaaaaaaaaaaaaaaaaeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyyd"
    "AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD",
)

# Telex/VNI common keyboard mistakes (chars left behind after broken composition)
TELEX_TYPOS = [
    ("ô", "oo"), ("ơ", "ow"), ("ê", "ee"), ("ư", "uw"), ("â", "aa"),
    ("ă", "aw"), ("đ", "dd"),
]

def drop_accent(s: str) -> str:
    return s.translate(ACCENT_MAP)

def telex_typo(s: str) -> str:
    """Simulate one composition mistake — 'ô' -> 'oo' for example."""
    for vowel, raw in TELEX_TYPOS:
        if vowel in s and random.random() < 0.5:
            return s.replace(vowel, raw, 1)
    return s

def random_typo(s: str) -> str:
    """Drop a random char (small chance) — simulates fast typing."""
    if len(s) < 6:
        return s
    i = random.randrange(len(s))
    if s[i] == " ":
        return s
    return s[:i] + s[i + 1:]

def char_swap(s: str) -> str:
    """Adjacent char swap — common touch-typing typo."""
    if len(s) < 4:
        return s
    i = random.randrange(len(s) - 1)
    if s[i] == " " or s[i + 1] == " ":
        return s
    return s[:i] + s[i + 1] + s[i] + s[i + 2:]

def drop_filler_word(s: str) -> str:
    """Drop one filler word."""
    fillers = {"thì", "là", "à", "ơi", "ạ", "vậy", "thế", "đó", "mà"}
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
    pre = random.choice(PRE_GREETINGS)
    post = random.choice(POST_PARTICLES)
    s = pre + text + post
    return re.sub(r"\s+", " ", s).strip()

def maybe_compound(intent: str, text: str) -> str:
    """With small probability, append a second clause from the same intent."""
    if random.random() < 0.10 and intent in TEMPLATES and intent != "OUT_OF_SCOPE":
        second = fill(random.choice(TEMPLATES[intent]))
        connector = random.choice(CONNECTORS)
        return f"{text} {connector} {second}"
    return text

def augment(text: str) -> str:
    """Apply 0-2 augmentations randomly. Conservative — keep most clean."""
    r = random.random()
    if r < 0.07:
        text = drop_accent(text)
    elif r < 0.12:
        text = telex_typo(text)
    elif r < 0.16:
        text = random_typo(text)
    elif r < 0.19:
        text = char_swap(text)
    elif r < 0.25:
        text = drop_filler_word(text)
    if random.random() < 0.30:
        text = cap_first(text)
    return text

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--per_intent", type=int, default=2000,
                   help="target samples per intent after dedup")
    p.add_argument("--seed", type=int, default=42)
    p.add_argument("--out", default=str(OUT_PATH))
    args = p.parse_args()

    random.seed(args.seed)
    out_path = Path(args.out)

    rows: list[dict] = []
    if SEED_PATH.exists():
        rows.extend(pd.read_csv(SEED_PATH).to_dict("records"))

    for intent, templates in TEMPLATES.items():
        seen = set(r["text"] for r in rows if r["intent"] == intent)
        kept = len(seen)  # O(1) running count for this intent (was O(N) per-iter scan)
        attempts = 0
        target_attempts = args.per_intent * 5
        while kept < args.per_intent and attempts < target_attempts:
            attempts += 1
            t = random.choice(templates)
            text = fill(t)
            text = decorate(text)
            text = maybe_compound(intent, text)
            text = augment(text)
            text = re.sub(r"\s+", " ", text).strip()
            if not text or text in seen or len(text) < 3:
                continue
            seen.add(text)
            rows.append({"text": text, "intent": intent})
            kept += 1

    out = pd.DataFrame(rows).drop_duplicates(subset=["text"]).reset_index(drop=True)
    out.to_csv(out_path, index=False, encoding="utf-8")

    counts = out["intent"].value_counts()
    print(f"[gen v3] wrote {len(out)} rows to {out_path}")
    for intent, n in counts.items():
        print(f"{intent:<14} {n}")

    from collections import Counter
    c = Counter()
    for t in out["text"]:
        c.update(t.lower().split())
    print(f"[gen v3] approx vocab (whitespace): {len(c)} unique tokens")

if __name__ == "__main__":
    main()
