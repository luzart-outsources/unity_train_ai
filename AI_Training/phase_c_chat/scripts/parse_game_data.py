"""
Phase C — Parse Unity ScriptableObject YAML game data into structured JSON.

Reads:
  Assets/_Data/Areas/*.asset       -> 6 Area definitions
  Assets/_Data/NPCs/*.asset        -> 3 NPCs + 2 Schedules
  Assets/_Data/Subjects/*.asset    -> 3 Subjects
  Assets/_Data/Quests/*.asset      -> 240 Quests (30 days x 8/day)
  Assets/_Data/Quizzes/*.asset     -> 18 Quiz sets
  Assets/_Data/Days/*.asset        -> 30 Days
  Assets/_Data/Config/GameConfig.asset

Outputs:
  AI_Training/phase_c_chat/data/game_entities.json
"""
import os
import re
import json
import yaml
import glob
from pathlib import Path
from collections import defaultdict

# Anchor relative to this script's location (phase_c_chat/scripts/)
SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent.parent.parent  # ../../../  -> repo root
DATA_DIR = REPO_ROOT / "Assets" / "_Data"
OUT_PATH = SCRIPT_DIR.parent / "data" / "game_entities.json"


# Unity YAML has %TAG and !u! directives + custom !u!114 tags.
# Use a permissive loader that ignores tags.
class IgnoreTagsLoader(yaml.SafeLoader):
    pass


