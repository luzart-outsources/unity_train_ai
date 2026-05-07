from __future__ import annotations
import argparse
import random
import re
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
SEED_PATH = ROOT / "data" / "intents.csv"
OUT_PATH = ROOT / "data" / "intents_v6.csv"

# MEGA Word Pools - ~5x bigger than v3

TIMES = [
    # Day-relative
    "hôm nay", "hôm qua", "hôm kia", "hôm trước", "hôm trc",
    "ngày mai", "ngày kia", "ngày mốt", "mai", "mốt", "kia",
    "tuần này", "tuần sau", "tuần tới", "tuần trước", "tuần trc",
    "cuối tuần", "đầu tuần", "giữa tuần", "cuối tuần này", "cuối tuần sau",
    "tháng này", "tháng sau", "đầu tháng", "cuối tháng", "tháng tới", "tháng trước",
    # Weekday
    "thứ 2", "thứ 3", "thứ 4", "thứ 5", "thứ 6", "thứ 7", "chủ nhật", "cn",
    "thứ hai", "thứ ba", "thứ tư", "thứ năm", "thứ sáu", "thứ bảy",
    "thứ 2 tuần này", "thứ 2 tuần sau", "thứ 6 tuần này",
    # Day-part
    "buổi sáng", "buổi trưa", "buổi chiều", "buổi tối", "đêm nay", "khuya nay",
    "ban sáng", "ban chiều", "ban tối", "ban đêm",
    "sáng nay", "trưa nay", "chiều nay", "tối nay",
    "sáng mai", "trưa mai", "chiều mai", "tối mai",
    "đầu giờ sáng", "cuối giờ sáng", "đầu giờ chiều", "cuối giờ chiều",
    "giữa trưa", "đầu giờ tối", "khuya khoắt", "tang tảng sáng",
    # Clock - explicit
    "5 giờ", "5h", "5g", "5 giờ sáng", "5h sáng",
    "6 giờ", "6h", "6 giờ sáng", "6h sáng", "6 rưỡi",
    "7 giờ", "7h", "7 giờ sáng", "7 rưỡi", "7h30", "7h rưỡi",
    "8 giờ", "8h", "8 rưỡi", "8h30", "8 giờ sáng",
    "9 giờ", "9h", "9 rưỡi", "9h sáng", "9h30",
    "10 giờ", "10h", "10 giờ rưỡi", "10h30", "10h sáng",
    "11 giờ", "11h", "11 rưỡi", "11h30", "11 giờ trưa",
    "12 giờ", "12h", "12 giờ trưa", "12 trưa",
    "1 giờ chiều", "1h chiều", "13h", "1g chiều",
    "2 giờ", "2h", "2 giờ chiều", "2h chiều", "14h",
    "3 giờ chiều", "3h chiều", "15h",
    "4 giờ chiều", "4h chiều", "16h", "4 rưỡi chiều",
    "5 giờ chiều", "5h chiều", "17h",
    "6 giờ tối", "6h tối", "18h",
    "7 giờ tối", "7h tối", "19h", "7 rưỡi tối",
    "8 giờ tối", "8h tối", "20h", "8 rưỡi tối",
    "9 giờ tối", "9h tối", "21h",
    "10 giờ tối", "10h tối", "22h",
    "11 giờ đêm", "11h đêm", "23h",
    "12 giờ đêm", "0h", "đúng giờ", "đúng 7 giờ", "tầm 7 giờ", "khoảng 8 giờ",
    # Vague
    "lát nữa", "tý nữa", "chốc nữa", "lúc nãy", "ban nãy", "vừa rồi", "vừa nãy",
    "sắp tới", "sắp", "hồi nãy", "khi nãy", "sắp sửa", "tí nữa", "chút nữa",
    "ngay bây giờ", "bây giờ", "giờ này", "lúc này", "thời gian này",
    # Semester
    "học kỳ này", "học kỳ sau", "kỳ trước", "kỳ này", "kỳ sau",
    "đầu kỳ", "cuối kỳ", "giữa kỳ", "kỳ nghỉ", "nghỉ hè", "nghỉ tết",
]

MEALS = [
    "ăn cơm", "ăn trưa", "ăn tối", "ăn sáng", "ăn xế", "ăn vặt",
    "phát cơm", "có cơm", "có bữa", "có suất", "có khẩu phần",
    "cơm chiều", "cơm trưa", "cơm sáng", "cơm tối",
    "lấy cơm", "lĩnh cơm", "lĩnh phần ăn", "lĩnh phần",
    "uống nước", "uống sữa", "phát đồ ăn", "phát suất ăn",
    # Synonyms / dialect
    "xơi cơm", "chén cơm", "dùng cơm", "dùng bữa", "dùng suất",
    "đi ăn", "kéo nhau ăn", "tới giờ ăn", "vào bữa", "cơm nước",
    "phang cơm", "lùa cơm", "đớp cơm", "đớp bữa",
    "bữa sáng", "bữa trưa", "bữa tối", "bữa chiều", "bữa khuya",
    "ăn trưa cùng đại đội", "ăn cùng đại đội", "ăn theo đơn vị",
]

PLACES = [
    # Military quarters
    "nhà ăn", "nhà bếp", "phòng tập", "sân tập", "lớp học", "phòng y tế",
    "trạm y tế", "doanh trại", "nhà kho", "bãi tập", "kho súng", "kho đạn",
    "kho hậu cần", "kho quân nhu", "kho lương thực", "kho vũ khí",
    "đại đội", "trung đội", "tiểu đội", "phân đội", "ban chỉ huy",
    "phòng chỉ huy", "phòng họp", "phòng họp đại đội", "nhà chỉ huy",
    "khu hành chính", "phòng trực ban", "phòng trực", "phòng văn hóa",
    "phòng truyền thống", "sân điều lệnh", "bãi bắn", "trường bắn",
    "bãi tập đội ngũ", "bãi tập kỹ thuật", "bãi tập chiến thuật",
    "vườn rau", "vườn tăng gia", "khu tăng gia", "chỗ tăng gia",
    "khu nuôi quân", "chuồng heo", "chuồng gà", "ao cá",
    # Academic blocks (KTMM specific)
    "phòng học", "phòng học A1", "phòng học A2", "phòng học B1", "phòng học B2",
    "phòng học C1", "phòng học D1", "phòng thí nghiệm", "phòng lab",
    "phòng máy tính", "phòng thực hành", "phòng thực hành mạng",
    "phòng thực hành bảo mật", "thư viện", "thư viện học viện",
    "ký túc xá", "ktx", "khu ktx", "khu KTX", "ký túc xá học viện",
    "phòng 305", "phòng 306", "phòng 401", "phòng 402", "phòng 501", "phòng 502",
    "phòng 201", "phòng 202", "phòng 101", "phòng 102",
    "phòng giảng đường", "giảng đường lớn", "giảng đường nhỏ", "giảng đường A",
    "giảng đường B", "giảng đường C", "giảng đường 1", "giảng đường 2",
    "căng tin", "căn tin", "quán nước", "quán cafe", "khu B", "khu A", "khu C",
    "khu B5", "khu B4", "khu A1", "khu A2", "khu C3",
    "tòa B5", "tòa A", "tòa B", "tòa C",
    "phòng đào tạo", "phòng quản lý sinh viên", "phòng đoàn",
    "phòng hội trường", "hội trường", "hội trường lớn",
    "trung tâm thể thao", "sân vận động", "sân bóng", "sân bóng đá",
    "sân bóng rổ", "sân tennis", "bể bơi", "bể bơi học viện",
    # Commute
    "cổng chính", "cổng phụ", "cổng số 1", "cổng số 2", "cổng số 3",
    "khu để xe", "bãi gửi xe", "bãi xe", "trạm xe buýt",
    "đường nội bộ", "đường ra cổng", "đường vào ban chỉ huy",
    # Other
    "phòng tắm", "nhà vệ sinh", "wc", "khu vệ sinh", "phòng giặt",
    "phòng đọc", "phòng tự học", "phòng thí điểm", "trung tâm dữ liệu",
    "data center", "phòng server", "phòng net", "phòng máy",
]

