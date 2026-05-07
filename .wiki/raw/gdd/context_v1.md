# CONTEXT HANDOFF — Dự án AI cho ĐATN của Nguyễn Mạnh Quyền

## Vai trò trong dự án

- **Quyền** (CT060236, Học viện KTMM): chủ đề tài, lo phần game Unity
- **Tôi** (người đang hỏi): bạn của Quyền, **chỉ phụ trách train 2 model AI**, KHÔNG làm game
- Đề tài chung: *"Xây dựng trò chơi giáo dục mô phỏng học kỳ quân đội sử dụng Unity + AI"* (ĐATN tốt nghiệp 5 tháng, 1/2026 – 5/2026)

## Tài liệu đã đọc & phân tích

1. **`BCTT_Nguyễn Mạnh Quyền_CT060236.docx`** — Báo cáo thực tập (đã viết xong, đang cần sửa)
2. **`Nguyễn Mạnh Quyền -Đề cương ĐATN.docx`** — Đề cương đồ án TN (đang ở giai đoạn nộp)
3. **`Untitled-2026-05-05-2236.svg`** — Sơ đồ luồng game 30 ngày (Quyền vẽ, scope cực rộng)

## Hiện trạng Quyền

**Đã làm:**
- Đọc tài liệu Unity, Sentis, ML-Agents, PPO ở mức lý thuyết
- Thiết kế *trên giấy* Observation Space, Reward Function
- Viết một số đoạn code minh họa C#

**CHƯA làm:**
- Chưa huấn luyện thực tế ML-Agents (lý do: thiếu GPU)
- Chưa tích hợp Sentis vào Unity (đang dùng LLM API ngoài thay tạm)
- Chưa hoàn thiện game / chưa đo hiệu năng thực tế
- Toàn bộ số liệu trong BCTT (FPS, Reward, độ chính xác 89.1%, khảo sát 20 người) là DỰ KIẾN, không phải đo thật → có mâu thuẫn nội bộ trong báo cáo

**Lỗi copy-paste trong tài liệu** (cần sửa):
- BCTT: mục 2.2 tiêu đề "Tổng quan về tóm tắt văn bản tiếng Việt" nhưng nội dung là Unity Engine
- BCTT: đoạn về MongoDB lạc trong phần đánh giá hiệu năng
- BCTT: phần Kết luận khẳng định "Reward đạt 1.85" nhưng phần 3.2.2 nói chưa train
- Đề cương: mục I.2 "Mục tiêu nghiên cứu" lạc đề hoàn toàn về ROUGE/BERTScore/LLM-as-Judge (không liên quan game)
- Đề cương: tiêu đề "Đối tượng nghiên cứu" trùng nhau (chỗ thứ 2 phải là "Phạm vi/Nội dung")
- Đề cương: tên người ký cuối (TS. Lê Đức Thuận, Thân Nhân Chính) khác trang đầu (TS. Nguyễn Đức Hiếu)

## Quyết định scope đã chốt với Quyền

**Cắt phần "đi học = mini-game"** — không làm 8 mini-game (Rhythm, Quiz, FPS, Tower Defense...). "Vào lớp" chỉ đơn giản là:
```
Đi tới lớp → Bấm E → Fade to black → Pass time → +stats → Quay lại điều khiển
```

**Phạm vi cuối cùng cho ĐATN (5 tháng)**:
- Vòng lặp ngày demo (ưu tiên 7 ngày, không cần 30)
- Tập thể dục sáng (rhythm đơn giản 30s)
- Đi học = fade + cộng stat (không mini-game)
- 3 lựa chọn Player Choice buổi tối
- 2 endings (Pass/Fail)
- AI lính tự đi theo schedule (ML-Agents)
- AI chỉ huy nói chuyện (Sentis hoặc LLM API)
- Hệ thống Adaptive: đo Kỷ luật/Thể lực/Kiến thức

## Phân vai AI (việc của tôi)

| Hệ thống | Vai trò | Công cụ |
|---|---|---|
| **Sentis chat** | NPC chỉ huy hiểu câu hỏi tiếng Việt → phân loại intent → tra response | Python + PyTorch + ONNX |
| **ML-Agents movement** | Lính NPC tự đi từ A → B, tránh chướng ngại | Unity + ML-Agents Toolkit + PPO |

## Kế hoạch chốt: Train độc lập, KHÔNG cần game tồn tại

### Phần A: Sentis chat (Tuần 1-2)

**Hoàn toàn Python, không cần Unity.**

Cần Quyền cung cấp: danh sách 8 intent (đã đề xuất `HOI_LICH`, `HOI_GIO_AN`, `HOI_VI_TRI`, `HOI_KIEN_THUC`, `BAO_CAO`, `XIN_PHEP`, `TAM_BIET`, `OUT_OF_SCOPE`).

