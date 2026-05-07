from __future__ import annotations
import json
from pathlib import Path

import sys
sys.path.insert(0, str(Path(__file__).parent))
import generate_dataset_v4 as gen

# Up to project root TrainAI_Unity/
ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets" / "AI" / "Resources" / "slot_vocab.json"

def dedupe_lower(items: list[str]) -> list[str]:
    seen = set()
    out = []
    for x in items:
        k = x.strip().lower()
        if not k or k in seen:
            continue
        seen.add(k)
        out.append(k)
    # Sort by length desc so longest-match works in C# (matters for greedy)
    out.sort(key=lambda s: (-len(s.split()), -len(s), s))
    return out

def main():
    payload = {
        "_README": (
            "Slot extractor vocabulary. Each list is sorted by token-count desc "
            "so a longest-match scan picks 'khu B5' over 'khu' etc. Used by "
            "EntityExtractor.cs in the v2 NPC brain."
        ),
        "place": dedupe_lower(gen.PLACES),
        "time":  dedupe_lower(gen.TIMES),
        "topic": dedupe_lower(gen.KNOWLEDGE),
        "meal":  dedupe_lower(gen.MEALS),
        "reason": dedupe_lower(gen.REASONS),
        "report": dedupe_lower(gen.REPORT_OBJ),
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)
    print(f"[dump] {OUT}")
    for k, v in payload.items():
        if k.startswith("_"):
            continue
        print(f"{k:<8} {len(v)} entries")

if __name__ == "__main__":
    main()