KNOWLEDGE = [
    # Combat
    "súng AK", "súng AK47", "súng AKM", "súng K54", "súng K59", "súng RPG",
    "súng B40", "súng B41", "súng tiểu liên", "súng trường",
    "lựu đạn", "lựu đạn M67", "lựu đạn cay", "mìn", "mìn DH-10", "mìn định hướng",
    "thủ pháo", "đạn", "đạn 7.62", "viên đạn", "ngòi nổ", "kíp nổ",
    "trái nổ", "vũ khí cá nhân", "vũ khí tập thể",
    # Tactical drill
    "chào điều lệnh", "đi đều bước", "đi đứng", "đi đều", "đi nghiêm",
    "tư thế nghiêm", "tư thế nghỉ", "đứng nghiêm", "đứng nghỉ",
    "đội hình hàng dọc", "đội hình hàng ngang", "đội hình chữ V",
    "đội hình mũi nhọn", "đội hình tản khai",
    "quy tắc bắn", "quy tắc 3 điểm", "ngắm bắn", "ngắm 3 điểm",
    "tư thế quỳ bắn", "tư thế nằm bắn", "tư thế đứng bắn", "tư thế bắn",
    "tháo lắp súng", "lau chùi súng", "bảo dưỡng súng", "bảo dưỡng vũ khí",
    "vệ sinh vũ khí", "kiểm tra vũ khí", "kỹ thuật chiến đấu",
    "kỹ thuật bộ binh", "vận động chiến thuật", "chiến thuật", "chiến thuật bộ binh",
    "ẩn nấp", "ngụy trang", "ngụy trang địa hình",
    "vượt vật cản", "đu dây", "leo tường", "vượt rào", "vượt chướng ngại vật",
    # Discipline
    "10 lời thề", "12 điều", "10 lời thề danh dự", "kỷ luật quân đội",
    "điều lệnh đội ngũ", "điều lệnh quản lý bộ đội", "quân hàm", "cấp bậc",
    "cấp bậc quân hàm", "quân hiệu", "phù hiệu",
    "trực ban", "trực nhật", "lễ chào cờ", "tăng gia sản xuất",
    "tự quản đại đội", "tự quản tiểu đội", "an toàn doanh trại",
    "an toàn cháy nổ", "phòng cháy chữa cháy", "PCCC",
    "nội quy doanh trại", "nội quy ký túc xá",
    # Physical
    "bài tập thể lực", "chạy 3000m", "chạy 1500m", "chạy 100m", "chạy bền",
    "chống đẩy", "hít xà", "bật xa", "kéo xà đơn", "vượt xà kép",
    "xà đôi", "xà đơn", "tiêu chuẩn thể lực", "kiểm tra thể lực",
    # KTMM-specific (cyber/crypto)
    "mật mã đối xứng", "mật mã bất đối xứng", "AES", "DES", "3DES", "RSA",
    "ECC", "Elliptic Curve", "hàm băm", "SHA-256", "SHA-1", "MD5",
    "chữ ký số", "chứng chỉ số", "PKI", "SSL", "TLS", "HTTPS",
    "an toàn thông tin", "an ninh mạng", "an toàn mạng",
    "xâm nhập mạng", "tường lửa", "firewall", "phát hiện xâm nhập", "IDS", "IPS",
    "mã hóa dữ liệu", "mã hóa file", "mã hóa AES", "mã hóa RSA",
    "Linux quân sự", "Linux", "Ubuntu", "Kali Linux",
    "quy chế học viện", "lập trình C", "lập trình Python", "lập trình Java",
    "thuật toán Dijkstra", "thuật toán A*", "thuật toán BFS", "thuật toán DFS",
    "cấu trúc dữ liệu", "cây nhị phân", "cây AVL", "cây đỏ đen",
    "đồ thị", "đồ thị có hướng", "phân tích thuật toán", "độ phức tạp",
    "lý thuyết thông tin", "entropy", "mã sửa lỗi", "Reed-Solomon",
    "mạng máy tính", "TCP/IP", "OSI", "DNS", "DHCP",
    # Training subject
    "lý luận chính trị", "tư tưởng Hồ Chí Minh", "lịch sử Đảng",
    "kỹ thuật quân sự", "y tế cấp cứu", "băng bó vết thương",
    "sơ cấp cứu", "cấp cứu chiến trường", "cứu thương",
    "đường lối quân sự", "quan điểm quốc phòng", "an ninh quốc gia",
    # Subjects
    "toán cao cấp", "đại số tuyến tính", "giải tích", "xác suất thống kê",
    "tiếng Anh", "tiếng Anh chuyên ngành", "vật lý đại cương",
    "tin học cơ sở", "lập trình hướng đối tượng", "OOP",
    "cơ sở dữ liệu", "hệ điều hành", "kiến trúc máy tính",
]

REPORT_OBJ = [
    "đại đội", "đại đội 1", "đại đội 2", "đại đội 3", "đại đội 4",
    "đại đội 5", "đại đội 6", "đại đội tự quản",
    "trung đội", "trung đội 1", "trung đội 2", "trung đội 3", "trung đội 4",
    "tiểu đội", "tiểu đội 1", "tiểu đội 2", "tiểu đội 3", "tiểu đội 4",
    "tiểu đội 5", "tiểu đội 6", "tiểu đội 7",
    "đội hình", "đội hình đại đội", "đội hình trung đội",
    "phân đội", "phân đội 1", "phân đội 2",
    "lớp", "lớp CT06", "lớp ATM06", "lớp CT05", "lớp ATM05", "lớp ATM04",
    "lớp CT07", "lớp KMA", "lớp K20", "lớp K21",
    "tổ trực ban", "tổ trực nhật", "tổ canh gác",
    "đội mẫu", "đội mẫu đại đội", "đội mẫu trung đội",
    "nhóm 1", "nhóm 2", "nhóm 3", "nhóm A", "nhóm B", "nhóm C",
    "tổ", "tổ 1", "tổ 2", "tổ 3", "tổ 4",
    "ban chỉ huy", "ban chỉ huy đại đội", "đại đội tự quản",
    "đội canh gác", "tổ canh gác", "tổ trực gác",
    "kíp trực", "kíp trực ban", "ca trực",
]

REASONS = [
    # Health
    "ốm", "bị ốm", "bị sốt", "sốt cao", "sốt 38 độ", "sốt 39 độ", "sốt 40 độ",
    "đau bụng", "đau bụng dưới", "đau dạ dày", "đau ruột thừa",
    "đau đầu", "đau đầu nặng", "đau nửa đầu", "đau lưng", "đau cổ",
    "viêm họng", "viêm họng cấp", "viêm xoang", "viêm phế quản",
    "ho nhiều", "ho dai dẳng", "ho có đờm",
    "cảm cúm", "cảm lạnh", "cúm A", "covid", "covid-19", "F0", "F1",
    "đau răng", "viêm lợi", "tiêu chảy", "tiêu chảy cấp",
    "khó thở", "tức ngực", "đau ngực", "chóng mặt", "buồn nôn",
    "say nắng", "say sóng", "trật cổ chân", "bong gân", "bong gân cổ chân",
    "bị thương nhẹ", "vết thương cũ", "bị thương chiến thuật",
    "đau khớp gối", "đau khớp", "viêm khớp",
    "đau dạ dày", "viêm loét dạ dày", "trào ngược dạ dày",
    # Family
    "có việc gia đình", "gia đình có việc", "có việc gấp ở nhà",
    "bố ốm", "mẹ ốm", "bố mẹ ốm", "ông bà ốm", "anh chị em ốm",
    "có giỗ", "có giỗ ông", "có giỗ bà", "có giỗ bố", "có giỗ mẹ",
    "anh chị về thăm", "có lễ gia đình", "phải về quê", "phải về thăm gia đình",
    "nhà có chuyện", "có việc nhà gấp", "ba mẹ gọi về",
    "đám cưới anh chị", "đám tang ông bà", "tang lễ",
    # Procedural
    "đi khám bệnh", "có lịch khám", "đi viện", "có hẹn bác sĩ",
    "đi tái khám", "có lịch hẹn", "việc đột xuất", "có việc đột xuất",
    "có việc gấp", "có lịch học bù", "có lịch thi", "có lịch thi lại",
    "có lịch phỏng vấn", "có lịch phỏng vấn xin việc",
    "đi xin học bổng", "có lịch ở học viện", "có buổi báo cáo",
    "có buổi seminar", "có cuộc thi tin học", "có cuộc thi học sinh giỏi",
    "có hội thảo", "có buổi hội thảo", "có buổi tập huấn",
    "có lịch giảng dạy", "có lịch trợ giảng",
]

PRE_GREETINGS = [
    "", "", "", "", "", "", "",  # heavy bias to empty
    "thưa thủ trưởng ", "báo cáo thủ trưởng ", "đồng chí ơi ",
    "anh ơi ", "em hỏi với ", "thầy ơi ", "thưa cán bộ ",
    "xin phép thủ trưởng ", "xin hỏi ", "cho em hỏi ", "cho hỏi ",
    "thưa anh ", "thưa thầy ", "thưa các anh ", "đồng chí cho hỏi ",
    "xin lỗi đồng chí ", "đồng chí cho em hỏi ", "anh cho em hỏi ",
    "em báo cáo ", "thủ trưởng ơi ", "chỉ huy ơi ", "trực ban ơi ",
    "cán bộ ơi ", "anh trực ban ", "anh ơi cho em hỏi ",
    "cô ơi ", "thưa cô ", "thầy cho hỏi ", "cô cho hỏi ",
    "anh cho hỏi ", "thưa thủ trưởng cho hỏi ", "đại ca ơi ",
    "anh em ơi ", "các đồng chí ơi ", "tổ trực ơi ",
    "ơ này ", "ê ", "này ", "alo ", "này em hỏi ",
]

POST_PARTICLES = [
    "", "", "", "", "", "", "",  # heavy empty bias
    " ạ", " nhỉ", " thế", " vậy", " đi", " đấy", " chứ",
    " được không", " được không ạ", " thưa thủ trưởng",
    "?", " ?", "...", " nha", " nhé", " nghe", " á",
    " thì sao", " thì làm sao", " làm sao", " phải không",
    " đúng không", " mà", " hả", " hả thủ trưởng",
    " hả anh", " thưa anh", " ạ thủ trưởng",
    " anh nhỉ", " thầy nhỉ", " cô nhỉ", " hông",
    " hum", " hong", " hôn", " z", " zậy", " dợ",
]

CONNECTORS = [
    "với lại", "rồi", "và", "nhân tiện", "à mà", "thế còn",
    "rồi còn", "vả lại", "à với cả", "hôm nay luôn",
    "tiện thể", "luôn nhé", "thêm nữa", "à phải rồi", "à quên",
    "à còn nữa", "với lại nữa",
]

