"""
Phase C — Generate massive paraphrased Q&A dataset from real game entities.

Strategy:
  - For each intent x each entity x each paraphrase template -> 1 Q
  - For each Q -> 1 canonical answer template (with slot placeholders)
  - Apply augmentation passes (typo, no-accent, slang, code-mix, ellipsis,
    politeness, filler words) for robustness.

Output:
  AI_Training/phase_c_chat/data/qa_pairs.jsonl   (one record per line)
  AI_Training/phase_c_chat/data/qa_summary.json  (stats + intent map)

Each record:
{
  "id":         "<intent>__<entityId>__<idx>",
  "intent":     "ASK_LOCATION",
  "question":   "khu thể dục ở đâu vậy",
  "answer":     "Sân vận động nằm ở phía đông doanh trại, đi thẳng khoảng 80m là tới.",
  "answer_template": "{areaName} nằm ở {direction}, đi thẳng khoảng {distance}m là tới.",
  "entityId":   "SanVanDong",
  "entityType": "area",
  "needsGameState": false,
  "augmentation": "none" | "no_accent" | "telex_typo" | "slang" | "code_mix" | "ellipsis",
}
"""
import json
import random
import re
from pathlib import Path

random.seed(42)

SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
GAME_PATH  = DATA_DIR / "game_entities.json"
OUT_JSONL  = DATA_DIR / "qa_pairs.jsonl"
OUT_SUMMARY = DATA_DIR / "qa_summary.json"


# =========================================================================
# PARAPHRASE TEMPLATES
# =========================================================================

# {X} = entity alias placeholder
ASK_LOCATION_Q = [
    "{X} ở đâu",
    "{X} ở đâu vậy",
    "{X} ở chỗ nào",
    "{X} chỗ nào",
    "{X} nằm ở đâu",
    "{X} nằm chỗ nào",
    "{X} ở khu nào",
    "{X} hướng nào",
    "Cho hỏi {X} ở đâu",
    "Cho em hỏi {X} ở đâu ạ",
    "Cho hỏi {X} chỗ nào",
    "Cho em hỏi {X} chỗ nào ạ",
    "Em hỏi {X} ở đâu",
    "Mình hỏi {X} ở đâu",
    "{X} đi lối nào",
    "Đi đâu thì tới {X}",
    "Làm sao đi tới {X}",
    "Đường nào đi tới {X}",
    "Tới {X} đi lối nào",
    "Tới {X} đi hướng nào",
    "Đường đi {X} thế nào",
    "Em muốn đi {X} đi lối nào",
    "Em muốn đến {X} thì đi đâu",
    "Đồng chí cho em hỏi {X} ở đâu",
    "Báo cáo, {X} ở đâu ạ",
    "Báo cáo thủ trưởng, {X} ở đâu",
    "Anh ơi {X} ở đâu",
    "Thủ trưởng ơi {X} chỗ nào",
    "Anh chỉ giúp em {X} với",
    "Chỉ giúp em {X} ở đâu",
    "Em chưa biết {X} ở đâu",
    "Em mới đến, {X} ở đâu vậy",
    "{X} đâu rồi",
    "{X} đâu nhỉ",
    "{X} ở chỗ nào nhỉ",
    "Mấy hôm trước em quên, {X} ở đâu",
    "Hỏi tý, {X} ở đâu",
    "{X} thì sao tới",
]

ASK_TIME_Q = [
    "Mấy giờ {ACTION}",
    "Mấy giờ thì {ACTION}",
    "{ACTION} mấy giờ",
    "{ACTION} bắt đầu lúc mấy giờ",
    "{ACTION} lúc nào",
    "Khi nào {ACTION}",
    "Bao giờ {ACTION}",
    "Lúc mấy giờ thì {ACTION}",
    "Thường mấy giờ {ACTION}",
    "Hôm nay mấy giờ {ACTION}",
    "Ngày mai mấy giờ {ACTION}",
    "Cho hỏi mấy giờ {ACTION}",
    "Em hỏi mấy giờ {ACTION}",
    "Anh ơi mấy giờ {ACTION}",
    "Báo cáo, {ACTION} lúc mấy giờ",
    "Báo cáo thủ trưởng, mấy giờ {ACTION} ạ",
    "{ACTION} đến mấy giờ",
    "{ACTION} kết thúc lúc nào",
    "{ACTION} kéo dài bao lâu",
    "Bao lâu thì {ACTION}",
    "Lát nữa {ACTION} đúng không",
    "Sắp {ACTION} chưa",
    "Còn bao lâu nữa {ACTION}",
    "Sắp đến giờ {ACTION} chưa",
    "Đến giờ {ACTION} chưa",
]

