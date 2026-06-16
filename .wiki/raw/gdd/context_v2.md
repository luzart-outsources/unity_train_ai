# CONTEXT HANDOFF v2 — Dự án AI cho ĐATN của Nguyễn Mạnh Quyền
**Cập nhật: 2026-05-07** (kế thừa từ `context_ai_quyen.md` v1)

> File này thay thế hoàn toàn v1. Trong v1 mới ở giai đoạn "lên kế hoạch trên giấy". V2 ghi lại tiến độ thực thi: môi trường training đã setup xong, pipeline Phase A chạy thông end-to-end, và phát hiện một vấn đề chất lượng dữ liệu cần xử lý tiếp.

---

## 1. Vai trò trong dự án

- **Quyền** (CT060236, Học viện KTMM): chủ đề tài, lo phần game Unity. Quyền đã xác nhận với tôi: **"chỉ cần training AI thôi, phần còn lại Quyền không cần làm game"** — tức tôi KHÔNG đụng vào game Unity, chỉ giao deliverable AI.
- **Tôi** (người đang hỏi): bạn của Quyền, chỉ phụ trách train 2 model AI, không phải dân kỹ thuật sâu, hội thoại bằng tiếng Việt.
- Đề tài chung: *"Xây dựng trò chơi giáo dục mô phỏng học kỳ quân đội sử dụng Unity + AI"* (ĐATN 5 tháng, 1/2026 – 5/2026, **deadline cuối tháng 5/2026 → còn ~3 tuần**).

## 2. Phân vai 2 model AI (việc của tôi)

| Hệ thống | Vai trò | Công cụ | Trạng thái 2026-05-07 |
|---|---|---|---|
| **Sentis chat (Phase A)** | NPC chỉ huy hiểu tiếng Việt → phân loại intent → tra response | Python + PyTorch + ONNX | ⚙️ **Pipeline xong, data cần upgrade** |
| **ML-Agents movement (Phase B)** | Lính NPC tự đi từ A → B, tránh chướng ngại | Unity 2022.3 + ML-Agents + PPO | ⏳ **Chưa bắt đầu** |

## 3. Môi trường máy đã verify

| Thành phần | Trạng thái | Chi tiết |
|---|---|---|
| OS | ✅ Windows | working dir: `D:\GithubUnity\UnityTrainAI\Assets` |
| Python hệ thống | ✅ 3.10.11 | `C:\Users\admin\AppData\Local\Programs\Python\Python310\python.exe` |
| GPU | ✅ NVIDIA GTX 1060 6GB Max-Q | Compute cap 6.1, driver 577.00, CUDA runtime 12.9 |
| Unity Editor | ✅ 2022.3.62f2 (đã cài) | Đã có Unity Hub + Editor đang chạy |
| Unity project | ✅ tồn tại | `D:\GithubUnity\UnityTrainAI\` (Assets/Library/Packages/ProjectSettings) |
| Phase A venv | ✅ tạo xong | `D:\GithubUnity\UnityTrainAI\AI_Training\phase_a_sentis\.venv\` |
| Phase A libs | ✅ cài xong | torch 2.5.1+cu121, pandas 2.2.3, sklearn 1.5.2, onnx 1.17.0, onnxruntime 1.20.1, underthesea 6.8.4, sentencepiece 0.2.0 |
| `torch.cuda.is_available()` | ✅ True | matmul GPU smoke test OK |

**Lưu ý điều chỉnh so với v1**:
- v1 đề xuất Unity 2023.2 LTS → đã chuyển sang **2022.3.62f2** (sẵn trên máy, ml-agents Unity package 2.0+ vẫn tương thích).
- v1 đề xuất `mlagents==0.30.0` → vẫn giữ kế hoạch này cho Phase B nhưng chưa cài.

## 4. Cấu trúc folder đã tạo cho Phase A

```
D:\GithubUnity\UnityTrainAI\
├── Assets\                       (Quyền sở hữu — Unity quét)
├── Library\ Packages\ ProjectSettings\
└── AI_Training\                  ★ TÔI sở hữu — Unity bỏ qua
    └── phase_a_sentis\
        ├── .venv\                Python 3.10 + torch + libs (~2.7GB)
        ├── .gitignore
        ├── requirements.txt
        ├── data\
        │   ├── intents.csv               (40 câu seed viết tay, 8 intent)
        │   └── intents_expanded.csv      (529 câu sinh từ template)
        ├── models\
        │   ├── fasttext_best.pt
        │   ├── fasttext_intent.onnx           ★ Deliverable Quyền
        │   ├── fasttext_intent_meta.json     ★ vocab+label kèm theo
        │   ├── vocab.json id2label.json label2id.json training_log.json
        └── scripts\
            ├── test_cuda.py       verify GPU
            ├── dataset.py         tokenizer (underthesea fallback split) + vocab builder
            ├── model.py           3 arch: FastText / LSTM / TinyTransformer
            ├── expand_dataset.py  sinh data từ template (chỉ baseline, chưa đủ ngon)
            ├── train.py           training loop, lưu best checkpoint
            ├── export_onnx.py     export opset 15, dynamic seq_len, verify vs PyTorch
            └── predict.py         interactive sanity-check