Workflow:
1. Tạo dataset CSV ~800-1200 câu (nhờ ChatGPT generate, duyệt tay)
2. Train Transformer/LSTM nhỏ trong PyTorch
3. Export ONNX (`opset_version=15`, `dynamic_axes` cho seq_len)
4. Tạo `vocab.json` + `responses.json`
5. Viết script C# mẫu `NPCDialogueBrain.cs` để Quyền paste vào Unity

**Deliverable**: `intent_model.onnx`, `vocab.json`, `responses.json`, `NPCDialogueBrain.cs`

### Phần B: ML-Agents movement (Tuần 3-8)

**Cần Unity Editor (free) nhưng KHÔNG cần game của Quyền.**

Insight quan trọng: phòng tập (training scene) là môi trường tối giản (1 plane sàn + vài cube chướng ngại + 1 capsule agent + 1 sphere target), HOÀN TOÀN tách biệt với doanh trại game thật. Train xong transplant model vào game.

Cần Quyền chốt qua tin nhắn (không cần file):
```
Observation Space (~25 số):
  - Vị trí Agent (3)
  - Vị trí target (3)
  - Vận tốc (3)
  - 8 raycast 360° × 2 (khoảng cách + tag) (16)

Action Space:
  - forward (-1..1)
  - turn (-1..1)

Agent: cao 1.8m, tốc độ tối đa 3.5 m/s
```

Cách nhanh nhất (đã đề xuất): clone https://github.com/Unity-Technologies/ml-agents, dùng scene mẫu **`Hallway`** hoặc **`PushBlock`** làm template, sửa thành `SoldierTraining.unity`.

Workflow:
1. Cài Unity 2023.2 LTS + `pip install mlagents==0.30.0`
2. Clone repo mẫu, chạy thử scene `Hallway` thành công
3. Sửa thành `SoldierTraining.unity` với spec ở trên, 16 phòng song song
4. Train thử 100k bước (~30 phút) verify Agent có hướng đi đúng
5. Sửa Reward Function nếu Agent ngu (đây là phần nhiều iteration nhất)
6. Train full 5M bước (6-24h với GPU, 1-3 ngày với CPU)
7. Export `Soldier.onnx`
8. Giao Quyền + spec để Quyền cắm vào prefab NPC trong game

Reward Function đề xuất:
```
+1.0  chạm target (kết thúc episode)
+0.001/bước  tiến gần target
-0.0005/bước  đứng yên
-0.1  đụng chướng ngại
-1.0  rơi khỏi map (kết thúc episode)
-0.5  timeout 5000 bước (kết thúc episode)
```

**Deliverable**: `soldier_model.onnx` + `SoldierAgent.cs` + `agent_spec.md`

## Rủi ro / Gotchas đã thảo luận

1. **GPU**: ML-Agents bắt buộc Unity Editor chạy local → KHÔNG train được trên Colab/Kaggle free. Cần GPU local hoặc thuê cloud GPU có Windows + Unity (~$0.5-1/h).
2. **Reward sai**: Train 12h xong Agent vẫn ngu → phải sửa reward, train lại. Mitigation: test 100k bước trước khi full run.
3. **Observation mismatch**: Nếu train với 25 số nhưng game của Quyền tạo 27 số → runtime error. Mitigation: chốt spec bằng văn bản, không nói miệng.
4. **Sentis 2-tầng**: Model chỉ phân loại intent, KHÔNG biết ngữ cảnh game (lịch hôm nay là gì). Quyền phải tự build tầng 2 trong Unity: nhận intent → tra schedule → ghép câu.
5. **Sim-to-real**: Train trong môi trường tối giản → có thể hành xử lạ trong doanh trại thật (kẹt hành lang hẹp...). Mitigation: vòng 2 train lại với phòng tập gần giống doanh trại — chỉ làm nếu còn thời gian.

## Câu hỏi đang chờ Quyền trả lời

1. Quyền có máy GPU không?
2. Quyền chốt được danh sách 8 intent không?
3. Quyền có thời gian dựng training scene cho ML-Agents, hay tôi tự dựng?

## Ngữ cảnh ngôn ngữ & cách trợ giúp mong muốn

- Hội thoại đang dùng **tiếng Việt** xuyên suốt
- Tôi không phải dân kỹ thuật sâu, cần giải thích bằng analogy + sơ đồ ASCII + bảng so sánh
- Quyền đang ở tình trạng cần được giúp **gỡ rối**, không cần lời khen — nhưng cũng không nên overwhelm

---

**Bước tiếp theo dự kiến**: đang chờ Quyền trả lời 3 câu trên để bắt đầu. Khi có spec sẽ cần trợ giúp viết `train.py` (Sentis) và `SoldierAgent.cs` (ML-Agents) cụ thể.