ASK_SCHEDULE_TODAY_Q = [
    "Hôm nay có lịch gì",
    "Hôm nay có gì",
    "Hôm nay làm gì",
    "Hôm nay học gì",
    "Hôm nay học môn gì",
    "Hôm nay phải làm gì",
    "Lịch hôm nay sao",
    "Lịch hôm nay thế nào",
    "Lịch hôm nay là gì",
    "Lịch hôm nay có gì",
    "Thời khóa biểu hôm nay",
    "Thời khóa biểu hôm nay sao",
    "Hôm nay có hoạt động gì",
    "Hôm nay có gì làm",
    "Hôm nay có việc gì",
    "Cho em hỏi lịch hôm nay",
    "Cho hỏi hôm nay học gì",
    "Báo cáo, lịch hôm nay sao ạ",
    "Anh ơi hôm nay có gì",
    "Schedule hôm nay",
    "Hôm nay schedule sao",
    "Hôm nay timetable thế nào",
    "Hôm nay sáng làm gì",
    "Hôm nay chiều làm gì",
    "Hôm nay tối làm gì",
    "Bữa nay học gì",
    "Bữa nay có gì",
    "Today học gì",
    "Today có gì",
    "Mai có lịch gì",
    "Mai học môn gì",
    "Ngày mai có gì",
]

ASK_SUBJECT_INFO_Q = [
    "Môn {X} là môn gì",
    "{X} học cái gì",
    "{X} có nội dung gì",
    "Học {X} để làm gì",
    "Em chưa biết {X} là gì",
    "Cho hỏi {X} là môn gì",
    "Giải thích {X} cho em với",
    "{X} học những gì",
    "Tóm tắt {X}",
    "{X} thì sao",
]

ASK_NPC_Q = [
    "{X} là ai",
    "Ai là {X}",
    "Cho hỏi {X} là ai",
    "Em chưa biết {X}",
    "{X} làm gì",
    "Vai trò của {X}",
    "{X} có nhiệm vụ gì",
    "Giới thiệu {X} cho em với",
    "{X} là người thế nào",
]

ASK_HOWTO_Q = [
    "Làm sao để {ACTION}",
    "Cách {ACTION} thế nào",
    "{ACTION} kiểu gì",
    "{ACTION} như thế nào",
    "Hướng dẫn {ACTION}",
    "Em chưa biết {ACTION}",
    "Em mới đến, {ACTION} sao",
    "{ACTION} ở đâu thì được",
    "Cho hỏi cách {ACTION}",
]

GREETING_Q = [
    "Xin chào",
    "Chào anh",
    "Chào đồng chí",
    "Chào thủ trưởng",
    "Em chào thủ trưởng",
    "Em chào anh",
    "Em chào đại đội trưởng",
    "Báo cáo, em đến rồi",
    "Báo cáo thủ trưởng",
    "Báo cáo đồng chí",
    "Em xin báo cáo",
    "Có mặt",
    "Em có mặt",
    "Hi",
    "Hello",
    "Hey",
    "Chào buổi sáng",
    "Chào buổi tối",
    "Chào buổi chiều",
    "Sáng ạ",
    "Tối ạ",
    "Em chào ạ",
    "Em mới đến",
]

GOODBYE_Q = [
    "Tạm biệt",
    "Em đi đây",
    "Em xin phép",
    "Em xin phép ra ngoài",
    "Em xin phép về",
    "Em xin phép đi",
    "Em xin phép thủ trưởng",
    "Em đi nhé",
    "Em chào ạ",
    "Em rút lui",
    "Em hết phép rồi",
    "Bye",
    "Bye bye",
    "Em xin phép kết thúc",
    "Chào thủ trưởng em đi",
]

THANKS_Q = [
    "Cảm ơn",
    "Cảm ơn anh",
    "Cảm ơn thủ trưởng",
    "Cảm ơn đồng chí",
    "Em cảm ơn",
    "Em cảm ơn ạ",
    "Thanks",
    "Thank you",
    "Tks",
    "Cám ơn nhiều",
    "Cảm ơn nhiều ạ",
    "Em biết ơn",
    "Em mang ơn",
]

