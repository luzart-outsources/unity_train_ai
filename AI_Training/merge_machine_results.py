from __future__ import annotations
import argparse
import json
import shutil
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent
DELIVERABLES = ROOT / "deliverables"
DELIVERABLES_M2 = ROOT / "deliverables_m2"

STATE_M1 = ROOT / "overnight_v3_state.json"
STATE_M2 = ROOT / "overnight_v3_m2_state.json"

def load_state(p: Path) -> dict | None:
    if not p.exists():
        return None
    with open(p, encoding="utf-8") as f:
        return json.load(f)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--report-only", action="store_true",
                    help="Chỉ ghi báo cáo, KHÔNG copy ONNX (dry-run)")
    args = ap.parse_args()

    s1 = load_state(STATE_M1)
    s2 = load_state(STATE_M2)

    if s1 is None and s2 is None:
        print("Không tìm thấy state file nào. Loop chưa chạy?")
        return

    lines = ["# MERGED REPORT — End of Day", "", f"Generated: {datetime.now().isoformat()}", ""]

    a1 = s1["best_phase_a"] if s1 else None
    a2 = s2["best_phase_a"] if s2 else None
    a1_acc = a1["acc"] if a1 else 0.0
    a2_acc = a2["acc"] if a2 else 0.0

    lines.append("## Phase A — Intent Classifier")
    lines.append("")
    lines.append("| Machine | Best acc | Arch | Iter | Data seed |")
    lines.append("|---|---|---|---|---|")
    if a1: lines.append(f"| Máy 1 | {a1['acc']*100:.2f}% | {a1.get('arch','?')} | {a1['iter']} | {a1.get('data_seed','?')} |")
    if a2: lines.append(f"| Máy 2 | {a2['acc']*100:.2f}% | {a2.get('arch','?')} | {a2['iter']} | {a2.get('data_seed','?')} |")

    if a1_acc > a2_acc:
        winner_a, winner_a_state = "m1", a1
        a_src_dir = DELIVERABLES
    elif a2_acc > a1_acc:
        winner_a, winner_a_state = "m2", a2
        a_src_dir = DELIVERABLES_M2
    else:
        winner_a, winner_a_state = "tie (m1 default)", a1
        a_src_dir = DELIVERABLES
    lines.append("")
    lines.append(f"**Winner: {winner_a}** ({winner_a_state['acc']*100:.2f}%, {winner_a_state.get('arch','?')})")
    lines.append("")

    b1 = s1["best_phase_b"] if s1 else None
    b2 = s2["best_phase_b"] if s2 else None
    b1_r = b1["reward"] if b1 else -1e9
    b2_r = b2["reward"] if b2 else -1e9

    lines.append("## Phase B — Movement PPO")
    lines.append("")
    lines.append("| Machine | Best reward | Iter | Seed | HP/tag |")
    lines.append("|---|---|---|---|---|")
    if b1: lines.append(f"| Máy 1 | {b1['reward']:.3f} | {b1['iter']} | {b1.get('seed','?')} | {b1.get('tag','?')} |")
    if b2: lines.append(f"| Máy 2 | {b2['reward']:.3f} | {b2['iter']} | {b2.get('seed','?')} | {b2.get('tag','?')} (hp={b2.get('hp','?')}) |")

    if b1_r > b2_r:
        winner_b, winner_b_state = "m1", b1
        b_src_file = DELIVERABLES / "soldier.onnx"
    elif b2_r > b1_r:
        winner_b, winner_b_state = "m2", b2
        b_src_file = DELIVERABLES_M2 / "soldier_m2.onnx"
    else:
        winner_b, winner_b_state = "tie (m1 default)", b1
        b_src_file = DELIVERABLES / "soldier.onnx"
    lines.append("")
    lines.append(f"**Winner: {winner_b}** ({winner_b_state['reward']:.3f})")
    lines.append("")

    if s2 and "best_phase_b_per_hp" in s2:
        lines.append("### Máy 2 — per-HP breakdown")
        lines.append("")
        lines.append("| HP | Best reward | Iter | Seed |")
        lines.append("|---|---|---|---|")
        for name, b in s2["best_phase_b_per_hp"].items():
            lines.append(f"| {name} | {b['reward']:.3f} | {b['iter']} | {b.get('seed','?')} |")
        lines.append("")

    lines.append("## Iteration counts")
    if s1: lines.append(f"- Máy 1: {s1['iter']} iter ({len(s1.get('history',[]))} entries in history)")
    if s2: lines.append(f"- Máy 2: {s2['iter']} iter ({len(s2.get('history',[]))} entries in history)")
    lines.append("")

    lines.append("## Canonical deliverables (after merge)")
    lines.append("")
    if not args.report_only:
        # Phase A canonical
        if winner_a == "m2":
            arch = winner_a_state.get("arch", "lstm")
            for fname in (f"{arch}_intent.onnx", f"{arch}_intent_meta.json"):
                src = a_src_dir / fname
                if src.exists():
                    shutil.copy2(src, DELIVERABLES / "intent_classifier.onnx" if "onnx" in fname else DELIVERABLES / "intent_classifier_meta.json")
            print(f"[merge] Phase A canonical = m2 ({winner_a_state['acc']*100:.2f}%)")
        else:
            print(f"[merge] Phase A canonical = m1 ({winner_a_state['acc']*100:.2f}%) — already in place")

        # Phase B canonical
        if winner_b == "m2":
            if b_src_file.exists():
                shutil.copy2(b_src_file, DELIVERABLES / "soldier.onnx")
                m2_meta = b_src_file.with_suffix(".meta.json")
                if m2_meta.exists():
                    shutil.copy2(m2_meta, DELIVERABLES / "soldier.meta.json")
                print(f"[merge] Phase B canonical = m2 ({winner_b_state['reward']:.3f})")
        else:
            print(f"[merge] Phase B canonical = m1 ({winner_b_state['reward']:.3f}) — already in place")

    lines.append(f"- `intent_classifier.onnx` <- {winner_a} winner")
    lines.append(f"- `soldier.onnx` <- {winner_b} winner")
    lines.append("")

    out_path = DELIVERABLES / "MERGED_REPORT.md"
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"[merge] report -> {out_path}")
    if args.report_only:
        print("[merge] --report-only: KHÔNG copy ONNX (dry-run)")

if __name__ == "__main__":
    main()
