from __future__ import annotations
import torch
import torch.nn as nn

class FastTextClassifier(nn.Module):
    """Embedding + mean-pool + 2 Linear. Tiny (~150K params), fast, robust ONNX.

    Good baseline. Reaches 85-92% on clean intent data with 1k samples.
    """

    def __init__(self, vocab_size: int, num_classes: int, embed_dim: int = 64, hidden: int = 32, pad_idx: int = 0):
        super().__init__()
        self.embed = nn.Embedding(vocab_size, embed_dim, padding_idx=pad_idx)
        self.fc1 = nn.Linear(embed_dim, hidden)
        self.act = nn.ReLU()
        self.dropout = nn.Dropout(0.2)
        self.fc2 = nn.Linear(hidden, num_classes)
        self.pad_idx = pad_idx

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        # x: (B, L)
        emb = self.embed(x)                        # (B, L, E)
        mask = (x != self.pad_idx).unsqueeze(-1).float()  # (B, L, 1)
        summed = (emb * mask).sum(dim=1)           # (B, E)
        denom = mask.sum(dim=1).clamp(min=1.0)     # (B, 1)
        pooled = summed / denom                    # (B, E)
        h = self.act(self.fc1(pooled))
        h = self.dropout(h)
        return self.fc2(h)                         # (B, C)

class LSTMClassifier(nn.Module):
    """Bidirectional LSTM. ~250K params. Better on long sentences with word order.

    Reaches 90-95% on clean intent data.
    """

    def __init__(self, vocab_size: int, num_classes: int, embed_dim: int = 64, hidden: int = 64, pad_idx: int = 0):
        super().__init__()
        self.embed = nn.Embedding(vocab_size, embed_dim, padding_idx=pad_idx)
        self.lstm = nn.LSTM(embed_dim, hidden, batch_first=True, bidirectional=True)
        self.dropout = nn.Dropout(0.3)
        self.fc = nn.Linear(hidden * 2, num_classes)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        emb = self.embed(x)               # (B, L, E)
        out, _ = self.lstm(emb)           # (B, L, 2H)
        pooled = out.mean(dim=1)          # (B, 2H)
        return self.fc(self.dropout(pooled))

class TinyTransformer(nn.Module):
    """Small Transformer encoder. ~400K params. Best accuracy on noisy data."""

    def __init__(self, vocab_size: int, num_classes: int, embed_dim: int = 64, nhead: int = 4, num_layers: int = 2, pad_idx: int = 0):
        super().__init__()
        self.embed = nn.Embedding(vocab_size, embed_dim, padding_idx=pad_idx)
        self.pos = nn.Embedding(64, embed_dim)  # max_len cap
        layer = nn.TransformerEncoderLayer(embed_dim, nhead, dim_feedforward=128, batch_first=True, dropout=0.2)
        self.encoder = nn.TransformerEncoder(layer, num_layers=num_layers)
        self.fc = nn.Linear(embed_dim, num_classes)
        self.pad_idx = pad_idx

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        B, L = x.shape
        pos_ids = torch.arange(L, device=x.device).unsqueeze(0).expand(B, L)
        h = self.embed(x) + self.pos(pos_ids)
        mask = (x == self.pad_idx)
        h = self.encoder(h, src_key_padding_mask=mask)
        # mean pool over non-pad positions
        keep = (~mask).unsqueeze(-1).float()
        pooled = (h * keep).sum(dim=1) / keep.sum(dim=1).clamp(min=1.0)
        return self.fc(pooled)

def build_model(arch: str, vocab_size: int, num_classes: int) -> nn.Module:
    arch = arch.lower()
    if arch == "fasttext":
        return FastTextClassifier(vocab_size, num_classes)
    if arch == "lstm":
        return LSTMClassifier(vocab_size, num_classes)
    if arch == "transformer":
        return TinyTransformer(vocab_size, num_classes)
    raise ValueError(f"Unknown arch: {arch}")
