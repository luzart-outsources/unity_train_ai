"""
Phase C ITER 1 — Generate v8 dataset: 5-10x bigger with:
  - More paraphrase patterns per intent (especially time-of-day)
  - Heavy augmentation (10x telex variants, slang chains, code-mix depth)
  - Meal vs sleep disambiguation pairs
  - Per-meal specific time samples (so model distinguishes slot_3/5/8)
  - Negative pair mining for explicit contrast

Target: 50-100K samples.
Output: data/qa_pairs_v8.jsonl
"""
import json
import random
import re
import unicodedata
from pathlib import Path
from collections import Counter

random.seed(2026)

SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
GAME_PATH  = DATA_DIR / "game_entities.json"
OUT_JSONL  = DATA_DIR / "qa_pairs_v8.jsonl"
OUT_SUMMARY = DATA_DIR / "qa_summary_v8.json"


# =====================================================================
# Vietnamese helpers
# =====================================================================
def no_accent(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    out = "".join(c for c in nfkd if not unicodedata.combining(c))
    return out.replace("Đ", "D").replace("đ", "d")


TELEX_VOWELS = ["a", "e", "i", "o", "u", "ô", "ơ", "ê", "ă", "â"]

def telex_typo(text: str, rate: float = 0.15) -> str:
    out = []
    for ch in text:
        out.append(ch)
        if ch.lower() in TELEX_VOWELS and random.random() < rate:
            out.append(ch)
    return "".join(out)


def heavy_telex(text: str) -> str:
    return telex_typo(text, rate=0.30)


SLANG_MAP = {
    "không":  "ko", "được": "đc", "vậy": "v", "biết": "bít",
    "đồng chí": "đ/c", "tại sao": "tsao", "thế nào": "tn",
    "rồi": "r", "lắm": "lém", "luôn": "luông",
    "này": "n", "đâu": "đou", "ạ": "", "nha": "nhe",
    "thôi": "thoi", "thế": "thía", "anh": "ah",
}

def slang_swap(text: str) -> str:
    for k, v in SLANG_MAP.items():
        if random.random() < 0.5:
            text = text.replace(k, v)
    return text


CODE_MIX_PREFIXES = ["", "Bro ", "Anh ơi ", "Hey ", "Yo ", "Cho hỏi ", "Cho em hỏi ", "Sorry "]

CODE_MIX_REPL = [
    ("ở đâu",        "where is"),
    ("là gì",        "what is"),
    ("mấy giờ",      "what time"),
    ("hôm nay",      "today"),
    ("ngày mai",     "tomorrow"),
    ("học",          "study"),
    ("ăn",           "eat"),
    ("ngủ",          "sleep"),
    ("nhà ăn",       "canteen"),
    ("sân vận động", "gym"),
    ("lớp học",      "classroom"),
    ("ký túc xá",    "dormitory"),
    ("ai",           "who"),
    ("khi nào",      "when"),
]

def code_mix(text: str) -> str:
    prefix = random.choice(CODE_MIX_PREFIXES)
    text = prefix + text
    n_repl = random.randint(0, 2)
    for _ in range(n_repl):
        vi, en = random.choice(CODE_MIX_REPL)
        if vi in text:
            text = text.replace(vi, en, 1)
    return text


def ellipsis(text: str) -> str:
    text = re.sub(
        r"^(cho (em |mình )?hỏi |em hỏi |anh ơi |báo cáo[^,]*,?\s*|đồng chí ơi |thủ trưởng ơi )",
        "", text, flags=re.IGNORECASE)
    return text


FILLERS = ["ạ", "ạ?", "nhỉ", "nhỉ?", "vậy", "thế", "đó", "nào", "à",
           "với", "nha", "luôn", "tí", "tý", "đi"]

def add_filler(text: str) -> str:
    if not text.endswith("?") and random.random() < 0.7:
        return text + " " + random.choice(FILLERS)
    return text


def add_double_filler(text: str) -> str:
    return add_filler(add_filler(text))


# =====================================================================
# Massively expanded Q templates
# =====================================================================

ASK_LOCATION_Q = [
    "{X} ở đâu", "{X} ở đâu vậy", "{X} ở chỗ nào", "{X} chỗ nào",
    "{X} nằm ở đâu", "{X} nằm chỗ nào", "{X} ở khu nào", "{X} hướng nào",
    "Cho hỏi {X} ở đâu", "Cho em hỏi {X} ở đâu ạ",
    "Cho hỏi {X} chỗ nào", "Cho em hỏi {X} chỗ nào ạ",
    "Em hỏi {X} ở đâu", "Mình hỏi {X} ở đâu",
    "{X} đi lối nào", "Đi đâu thì tới {X}", "Làm sao đi tới {X}",
    "Đường nào đi tới {X}", "Tới {X} đi lối nào",
    "Tới {X} đi hướng nào", "Đường đi {X} thế nào",
    "Em muốn đi {X} đi lối nào", "Em muốn đến {X} thì đi đâu",
    "Đồng chí cho em hỏi {X} ở đâu",
    "Báo cáo, {X} ở đâu ạ", "Báo cáo thủ trưởng, {X} ở đâu",
    "Anh ơi {X} ở đâu", "Thủ trưởng ơi {X} chỗ nào",
    "Anh chỉ giúp em {X} với", "Chỉ giúp em {X} ở đâu",
    "Em chưa biết {X} ở đâu", "Em mới đến, {X} ở đâu vậy",
    "{X} đâu rồi", "{X} đâu nhỉ", "{X} ở chỗ nào nhỉ",
    "Mấy hôm trước em quên, {X} ở đâu", "Hỏi tý, {X} ở đâu",
    "{X} thì sao tới", "{X} bên nào", "{X} phía nào",
    "Khu nào có {X}", "Tìm {X} ở đâu", "Đi {X} kiểu gì",
    "Đường tới {X} ra sao", "{X} cách đây bao xa",
    "Mất bao lâu đi tới {X}", "Em đang lạc đường, {X} ở đâu",
    "Cho hỏi tới {X} ra sao", "Muốn tới {X} đi đường nào",
    "Em đi từ đây tới {X} mất bao xa", "{X} nó ở đâu",
    "Báo cáo đồng chí, {X} ở chỗ nào", "Trình bày {X} ở đâu",
]

# Per-meal Q templates — for disambiguation
ASK_MEAL_TIME_Q = {
    # slot 3 (ăn sáng) specific
    "an_sang": [
        "Mấy giờ ăn sáng", "Ăn sáng mấy giờ", "Bao giờ ăn sáng",
        "Khi nào ăn sáng", "Ăn sáng bắt đầu lúc mấy giờ",
        "Bữa sáng mấy giờ", "Sáng ăn lúc nào", "Cho hỏi ăn sáng mấy giờ",
        "Lúc nào ăn sáng", "Ăn sáng tới mấy giờ",
        "Mấy giờ thì có ăn sáng", "Em hỏi mấy giờ ăn sáng",
        "Báo cáo, mấy giờ ăn sáng ạ", "Ăn sáng đến mấy giờ",
        "Bữa sáng kéo dài bao lâu", "Hôm nay mấy giờ ăn sáng",
        "Sáng nay ăn lúc mấy giờ", "Anh ơi mấy giờ ăn sáng",
        "Bữa sáng từ mấy giờ", "Ăn sáng lúc mấy giờ vậy",
    ],
    # slot 5 (nghỉ trưa, ăn trưa, ăn cơm trưa)
    "an_trua": [
        "Mấy giờ ăn trưa", "Ăn trưa mấy giờ", "Bao giờ ăn trưa",
        "Khi nào ăn trưa", "Ăn cơm trưa mấy giờ", "Bữa trưa mấy giờ",
        "Trưa ăn lúc nào", "Mấy giờ ăn cơm trưa", "Em hỏi giờ ăn trưa",
        "Báo cáo, mấy giờ ăn trưa ạ", "Cho hỏi ăn trưa mấy giờ",
        "Nghỉ trưa mấy giờ", "Bao giờ nghỉ trưa",
        "Mấy giờ thì ăn trưa", "Anh ơi mấy giờ ăn trưa",
        "Bữa trưa kéo dài tới mấy giờ", "Trưa nay mấy giờ ăn",
        "Hôm nay mấy giờ ăn trưa", "Em mới đến, mấy giờ ăn trưa",
    ],
    # slot 8 is "đi ngủ" — these are NOT meal questions
    "di_ngu": [
        "Mấy giờ đi ngủ", "Đi ngủ mấy giờ", "Bao giờ đi ngủ",
        "Khi nào đi ngủ", "Mấy giờ thì được đi ngủ",
        "Ngủ lúc mấy giờ", "Bao giờ được ngủ", "Mấy giờ ngủ",
        "Cho hỏi mấy giờ ngủ", "Em hỏi mấy giờ đi ngủ",
        "Báo cáo, mấy giờ đi ngủ ạ", "Hôm nay mấy giờ ngủ",
        "Tối nay mấy giờ ngủ", "Mấy giờ lên giường",
        "Mấy giờ về phòng ngủ", "Báo cáo đi ngủ mấy giờ",
    ],
    # Ăn tối isn't actually a slot — slot 7 is "tự do" between 17-18:30 but might include eating
    # Treat "ăn tối" as ambiguous — put under OUT_OF_SCOPE or generic ASK_TIME
}

# Generic time Q (used when entity is the activity name)
ASK_TIME_Q = [
    "Mấy giờ {ACTION}", "Mấy giờ thì {ACTION}", "{ACTION} mấy giờ",
    "{ACTION} bắt đầu lúc mấy giờ", "{ACTION} lúc nào",
    "Khi nào {ACTION}", "Bao giờ {ACTION}",
    "Lúc mấy giờ thì {ACTION}", "Thường mấy giờ {ACTION}",
    "Hôm nay mấy giờ {ACTION}", "Ngày mai mấy giờ {ACTION}",
    "Cho hỏi mấy giờ {ACTION}", "Em hỏi mấy giờ {ACTION}",
    "Anh ơi mấy giờ {ACTION}", "Báo cáo, {ACTION} lúc mấy giờ",
    "Báo cáo thủ trưởng, mấy giờ {ACTION} ạ",
    "{ACTION} đến mấy giờ", "{ACTION} kết thúc lúc nào",
    "{ACTION} kéo dài bao lâu", "Bao lâu thì {ACTION}",
    "Lát nữa {ACTION} đúng không", "Sắp {ACTION} chưa",
    "Còn bao lâu nữa {ACTION}", "Sắp đến giờ {ACTION} chưa",
    "Đến giờ {ACTION} chưa", "Hôm nay {ACTION} mấy giờ",
    "{ACTION} từ mấy giờ", "{ACTION} mất bao lâu",
    "Em báo cáo, mấy giờ {ACTION}", "Đồng chí cho em hỏi mấy giờ {ACTION}",
    "Cho em hỏi {ACTION} mấy giờ ạ",
]

ASK_SCHEDULE_TODAY_Q = [
    "Hôm nay có lịch gì", "Hôm nay có gì", "Hôm nay làm gì",
    "Hôm nay học gì", "Hôm nay học môn gì", "Hôm nay phải làm gì",
    "Lịch hôm nay sao", "Lịch hôm nay thế nào", "Lịch hôm nay là gì",
    "Lịch hôm nay có gì", "Thời khóa biểu hôm nay",
    "Thời khóa biểu hôm nay sao", "Hôm nay có hoạt động gì",
    "Hôm nay có gì làm", "Hôm nay có việc gì",
    "Cho em hỏi lịch hôm nay", "Cho hỏi hôm nay học gì",
    "Báo cáo, lịch hôm nay sao ạ", "Anh ơi hôm nay có gì",
    "Schedule hôm nay", "Hôm nay schedule sao", "Hôm nay timetable thế nào",
    "Hôm nay sáng làm gì", "Hôm nay chiều làm gì", "Hôm nay tối làm gì",
    "Bữa nay học gì", "Bữa nay có gì", "Today học gì", "Today có gì",
    "Mai có lịch gì", "Mai học môn gì", "Ngày mai có gì",
    "Hôm nay agenda gì", "Cho em xem lịch hôm nay",
    "Lịch hôm nay đâu", "Coi lịch hôm nay với",
    "Em không nhớ lịch hôm nay", "Hôm nay routine sao",
]

ASK_SUBJECT_INFO_Q = [
    "Môn {X} là môn gì", "{X} học cái gì", "{X} có nội dung gì",
    "Học {X} để làm gì", "Em chưa biết {X} là gì",
    "Cho hỏi {X} là môn gì", "Giải thích {X} cho em với",
    "{X} học những gì", "Tóm tắt {X}", "{X} thì sao",
    "{X} dạy gì", "Học {X} mấy buổi", "{X} có bao nhiêu cấp",
    "{X} cần học gì", "Nói cho em về {X}",
    "{X} là môn như thế nào", "Em hỏi về môn {X}",
    "Cho em biết về {X}", "{X} có khó không",
]

ASK_NPC_Q = [
    "{X} là ai", "Ai là {X}", "Cho hỏi {X} là ai",
    "Em chưa biết {X}", "{X} làm gì", "Vai trò của {X}",
    "{X} có nhiệm vụ gì", "Giới thiệu {X} cho em với",
    "{X} là người thế nào", "{X} có chức vụ gì",
    "Em mới đến, {X} là ai", "Báo cáo, {X} là ai ạ",
    "Anh ơi {X} là ai", "Cho em biết về {X}",
    "{X} làm chức gì", "Em hỏi tý, {X} là ai",
]

ASK_HOWTO_Q = [
    "Làm sao để {ACTION}", "Cách {ACTION} thế nào", "{ACTION} kiểu gì",
    "{ACTION} như thế nào", "Hướng dẫn {ACTION}",
    "Em chưa biết {ACTION}", "Em mới đến, {ACTION} sao",
    "{ACTION} ở đâu thì được", "Cho hỏi cách {ACTION}",
    "{ACTION} cần làm gì", "Báo cáo, {ACTION} sao ạ",
    "Anh chỉ em {ACTION} với",
]

GREETING_Q = [
    "Xin chào", "Chào anh", "Chào đồng chí", "Chào thủ trưởng",
    "Em chào thủ trưởng", "Em chào anh", "Em chào đại đội trưởng",
    "Báo cáo, em đến rồi", "Báo cáo thủ trưởng", "Báo cáo đồng chí",
    "Em xin báo cáo", "Có mặt", "Em có mặt", "Hi", "Hello", "Hey",
    "Chào buổi sáng", "Chào buổi tối", "Chào buổi chiều",
    "Sáng ạ", "Tối ạ", "Em chào ạ", "Em mới đến",
    "Em đến báo cáo", "Em xin chào thủ trưởng", "Em xin chào ạ",
    "Chào đại đội trưởng ạ", "Em đến đây ạ", "Em đến rồi đây",
]

GOODBYE_Q = [
    "Tạm biệt", "Em đi đây", "Em xin phép", "Em xin phép ra ngoài",
    "Em xin phép về", "Em xin phép đi", "Em xin phép thủ trưởng",
    "Em đi nhé", "Em chào ạ", "Em rút lui", "Em hết phép rồi",
    "Bye", "Bye bye", "Em xin phép kết thúc",
    "Chào thủ trưởng em đi", "Em xin phép rời đi",
    "Em đi nhiệm vụ", "Em xin phép chào", "Em xin phép tạm dừng",
]

THANKS_Q = [
    "Cảm ơn", "Cảm ơn anh", "Cảm ơn thủ trưởng", "Cảm ơn đồng chí",
    "Em cảm ơn", "Em cảm ơn ạ", "Thanks", "Thank you", "Tks",
    "Cám ơn nhiều", "Cảm ơn nhiều ạ", "Em biết ơn", "Em mang ơn",
    "Em cảm ơn nhiều", "Thanks bro", "Em xin cảm ơn",
    "Cảm tạ", "Em vô cùng cảm ơn",
]

SMALL_TALK_Q = [
    "Hôm nay thế nào", "Khỏe không", "Anh khỏe không", "Ổn không",
    "Đồng chí khỏe không", "Hôm nay anh thế nào", "Trời đẹp nhỉ",
    "Mệt quá", "Đói quá", "Buồn ngủ quá", "Chán quá",
    "Wifi yếu nhỉ", "Phòng nóng quá", "Tối nay làm gì cho vui",
    "Đùa tý", "Cho em hỏi vu vơ", "Em hơi mệt",
    "Hôm nay em buồn", "Em đói bụng", "Em khát nước",
]

OOS_Q = [
    "Trận đấu tối qua sao", "Có biết Messi không", "Pokemon là gì",
    "Crypto giảm sốc", "Stock thị trường", "Wifi password",
    "iPhone mới ra mắt", "Bitcoin giá nay", "Em đói",
    "Nhạc gì hay", "Phim gì hay", "Có người yêu chưa",
    "Yêu em không", "Anh có vợ chưa", "Em là ai",
    "Em là người máy à", "Anh là AI à", "ChatGPT là gì",
]


# Answer templates (same as v1 but more variation)
ANSWER_LOCATION = [
    "{areaName} nằm ở {direction}, từ vị trí hiện tại đi thẳng tầm {distance}m là tới.",
    "Đồng chí đi {direction}, qua sân điều lệnh tầm {distance}m là tới {areaName}.",
    "Muốn tới {areaName} thì đi {direction} doanh trại, có biển chỉ dẫn rồi đó.",
    "{areaName} ở {direction}, đi theo đường nội bộ tầm {distance}m thấy ngay.",
    "Đồng chí đi thẳng từ cổng chính theo hướng {direction}, {areaName} ở đó.",
    "{areaName} là khu nằm phía {direction} doanh trại, dễ tìm thôi đồng chí.",
    "Đi {direction} doanh trại khoảng {distance}m là tới {areaName}.",
    "{areaName} ở hướng {direction}, đi men theo lối nội bộ là gặp.",
]

ANSWER_TIME = [
    "{actionVi_cap} bắt đầu lúc {startTime} và kết thúc lúc {endTime}.",
    "Theo thời khóa biểu, {actionVi} từ {startTime} đến {endTime}.",
    "{actionVi_cap} lúc {startTime} đến {endTime}. Đồng chí đến đúng giờ nhé.",
    "Giờ {actionVi} là {startTime}, sai giờ là phạt đó đồng chí.",
    "{actionVi_cap} kéo dài từ {startTime} đến {endTime} hằng ngày.",
    "{actionVi_cap} diễn ra trong khung {startTime}-{endTime}.",
    "Lịch quy định: {actionVi} từ {startTime} đến {endTime}.",
]

ANSWER_SCHEDULE_TODAY = ["{__SCHEDULE_TODAY__}"]

ANSWER_SUBJECT_INFO = [
    "Môn {subjectName} là một trong 3 môn chính ở học viện. Học qua 6 cấp độ, mỗi cấp 4 câu hỏi.",
    "{subjectName} là môn cốt lõi của đại đội ta, đồng chí học đủ 6 lần để qua môn.",
    "{subjectName} - môn này quan trọng đó đồng chí, học đầy đủ để được tốt nghiệp.",
    "Học {subjectName} ở lớp học, mỗi lần đủ 4 câu hỏi. Cố gắng nhé.",
    "Môn {subjectName} có 6 cấp, đồng chí phải hoàn thành hết.",
]

ANSWER_NPC = [
    "{npcName} là {npcRole}, đồng chí gặp ở khu vực doanh trại.",
    "{npcName} - {npcRole}.",
    "Đó là {npcName}, {npcRole} của đại đội mình.",
    "{npcName} là {npcRole}, người chỉ huy mọi sinh hoạt của đại đội.",
]

ANSWER_HOWTO = [
    "Để {actionVi}, đồng chí tới {areaName} đúng giờ ({startTime}) và làm theo hướng dẫn.",
    "{actionVi_cap} thì cứ tới {areaName} từ {startTime} đến {endTime} là được.",
    "Đồng chí đi tới {areaName} lúc {startTime}, hệ thống sẽ kích hoạt {actionVi}.",
    "{actionVi_cap} đơn giản thôi: đến {areaName} đúng giờ {startTime}.",
]

ANSWER_GREETING = [
    "Chào đồng chí, hôm nay có việc gì cần báo cáo?",
    "Em có việc gì đó nào, báo cáo đi.",
    "Chào đồng chí, có gì cần hỏi không?",
    "Có mặt rồi à, hôm nay đồng chí thế nào?",
    "Chào em, vào việc thôi.",
    "Báo cáo nhận đầy đủ, có gì hỏi tôi.",
    "Đồng chí, hôm nay đến đúng giờ đấy. Có việc gì?",
]

ANSWER_GOODBYE = [
    "Đồng chí về vị trí của mình đi. Nhớ đúng giờ.",
    "Đồng ý, đồng chí đi đi. Giữ gìn kỷ luật nhé.",
    "Được, đồng chí về vị trí.",
    "OK, nhớ đúng giờ tập trung lại.",
    "Đi đi, đừng để muộn giờ tiếp theo.",
    "Đồng chí đi nhanh, không để trễ giờ.",
]

ANSWER_THANKS = [
    "Không có gì, đồng chí cứ làm tốt nhiệm vụ là được.",
    "Khỏi cảm ơn, nhiệm vụ của tôi mà.",
    "Cứ làm tốt là tôi vui rồi.",
    "Không phải cảm ơn, đồng chí tập trung học tập đi.",
    "Không có gì, làm việc tốt đi đồng chí.",
]

ANSWER_SMALL_TALK = [
    "Hôm nay tôi ổn, đồng chí đừng lo. Có việc gì cần hỏi không?",
    "Đời lính mà đồng chí, có vất vả nhưng vui.",
    "Tâm trạng ổn, đồng chí có việc gì cần báo cáo cứ nói.",
    "Vẫn bình thường thôi đồng chí, tập trung vào nhiệm vụ nhé.",
    "Cuộc sống ổn, đồng chí giữ vững tinh thần.",
]

ANSWER_OOS = [
    "Cái đó không thuộc phạm vi tôi nắm được. Đồng chí hỏi việc trong đại đội đi.",
    "Tôi là chỉ huy đại đội, mấy việc đó tôi không rõ. Có việc gì trong học viện không?",
    "Câu này tôi không trả lời được. Hỏi gì về lịch, vị trí, môn học - tôi biết.",
    "Ngoài chuyên môn rồi đồng chí. Tập trung vào học viện đi.",
    "Tôi không nắm được vấn đề đó, đồng chí hỏi cái khác đi.",
]


# Augmentation list: each function with weight (more applied = more variants)
AUGMENTATIONS = [
    ("none",        lambda t: t,        1),
    ("no_accent",   no_accent,          2),  # high weight - common
    ("telex",       telex_typo,         2),
    ("heavy_telex", heavy_telex,        2),  # NEW: more aggressive
    ("slang",       slang_swap,         2),
    ("code_mix",    code_mix,           2),
    ("ellipsis",    ellipsis,           1),
    ("filler",      add_filler,         2),
    ("dbl_filler",  add_double_filler,  1),
]


def render(template: str, **kw) -> str:
    try: return template.format(**kw)
    except KeyError: return template


def title_first(s: str) -> str:
    return s[:1].upper() + s[1:] if s else s


def gen_area_qa(game, records):
    for aid, area in game["areas"].items():
        wx, _wy, wz = area["worldPos"]
        dist = max(30, int((abs(wx) + abs(wz)) ** 0.5 * 10))
        for alias in area["aliases"]:
            for q_tpl in ASK_LOCATION_Q:
                q = render(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_LOCATION)
                a = a_tpl.format(
                    areaName=area["displayName"],
                    direction=area["direction"],
                    distance=dist,
                )
                records.append({
                    "intent": "ASK_LOCATION", "question": q, "answer": a,
                    "answer_template": a_tpl, "entityId": aid, "entityType": "area",
                    "needsGameState": False,
                })


def gen_time_qa(game, records):
    routine = game["routine"]
    # === Generic ASK_TIME for ALL slots ===
    for slot, info in routine.items():
        actionVi = info["displayVi"]
        for q_tpl in ASK_TIME_Q:
            q = render(q_tpl, ACTION=actionVi)
            a_tpl = random.choice(ANSWER_TIME)
            a = a_tpl.format(
                actionVi=actionVi, actionVi_cap=title_first(actionVi),
                startTime=info["startTime"], endTime=info["endTime"],
            )
            records.append({
                "intent": "ASK_TIME", "question": q, "answer": a,
                "answer_template": a_tpl,
                "entityId": f"slot_{slot}", "entityType": "schedule_slot",
                "needsGameState": False,
            })
        # Howto
        area = game["areas"].get(info["areaId"], {})
        area_name = area.get("displayName", info["areaId"] or "khu này")
        for q_tpl in ASK_HOWTO_Q:
            q = render(q_tpl, ACTION=actionVi)
            a_tpl = random.choice(ANSWER_HOWTO)
            a = a_tpl.format(
                actionVi=actionVi, actionVi_cap=title_first(actionVi),
                startTime=info["startTime"], endTime=info["endTime"],
                areaName=area_name,
            )
            records.append({
                "intent": "ASK_HOWTO", "question": q, "answer": a,
                "answer_template": a_tpl, "entityId": f"slot_{slot}",
                "entityType": "schedule_slot", "needsGameState": False,
            })

    # === SPECIFIC meal/sleep time questions for disambiguation ===
    # slot 3 = ăn sáng (07:00-07:30)
    s3 = routine["3"]
    for q in ASK_MEAL_TIME_Q["an_sang"]:
        a = f"Ăn sáng từ {s3['startTime']} đến {s3['endTime']}, đồng chí đến đúng giờ nhé."
        records.append({
            "intent": "ASK_TIME", "question": q, "answer": a,
            "answer_template": "MEAL_BREAKFAST",
            "entityId": "slot_3", "entityType": "schedule_slot",
            "needsGameState": False,
        })
    # slot 5 = nghỉ trưa (11:30-14:00) — bao gồm ăn trưa
    s5 = routine["5"]
    for q in ASK_MEAL_TIME_Q["an_trua"]:
        a = f"Nghỉ trưa và ăn trưa từ {s5['startTime']} đến {s5['endTime']}, có 2.5 tiếng cả ăn lẫn nghỉ."
        records.append({
            "intent": "ASK_TIME", "question": q, "answer": a,
            "answer_template": "MEAL_LUNCH",
            "entityId": "slot_5", "entityType": "schedule_slot",
            "needsGameState": False,
        })
    # slot 8 = đi ngủ (18:30-23:59) — NOT a meal
    s8 = routine["8"]
    for q in ASK_MEAL_TIME_Q["di_ngu"]:
        a = f"Đi ngủ từ {s8['startTime']}, lên giường đúng giờ giữ sức khỏe."
        records.append({
            "intent": "ASK_TIME", "question": q, "answer": a,
            "answer_template": "SLEEP_TIME",
            "entityId": "slot_8", "entityType": "schedule_slot",
            "needsGameState": False,
        })


def gen_schedule_qa(game, records):
    for q_tpl in ASK_SCHEDULE_TODAY_Q:
        records.append({
            "intent": "ASK_SCHEDULE_TODAY", "question": q_tpl,
            "answer": "{__SCHEDULE_TODAY__}", "answer_template": "{__SCHEDULE_TODAY__}",
            "entityId": "today", "entityType": "schedule_day",
            "needsGameState": True,
        })


def gen_subject_qa(game, records):
    for sid, sub in game["subjects"].items():
        for alias in sub["aliases"]:
            for q_tpl in ASK_SUBJECT_INFO_Q:
                q = render(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_SUBJECT_INFO)
                a = a_tpl.format(subjectName=sub["displayName"])
                records.append({
                    "intent": "ASK_SUBJECT_INFO", "question": q, "answer": a,
                    "answer_template": a_tpl, "entityId": sid,
                    "entityType": "subject", "needsGameState": False,
                })


def gen_npc_qa(game, records):
    for nid, npc in game["npcs"].items():
        for alias in npc["aliases"]:
            for q_tpl in ASK_NPC_Q:
                q = render(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_NPC)
                a = a_tpl.format(npcName=npc["displayName"], npcRole=npc["role"])
                records.append({
                    "intent": "ASK_NPC", "question": q, "answer": a,
                    "answer_template": a_tpl, "entityId": nid,
                    "entityType": "npc", "needsGameState": False,
                })


def gen_chitchat(records):
    chitchat = [
        ("GREETING",   GREETING_Q,   ANSWER_GREETING),
        ("GOODBYE",    GOODBYE_Q,    ANSWER_GOODBYE),
        ("THANKS",     THANKS_Q,     ANSWER_THANKS),
        ("SMALL_TALK", SMALL_TALK_Q, ANSWER_SMALL_TALK),
        ("OUT_OF_SCOPE", OOS_Q,      ANSWER_OOS),
    ]
    for intent, qs, answers in chitchat:
        for q in qs:
            for a in answers:
                records.append({
                    "intent": intent, "question": q, "answer": a,
                    "answer_template": a, "entityId": intent.lower(),
                    "entityType": "chitchat", "needsGameState": False,
                })


def apply_augmentation(records, weight_factor=1.0):
    augmented = []
    for r in records:
        augmented.append({**r, "augmentation": "none"})
        # weighted random choice — heavy augs run more often
        for tag, fn, weight in AUGMENTATIONS[1:]:
            n = int(weight * weight_factor)
            for _ in range(n):
                new_q = fn(r["question"])
                if new_q == r["question"]:
                    continue
                augmented.append({**r, "question": new_q, "augmentation": tag})
    return augmented


def deduplicate(records):
    seen = set()
    out = []
    for r in records:
        key = (r["intent"], r["question"].lower().strip(), r.get("entityId", ""))
        if key in seen:
            continue
        seen.add(key)
        out.append(r)
    return out


def main():
    game = json.loads(GAME_PATH.read_text(encoding="utf-8"))

    records = []
    print("[v8] generating base records...")
    gen_area_qa(game, records)
    gen_time_qa(game, records)
    gen_schedule_qa(game, records)
    gen_subject_qa(game, records)
    gen_npc_qa(game, records)
    gen_chitchat(records)
    print(f"[v8] base = {len(records):,}")

    print("[v8] heavy augmentation (weight_factor=1.5)...")
    records = apply_augmentation(records, weight_factor=1.5)
    print(f"[v8] post-aug = {len(records):,}")

    print("[v8] dedup...")
    records = deduplicate(records)
    print(f"[v8] post-dedup = {len(records):,}")

    for i, r in enumerate(records):
        r["id"] = f"qa8_{i:07d}"
        r.setdefault("augmentation", "none")

    OUT_JSONL.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_JSONL, "w", encoding="utf-8") as f:
        for r in records:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"[v8] wrote {OUT_JSONL}  ({OUT_JSONL.stat().st_size/1024/1024:.2f} MiB)")

    from collections import Counter
    by_intent = Counter(r["intent"] for r in records)
    by_aug    = Counter(r["augmentation"] for r in records)
    summary = {
        "total": len(records),
        "byIntent": dict(by_intent),
        "byAugmentation": dict(by_aug),
        "intents": sorted(by_intent.keys()),
    }
    OUT_SUMMARY.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print()
    print("=== Per-intent ===")
    for k, v in sorted(by_intent.items(), key=lambda x: -x[1]):
        print(f"  {k:25s} {v}")
    print()
    print("=== Per-augmentation ===")
    for k, v in sorted(by_aug.items(), key=lambda x: -x[1]):
        print(f"  {k:15s} {v}")


if __name__ == "__main__":
    main()