SMALL_TALK_Q = [
    "Hôm nay thế nào",
    "Khỏe không",
    "Anh khỏe không",
    "Ổn không",
    "Đồng chí khỏe không",
    "Hôm nay anh thế nào",
    "Trời đẹp nhỉ",
    "Mệt quá",
    "Đói quá",
    "Buồn ngủ quá",
    "Chán quá",
    "Wifi yếu nhỉ",
    "Phòng nóng quá",
    "Tối nay làm gì cho vui",
    "Đùa tý",
    "Cho em hỏi vu vơ",
]

OOS_Q = [
    "Trận đấu tối qua sao",
    "Có biết Messi không",
    "Pokemon là gì",
    "Crypto giảm sốc",
    "Stock thị trường",
    "Wifi password",
    "iPhone mới ra mắt",
    "Bitcoin giá nay",
    "Em đói",
    "Nhạc gì hay",
    "Phim gì hay",
    "Có người yêu chưa",
    "Yêu em không",
]


# =========================================================================
# ANSWER TEMPLATES
# =========================================================================

# Each intent maps to N response templates with placeholder slots.
# {areaName}, {direction}, {distance} - area data
# {actionVi} - activity Vietnamese name
# {startTime}, {endTime} - schedule
# {subjectName} - subject Vietnamese name
# {npcName}, {npcRole} - NPC data
# Placeholders that need runtime data are marked needsGameState=True
# (the answer template itself stays static).

ANSWER_LOCATION = [
    "{areaName} nằm ở {direction}, từ vị trí hiện tại đi thẳng tầm {distance}m là tới.",
    "Đồng chí đi {direction}, qua sân điều lệnh tầm {distance}m là tới {areaName}.",
    "Muốn tới {areaName} thì đi {direction} doanh trại, có biển chỉ dẫn rồi đó.",
    "{areaName} ở {direction}, đi theo đường nội bộ tầm {distance}m thấy ngay.",
    "Đồng chí đi thẳng từ cổng chính theo hướng {direction}, {areaName} ở đó.",
    "{areaName} là khu nằm phía {direction} doanh trại, dễ tìm thôi đồng chí.",
]

ANSWER_TIME = [
    "{actionVi_capitalized} bắt đầu lúc {startTime} và kết thúc lúc {endTime}.",
    "Theo thời khóa biểu, {actionVi} từ {startTime} đến {endTime}.",
    "{actionVi_capitalized} lúc {startTime} đến {endTime}. Đồng chí đến đúng giờ nhé.",
    "Giờ {actionVi} là {startTime}, sai giờ là phạt đó đồng chí.",
    "{actionVi_capitalized} kéo dài từ {startTime} đến {endTime} hằng ngày.",
]

ANSWER_SCHEDULE_TODAY = [
    "{__SCHEDULE_TODAY__}",  # runtime: read current day, dump 8-slot routine
]

ANSWER_SUBJECT_INFO = [
    "Môn {subjectName} là một trong 3 môn chính ở học viện. Học qua 6 cấp độ, mỗi cấp 4 câu hỏi.",
    "{subjectName} là môn cốt lõi của đại đội ta, đồng chí học đủ 6 lần để qua môn.",
    "{subjectName} - môn này quan trọng đó đồng chí, học đầy đủ để được tốt nghiệp.",
    "Học {subjectName} ở lớp học, mỗi lần đủ 4 câu hỏi. Cố gắng nhé.",
]

ANSWER_NPC = [
    "{npcName} là {npcRole}, đồng chí gặp ở khu vực doanh trại.",
    "{npcName} - {npcRole}.",
    "Đó là {npcName}, {npcRole} của đại đội mình.",
]

ANSWER_HOWTO = [
    "Để {actionVi}, đồng chí tới {areaName} đúng giờ ({startTime}) và làm theo hướng dẫn.",
    "{actionVi_capitalized} thì cứ tới {areaName} từ {startTime} đến {endTime} là được.",
    "Đồng chí đi tới {areaName} lúc {startTime}, hệ thống sẽ kích hoạt {actionVi}.",
]

ANSWER_GREETING = [
    "Chào đồng chí, hôm nay có việc gì cần báo cáo?",
    "Em có việc gì đó nào, báo cáo đi.",
    "Chào đồng chí, có gì cần hỏi không?",
    "Có mặt rồi à, hôm nay đồng chí thế nào?",
    "Chào em, vào việc thôi.",
    "Báo cáo nhận đầy đủ, có gì hỏi tôi.",
]