```

**Lý do tách `AI_Training\` ngoài `Assets\`**: Unity AssetDatabase scan toàn bộ Assets để gen `.meta` cho mỗi file. Dataset CSV + checkpoint PyTorch (vài trăm MB) sẽ làm Editor lag và Library phình to. Quy ước cộng đồng: training code đi cạnh project, KHÔNG bên trong Assets.

## 5. 8 Intent chuẩn (chưa Quyền duyệt nhưng đã dùng để train)

```
HOI_LICH        — hỏi về lịch / thời khoá biểu
HOI_GIO_AN      — hỏi giờ ăn cơm
HOI_VI_TRI      — hỏi địa điểm (nhà ăn, phòng tập, lớp...)
HOI_KIEN_THUC   — hỏi về kiến thức quân đội (súng AK, điều lệnh...)
BAO_CAO         — báo cáo lên cấp trên
XIN_PHEP        — xin phép vắng mặt / ra ngoài
TAM_BIET        — chào kết thúc
OUT_OF_SCOPE    — câu không liên quan (fallback)
```

## 6. Kết quả train Phase A baseline

Train FastText 30 epochs trên `intents_expanded.csv` (529 câu, 8 intent):
- **val_acc = 1.00** (100%) — **NHƯNG ĐÂY LÀ MISLEADING**
- Train time: 25.6s trên GTX 1060
- Model size: 12,904 params, ONNX 52KB

### ⚠️ Phát hiện quan trọng — model overfit nặng

Test trên 8 câu thật **chưa từng có trong training set**:

| Câu thật | Đúng | Model đoán | Đúng/Sai |
|---|---|---|---|
| Hôm nay có lịch gì | HOI_LICH | HOI_KIEN_THUC 60% | ❌ |
| Mấy giờ thì ăn cơm | HOI_GIO_AN | HOI_KIEN_THUC 78% | ❌ |
| Nhà ăn nằm đâu | HOI_VI_TRI | HOI_KIEN_THUC 78% | ❌ |
| Quy tắc bắn 3 điểm là gì | HOI_KIEN_THUC | HOI_KIEN_THUC 59% | ✅ |
| Báo cáo đầy đủ | BAO_CAO | HOI_KIEN_THUC 78% | ❌ |
| Cho em xin nghỉ | XIN_PHEP | TAM_BIET 51% | ❌ |
| Chào thủ trưởng | TAM_BIET | HOI_KIEN_THUC 78% | ❌ |
| Messi đá hay không | OUT_OF_SCOPE | HOI_KIEN_THUC 39% | ❌ |

**Tỷ lệ đúng: 1/8 = 12.5%** = bằng random với 8 class.

**Nguyên nhân**: vocab chỉ có 165 từ (sinh từ ~50 template). Câu thật có nhiều từ chưa từng thấy → tất cả thành UNK → model fallback về class chiếm ưu thế (`HOI_KIEN_THUC`).

**Bài học**: 100% val_acc trên synthetic templated data ≠ generalize được. Val set phải đa dạng bằng "thế giới thật" mới đo được chất lượng thực.

## 7. Kế hoạch upgrade chất lượng (đã thảo luận, chưa thực thi)

| # | Bước | Tác động kỳ vọng | Effort |
|---|---|---|---|
| 1 | **Sinh data đa dạng bằng LLM** (ChatGPT/Claude) — 100-150 câu/intent với từ ngữ đời thường | val_acc thật 80-90% | 2-4h prompt + duyệt |
| 2 | Cân bằng số câu mỗi intent | giảm bias | 30 phút |
| 3 | Augmentation tự động (typo, đảo từ, cắt ngắn) | + robustness | 1h |
| 4 | Đổi sang LSTM (đã setup sẵn) | + 2-5% acc | đổi 1 flag |
| 5 | Đổi sang TinyTransformer (đã setup sẵn) | + 3-7% acc | đổi flag |
| 6 | PhoBERT/ViSoBERT pretrained | 95%+ nhưng có thể quá nặng cho Sentis | nhiều |

**Insight quan trọng**: Bước 1 (data) > đổi kiến trúc. Garbage in → garbage out. Tiny Transformer ~400K params là trần hợp lý cho Sentis Unity Editor.

## 8. Pending — đang chờ user quyết định

Khi tôi gom context xong, user đang phải chọn 1 trong 4 hướng:

| Lựa chọn | Mô tả | Thời gian |
|---|---|---|
| **A.** Tôi viết prompt LLM → user paste sang ChatGPT/Claude sinh 1000 câu | 30 phút prompt + 2h duyệt |
| **B.** Tôi cải tiến `expand_dataset.py` (vẫn synthetic) | 1h |
| **C.** Setup Phase B (ML-Agents) song song | 1-2h |
| **D.** Train LSTM/Transformer trên data hiện tại để baseline | 5 phút |

**Khuyến nghị tôi đề xuất: A** — đòn bẩy cao nhất.

## 9. Pending từ Quyền (vẫn chưa giải)

1. ✅ ~~Quyền có muốn user làm game?~~ — **ĐÃ GIẢI**: Quyền nói chỉ cần train AI.
2. ❓ Quyền chốt 8 intent này hay đổi? — đang dùng tạm list trên.
3. ⚠️ Quyền có GPU chạy ML-Agents không? — không quan trọng nữa vì user có GTX 1060 (đã verify).

## 10. Phase B (ML-Agents) — kế hoạch chưa thực thi

Giữ nguyên từ v1, điều chỉnh nhẹ:

```
Observation Space (~25 số):
  - Vị trí Agent (3) + Vị trí target (3) + Vận tốc (3)
  - 8 raycast 360° × 2 (khoảng cách + tag) = 16