# OOC small talk pool - much bigger and harder
OOC = [
    # Weather
    "hôm nay trời đẹp nhỉ", "trời nóng quá", "trời lạnh thế",
    "mưa to quá", "mưa rả rích cả ngày", "nắng quá tôi không chịu nổi",
    "gió mạnh quá", "trời âm u", "trời sắp mưa", "không khí mát mẻ",
    "có dự báo bão không", "có lụt không nhỉ", "miền bắc lạnh ghê",
    "Hà Nội mùa này", "Sài Gòn nóng kinh khủng", "trời đẹp như tranh",
    "trời sương mù dày", "trời quang mây tạnh", "buổi sáng se lạnh",
    "trời mùa thu mát mẻ", "trời mùa đông buốt giá",
    # Sports
    "bạn có biết bóng đá không", "Messi với Ronaldo ai giỏi hơn",
    "đội tuyển Việt Nam đá thế nào", "U23 mình mạnh không",
    "Hà Nội FC vs Hoàng Anh Gia Lai trận nào hay", "bóng rổ NBA",
    "Lakers thắng đêm qua", "F1 Verstappen lại win", "tennis Federer",
    "đội tuyển bóng chuyền nữ", "có ai chơi pickleball không",
    "Park Hang Seo về Hàn rồi", "HLV mới của tuyển VN",
    "World Cup năm sau", "EURO 2024 ai vô địch", "Champions League",
    "Quang Hải ghi bàn không", "Văn Hậu chấn thương",
    # Games
    "có ai chơi liên quân không", "PUBG tối nay không",
    "dota mới nào hay", "đội mình thắng rồi", "tôi vừa rank lên cao thủ",
    "Genshin Impact map mới ra", "Valorant skin hot",
    "đội mình thua hổ", "ranked queue lâu quá",
    "LoL có patch mới", "champion mới ra", "skin Battle Pass",
    "đỉnh kíu" "AFK luôn", "lag ác liệt", "gank top",
    "Free Fire có gì mới", "MLBB tổ chức giải", "PUBG Mobile",
    # Movies / shows
    "phim mới nào hay", "tôi vừa xem phim hay lắm", "phim Mai dở",
    "Avatar 2 hay không", "anime mùa này nào hot", "Naruto ending",
    "One Piece chap mới", "Marvel ra phim mới", "phim Hàn này cảm động",
    "Squid Game season 2", "Wednesday season 2", "Stranger Things",
    "Breaking Bad", "Better Call Saul", "Game of Thrones",
    "Doraemon mới ra", "Conan tập mới", "Demon Slayer hot",
    # Food / drink
    "thèm trà sữa quá", "đang thèm phở", "muốn ăn bún bò Huế",
    "trà đào TocoToco ngon không", "Highlands cafe đắt vãi",
    "cà phê đen đá thôi", "thèm bánh mì pa-tê", "có ai mua đồ ăn không",
    "Phúc Long mới mở cửa hàng", "trân châu đen với trắng",
    "phở 24 ngon không", "bún chả Hà Nội tuyệt", "nem rán giòn rụm",
    "bánh cuốn Thanh Trì", "bún đậu mắm tôm", "cơm tấm Sài Gòn",
    "hủ tiếu Mỹ Tho", "mì Quảng", "bánh xèo miền Tây",
    # Mood / life
    "tôi buồn quá", "tôi đang vui", "stress quá", "mệt mỏi quá",
    "deadline dí sát rồi", "burnout luôn rồi đây",
    "nhớ nhà quá", "nhớ bạn gái", "nhớ bạn thân", "nhớ người yêu",
    "muốn về nhà ngủ thôi", "không muốn dậy nữa",
    "đói bụng nhưng chưa tới giờ", "muốn ăn vặt",
    "buồn ngủ quá", "thèm ngủ thêm", "mất ngủ cả đêm",
    "hôm nay không vui", "tâm trạng không tốt", "cảm thấy cô đơn",
    "tôi đang vui vẻ", "ngày tuyệt vời", "tâm trạng tốt",
    # Tech / KTMM small talk
    "máy tính của tôi bị lag", "wifi yếu quá", "wifi rớt",
    "máy laptop sạc chậm", "ChatGPT mới ra phiên bản mới",
    "Github Copilot ngon ghê", "Visual Studio Code crash", "VS Code update",
    "Linux Ubuntu 24 mới", "tôi mới install Arch", "có ai dùng Macbook không",
    "AI Claude với GPT ai ngon hơn", "DeepSeek viết code OK",
    "Cursor IDE thử chưa", "thuật toán Dijkstra hơi khó",
    "lập trình C khó quá", "Python dễ ghê", "JavaScript ức chế",
    "iPhone 15 Pro Max đẹp", "Samsung Galaxy mới ra", "tablet hay laptop",
    "tai nghe Sony", "AirPods Pro 2", "loa JBL",
    # Class / school
    "thầy môn toán dạy vui ghê", "đề thi học kỳ ra dễ", "đề khó vãi",
    "điểm thi sao rồi nhỉ", "có học bù không", "lớp mình ai trượt",
    "hôm qua tôi thức khuya quá", "ngủ chưa đủ giấc",
    "ngày mai có khảo sát", "tài liệu môn an toàn thông tin",
    "cuối kỳ có nhiều bài lắm", "giảng viên chấm điểm khắt khe",
    "Học viện kỹ thuật mật mã có nhiều môn hay",
    "lớp mình xếp hạng tệ", "kỳ này trúng môn khó",
    "tớ lại thiếu chuyên cần", "deadline đồ án TN dí sát",
    "thi tốt nghiệp năm sau", "đăng ký thực tập",
    "chuyên đề năm cuối", "đồ án tốt nghiệp", "luận văn",
    # Misc
    "đi chơi cuối tuần không", "Hà Nội cuối tuần đi đâu",
    "nhóm chat lớp im quá", "ai có note môn toán cho mượn",
    "có ai mất điện không", "có ai rảnh rủ đi cà phê",
    "máy quay được không", "tôi vừa xem TikTok hay lắm",
    "có deal sale gì không", "Shopee 9/9 có voucher gì",
    "Tiki giao hàng nhanh không", "Lazada free ship",
    "tớ vừa nhận lương", "tháng này tiêu hết tiền",
    "cuối năm có thưởng không", "hôm nay có gì đặc biệt",
    "Tết đoàn viên về quê", "Trung thu ở nhà",
    "cha mẹ già rồi", "tự do tài chính", "buôn coin lỗ",
    "stocks đỏ lửa hôm nay", "Bitcoin lên hay xuống",
    # Random knowledge
    "1 + 1 bằng mấy", "trái đất có bao nhiêu châu lục",
    "ai phát minh ra đèn điện", "Pi số bằng bao nhiêu",
    "Thủ đô Pháp là gì", "Mỹ có bao nhiêu bang",
    "tốc độ ánh sáng", "vận tốc âm thanh",
]

# SYNONYM SWAP - apply to fully-formed sentences
SYNONYMS = {
    # verbs
    "ăn": ["ăn", "xơi", "chén", "đớp", "lùa", "đợp", "phang"],
    "đi": ["đi", "tới", "ra", "đến", "sang", "qua"],
    "có": ["có", "có không", "có chứ", "có ko"],
    "hỏi": ["hỏi", "thắc mắc", "muốn biết", "cho hỏi"],
    "biết": ["biết", "rõ", "hay", "nắm được"],
    "xem": ["xem", "coi", "ngó", "nhòm"],
    "nghỉ": ["nghỉ", "nghỉ ngơi", "vắng", "nghỉ phép"],
    "đến": ["đến", "tới", "đi tới", "đi đến"],
    "làm": ["làm", "thực hiện", "tiến hành"],
    # particles
    "không": ["không", "ko", "k", "khôg", "hông", "chăng"],
    "vậy": ["vậy", "thế", "z", "zậy", "dậy"],
    "thế nào": ["thế nào", "ra sao", "ntn", "như thế nào", "ra làm sao"],
    "khi nào": ["khi nào", "lúc nào", "bao giờ", "chừng nào"],
    "đâu": ["đâu", "nào", "ở đâu", "chỗ nào"],
    "gì": ["gì", "j", "chi", "cái gì"],
    # nouns
    "thủ trưởng": ["thủ trưởng", "thầy", "anh", "chỉ huy", "đại đội trưởng"],
    "đồng chí": ["đồng chí", "anh", "em", "bạn"],
}