ANSWER_GOODBYE = [
    "Đồng chí về vị trí của mình đi. Nhớ đúng giờ.",
    "Đồng ý, đồng chí đi đi. Giữ gìn kỷ luật nhé.",
    "Được, đồng chí về vị trí.",
    "OK, nhớ đúng giờ tập trung lại.",
    "Đi đi, đừng để muộn giờ tiếp theo.",
]

ANSWER_THANKS = [
    "Không có gì, đồng chí cứ làm tốt nhiệm vụ là được.",
    "Khỏi cảm ơn, nhiệm vụ của tôi mà.",
    "Cứ làm tốt là tôi vui rồi.",
    "Không phải cảm ơn, đồng chí tập trung học tập đi.",
]

ANSWER_SMALL_TALK = [
    "Hôm nay tôi ổn, đồng chí đừng lo. Có việc gì cần hỏi không?",
    "Đời lính mà đồng chí, có vất vả nhưng vui.",
    "Tâm trạng ổn, đồng chí có việc gì cần báo cáo cứ nói.",
    "Vẫn bình thường thôi đồng chí, tập trung vào nhiệm vụ nhé.",
]

ANSWER_OOS = [
    "Cái đó không thuộc phạm vi tôi nắm được. Đồng chí hỏi việc trong đại đội đi.",
    "Tôi là chỉ huy đại đội, mấy việc đó tôi không rõ. Có việc gì trong học viện không?",
    "Câu này tôi không trả lời được. Hỏi gì về lịch, vị trí, môn học - tôi biết.",
    "Ngoài chuyên môn rồi đồng chí. Tập trung vào học viện đi.",
]


# =========================================================================
# AUGMENTATION
# =========================================================================