Action Space:
  - forward (-1..1)
  - turn (-1..1)

Agent: cao 1.8m, tốc độ tối đa 3.5 m/s
```

Reward Function đề xuất:
```
+1.0   chạm target (kết thúc episode)
+0.001/bước  tiến gần target
-0.0005/bước đứng yên
-0.1   đụng chướng ngại
-1.0   rơi khỏi map (kết thúc episode)
-0.5   timeout 5000 bước (kết thúc episode)
```

Workflow đề xuất:
1. `pip install mlagents==0.30.0` trong venv riêng `phase_b_mlagents/.venv`
2. Clone `https://github.com/Unity-Technologies/ml-agents`, dùng scene `Hallway` hoặc `PushBlock` làm template
3. Sửa thành `SoldierTraining.unity` (Unity 2022.3.62f2 — không phải 2023.2 như v1 nói)
4. Train thử 100k bước (~30 phút) verify
5. Tune reward, train full 5M bước (8-15h trên GTX 1060)
6. Export `Soldier.onnx` + viết `SoldierAgent.cs` mẫu + `agent_spec.md`

**Ước tính trên GTX 1060 6GB Max-Q**: chậm 3-4x so với GPU đời mới → 5M bước mất ~10-15h, nên train qua đêm.

## 11. Lệnh nhanh để resume Phase A

Tất cả từ folder `D:\GithubUnity\UnityTrainAI\AI_Training\phase_a_sentis\`:

```bash
# Verify môi trường
.venv/Scripts/python.exe scripts/test_cuda.py

# Sinh data từ template (đã chạy, ra 529 câu)
.venv/Scripts/python.exe scripts/expand_dataset.py --target 120

# Train (đã chạy, val_acc 1.00 nhưng overfit)
.venv/Scripts/python.exe scripts/train.py --arch fasttext --epochs 30 --data data/intents_expanded.csv

# Export ONNX (đã chạy)
.venv/Scripts/python.exe scripts/export_onnx.py --arch fasttext

# Test interactive (đã dùng để phát hiện overfit)
.venv/Scripts/python.exe scripts/predict.py --arch fasttext --top_k 2