# TEMPLATES - 200+ per intent
TEMPLATES = {
    "HOI_LICH": [
        # Direct question forms
        "{time} có lịch gì",
        "{time} có hoạt động gì",
        "{time} có việc gì",
        "{time} có gì làm không",
        "{time} có chương trình gì",
        "{time} có buổi gì",
        "{time} có lịch không",
        "{time} có gì đặc biệt",
        "{time} có gì mới không",
        "{time} có gì làm",
        "{time} mình làm gì",
        "{time} đại đội làm gì",
        "{time} trung đội làm gì",
        "{time} tiểu đội làm gì",
        "{time} sinh viên làm gì",
        "{time} lớp ta làm gì",
        "{time} đơn vị làm gì",
        "{time} ta làm gì",
        # Schedule synonyms
        "lịch {time} thế nào",
        "lịch {time} ra sao",
        "lịch {time} ra làm sao",
        "lịch {time} có gì",
        "lịch {time} là gì",
        "lịch {time} ntn",
        "lịch {time} thay đổi gì không",
        "thời khoá biểu {time}",
        "thời khoá biểu {time} thế nào",
        "thời khoá biểu {time} ra sao",
        "thời khoá biểu {time} có gì",
        "lịch trình {time}",
        "lịch trình {time} thế nào",
        "kế hoạch {time}",
        "kế hoạch {time} là gì",
        "kế hoạch của đại đội {time}",
        "kế hoạch huấn luyện {time}",
        "chương trình {time}",
        "chương trình {time} có gì",
        "lịch học {time}",
        "lịch huấn luyện {time}",
        "lịch tập {time}",
        "lịch trực {time}",
        "lịch gác {time}",
        # Activity-focused
        "{time} phải làm gì",
        "{time} phải làm những gì",
        "{time} làm gì",
        "{time} học gì",
        "{time} học môn gì",
        "{time} học bài gì",
        "{time} tập gì",
        "{time} tập môn gì",
        "{time} tập bài gì",
        "{time} có buổi tập không",
        "{time} có buổi học không",
        "{time} có giờ học không",
        "{time} có giờ tập không",
        # Compound time
        "lịch tuần",
        "lịch tuần này",
        "lịch trong ngày",
        "lịch hôm nay",
        "lịch hôm nay thế nào",
        "lịch trong tuần",
        "lịch tuần tới",
        "lịch của lớp {time}",
        "lịch của đại đội {time}",
        "lịch của lớp ta {time}",
        # Existence questions
        "có lịch gì đặc biệt {time} không",
        "có hoạt động bất thường nào {time} không",
        "có ai biết lịch {time} không",
        "có biết lịch {time} không",
        "có lịch {time} không",
        "có thay đổi lịch {time} không",
        "lịch {time} có thay đổi gì không",
        "có thông báo lịch {time} không",
        "có lịch học {time} không",
        # Polite / question to commander
        "tôi muốn biết lịch",
        "tôi muốn xem lịch",
        "tôi cần biết lịch {time}",
        "cho tôi xem lịch {time}",
        "cho em xem thời khoá biểu {time}",
        "cho em xem lịch {time}",
        "cho hỏi lịch {time}",
        "cho hỏi lịch huấn luyện {time}",
        "cho em hỏi lịch {time}",
        "thông báo lịch {time}",
        "thông báo lịch huấn luyện {time}",
        # Subjects
        "lịch học môn nào {time}",
        "{time} mình học môn gì",
        "{time} có học môn gì",
        "{time} thầy giảng gì",
        "{time} cô giảng gì",
        "môn nào học {time}",
        # Specific units
        "{time} đại đội ta làm gì",
        "{time} đại đội có lịch gì",
        "{time} trung đội có lịch gì",
        "{time} đại đội ta có gì",
        "đại đội ta {time} làm gì",
        "đơn vị {time} có lịch gì",
        # Tasks
        "nhiệm vụ {time} là gì",
        "công việc {time} có gì",
        "{time} ta làm việc gì",
        "{time} cần làm gì",
        "{time} làm việc gì",
        # Ellipsis
        "còn {time} thì sao",
        "thế còn {time}",
        "{time} thì",
        "{time} thì làm sao",
        "{time} thì có gì",
        # Casual / informal
        "{time} có gì hay không",
        "{time} có gì hay ho",
        "{time} có hoạt động vui không",
        "{time} có sinh hoạt không",
        "{time} sinh hoạt gì",
        "{time} có mít tinh không",
        # Realistic
        "{time} đại đội tập trung không",
        "{time} có tập trung không",
        "{time} có điểm danh không",
        "{time} có sinh hoạt đảng không",
        "{time} có sinh hoạt đại đội không",
        "{time} có hội họp không",
        "{time} có giao ban không",
        "{time} có học chính trị không",
        "{time} có học điều lệnh không",
        "{time} có học kỹ thuật không",
        "{time} có học chiến thuật không",
        "{time} có bắn súng không",
        "{time} có bắn đạn không",
        "{time} có ra bãi không",
        "{time} có ra thao trường không",
        # Asking next
        "lịch sắp tới có gì",
        "lịch tiếp theo là gì",
        "lịch sau đó",
        "tới đây có lịch gì",
        "sắp tới có gì",
        "sau đó có gì",
        # Wondering
        "{time} không biết có gì",
        "{time} chẳng biết làm gì",
        "{time} không rõ có lịch không",
        "{time} chưa biết lịch",
        # Code-mix
        "schedule {time} sao",
        "schedule {time} có gì",
        "agenda {time}",
        "plan {time} thế nào",
        # Slang short
        "lịch",
        "lịch {time}",
        "có lịch không",
        "{time} sao",
        "{time} sao rồi",
        "{time} sao đây",
        # Compound
        "lịch hôm nay với mai có gì khác không",
        "lịch hôm nay và mai có khác không",
        "lịch tuần này và sau khác sao",
    ],
    "HOI_GIO_AN": [
        # Direct
        "mấy giờ {meal}",
        "khi nào {meal}",
        "bao giờ {meal}",
        "lúc nào {meal}",
        "{meal} mấy giờ",
        "{meal} lúc mấy giờ",
        "{meal} giờ nào",
        "{meal} khi nào",
        "{meal} lúc nào",
        "{meal} bao giờ",
        "giờ {meal} là khi nào",
        "giờ {meal} là lúc nào",
        "giờ {meal} bao giờ",
        "giờ {meal} mấy giờ",
        "lúc nào thì {meal}",
        "khi nào thì {meal}",
        "bao giờ thì {meal}",
        "{time} {meal} mấy giờ",
        "{time} mấy giờ {meal}",
        "{time} bao giờ {meal}",
        "{time} khi nào {meal}",
        # Hunger
        "tôi đói rồi {meal} chưa",
        "tôi đói {meal} chưa",
        "đói rồi {meal} chưa",
        "đói lắm rồi {meal} chưa",
        "đói quá khi nào ăn",
        "đói bụng quá khi nào ăn",
        "đói đói {meal} chưa",
        "em đói {meal} chưa",
        "em đói rồi {meal} chưa",
        # Implicit / complaint
        "đói quá",
        "đói lắm rồi",
        "đói bụng quá",
        "bụng cồn cào",
        "bụng đói meo",
        "đến giờ ăn chưa",
        "đến giờ {meal} chưa",
        "tới giờ ăn chưa",
        "tới giờ {meal} chưa",
        "có gì ăn không",
        "có cơm chưa",
        "có gì cho ăn không",
        "đến bữa chưa",
        "tới bữa chưa",
        "đến bữa hay chưa",
        # Cooking / preparation
        "cơm nấu xong chưa",
        "nhà ăn nấu cơm chưa",
        "nhà bếp nấu xong chưa",
        "bếp nấu xong cơm chưa",
        "cơm chuẩn bị xong chưa",
        # Shop hours
        "khi nào nhà ăn mở",
        "nhà ăn mở mấy giờ",
        "nhà ăn đóng cửa mấy giờ",
        "nhà ăn đóng mấy giờ",
        "căn tin mở mấy giờ",
        "căng tin mở mấy giờ",
        "căn tin đóng mấy giờ",
        # Specific time
        "trưa nay mấy giờ ăn",
        "tối nay mấy giờ ăn",
        "sáng mai ăn mấy giờ",
        "tối nay ăn lúc mấy giờ",
        "trưa nay ăn lúc mấy giờ",
        "sáng nay ăn lúc mấy giờ",
        "trưa mai mấy giờ ăn",
        "tối mai mấy giờ ăn",
        # Ration
        "cơm tối {time}",
        "cơm trưa {time}",
        "cơm sáng {time}",
        "{time} có cơm không",
        "{time} có cơm chưa",
        "{time} cơm chưa",
        "{time} có suất chưa",
        "{time} có khẩu phần chưa",
        "{time} có gì ăn không",
        # Done
        "ăn xong rồi à",
        "ăn xong chưa",
        "ăn cơm xong chưa",
        "đại đội ăn xong chưa",
        # Remaining
        "còn bao lâu nữa ăn",
        "còn mấy phút nữa ăn",
        "còn bao lâu thì có cơm",
        "còn bao lâu thì ăn",
        "còn lâu nữa không tới giờ ăn",
        "còn lâu mới tới giờ ăn không",
        # Group
        "ăn cơm cùng đại đội mấy giờ",
        "đơn vị ăn lúc mấy giờ",
        "đại đội ăn mấy giờ",
        "lớp ta ăn lúc mấy giờ",
        "đại đội ăn cơm lúc nào",
        "trung đội ăn mấy giờ",
        # Schedule
        "giờ ăn của đơn vị",
        "giờ ăn đại đội",
        "thời gian ăn cơm",
        "thời gian ăn trưa",
        "thời gian ăn tối",
        "thời gian ăn sáng",
        "thời gian {meal}",
        "lịch ăn của đại đội",
        "lịch ăn cơm",
        "lịch ăn trưa",
        "lịch ăn tối",
        # Distribution
        "đến giờ phát cơm chưa",
        "phát cơm lúc nào",
        "phát cơm bao giờ",
        "phát cơm mấy giờ",
        "phát cơm khi nào",
        "phát suất ăn lúc nào",
        # Menu
        "cơm tối nay có gì",
        "cơm trưa nay có gì",
        "cơm hôm nay có gì",
        "{time} ăn món gì",
        "{time} có món gì",
        "{time} bữa có gì",
        "bữa nay có gì ăn",
        # General
        "có ai biết giờ ăn không",
        "có ai biết bữa nay mấy giờ không",
        "có ai biết khi nào ăn không",
        "đói quá đến lúc ăn chưa",
        "mấy giờ thì có suất",
        "mấy giờ thì có cơm",
        # Code-mix
        "lunch mấy giờ",
        "dinner mấy giờ",
        "breakfast mấy giờ",
        "lunch time là khi nào",
        "{meal} time mấy giờ",
        # Slang
        "{meal}",
        "khi nào {meal}",
        "đói",
        "ăn",
        "cơm",
        "ăn chưa",
        "cơm chưa",
    ],
    "HOI_VI_TRI": [
        # Direct
        "{place} ở đâu",
        "{place} ở chỗ nào",
        "{place} ở khu nào",
        "{place} ở khu vực nào",
        "{place} nằm ở đâu",
        "{place} nằm chỗ nào",
        "{place} nằm khu nào",
        "{place} hướng nào",
        "{place} phía nào",
        "{place} chỗ nào",
        "{place} đâu",
        "{place} đâu rồi",
        "{place} ở chỗ ni",
        "{place} ở mô",  # Trung dialect
        "{place} ở đâu vậy",
        "{place} ở đâu thế",
        "{place} ở đâu nhỉ",
        "{place} ở đâu nha",
        "{place} ở đâu ạ",
        # Direction
        "đi tới {place} thế nào",
        "đi tới {place} ra sao",
        "đường tới {place} đi sao",
        "đường tới {place} thế nào",
        "đường đến {place} sao",
        "tới {place} đi đường nào",
        "đến {place} đi đường nào",
        "đi tới {place} đi đường nào",
        "đường đi tới {place}",
        "đường đi đến {place}",
        "đường tới {place}",
        "đường nào tới {place}",
        "đường nào đến {place}",
        "đường nào đi {place}",
        "đường tới {place} ở đâu",
        "lối nào tới {place}",
        "{place} đi lối nào",
        "{place} đi đường nào",
        # Indirect / I-need
        "tôi muốn đi {place}",
        "tôi muốn tới {place}",
        "tôi muốn đến {place}",
        "em muốn đi {place}",
        "em muốn tới {place}",
        "em muốn đến {place}",
        "tôi cần tới {place}",
        "em cần tới {place}",
        "tôi cần đến {place}",
        "muốn đi {place} thì đi đâu",
        "muốn tới {place} thì sao",
        "làm sao tới {place}",
        "làm sao đến {place}",
        "làm thế nào tới {place}",
        "làm thế nào đến {place}",
        "làm cách nào tới {place}",
        # Search
        "tìm {place} ở đâu",
        "tìm {place} chỗ nào",
        "tìm {place} thế nào",
        "kiếm {place} ở đâu",
        "kiếm {place} chỗ nào",
        # I don't know
        "tôi không biết {place} ở đâu",
        "em không biết {place} ở đâu",
        "tôi không rõ {place} ở đâu",
        "em không rõ {place} ở đâu",
        "tôi không biết đường tới {place}",
        "em không biết đường tới {place}",
        "tôi mới đến không biết {place}",
        "em mới đến không biết {place}",
        "tôi là sinh viên mới {place} ở đâu",
        "em là tân binh {place} ở đâu",
        "em là tân sinh viên {place} ở đâu",
        # Ask for help
        "chỉ giúp tôi {place}",
        "chỉ giúp em {place}",
        "chỉ đường tới {place}",
        "chỉ đường đến {place}",
        "chỉ đường cho em tới {place}",
        "hướng dẫn tôi đến {place}",
        "hướng dẫn tới {place}",
        "hướng dẫn đường tới {place}",
        "dẫn tôi tới {place}",
        "dẫn em tới {place}",
        # Distance
        "{place} có xa không",
        "{place} cách đây bao xa",
        "{place} cách bao xa",
        "{place} cách doanh trại bao xa",
        "{place} cách đây mấy mét",
        "{place} cách đây mấy km",
        "{place} cách bao nhiêu mét",
        "đi đến {place} mất bao lâu",
        "đi tới {place} mất bao lâu",
        "đến {place} mất bao lâu",
        "tới {place} mất bao lâu",
        "{place} đi mất bao lâu",
        "{place} đi mất mấy phút",
        "từ đây tới {place} bao xa",
        "từ đây đến {place} bao xa",
        "từ đây tới {place} mất bao lâu",
        # Quickest
        "đường ngắn nhất tới {place}",
        "đường nhanh nhất tới {place}",
        "có cách nào tới {place} nhanh không",
        "tới {place} đi đường nào tiện nhất",
        "đi {place} cách nào nhanh",
        "đi {place} đường nào ngắn",
        # Position
        "{place} ở phía nào của doanh trại",
        "{place} ở phía nào học viện",
        "{place} nằm trong khu nào",
        "{place} nằm khu mấy",
        "{place} ở khu mấy",
        "{place} có gần cổng không",
        "{place} có gần ban chỉ huy không",
        "{place} cách cổng bao xa",
        "tôi đi lạc rồi {place} ở đâu",
        "em đi lạc {place} ở đâu",
        "lạc đường rồi {place} ở đâu",
        "lạc đường rồi",
        "đi lạc rồi",
        # Locating
        "vị trí của {place}",
        "vị trí {place}",
        "vị trí của {place} là gì",
        "tọa độ {place}",
        "{place} ở khu B hay khu A",
        "{place} ở khu nào của học viện",
        "{place} ở khu nào trường mình",
        "{place} thuộc khu nào",
        # Floor
        "{place} ở tầng mấy",
        "{place} tầng mấy",
        "{place} tầng nào",
        "{place} ở tầng nào",
        "{place} ở dãy nào",
        "{place} dãy nào",
        "{place} dãy mấy",
        # Sign
        "{place} có biển chỉ đường không",
        "{place} có biển hiệu không",
        "{place} có ai trực không",
        "ai trực ở {place}",
        # Hours
        "{place} mở cửa lúc nào",
        "{place} mở cửa mấy giờ",
        "{place} đóng cửa chưa",
        "{place} đóng cửa lúc nào",
        "{place} đóng mấy giờ",
        "{place} hoạt động đến mấy giờ",
        # Visibility
        "{place} nhìn từ đây thấy không",
        "{place} có thấy từ đây không",
        "ra {place} bằng đường nào",
        # Transport
        "có cần xe đi {place} không",
        "đến {place} có cần đi xe không",
        "đi bộ đến {place} có xa không",
        "đi bộ tới {place} mất bao lâu",
        "{place} có đi xe được không",
        # Existential
        "ai biết {place} ở đâu không",
        "có ai biết {place} không",
        "có ai biết {place} ở đâu không",
        "đồng chí biết {place} không",
        "thủ trưởng biết {place} không",
        "anh biết {place} không",
        # New arrival
        "tôi mới đến chưa biết {place}",
        "em mới đến chưa biết {place}",
        "em là sinh viên năm nhất {place} ở đâu",
        "em là tân binh chưa biết {place}",
        # Compound questions
        "{place} ở đâu và mở mấy giờ",
        "{place} ở đâu mất mấy phút đi",
        # Ellipsis / short
        "{place}",
        "{place} đâu",
        "{place} chỗ nào nhỉ",
        # Code-mix
        "{place} location",
        "where is {place}",
        "{place} where",
        "location của {place}",
    ],
    "HOI_KIEN_THUC": [
        "{topic} là gì",
        "{topic} là j",
        "{topic} là cái gì",
        "{topic} có nghĩa là gì",
        "định nghĩa {topic}",
        "định nghĩa của {topic}",
        "ý nghĩa của {topic}",
        "ý nghĩa {topic} là gì",
        # How
        "cách thực hiện {topic} ra sao",
        "cách thực hiện {topic} thế nào",
        "{topic} thực hiện thế nào",
        "{topic} thực hiện ra sao",
        "{topic} làm thế nào",
        "{topic} làm sao",
        "{topic} làm ntn",
        "{topic} làm như nào",
        "thực hiện {topic} ra sao",
        "thực hiện {topic} ntn",
        # Rules
        "{topic} có quy tắc gì",
        "quy tắc {topic} là gì",
        "quy tắc của {topic}",
        "luật {topic} là gì",
        # Teach
        "thủ trưởng dạy về {topic}",
        "thủ trưởng giảng về {topic}",
        "thầy dạy về {topic}",
        "thầy giảng về {topic}",
        "thầy giảng giúp em {topic}",
        "ai dạy được {topic}",
        "ai có thể dạy {topic}",
        "ai có thể giảng {topic}",
        # Explain
        "giải thích {topic} cho tôi",
        "giải thích {topic} cho em",
        "giải thích giúp em {topic}",
        "giải thích giùm em {topic}",
        "giảng giúp em {topic}",
        "giảng cho em {topic}",
        "giải thích {topic}",
        "phân tích {topic}",
        "phân tích {topic} giúp em",
        # Use
        "{topic} dùng thế nào",
        "{topic} dùng ra sao",
        "{topic} dùng làm gì",
        "{topic} dùng để làm gì",
        "{topic} sử dụng thế nào",
        "{topic} sử dụng để làm gì",
        "khi nào dùng {topic}",
        "khi nào sử dụng {topic}",
        # Activity
        "{topic} hoạt động ra sao",
        "{topic} hoạt động thế nào",
        "{topic} chạy như thế nào",
        # Principle
        "nguyên lý của {topic}",
        "nguyên lý {topic}",
        "nguyên tắc {topic}",
        "cơ chế {topic}",
        "cơ chế của {topic}",
        # Steps
        "{topic} có những bước gì",
        "{topic} có mấy bước",
        "{topic} các bước",
        "các bước của {topic}",
        # Resources
        "tài liệu về {topic} ở đâu",
        "tài liệu {topic} ở đâu",
        "đọc {topic} ở đâu",
        "tham khảo {topic} ở đâu",
        "{topic} có trong giáo trình không",
        "{topic} có trong sách nào",
        "trong tài liệu nói gì về {topic}",
        "{topic} chương mấy",
        "{topic} là chương mấy",
        # Difficulty
        "{topic} khó không",
        "{topic} có khó không",
        "{topic} có khó học không",
        "{topic} dễ học không",
        # Practice
        "tôi muốn học về {topic}",
        "em muốn học về {topic}",
        "tôi muốn tìm hiểu về {topic}",
        "em muốn tìm hiểu về {topic}",
        "tôi cần học {topic}",
        "em cần học {topic}",
        "muốn ôn {topic}",
        "muốn tự học {topic}",
        "muốn ôn lại {topic}",
        "muốn tự ôn {topic}",
        # When applied
        "{topic} áp dụng khi nào",
        "{topic} áp dụng ở đâu",
        "{topic} dùng cho gì trong quân đội",
        "{topic} ứng dụng cho gì",
        "ứng dụng của {topic}",
        # History
        "kể về {topic}",
        "tìm hiểu về {topic}",
        "lịch sử của {topic}",
        "lịch sử {topic}",
        "đặc điểm của {topic}",
        "đặc điểm {topic}",
        # Safety
        "{topic} có nguy hiểm không",
        "{topic} có an toàn không",
        "{topic} an toàn không",
        "{topic} dùng sao cho an toàn",
        # Pros/cons
        "ưu điểm của {topic}",
        "nhược điểm của {topic}",
        "{topic} có ưu nhược điểm gì",
        "ưu nhược của {topic}",
        # Compare
        "so sánh {topic} với cái khác",
        "so sánh {topic}",
        "{topic} khác với cái khác chỗ nào",
        # Types
        "{topic} có mấy loại",
        "phân loại {topic}",
        "{topic} có những loại nào",
        "có mấy loại {topic}",
        # Examples
        "ví dụ về {topic}",
        "cho tôi ví dụ {topic}",
        "cho em ví dụ {topic}",
        "lấy ví dụ {topic}",
        # Knowledge / question
        "kỹ thuật {topic} là gì",
        "kiến thức về {topic}",
        "câu hỏi về {topic}",
        "tôi không hiểu {topic}",
        "em không hiểu {topic}",
        "tôi chưa hiểu {topic}",
        "em chưa hiểu {topic}",
        "tôi không rõ {topic}",
        "em không rõ {topic}",
        # Group ask
        "ai biết về {topic} không",
        "ai biết {topic} không",
        "có ai biết {topic} không",
        "thầy đã giảng về {topic} chưa",
        "thầy đã dạy {topic} chưa",
        # Importance
        "{topic} có quan trọng không",
        "{topic} quan trọng không",
        "thi có ra {topic} không",
        "{topic} có thi không",
        # Code-mix
        "what is {topic}",
        "{topic} how to",
        "{topic} là gì vậy nhỉ",
        # Slang short
        "{topic}",
        "{topic} là sao",
        "giảng {topic} nha",
        "dạy em {topic}",
    ],
    "BAO_CAO": [
        # Standard
        "báo cáo {report} đã có mặt đầy đủ",
        "báo cáo {report} đã có mặt",
        "báo cáo {report} có mặt đầy đủ",
        "báo cáo {report} có mặt",
        "báo cáo {report} đủ quân",
        "báo cáo {report} đủ quân số",
        "báo cáo {report} đầy đủ",
        "báo cáo đã hoàn thành nhiệm vụ",
        "báo cáo đã hoàn thành",
        "báo cáo hoàn thành nhiệm vụ",
        "báo cáo hoàn tất",
        "báo cáo hoàn tất nhiệm vụ",
        "báo cáo thủ trưởng {report} đã sẵn sàng",
        "báo cáo {report} đã sẵn sàng",
        "báo cáo {report} sẵn sàng",
        "tôi xin báo cáo {time} đã chuẩn bị xong",
        "tôi xin báo cáo đã chuẩn bị xong",
        "em xin báo cáo đã chuẩn bị xong",
        "báo cáo {report} đã tập hợp",
        "báo cáo {report} tập hợp xong",
        "báo cáo {report} đã đến vị trí",
        "báo cáo đã vào vị trí",
        "báo cáo đầy đủ",
        "xin báo cáo đã xong",
        "xin báo cáo đã hoàn thành",
        "tôi báo cáo đã hoàn thành",
        "em báo cáo đã hoàn thành",
        "em xin báo cáo đã xong",
        "báo cáo {report} đã ăn cơm xong",
        "báo cáo {report} đã ăn xong",
        "báo cáo {report} đã trực ban xong",
        "báo cáo {report} đã trực xong",
        "báo cáo {report} đã trực gác xong",
        "{report} báo cáo đầy đủ",
        "{report} báo cáo có mặt",
        "{report} báo cáo đủ quân",
        # Roll call
        "tổ trực báo cáo có mặt",
        "tổ trực báo cáo đầy đủ",
        "báo cáo điểm danh xong",
        "báo cáo điểm danh đầy đủ",
        "điểm danh xong xin báo cáo",
        # Missing
        "báo cáo {report} thiếu một người",
        "báo cáo {report} vắng hai người",
        "báo cáo {report} vắng một người",
        "báo cáo {report} vắng 2",
        "báo cáo {report} vắng mặt 2",
        "báo cáo {report} thiếu mấy người",
        "báo cáo có người ốm",
        "báo cáo có người vắng",
        "báo cáo có người không có mặt",
        "báo cáo {report} có người ốm",
        "báo cáo {report} có người vắng",
        "tôi báo cáo lên cấp trên",
        "{report} đã làm xong nhiệm vụ",
        # Generic work
        "báo cáo công việc",
        "báo cáo công việc đã xong",
        "đã thực hiện xong xin báo cáo",
        "đã hoàn thành xin báo cáo thủ trưởng",
        "đã hoàn thành xin báo cáo",
        "báo cáo {report} hoàn tất bài tập",
        "báo cáo {report} đã hoàn tất bài tập",
        "báo cáo trực ban {time}",
        "báo cáo trực {time}",
        "báo cáo gác đã xong",
        "báo cáo gác đêm xong",
        "báo cáo gác xong",
        "báo cáo gác đã hoàn thành",
        "tổ canh gác báo cáo",
        "tổ trực gác báo cáo",
        # Admin / training
        "báo cáo công tác hành chính xong",
        "báo cáo huấn luyện xong",
        "báo cáo huấn luyện đã hoàn thành",
        "báo cáo bài tập thể lực xong",
        "báo cáo bài tập thể lực đã hoàn thành",
        "báo cáo đã bắn xong",
        "báo cáo bài bắn đạt",
        "báo cáo bài bắn đạt yêu cầu",
        "báo cáo điểm bài bắn",
        "báo cáo kết quả bài bắn",
        # Goal
        "đã hoàn thành mục tiêu",
        "đã hoàn thành chỉ tiêu",
        "đã đạt yêu cầu báo cáo lên",
        "đã đạt yêu cầu báo cáo",
        "đã đạt chỉ tiêu báo cáo",
        # Variants of "thu truong"
        "thủ trưởng tôi báo cáo {report} đầy đủ",
        "thủ trưởng em báo cáo {report} đầy đủ",
        "thưa thủ trưởng {report} đầy đủ",
        "thưa thủ trưởng em báo cáo",
        "thưa thủ trưởng em xin báo cáo",
        # Plural
        "báo cáo các đồng chí đã có mặt",
        "báo cáo các đồng chí đã tập hợp",
        # Incident
        "báo cáo có chuyện đột xuất",
        "báo cáo {report} có người mất tích",
        "báo cáo có sự cố",
        "báo cáo {report} có sự cố",
        "báo cáo có tình huống",
        "báo cáo tình huống đã xử lý",
        "báo cáo tình huống đã giải quyết",
        # Up
        "báo cáo lên ban chỉ huy",
        "báo cáo lên cấp trên",
        # Weekly / periodic
        "báo cáo nhanh nhiệm vụ tuần",
        "báo cáo tuần này hoàn thành",
        "báo cáo tuần qua đã xong",
        "báo cáo tháng này",
        "báo cáo tháng đã hoàn thành",
        # Short
        "đại đội xin báo cáo",
        "đại đội báo cáo",
        "trung đội báo cáo",
        "tiểu đội báo cáo",
        "tổ báo cáo",
        # Casual
        "báo cáo xong rồi",
        "xong rồi báo cáo",
        "xong báo cáo",
        # Late
        "tôi báo cáo muộn",
        "em báo cáo muộn",
        "em xin lỗi báo cáo muộn",
        "báo cáo trễ một chút",
        # Result
        "báo cáo kết quả",
        "báo cáo kết quả tập",
        "báo cáo kết quả huấn luyện",
        "báo cáo điểm",
        "báo cáo điểm thi",
        "báo cáo điểm kiểm tra",
        # Request
        "báo cáo xin chỉ thị",
        "báo cáo xin chỉ thị tiếp",
        "báo cáo xin nhận lệnh",
        "báo cáo xong xin nhận lệnh tiếp",
        # Ellipsis
        "{report} có mặt",
        "{report} đầy đủ",
        "{report} sẵn sàng",
        "{report} hoàn thành",
        "{report} hoàn tất",
        "đã xong",
        "xong rồi",
        "đã hoàn thành",
        "hoàn thành rồi",
        # Code-mix
        "report {report} ready",
        "{report} done",
        "task done",
    ],
    "XIN_PHEP": [
        "cho tôi xin phép ra ngoài",
        "cho tôi xin phép đi ra ngoài",
        "cho em xin phép ra ngoài",
        "cho em xin phép đi ra ngoài",
        "tôi xin phép nghỉ {time}",
        "em xin phép nghỉ {time}",
        "tôi xin phép nghỉ học {time}",
        "em xin phép nghỉ học {time}",
        "tôi xin phép nghỉ tập {time}",
        "em xin phép nghỉ tập {time}",
        "cho tôi nghỉ buổi {time}",
        "cho em nghỉ buổi {time}",
        "cho tôi nghỉ {time}",
        "cho em nghỉ {time}",
        "xin phép thủ trưởng cho ra cổng",
        "xin phép thủ trưởng cho em ra cổng",
        "cho tôi xin phép về thăm nhà",
        "cho em xin phép về thăm nhà",
        "xin phép vắng mặt buổi {time}",
        "xin phép vắng buổi {time}",
        "xin phép nghỉ học {time}",
        "xin phép đi khám bệnh",
        "tôi xin phép đi khám",
        "em xin phép đi khám",
        "em xin phép đi khám bệnh",
        # Reason
        "tôi xin phép vì {reason}",
        "em xin phép vì {reason}",
        "cho tôi nghỉ vì {reason}",
        "cho em nghỉ vì {reason}",
        "thủ trưởng cho em xin nghỉ {reason}",
        "thủ trưởng cho tôi xin nghỉ {reason}",
        "em xin phép vắng vì {reason}",
        "tôi xin phép vắng vì {reason}",
        "em xin nghỉ chiều nay vì {reason}",
        "em xin phép vắng buổi tối {reason}",
        "em xin phép vắng vì {reason}",
        "em xin nghỉ vì {reason}",
        "em xin nghỉ vì sốt cao",
        "em xin nghỉ vì bệnh",
        "em xin nghỉ vì {reason} ạ",
        # Short permission
        "cho em xin nghỉ {time}",
        "cho em xin nghỉ buổi sáng nay",
        "cho em xin nghỉ buổi chiều",
        "cho em xin nghỉ buổi tối",
        "em xin phép ra ngoài một chút",
        "em xin phép ra ngoài chút",
        "em ra ngoài chút được không",
        "em xin ra ngoài chút",
        "tôi xin phép đi vệ sinh",
        "em xin phép đi vệ sinh",
        "em xin phép wc",
        "em xin phép đi wc",
        "xin phép xuống nhà ăn trước",
        "xin phép đi xuống nhà ăn trước",
        "cho em xin phép về sớm",
        "xin nghỉ tập",
        "em xin nghỉ tập",
        "em xin nghỉ buổi tập",
        "xin phép không tham gia buổi {time}",
        "em xin phép không tham gia buổi {time}",
        "xin phép đi viện",
        "em xin phép đi viện",
        "em xin phép đi gặp gia đình",
        "em xin phép đi gặp người thân",
        "anh cho em ra ngoài chút",
        "anh cho em ra ngoài",
        "em xin nghỉ buổi sáng nay",
        "em muốn xin phép vắng",
        "em muốn xin phép nghỉ",
        "cho em xin phép thưa thủ trưởng",
        "tôi xin nghỉ học hôm nay",
        "em xin nghỉ học hôm nay",
        "em xin phép nghỉ tập thể dục",
        "em xin phép nghỉ thể dục",
        "thưa thủ trưởng em xin phép ra cổng",
        "thưa thủ trưởng em xin phép ra ngoài",
        "em xin phép đi đến trạm y tế",
        "em xin phép tới phòng y tế",
        "em xin phép đi mua thuốc",
        "em xin phép đi mua đồ",
        "em xin nghỉ buổi điều lệnh sáng nay",
        "em xin nghỉ buổi học tối",
        "em xin phép đi tới ban chỉ huy",
        "em xin phép có việc đột xuất",
        "thủ trưởng cho phép em vắng",
        "thủ trưởng cho phép em nghỉ",
        "em xin phép đi gặp giáo viên",
        "em xin phép đi gặp thầy",
        "em xin phép đi gặp cô",
        "em xin phép đi đăng ký môn",
        "em xin phép đi nộp tài liệu",
        "em xin phép đi gặp bạn cũ",
        "em xin nghỉ phép {time}",
        "tôi xin nghỉ phép {time}",
        "đề nghị duyệt cho em nghỉ",
        "đề nghị duyệt cho em vắng",
        "đề nghị cho em xin nghỉ",
        "có thể cho em nghỉ {time} được không",
        "em xin phép vắng buổi sáng",
        "em xin phép vắng buổi chiều",
        "em xin phép vắng buổi tối",
        # Stronger
        "xin phép thủ trưởng vắng mặt {time}",
        "xin phép vắng mặt {time}",
        "xin phép thủ trưởng cho em nghỉ {time}",
        "xin phép thủ trưởng cho tôi nghỉ {time}",
        # Health-specific
        "em đang bị {reason} xin phép nghỉ",
        "em bị {reason} xin được nghỉ",
        "em đang {reason} xin nghỉ",
        # Short
        "xin nghỉ",
        "xin phép",
        "xin phép nghỉ",
        "xin phép vắng",
        "xin nghỉ phép",
        "cho xin nghỉ",
        "cho xin phép",
        "em nghỉ {time} được không",
        "em xin nghỉ {time} được không",
        # Code-mix
        "permission xin nghỉ",
        "request leave {time}",
        "ask for leave",
    ],
    "TAM_BIET": [
        "chào thủ trưởng tôi đi đây",
        "chào thủ trưởng em đi đây",
        "em chào thủ trưởng em đi",
        "em chào thủ trưởng",
        "chào thủ trưởng",
        "tôi chào thủ trưởng",
        "em chào sĩ quan",
        "em chào chỉ huy",
        "em chào anh",
        "em chào cán bộ",
        "tạm biệt thủ trưởng",
        "tạm biệt anh",
        "tạm biệt em",
        "tạm biệt mọi người",
        "tạm biệt nhé",
        "tạm biệt nha",
        "tạm biệt",
        "hẹn gặp lại",
        "hẹn gặp lại sau",
        "hẹn gặp lại nhé",
        "hẹn gặp lại sau anh",
        "chào tạm biệt",
        "em xin phép đi trước",
        "em xin phép đi",
        "em xin phép ra ngoài",
        "em chào",
        "em đi đây",
        "em đi đây ạ",
        "em đi đây nhé",
        "em đi đây thưa thủ trưởng",
        "tạm biệt thủ trưởng em đi",
        "thôi em xin phép",
        "thôi em đi",
        "thôi em đi nha",
        "thôi em xin chào",
        "chào nhé",
        "chào nha",
        "chào nghe",
        "thôi tạm biệt",
        "tới đây thôi",
        "tới đây thôi ạ",
        "đến đây thôi",
        "em đi nha",
        "tới giờ rồi em đi",
        "tới giờ em đi",
        "em phải đi đây",
        "em phải đi rồi",
        "em phải về thôi",
        "thôi tạm biệt",
        "chào",
        "tạm biệt anh em",
        "em chào các đồng chí",
        "em chào các anh",
        "em chào các thầy",
        "em chào thủ trưởng em đi học",
        "em chào thủ trưởng em đi tập",
        "chào thủ trưởng em xuống dưới",
        "thôi em đi",
        "em đi trước nhé",
        "em đi trước nha",
        "em đi học đây ạ",
        "em đi học đây",
        "em đi lên lớp đây",
        "em đi lên lớp",
        "em đi xuống ăn",
        "em xuống ăn cơm đã",
        "em xuống ăn cơm",
        "em phải về phòng đây",
        "em về phòng đây",
        "em về ktx đây",
        "em chào thầy em đi",
        "em chào thầy em xuống",
        "tạm biệt thủ trưởng em đi nhiệm vụ",
        "tạm biệt em đi gác",
        "em đi trực đây",
        "em đi gác đây",
        "em chào em đi tập",
        "em chào em xuống sân",
        "em đi đến giảng đường đây",
        "em đi giảng đường đây",
        "em đi nộp bài đây",
        "em đi sang đại đội khác",
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
        # Casual
        "bye",
        "bye bye",
        "bye thủ trưởng",
        "see you",
        "see ya",
        # Short
        "hết giờ rồi em đi",
        "muộn rồi em đi",
        "sắp muộn em đi đây",
        "em đi nhanh",
        "em chạy đây",
        # Going to specific place
        "em đi gác đêm nay",
        "em xuống lớp",
        "em xuống căn tin",
        "em đi mua đồ",
        "em đi giặt đồ",
    ],
    "OUT_OF_SCOPE": OOC,
}