# Vietnamese diacritic stripping via Unicode NFD decomposition.
import unicodedata
def no_accent(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    stripped = "".join(c for c in nfkd if not unicodedata.combining(c))
    # Đ/đ -> D/d
    return stripped.replace("Đ", "D").replace("đ", "d")

# Telex typo: random double-vowel duplication (mimics fast typing).
TELEX_TARGETS = ["a","e","i","o","u","ô","ơ","ê","ă","â"]

# Slang / internet abbreviations.
SLANG_MAP = {
    "không":  "ko",
    "được":   "đc",
    "vậy":    "v",
    "biết":   "bít",
    "đồng chí": "đ/c",
    "tại sao": "tsao",
    "thế nào": "tn",
    "thế":    "thế",
    "ờ":      "uh",
    "ạ":      "",   # drop politeness
}

# Code-mix insertions: prepend or wrap with English phrasing.
CODE_MIX_PREFIXES = ["", "Bro ", "Anh ơi ", "Hey ", "Yo ", "Cho hỏi "]
CODE_MIX_INFIXES  = [
    ("ở đâu",        "where is"),
    ("là gì",        "what is"),
    ("mấy giờ",      "what time"),
    ("hôm nay",      "today"),
    ("ngày mai",     "tomorrow"),
    ("học",          "study"),
    ("lớp học",      "classroom"),
    ("nhà ăn",       "canteen"),
    ("sân vận động", "gym"),
]


def telex_typo(text: str, rate: float = 0.10) -> str:
    """Double random vowels to simulate Telex auto-correct artifacts."""
    out = []
    for ch in text:
        out.append(ch)
        if ch.lower() in TELEX_TARGETS and random.random() < rate:
            out.append(ch)  # duplicate
    return "".join(out)


def slang_swap(text: str) -> str:
    for k, v in SLANG_MAP.items():
        if random.random() < 0.5:
            text = text.replace(k, v)
    return text


def code_mix(text: str) -> str:
    prefix = random.choice(CODE_MIX_PREFIXES)
    text = prefix + text
    if random.random() < 0.4:
        vi, en = random.choice(CODE_MIX_INFIXES)
        text = text.replace(vi, en, 1)
    return text


def ellipsis(text: str) -> str:
    """Strip leading politeness/filler words, mimicking quick questions."""
    text = re.sub(r"^(cho (em |mình )?hỏi |em hỏi |anh ơi |báo cáo[^,]*,?\s*|đồng chí ơi |thủ trưởng ơi )", "", text, flags=re.IGNORECASE)
    return text


def add_filler(text: str) -> str:
    """Add common filler words/particles found in spoken Vietnamese."""
    fillers = ["ạ", "ạ?", "nhỉ", "nhỉ?", "vậy", "thế", "đó", "nào"]
    if not text.endswith("?"):
        text += " " + random.choice(fillers)
    return text


AUGMENTATIONS = [
    ("none",        lambda t: t),
    ("no_accent",   no_accent),
    ("telex_typo",  telex_typo),
    ("slang",       slang_swap),
    ("code_mix",    code_mix),
    ("ellipsis",    ellipsis),
    ("filler",      add_filler),
]


# =========================================================================
# GENERATION
# =========================================================================

def title_first(s: str) -> str:
    return s[:1].upper() + s[1:] if s else s


def render_q(template: str, **kw) -> str:
    """Fill template placeholders. Falls back to bare template if no slot."""
    try:
        return template.format(**kw)
    except KeyError:
        return template


def gen_area_qa(game, records: list):
    """Generate ASK_LOCATION & ASK_HOWTO Q&A for each area."""
    for aid, area in game["areas"].items():
        # Distance heuristic from worldPos magnitude (placeholder).
        wx, wy, wz = area["worldPos"]
        dist = max(30, int((abs(wx) + abs(wz)) ** 0.5 * 10))
        for alias in area["aliases"]:
            for q_tpl in ASK_LOCATION_Q:
                q = render_q(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_LOCATION)
                a = a_tpl.format(
                    areaName=area["displayName"],
                    direction=area["direction"],
                    distance=dist,
                )
                records.append({
                    "intent":       "ASK_LOCATION",
                    "question":     q,
                    "answer":       a,
                    "answer_template": a_tpl,
                    "entityId":     aid,
                    "entityType":   "area",
                    "needsGameState": False,
                })


def gen_time_qa(game, records: list):
    """Generate ASK_TIME Q&A using routine slots (canonical activity Vietnamese)."""
    routine = game["routine"]
    for slot, info in routine.items():
        actionVi = info["displayVi"]  # e.g. "tập thể dục"
        startTime = info["startTime"]
        endTime   = info["endTime"]
        area_id   = info["areaId"]
        area      = game["areas"].get(area_id, {})
        area_name = area.get("displayName", area_id or "khu này")
        for q_tpl in ASK_TIME_Q:
            q = render_q(q_tpl, ACTION=actionVi)
            a_tpl = random.choice(ANSWER_TIME)
            a = a_tpl.format(
                actionVi=actionVi,
                actionVi_capitalized=title_first(actionVi),
                startTime=startTime,
                endTime=endTime,
                areaName=area_name,
            )
            records.append({
                "intent":       "ASK_TIME",
                "question":     q,
                "answer":       a,
                "answer_template": a_tpl,
                "entityId":     f"slot_{slot}",
                "entityType":   "schedule_slot",
                "needsGameState": False,
            })
        # ASK_HOWTO for the same activity.
        for q_tpl in ASK_HOWTO_Q:
            q = render_q(q_tpl, ACTION=actionVi)
            a_tpl = random.choice(ANSWER_HOWTO)
            a = a_tpl.format(
                actionVi=actionVi,
                actionVi_capitalized=title_first(actionVi),
                startTime=startTime,
                endTime=endTime,
                areaName=area_name,
            )
            records.append({
                "intent":       "ASK_HOWTO",
                "question":     q,
                "answer":       a,
                "answer_template": a_tpl,
                "entityId":     f"slot_{slot}",
                "entityType":   "schedule_slot",
                "needsGameState": False,
            })


def gen_schedule_qa(game, records: list):
    """Generate ASK_SCHEDULE_TODAY Q&A (runtime-resolved answer)."""
    for q_tpl in ASK_SCHEDULE_TODAY_Q:
        q = q_tpl
        a = "{__SCHEDULE_TODAY__}"  # runtime placeholder, resolved by C# context
        records.append({
            "intent":       "ASK_SCHEDULE_TODAY",
            "question":     q,
            "answer":       a,
            "answer_template": a,
            "entityId":     "today",
            "entityType":   "schedule_day",
            "needsGameState": True,
        })


def gen_subject_qa(game, records: list):
    for sid, sub in game["subjects"].items():
        for alias in sub["aliases"]:
            for q_tpl in ASK_SUBJECT_INFO_Q:
                q = render_q(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_SUBJECT_INFO)
                a = a_tpl.format(subjectName=sub["displayName"])
                records.append({
                    "intent":       "ASK_SUBJECT_INFO",
                    "question":     q,
                    "answer":       a,
                    "answer_template": a_tpl,
                    "entityId":     sid,
                    "entityType":   "subject",
                    "needsGameState": False,
                })


def gen_npc_qa(game, records: list):
    for nid, npc in game["npcs"].items():
        for alias in npc["aliases"]:
            for q_tpl in ASK_NPC_Q:
                q = render_q(q_tpl, X=alias)
                a_tpl = random.choice(ANSWER_NPC)
                a = a_tpl.format(npcName=npc["displayName"], npcRole=npc["role"])
                records.append({
                    "intent":       "ASK_NPC",
                    "question":     q,
                    "answer":       a,
                    "answer_template": a_tpl,
                    "entityId":     nid,
                    "entityType":   "npc",
                    "needsGameState": False,
                })


def gen_chitchat_qa(records: list):
    chitchat_intents = [
        ("GREETING",   GREETING_Q,   ANSWER_GREETING),
        ("GOODBYE",    GOODBYE_Q,    ANSWER_GOODBYE),
        ("THANKS",     THANKS_Q,     ANSWER_THANKS),
        ("SMALL_TALK", SMALL_TALK_Q, ANSWER_SMALL_TALK),
        ("OUT_OF_SCOPE", OOS_Q,      ANSWER_OOS),
    ]
    for intent, qs, answers in chitchat_intents:
        for q in qs:
            for a_tpl in answers:
                records.append({
                    "intent":       intent,
                    "question":     q,
                    "answer":       a_tpl,
                    "answer_template": a_tpl,
                    "entityId":     intent.lower(),
                    "entityType":   "chitchat",
                    "needsGameState": False,
                })


def apply_augmentation(records: list, n_aug_per_rec: int = 3) -> list:
    """For each base record, append N augmented variants of the question.
    The answer stays canonical."""
    aug = []
    for r in records:
        aug.append(r)  # keep original
        chosen = random.sample(AUGMENTATIONS[1:], k=min(n_aug_per_rec, len(AUGMENTATIONS) - 1))
        for tag, fn in chosen:
            new_q = fn(r["question"])
            if new_q == r["question"]:
                continue
            aug.append({**r, "question": new_q, "augmentation": tag})
    return aug


def deduplicate(records: list) -> list:
    seen = set()
    out = []
    for r in records:
        key = (r["intent"], r["question"].lower().strip())
        if key in seen:
            continue
        seen.add(key)
        out.append(r)
    return out


def main():
    if not GAME_PATH.exists():
        raise SystemExit(f"missing {GAME_PATH}, run parse_game_data.py first")
    game = json.loads(GAME_PATH.read_text(encoding="utf-8"))

    print("[gen] generating base Q&A from real game entities...")
    records: list = []
    gen_area_qa(game, records)
    gen_time_qa(game, records)
    gen_schedule_qa(game, records)
    gen_subject_qa(game, records)
    gen_npc_qa(game, records)
    gen_chitchat_qa(records)
    print(f"[gen] base records = {len(records)}")

    print("[gen] applying augmentation (4 variants/record)...")
    records = apply_augmentation(records, n_aug_per_rec=4)
    print(f"[gen] post-aug      = {len(records)}")

    print("[gen] deduplicating...")
    records = deduplicate(records)
    print(f"[gen] post-dedup    = {len(records)}")

    # Assign IDs.
    for i, r in enumerate(records):
        r["id"] = f"qa_{i:07d}"
        r.setdefault("augmentation", "none")

    # Write JSONL.
    OUT_JSONL.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_JSONL, "w", encoding="utf-8") as f:
        for r in records:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"[gen] wrote {OUT_JSONL}  ({OUT_JSONL.stat().st_size/1024:.1f} KiB)")

    # Summary stats.
    from collections import Counter
    by_intent = Counter(r["intent"] for r in records)
    by_aug    = Counter(r["augmentation"] for r in records)
    summary = {
        "total":        len(records),
        "byIntent":     dict(by_intent),
        "byAugmentation": dict(by_aug),
        "intents":      sorted(by_intent.keys()),
    }
    OUT_SUMMARY.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[gen] wrote {OUT_SUMMARY}")
    print()
    print("=== Per-intent count ===")
    for k, v in sorted(by_intent.items(), key=lambda x: -x[1]):
        print(f"  {k:25s} {v}")
    print()
    print("=== Per-augmentation count ===")
    for k, v in sorted(by_aug.items(), key=lambda x: -x[1]):
        print(f"  {k:15s} {v}")


if __name__ == "__main__":
    main()