# Đổi arch khi data tốt hơn:
.venv/Scripts/python.exe scripts/train.py --arch lstm --epochs 40 --data data/intents_better.csv
.venv/Scripts/python.exe scripts/train.py --arch transformer --epochs 50 --data data/intents_better.csv
```

## 12. Lỗi tài liệu của Quyền (kế thừa từ v1, chưa sửa)

Vẫn cần sửa nếu user có thời gian:

**BCTT (`BCTT_Nguyễn Mạnh Quyền_CT060236.docx`)**:
- Mục 2.2 tiêu đề "Tổng quan về tóm tắt văn bản tiếng Việt" nhưng nội dung là Unity Engine (sai tiêu đề)
- Đoạn về MongoDB lạc trong phần đánh giá hiệu năng
- Phần Kết luận khẳng định "Reward đạt 1.85" nhưng phần 3.2.2 nói chưa train (mâu thuẫn)
- Toàn bộ số liệu (FPS, Reward, độ chính xác 89.1%, khảo sát 20 người) là DỰ KIẾN không đo thật

**Đề cương (`Nguyễn Mạnh Quyền -Đề cương ĐATN.docx`)**:
- Mục I.2 "Mục tiêu nghiên cứu" lạc về ROUGE/BERTScore/LLM-as-Judge (không liên quan game)
- Tiêu đề "Đối tượng nghiên cứu" trùng nhau (chỗ thứ 2 phải là "Phạm vi/Nội dung")
- Tên người ký cuối (TS. Lê Đức Thuận, Thân Nhân Chính) khác trang đầu (TS. Nguyễn Đức Hiếu)

## 13. Quyết định scope đã chốt (kế thừa v1)

**Đã CẮT**: 8 mini-game (Rhythm, Quiz, FPS, Tower Defense...) khi "đi học". Vào lớp giờ chỉ là `Đi tới lớp → Bấm E → Fade to black → Pass time → +stats → Quay lại điều khiển`.

**Phạm vi cuối cùng**:
- Vòng lặp ngày demo (ưu tiên 7 ngày, không bắt buộc 30)
- Tập thể dục sáng (rhythm đơn giản 30s)
- Đi học = fade + cộng stat
- 3 lựa chọn Player Choice buổi tối
- 2 endings (Pass / Fail)
- AI lính tự đi theo schedule (ML-Agents) — Phase B
- AI chỉ huy nói chuyện (Sentis) — Phase A
- Hệ thống Adaptive: đo Kỷ luật / Thể lực / Kiến thức

## 14. Risks (kế thừa v1, cập nhật trạng thái)

| # | Risk | Trạng thái 2026-05-07 |
|---|---|---|
| 1 | GPU thiếu | ✅ Đã giải (GTX 1060 6GB) |
| 2 | Reward sai (Phase B) | ⏳ Chưa relevant (chưa train B) |
| 3 | Observation mismatch | ⏳ Sẽ cần `agent_spec.md` văn bản |
| 4 | Sentis 2-tầng (model chỉ phân loại intent, KHÔNG biết ngữ cảnh) | ⏳ Quyền tự build tầng 2 trong Unity |
| 5 | Sim-to-real (Phase B) | ⏳ Chỉ làm vòng 2 nếu còn thời gian |
| **6 NEW** | **Overfit trên synthetic data** | ⚠️ **Phát hiện 7/5/2026, đang xử lý** |

## 15. Cách trợ giúp mong muốn (kế thừa v1)

- Hội thoại bằng **tiếng Việt** xuyên suốt
- User không phải dân kỹ thuật sâu → analogy + sơ đồ ASCII + bảng so sánh
- Quyền cần được giúp gỡ rối, không cần lời khen
- Không nên overwhelm bằng quá nhiều chi tiết cùng lúc

---

## Bước tiếp theo dự kiến

User đang chọn 1 trong 4 hướng (A/B/C/D ở mục 8). Khi chat mới mở:

1. Đọc xong file này.
2. Hỏi user "Bạn đã chọn A/B/C/D chưa, hay muốn tôi recap lại?"
3. Nếu **A**: viết prompt cho ChatGPT/Claude sinh data — cấu trúc CSV `text,intent` để paste thẳng vào `data/intents_v2.csv`.
4. Nếu **B**: edit `scripts/expand_dataset.py` thêm template + augmentation.
5. Nếu **C**: tạo `phase_b_mlagents/`, `pip install mlagents==0.30.0`, clone `Unity-Technologies/ml-agents`.
6. Nếu **D**: chạy `train.py --arch lstm` và `--arch transformer` trên data hiện tại để so sánh, biết là vẫn sẽ overfit.
