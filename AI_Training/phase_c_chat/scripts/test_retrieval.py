"""
Phase C — Quick retrieval sanity test.

Loads the embedding index and queries it with hand-crafted Vietnamese
sentences spanning all intents + augmentation variants. Prints top-3
hits per query with similarity scores.
"""
import json
import time
import numpy as np
from pathlib import Path
from sentence_transformers import SentenceTransformer

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"

EMB = np.load(MODELS_DIR / "embeddings.npy")
META = json.loads((MODELS_DIR / "qa_metadata.json").read_text(encoding="utf-8"))
INFO = json.loads((MODELS_DIR / "index_info.json").read_text(encoding="utf-8"))
print(f"[test] loaded {EMB.shape}, model = {INFO['modelName']}")

model = SentenceTransformer(INFO["modelName"])


def retrieve(query: str, k: int = 3):
    """Encode query, cosine sim against bank, return top-k."""
    q = model.encode([query], normalize_embeddings=True, convert_to_numpy=True).astype(np.float32)[0]
    sims = EMB @ q  # cosine sim because both are L2-normalized
    idx = np.argsort(-sims)[:k]
    return [(float(sims[i]), META[i]) for i in idx]


def show(query: str):
    print(f"\nQ: {query!r}")
    t0 = time.time()
    hits = retrieve(query, k=3)
    dt = (time.time() - t0) * 1000
    print(f"   ({dt:.1f}ms)")
    for rank, (score, m) in enumerate(hits, 1):
        flag = "**" if rank == 1 else "  "
        print(f"  {flag} #{rank} {score:.3f} [{m['intent']:18s} | {m['entityId']:15s}] {m['question']}")
        if rank == 1:
            print(f"        -> A: {m['answer']}")


tests = [
    # ASK_LOCATION variants — the original bug scenario
    "khu thể dục ở đâu",
    "khu sân vận động ở đâu",       # exact match
    "san van dong o dau",            # no accent
    "Svd chỗ nào ạ",                 # abbrev + slang
    "Cho em hỏi sân tập ở đâu ạ",
    "Where is gym",                  # code-mix
    "Sân bóng đá ở chỗ nào",         # alias
    # different entity to verify NOT collapsing
    "ký túc xá ở đâu",
    "nhà ăn ở đâu",
    "lớp học chỗ nào",
    "khu tự do hướng nào",
    "khu dọn vệ sinh đi lối nào",
    # ASK_TIME
    "mấy giờ ăn cơm",
    "tập thể dục lúc mấy giờ",
    "Hôm nay học môn gì",
    # NPC
    "Đại đội trưởng là ai",
    "Thủ trưởng là ai",
    "ddt la ai",                      # no accent + abbrev
    # subject
    "Lịch sử là môn gì",
    "GDQP học những gì",
    # chitchat
    "Em chào thủ trưởng",
    "Cảm ơn anh",
    "Em xin phép về",
    # OOS / adversarial
    "Crypto giảm sốc",
    "iPhone giá bao nhiêu",
    "Anh có người yêu chưa",
    # The ORIGINAL bug
    "khu A1 ở đâu",                  # not in game data — should return low-similarity or related
    "khu A2 ở đâu",
    "khu B ở đâu",
]

for q in tests:
    show(q)