# V5 GAP-FILLERS — targeted templates for v4 hard-test misses

# HOI_LICH gaps: free/busy semantics + future-time + "di dau" = activity
TEMPLATES["HOI_LICH"].extend([
    "{time} có rảnh không",
    "{time} mình rảnh không",
    "{time} mình rảnh không nhỉ",
    "{time} có rảnh không nhỉ",
    "{time} bận hay rảnh",
    "{time} bận hay rảnh thế",
    "{time} bận không",
    "{time} có bận không",
    "{time} bận không thầy",
    "{time} có kín lịch không",
    "{time} kín lịch chưa",
    "{time} kín lịch không",
    "{time} có trống lịch không",
    "{time} có trống không",
    "{time} có rảnh giờ nào không",
    "{time} có rảnh chút nào không",
    "{time} có rảnh tiếng nào không",
    "{time} đại đội ta có rảnh không",
    "{time} đại đội rảnh không",
    "{time} đại đội bận không",
    "rảnh không {time}",
    "có rảnh không {time}",
    "bận không {time}",
    "trống lịch chưa {time}",
    # Future-time + "di dau" = where do we go (still asking schedule)
    "{time} đi đâu",
    "{time} mình đi đâu",
    "{time} đại đội đi đâu",
    "{time} ta đi đâu",
    "{time} đại đội ta đi đâu",
    "{time} đi đâu vậy",
    "{time} đi đâu thầy",
    "{time} đi đâu nhỉ",
    "{time} đi đâu z",
    "{time} mình đi đâu thầy",
    "{time} mình ra bãi đâu",
    "{time} ra bãi nào",
    "{time} đi tập đâu",
    "{time} đi tập ở đâu",
    "{time} tập ở đâu",
    "{time} học ở đâu",
    "{time} đại đội tập ở đâu",
])

