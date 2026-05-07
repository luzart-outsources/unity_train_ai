from __future__ import annotations
import json
import re
from collections import Counter
from pathlib import Path
from typing import List, Tuple

import pandas as pd
import torch
from torch.utils.data import Dataset

PAD_TOKEN = "<pad>"
UNK_TOKEN = "<unk>"

def vi_tokenize(text: str) -> List[str]:
    """Tokenize Vietnamese text. Lowercase + word segmentation."""
    text = text.lower().strip()
    text = re.sub(r"[^\wàáâãèéêìíòóôõùúýăđĩũơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ\s]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    try:
        from underthesea import word_tokenize
        return word_tokenize(text)
    except Exception:
        return text.split()

def build_vocab(texts: List[str], min_freq: int = 1, max_size: int = 5000) -> dict:
    """Return token -> id mapping. Reserves 0 for PAD, 1 for UNK."""
    counter = Counter()
    for t in texts:
        counter.update(vi_tokenize(t))
    vocab = {PAD_TOKEN: 0, UNK_TOKEN: 1}
    for tok, freq in counter.most_common(max_size - 2):
        if freq < min_freq:
            break
        vocab[tok] = len(vocab)
    return vocab

def encode(text: str, vocab: dict, max_len: int = 32) -> List[int]:
    ids = [vocab.get(tok, vocab[UNK_TOKEN]) for tok in vi_tokenize(text)]
    ids = ids[:max_len]
    ids += [vocab[PAD_TOKEN]] * (max_len - len(ids))
    return ids

class IntentDataset(Dataset):
    """Pre-encodes all texts once at init — avoids re-tokenizing per epoch.

    On 16k samples this is the difference between ~9 min and ~1.5 min for a
    30-epoch FastText run, since underthesea word_tokenize is the dominant
    cost (~0.5 ms/sentence × 16000 × 30 epochs ≈ 240 sec saved).
    """

    def __init__(self, df: pd.DataFrame, vocab: dict, label2id: dict, max_len: int = 32):
        self.labels = df["intent"].tolist()
        self.label2id = label2id
        self.max_len = max_len
        # Pre-encode once
        texts = df["text"].tolist()
        self._x = torch.tensor(
            [encode(t, vocab, max_len) for t in texts], dtype=torch.long
        )
        self._y = torch.tensor(
            [label2id[l] for l in self.labels], dtype=torch.long
        )

    def __len__(self) -> int:
        return self._x.shape[0]

    def __getitem__(self, idx: int) -> Tuple[torch.Tensor, torch.Tensor]:
        return self._x[idx], self._y[idx]

def load_csv(path: str | Path) -> pd.DataFrame:
    df = pd.read_csv(path)
    df = df.dropna().reset_index(drop=True)
    df["text"] = df["text"].astype(str).str.strip()
    df["intent"] = df["intent"].astype(str).str.strip()
    return df

def save_json(obj, path: str | Path) -> None:
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