def _ignore_unknown(loader, suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    return loader.construct_scalar(node)


IgnoreTagsLoader.add_multi_constructor("tag:unity3d.com,2011:", _ignore_unknown)
IgnoreTagsLoader.add_multi_constructor("!u!", _ignore_unknown)


def parse_unity_asset(path: Path) -> dict | None:
    """Parse a Unity .asset YAML file. Returns the MonoBehaviour content dict or None."""
    try:
        text = path.read_text(encoding="utf-8")
    except Exception as e:
        print(f"[WARN] could not read {path}: {e}")
        return None

    # Strip Unity-specific YAML preamble & document tags.
    # Replace `--- !u!114 &11400000` with just `---`.
    cleaned = re.sub(r"---\s+!u!\d+\s+&\d+", "---", text)

    try:
        docs = list(yaml.load_all(cleaned, Loader=IgnoreTagsLoader))
    except Exception as e:
        print(f"[WARN] yaml parse failed for {path}: {e}")
        return None

    for doc in docs:
        if isinstance(doc, dict):
            mb = doc.get("MonoBehaviour")
            if mb:
                return mb
    return None


# ---------- Area parser ----------
def parse_areas() -> dict:
    """Parse all Area_*.asset files."""
    areas = {}
    # Vietnamese display names for each Area ID (from game design docs).
    display_names_vi = {
        "SanVanDong":   "sân vận động",
        "DonVeSinh":    "khu dọn vệ sinh",
        "NhaAn_Door":   "nhà ăn",
        "LopHoc_Door":  "lớp học",
        "KTX_Door":     "ký túc xá",
        "FreeArea":     "khu tự do",
    }
    # Synonyms / aliases that players may type when asking about each area.
    aliases_vi = {
        "SanVanDong": [
            "sân vận động", "san van dong", "svd", "sân thể dục", "sân tập thể dục",
            "sân tập", "sân thể thao", "chỗ tập thể dục", "nơi tập thể dục",
            "khu thể dục", "sân chạy", "sân đá bóng", "sân bóng",
        ],
        "DonVeSinh": [
            "khu dọn vệ sinh", "don ve sinh", "chỗ dọn vệ sinh", "khu vệ sinh",
            "nơi dọn vệ sinh", "khu dọn dẹp", "khu lau dọn", "khu tăng gia",
            "khu lao động", "chỗ dọn rác",
        ],
        "NhaAn_Door": [
            "nhà ăn", "nha an", "căng tin", "căn tin", "canteen", "chỗ ăn cơm",
            "nơi ăn cơm", "khu ăn cơm", "phòng ăn", "phòng ăn cơm",
            "khu ẩm thực", "chỗ ăn", "nơi ăn",
        ],
        "LopHoc_Door": [
            "lớp học", "lop hoc", "phòng học", "giảng đường", "nhà học",
            "khu học tập", "chỗ học", "nơi học", "phòng giảng", "lớp",
        ],
        "KTX_Door": [
            "ký túc xá", "ky tuc xa", "ktx", "phòng ngủ", "phòng ở",
            "chỗ ngủ", "nơi ngủ", "khu ngủ", "nhà ở", "doanh trại",
            "phòng tập thể", "nhà tập thể",
        ],
        "FreeArea": [
            "khu tự do", "khu free", "free area", "khu giải trí",
            "chỗ chơi", "nơi nghỉ", "chỗ tự do", "khu nghỉ ngơi",
            "khu thư giãn",
        ],
    }
    # Directional/coordinate hint for response generation.
    direction_hint = {
        "SanVanDong":   "phía đông doanh trại",
        "DonVeSinh":    "phía tây nam doanh trại",
        "NhaAn_Door":   "trung tâm doanh trại",
        "LopHoc_Door":  "phía bắc doanh trại",
        "KTX_Door":     "phía nam doanh trại",
        "FreeArea":     "phía đông nam doanh trại",
    }

    for asset in sorted((DATA_DIR / "Areas").glob("Area_*.asset")):
        data = parse_unity_asset(asset)
        if not data:
            continue
        aid = data.get("id") or asset.stem.replace("Area_", "")
        pos = data.get("worldPos", {})
        size = data.get("size", {})
        areas[aid] = {
            "id": aid,
            "displayName": display_names_vi.get(aid, aid),
            "aliases": aliases_vi.get(aid, [display_names_vi.get(aid, aid)]),
            "direction": direction_hint.get(aid, "doanh trại"),
            "worldPos": [pos.get("x", 0), pos.get("y", 0), pos.get("z", 0)],
            "size":     [size.get("x", 0), size.get("y", 0), size.get("z", 0)],
            "assetFile": asset.name,
        }
    return areas


# ---------- NPC parser ----------
def parse_npcs() -> dict:
    npcs = {}
    display_names = {
        "DaiDoiTruong":  "Đại đội trưởng",
        "HocSinh_01":    "Học viên 01",
        "HocSinh_02":    "Học viên 02",
    }
    aliases = {
        "DaiDoiTruong": [
            "đại đội trưởng", "dai doi truong", "ddt", "chỉ huy", "chi huy",
            "trưởng đội", "thủ trưởng", "thủ trướng", "chỉ huy trưởng",
            "anh đại đội trưởng", "đồng chí đại đội trưởng",
        ],
        "HocSinh_01": [
            "học viên 01", "học viên một", "học sinh 01", "hoc sinh 01",
            "đồng chí 01", "bạn 01", "hv 01",
        ],
        "HocSinh_02": [
            "học viên 02", "học viên hai", "học sinh 02", "hoc sinh 02",
            "đồng chí 02", "bạn 02", "hv 02",
        ],
    }
    role = {
        "DaiDoiTruong":  "chỉ huy trực tiếp của đại đội",
        "HocSinh_01":    "học viên đồng đội",
        "HocSinh_02":    "học viên đồng đội",
    }
    for asset in sorted((DATA_DIR / "NPCs").glob("NPC_*.asset")):
        data = parse_unity_asset(asset)
        if not data:
            continue
        nid = data.get("id") or asset.stem.replace("NPC_", "")
        npcs[nid] = {
            "id": nid,
            "displayName": display_names.get(nid, nid),
            "aliases": aliases.get(nid, [display_names.get(nid, nid)]),
            "role": role.get(nid, ""),
            "assetFile": asset.name,
        }
    return npcs


# ---------- Subject parser ----------
def parse_subjects() -> dict:
    subs = {}
    display_names = {
        "ChinhTri":     "Chính trị",
        "GDQuocPhong":  "Giáo dục Quốc phòng",
        "LichSu":       "Lịch sử",
    }
    aliases = {
        "ChinhTri":     ["chính trị", "chinh tri", "ct", "môn chính trị", "học chính trị"],
        "GDQuocPhong":  ["giáo dục quốc phòng", "quốc phòng", "gdqp", "gd quốc phòng", "qp", "môn quốc phòng"],
        "LichSu":       ["lịch sử", "lich su", "sử", "ls", "môn sử", "học sử", "môn lịch sử"],
    }
    for asset in sorted((DATA_DIR / "Subjects").glob("Subject_*.asset")):
        data = parse_unity_asset(asset)
        if not data:
            continue
        sid = data.get("id") or asset.stem.replace("Subject_", "")
        subs[sid] = {
            "id": sid,
            "displayName": display_names.get(sid, sid),
            "aliases": aliases.get(sid, [display_names.get(sid, sid)]),
            "maxPoints": data.get("maxPointsPerLesson", 40),
            "assetFile": asset.name,
        }
    return subs


# ---------- Quest parser ----------
def parse_quests() -> list:
    quests = []
    # Activity Vietnamese display map based on Quest title.
    activity_map = {
        "Tap_the_duc":          ("tập thể dục", "exercise"),
        "Don_ve_sinh":          ("dọn vệ sinh", "cleaning"),
        "An_sang":              ("ăn sáng", "breakfast"),
        "Nghi_trua":            ("nghỉ trưa", "lunch break"),
        "An_trua":              ("ăn trưa", "lunch"),
        "An_toi":               ("ăn tối", "dinner"),
        "Tu_do":                ("thời gian tự do", "free time"),
        "Di_ngu":               ("đi ngủ", "sleep"),
    }

    # Slot → standard activity name (for matching).
    for asset in sorted((DATA_DIR / "Quests").glob("Quest_*.asset")):
        data = parse_unity_asset(asset)
        if not data:
            continue
        # Parse day & slot from filename: Quest_DD_SS_Name.asset
        m = re.match(r"Quest_(\d{2})_(\d{2})_(.+)", asset.stem)
        if not m:
            continue
        day  = int(m.group(1))
        slot = int(m.group(2))
        name = m.group(3)
        window = data.get("window", {})
        # Resolve area by GUID -> look up later. For now, infer from name+slot.
        area_id = None
        if "Tap_the_duc" in name:   area_id = "SanVanDong"
        elif "Don_ve_sinh" in name: area_id = "DonVeSinh"
        elif "An_" in name or "Nghi_trua" in name: area_id = "NhaAn_Door"
        elif "Hoc_" in name:        area_id = "LopHoc_Door"
        elif "Tu_do" in name:       area_id = "FreeArea"
        elif "Di_ngu" in name:      area_id = "KTX_Door"

        # If quest is a "Hoc_<Subject>_<block>" infer subject.
        subject_id = None
        if name.startswith("Hoc_"):
            sub_block = name[4:]  # "LichSu_b1" or "ChinhTri_b2"
            for s in ("LichSu", "ChinhTri", "GDQuocPhong"):
                if sub_block.startswith(s):
                    subject_id = s
                    break

        # Display activity (Vietnamese).
        display = None
        for k, (vi, _) in activity_map.items():
            if k in name:
                display = vi
                break
        if display is None and subject_id is not None:
            display = f"học {subject_id}"
        if display is None:
            display = name.replace("_", " ").lower()

        quests.append({
            "id":          data.get("id") or f"Q_d{day:02d}_{slot:02d}",
            "day":         day,
            "slot":        slot,
            "title":       data.get("title", display),
            "displayVi":   display,
            "areaId":      area_id,
            "subjectId":   subject_id,
            "startHour":   window.get("startHour", 0),
            "startMinute": window.get("startMinute", 0),
            "endHour":     window.get("endHour", 0),
            "endMinute":   window.get("endMinute", 0),
            "latePenalty": data.get("latePenalty", 0),
            "confirmText": data.get("confirmText", ""),
            "skipToHour":  data.get("skipToHour", 0),
            "skipToMin":   data.get("skipToMinute", 0),
            "assetFile":   asset.name,
        })
    return quests


# ---------- Daily routine summary ----------
def summarize_routine(quests: list) -> dict:
    """Aggregate all 240 quests into the canonical 8-slot daily pattern.
    Returns {slot: {displayVi, areaId, startTime, endTime, subjectId?}}."""
    by_slot = defaultdict(list)
    for q in quests:
        by_slot[q["slot"]].append(q)

    routine = {}
    for slot, qs in sorted(by_slot.items()):
        # All days share same slot pattern (mostly). Take first.
        q0 = qs[0]
        routine[str(slot)] = {
            "slot": slot,
            "displayVi": q0["displayVi"],
            "areaId":    q0["areaId"],
            "startTime": f"{q0['startHour']:02d}:{q0['startMinute']:02d}",
            "endTime":   f"{q0['endHour']:02d}:{q0['endMinute']:02d}",
            "subjects":  sorted({q["subjectId"] for q in qs if q["subjectId"]}),
        }
    return routine


# ---------- Main ----------
def main():
    print(f"[parse] DATA_DIR  = {DATA_DIR}")
    print(f"[parse] OUT_PATH  = {OUT_PATH}")
    if not DATA_DIR.exists():
        raise SystemExit(f"DATA_DIR not found: {DATA_DIR}")

    areas    = parse_areas()
    npcs     = parse_npcs()
    subjects = parse_subjects()
    quests   = parse_quests()
    routine  = summarize_routine(quests)

    out = {
        "schemaVersion": 1,
        "generatedAt":   "2026-06-03",
        "areas":         areas,
        "npcs":          npcs,
        "subjects":      subjects,
        "quests":        quests,
        "routine":       routine,
        "gameMeta": {
            "totalDays":    30,
            "quizzesPerSubject": 6,
            "questionsPerQuiz":  4,
            "subjectsTotal":     3,
        },
    }
    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[parse] wrote {OUT_PATH}")
    print(f"  areas    = {len(areas)}")
    print(f"  npcs     = {len(npcs)}")
    print(f"  subjects = {len(subjects)}")
    print(f"  quests   = {len(quests)}")
    print(f"  routine  = {len(routine)} slots/day")


if __name__ == "__main__":
    main()