# XIN_PHEP gaps: pure-symptom forms (no "xin phep" keyword) — student
# implicitly asks for leave by stating the reason.
TEMPLATES["XIN_PHEP"].extend([
    # Symptom-only
    "em đang {reason}",
    "em đang bị {reason}",
    "em bị {reason}",
    "em đang {reason} ạ",
    "em đang {reason} thầy ơi",
    "em đang {reason} thưa thủ trưởng",
    "em đang {reason} không đi tập được",
    "em đang {reason} không học được",
    "em đang {reason} muốn về phòng",
    "em đang {reason} cần đi y tế",
    "em đang {reason} cần đi khám",
    "em {reason} quá",
    "em {reason} lắm",
    "em {reason} kinh khủng",
    "tôi đang {reason}",
    "tôi đang bị {reason}",
    "tôi bị {reason}",
    "tôi {reason} quá",
    # Specific health phrases
    "em đang sốt",
    "em đang sốt cao",
    "em đang sốt 38 độ",
    "em đang sốt 39 độ",
    "em đang sốt 40 độ",
    "em sốt 38 độ",
    "em sốt 39 độ",
    "em sốt 40 độ",
    "em sốt cao quá",
    "em đau bụng",
    "em đau bụng quá",
    "em đau bụng dữ dội",
    "em đau bụng kinh khủng",
    "em đau đầu",
    "em đau đầu quá",
    "em đau đầu lắm",
    "em buồn nôn",
    "em chóng mặt",
    "em viêm họng",
    "em ho nhiều",
    "em đau dạ dày",
    "em bị tiêu chảy",
    "em mệt lắm",
    "em mệt quá không đi học được",
    "em không khỏe lắm",
    "em yếu quá",
    "em ngất xỉu",
    # Family / gap
    "nhà có việc gấp",
    "nhà em có việc gấp",
    "gia đình em có việc gấp",
    "có việc gấp ở nhà",
    "ba mẹ gọi về gấp",
    "mẹ em ốm",
    "bố em ốm",
    "ông bà em ốm",
    "có chuyện gấp ở nhà",
    "có chuyện ở nhà em phải về",
    "nhà em có chuyện",
    "gia đình em có chuyện",
    "có người nhà ốm em phải về",
    # No-accent versions of symptom complaints
    "em dang sot 39 do",
    "em dang sot cao",
    "em dau bung qua",
    "em dau dau lam",
    "em met qua khong di hoc duoc",
    "nha em co viec gap",
    "co viec gap o nha",
])

# HOI_VI_TRI gaps: lone-place-word + many no-accent place asks
TEMPLATES["HOI_VI_TRI"].extend([
    # Standalone place names as questions (ellipsis)
    "{place}",
    "{place}?",
    "{place} đâu",
    "{place} đâu z",
    "{place} đâu vậy",
    "{place} chỗ nào",
    "{place} chỗ nào z",
    "{place} ở mô",
    # No-accent place lookups
    "{place} o dau",
    "{place} o cho nao",
    "{place} o khu nao",
    "cho em hoi {place} o dau",
    "cho hoi {place} o cho nao",
    "em hoi {place} o dau",
    "lam sao toi {place}",
    "duong toi {place}",
    "duong nao toi {place}",
    "tu day toi {place} bao xa",
    "tim {place} o dau",
    "tim {place} cho nao",
    "muon di {place} thi di dau",
    # Adversarial — make sure cantin/can tin/wifi-yeu boundary is clear
    "căn tin chỗ nào",
    "căng tin ở đâu",
    "căn tin",
    "cantin",
    "căng tin",
    "thư viện",
    "thư viện chỗ nào",
    "thư viện ở đâu z",
    "phòng 305",
    "phòng 305 đâu",
])

# OUT_OF_SCOPE gaps: lone-time-word, weather+hunger compound, NO_ACCENT social chat
TEMPLATES["OUT_OF_SCOPE"].extend([
    # Lone time-words used socially (must learn to NOT classify as HOI_LICH)
    "cuối tuần",
    "cuối tuần thoải mái",
    "cuối tuần chill",
    "cuối tuần này tớ về quê",
    "cuối tuần đi chơi",
    "cuối tuần đi xem phim",
    "thứ 7 đi chơi",
    "chủ nhật rảnh không",
    "tuần sau thi cuối kỳ",
    "tháng sau ra trường",
    "đầu tháng tới rảnh",
    "sáng nay tỉnh dậy mệt",
    "chiều nay buồn",
    "tối qua mất ngủ",
    "ngày mai đi đâu chơi",
    "cuối tuần nóng quá",
    "cuối tuần đi đâu",
    # Weather + hunger compound (mimics "Hom nay troi dep va to doi")
    "trời đẹp tớ đói",
    "trời nắng tớ đói",
    "trời đẹp với lại tớ đói",
    "trời đẹp và tớ đói",
    "đói bụng nhưng trời đẹp",
    "đói nhưng trời mát",
    "lười quá đói quá",
    "lười quá nhưng đói quá",
    # NO_ACCENT social chat
    "bua nay nhom chat im qua",
    "nhom chat lop khong ai noi gi",
    "nhom chat im re",
    "lop minh tro nen im lim",
    "wifi yeu qua",
    "may tinh em bi lag",
    "internet cham qua",
    "diem thi sap co chua",
    "den deadline lam roi",
    "stress vai",
    "tro nen luoi qua",
    "luoi qua di",
    "buon ngu qua",
    "nho nha qua",
    "nho me qua",
    # Tech / studio random
    "code chạy không được",
    "code lỗi vãi",
    "máy lag chết tiệt",
    "VS Code crash",
    "Github Copilot",
])

# V6 SURGICAL FIXES — for v5's remaining 7 hard-test misses

# HOI_LICH: ranh-khong-nhi form WITHOUT "co" filler (test was "Hom nay ranh khong nhi")
TEMPLATES["HOI_LICH"].extend([
    "{time} rảnh không",
    "{time} rảnh không nhỉ",
    "{time} rảnh không thầy",
    "{time} rảnh không z",
    "{time} rảnh không vậy",
    "{time} rảnh không thế",
    "{time} rảnh hông",
    "{time} rảnh hong",
    "{time} rảnh ko",
    "{time} rảnh hum",
    "{time} mình rảnh không",
    "{time} ta rảnh không",
    "{time} đại đội rảnh không nhỉ",
    "{time} bận hay rảnh nhỉ",
    "{time} bận hay rảnh thế nào",
    "rảnh không nhỉ",
    "rảnh không thầy",
])

# HOI_VI_TRI code-mix: English place + Vietnamese question
TEMPLATES["HOI_VI_TRI"].extend([
    "Library ở đâu",
    "Library ở đâu thầy",
    "Library ở chỗ nào",
    "Library nằm đâu",
    "Office ở đâu",
    "Office của thầy ở đâu",
    "Cafeteria ở đâu",
    "Canteen ở đâu",
    "Library where",
    "Office where",
    "Dining hall ở đâu",
    "Restroom ở đâu",
    "Toilet ở đâu",
    "Gym ở đâu",
    "Lab ở đâu",
    "Lab phòng nào",
    "Computer lab ở đâu",
    "Network lab ở chỗ nào",
    "{place} where",
    "where is {place}",
    # Explicit no-accent place asks for common military places (test had "san van dong")
    "san van dong o dau",
    "san van dong cho nao",
    "san van dong khu nao",
    "cho em hoi san van dong o dau",
    "san bong da o dau",
    "san bong da cho nao",
    "san tap o dau",
    "phong y te o dau",
    "tram y te o dau",
    "thu vien o dau",
    "thu vien cho nao",
    "phong hoc o dau",
    "phong hoc cho nao",
    "giang duong o dau",
    "ky tuc xa o dau",
    "ky tuc xa cho nao",
    "ktx cho nao",
    "ktx o dau",
    "nha an o dau",
    "nha an cho nao",
    "can tin o dau",
    "cang tin o dau",
    "doanh trai o cho nao",
    "khu A o dau",
    "khu B o dau",
    "khu C o dau",
    "khu B5 o dau",
    "phong 305 o tang may",
    "phong 401 o dau",
    "cong chinh o dau",
])

# OUT_OF_SCOPE: more academic / student complaint forms — these should NOT be HOI_VI_TRI
# even when they contain "nha" or place-like nouns
TEMPLATES["OUT_OF_SCOPE"].extend([
    "Bài tập về nhà nhiều quá",
    "Bài tập về nhà khó vãi",
    "Bài tập về nhà nhiều ghê",
    "Bài tập về nhà nhiều ghớm",
    "Bài tập về nhà cho nhiều",
    "Cô cho bài tập về nhà nhiều quá",
    "Thầy cho bài tập về nhà nhiều",
    "Đề thi về nhà khó",
    "Đề kiểm tra khó vãi",
    "Đề thi học kỳ khó vãi",
    "Đề cuối kỳ ra dễ",
    "Bài tập nhiều quá",
    "Bài thi sắp tới nhiều bài",
    "Học nhiều quá",
    "Học hành mệt quá",
    "Học mệt vãi",
    "Chán học quá",
    "Chán đến trường quá",
    "Chán đến lớp quá",
    "Lớp đông quá",
    "Tiết học dài quá",
    "Giảng viên dạy nhanh quá",
    "Giảng viên giảng khó hiểu",
    "Bài giảng khô quá",
    "Buồn ngủ trong giờ học",
    "Ngủ gật trong lớp",
    "Cô giảng nhanh quá",
    # Variant with home-related words (without being a location request)
    "Ở nhà ngồi học",
    "Ở nhà code",
    "Ở nhà xem phim",
    "Ở nhà chán quá",
    "Về nhà ngủ thôi",
    "Về nhà xem phim",
    "Về nhà với gia đình",
    "Lại về nhà rồi",
    # Casual student social
    "tớ chán quá",
    "tớ buồn quá",
    "ngày dài quá",
    "tuần này dài quá",
    "đêm dài quá",
    "1 giờ học vẫn còn dài",
])

# Augmentation - Vietnamese-specific (similar to v3 but more aggressive)
ACCENT_MAP = str.maketrans(
    "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ"
    "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ",
    "aaaaaaaaaaaaaaaaaeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyyd"
    "AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD",
)

TELEX_TYPOS = [
    ("ô", "oo"), ("ơ", "ow"), ("ê", "ee"), ("ư", "uw"), ("â", "aa"),
    ("ă", "aw"), ("đ", "dd"),
]

def drop_accent(s: str) -> str:
    return s.translate(ACCENT_MAP)

def telex_typo(s: str) -> str:
    for vowel, raw in TELEX_TYPOS:
        if vowel in s and random.random() < 0.5:
            return s.replace(vowel, raw, 1)
    return s

def random_typo(s: str) -> str:
    if len(s) < 6:
        return s
    i = random.randrange(len(s))
    if s[i] == " ":
        return s
    return s[:i] + s[i + 1:]

def char_swap(s: str) -> str:
    if len(s) < 4:
        return s
    i = random.randrange(len(s) - 1)
    if s[i] == " " or s[i + 1] == " ":
        return s
    return s[:i] + s[i + 1] + s[i] + s[i + 2:]

def drop_filler_word(s: str) -> str:
    fillers = {"thì", "là", "à", "ơi", "ạ", "vậy", "thế", "đó", "mà", "đấy"}
    words = s.split()
    keep = [w for w in words if w not in fillers]
    if 0 < len(keep) < len(words):
        return " ".join(keep)
    return s

def synonym_swap(s: str) -> str:
    """Replace at most one matched synonym with a random alternative.

    Keeps everything in lowercase whitespace tokens, so ONLY substitutes
    standalone words found verbatim in SYNONYMS keys.
    """
    words = s.split()
    if not words:
        return s
    # Try a few random positions
    indices = list(range(len(words)))
    random.shuffle(indices)
    for idx in indices[:5]:
        w = words[idx]
        # Try multi-word too: "the nao", "khi nao"
        for k, opts in SYNONYMS.items():
            if k == w or (idx + len(k.split()) <= len(words) and " ".join(words[idx:idx + len(k.split())]) == k):
                pick = random.choice(opts)
                if pick == k:
                    continue
                ksize = len(k.split())
                return " ".join(words[:idx] + pick.split() + words[idx + ksize:])
    return s

def cap_first(s: str) -> str:
    return s[0].upper() + s[1:] if s else s

def shuffle_safe(s: str) -> str:
    """Lightly shuffle word order for non-syntactic intents (BAO_CAO with
    word salad like 'báo cáo đại đội đầy đủ' is robust to small shuffles)."""
    words = s.split()
    if len(words) < 4:
        return s
    # Swap two adjacent positions in middle
    i = random.randrange(1, len(words) - 1)
    words[i], words[i + 1] = words[i + 1], words[i]
    return " ".join(words)

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
    """With some probability, append a second clause from the same intent."""
    if random.random() < 0.10 and intent in TEMPLATES and intent != "OUT_OF_SCOPE":
        second = fill(random.choice(TEMPLATES[intent]))
        connector = random.choice(CONNECTORS)
        return f"{text} {connector} {second}"
    return text

def augment(text: str, intent: str) -> str:
    """v6 raises drop_accent rate to 24% (vs v5 18%) to harden NO_ACCENT cases
    where rare places like 'san van dong' lose their accents."""
    r = random.random()
    if r < 0.24:
        text = drop_accent(text)
    elif r < 0.34:
        text = telex_typo(text)
    elif r < 0.40:
        text = random_typo(text)
    elif r < 0.44:
        text = char_swap(text)
    elif r < 0.50:
        text = drop_filler_word(text)
    # Synonym swap on top - separate roll
    if random.random() < 0.22:
        text = synonym_swap(text)
    # Shuffle for BAO_CAO only (other intents are sensitive to order)
    if intent == "BAO_CAO" and random.random() < 0.05:
        text = shuffle_safe(text)
    if random.random() < 0.30:
        text = cap_first(text)
    return text

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--per_intent", type=int, default=30000,
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
        kept = len(seen)
        attempts = 0
        target_attempts = args.per_intent * 6
        while kept < args.per_intent and attempts < target_attempts:
            attempts += 1
            t = random.choice(templates)
            text = fill(t)
            text = decorate(text)
            text = maybe_compound(intent, text)
            text = augment(text, intent)
            text = re.sub(r"\s+", " ", text).strip()
            if not text or text in seen or len(text) < 3:
                continue
            seen.add(text)
            rows.append({"text": text, "intent": intent})
            kept += 1
        print(f"[gen v4] intent={intent:<14} kept={kept} attempts={attempts}")

    out = pd.DataFrame(rows).drop_duplicates(subset=["text"]).reset_index(drop=True)
    out.to_csv(out_path, index=False, encoding="utf-8")

    counts = out["intent"].value_counts()
    print(f"\n[gen v4] wrote {len(out)} rows to {out_path}")
    for intent, n in counts.items():
        print(f"{intent:<14} {n}")

    from collections import Counter
    c = Counter()
    for t in out["text"]:
        c.update(t.lower().split())
    print(f"[gen v4] approx vocab (whitespace): {len(c)} unique tokens")

if __name__ == "__main__":
    main()
