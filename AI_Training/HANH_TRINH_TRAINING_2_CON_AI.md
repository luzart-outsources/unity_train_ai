# Hành trình tôi tự train 2 con AI cho ĐATN

> Bản thuyết minh dạng kể chuyện về quá trình tôi xây dựng và huấn luyện 2 hệ AI cho game *"Học kỳ quân đội — KTMM"* trên Unity. Tôi viết để người đọc hiểu được tôi đã trải qua những giai đoạn nào, mắc lỗi gì, fix ra sao, và vì sao cuối cùng tôi chọn những hướng đi như hiện tại. Văn phong là cách tôi sẽ trình bày trực tiếp trước hội đồng — nói thật, không nhấn mạnh quá đà, có gì sai thì kể lại đúng như vậy.

---

## 1. Bối cảnh — vì sao cần 2 con AI

Đề tài tốt nghiệp của tôi là làm **một game giáo dục mô phỏng học kỳ quân đội** trên Unity, tức là người chơi vào vai sinh viên KTMM bị đưa lên huấn luyện ở doanh trại. Lúc xây kịch bản, tôi nhận ra có **2 chỗ nếu code tay theo kiểu if/else thì sẽ rất giả**:

1. **Sĩ quan chỉ huy NPC** phải hiểu được người chơi gõ tiếng Việt vào để hỏi lịch, hỏi giờ ăn, xin phép, báo cáo, v.v. Code tay từng câu thì viết đến đời nào cũng không xong, mà người chơi gõ chệch một chữ là vỡ trận.
2. **Lính NPC** phải tự đi từ điểm A đến điểm B trong doanh trại, tránh chướng ngại vật một cách tự nhiên. Nếu tôi viết pathfinding A* tay thì đường đi cứng, không giống lính thật.

Tôi gọi 2 phần này lần lượt là **Phase A — Sentis Chat** (intent classification) và **Phase B — Movement AI** (reinforcement learning navigation). Cả hai đều phải chạy được offline trong Unity 6 qua package `com.unity.ai.inference 2.6.1` (tức Sentis cũ, đã đổi tên), nghĩa là **model phải export ra ONNX**, vì Sentis chỉ ăn ONNX.

Trước khi vào chi tiết, có một quyết định scope quan trọng: ban đầu đề cương của tôi có **8 mini-game** khi đi học (Rhythm, Quiz, FPS, Tower Defense...). Sau khi nhẩm thời gian, tôi nhận thấy 1 mình + 5 tháng không đủ. Tôi cắt hết về thành "vào lớp = fade to black + cộng stats", dồn toàn lực vào 2 hệ AI và vòng lặp ngày demo. Đó là quyết định đầu tiên tôi tự chịu trách nhiệm về phạm vi.

---

## 2. Phase A — NPC Sĩ quan chỉ huy hiểu tiếng Việt

### 2.1. Bài toán đặt ra

Tôi cần model **phân loại 1 câu tiếng Việt người chơi gõ ra thành 1 trong 8 intent**:

| Intent | Người chơi định hỏi gì |
|---|---|
| `HOI_LICH` | Hôm nay/ngày mai có lịch gì, tập gì |
| `HOI_GIO_AN` | Mấy giờ ăn cơm, bao giờ phát suất ăn |
| `HOI_VI_TRI` | Nhà ăn / phòng học / căng tin ở đâu |
| `HOI_KIEN_THUC` | Súng AK dùng thế nào, RSA là gì (KTMM) |
| `BAO_CAO` | Báo cáo trung đội đủ quân, hoàn thành nhiệm vụ |
| `XIN_PHEP` | Cho em xin nghỉ, em đang ốm |
| `TAM_BIET` | Em chào, em đi học đây |
| `OUT_OF_SCOPE` | Người chơi nói chuyện linh tinh, model phải biết "tôi không xử lý được cái này" |

Sau khi model phân loại xong → tôi tra một file `responses.json` chứa 4-5 mẫu câu trả lời cho mỗi intent (có placeholder `{place}`, `{scheduled_today}`, `{meal_time}`, ...) → fill bằng game state runtime → trả về text NPC nói. **Tầng 1 là AI, tầng 2 là logic Unity tôi tự lo.**

### 2.2. Phiên bản 1 — Bài học đầu tiên: train val 100% chưa phải giỏi

Lần đầu tôi viết generator template thật đơn giản: ~50 mẫu câu × pool từ 30-60 từ → ra **529 câu training**. Vocab chỉ có 165 token. Tôi train một mạng FastText 30 epoch trên đó, nhìn val_acc lên 1.00, sướng thật, tưởng xong.

Nhưng khi test bằng 8 câu thật tự nghĩ ra, model đoán đúng chỉ **1/8 = 12.5%**. Lúc đó tôi mới hiểu: **val_acc cao trên data synthetic không nói lên model giỏi** — nó chỉ nói lên model **học thuộc đúng cái phân phối tôi tạo ra**. Khi gặp câu ngoài distribution (vocab không biết → UNK token → model mặc định trượt về class đông nhất) thì sập ngay.

### 2.3. Phiên bản 2 — Mở rộng từ vựng, train 3 kiến trúc

Tôi quyết định:
- Generator giàu hơn: **22 template × 60+ từ/pool → 2.000 câu, vocab 769 token**
- Train **3 kiến trúc song song** rồi so sánh: FastText (51K params), LSTM (116K), Transformer (120K)
- Pool từ phải có cả thuật ngữ KTMM (mật mã đối xứng, RSA, AES, chữ ký số, tường lửa, IDS/IPS) bên cạnh quân đội (súng AK, lựu đạn, điều lệnh) để con AI làm được vai trò sĩ quan KTMM, không phải sĩ quan thuần huấn luyện

Tôi viết một test set 16 câu thực tế. Kết quả:
- **FastText: 16/16 = 100%** ⭐
- LSTM: 15/16 = 93.8%
- Transformer: 14/16 = 87.5%

Lúc đó tôi viết HANDOFF kết luận **"FastText là canonical winner, vừa nhỏ vừa giỏi"**. Một quyết định trông rất ổn — model 51K param mà thắng được LSTM 116K, ai cũng thấy hợp lý.

### 2.4. Phát hiện FastText "lừa" — bài học quan trọng nhất phase A v1

Buổi trưa hôm sau, tôi thử bump dataset lên 16k câu rồi train lại. Tất cả 3 kiến trúc đều **100% trên test 16 câu**. Đây là dấu hiệu cảnh báo: **metric đã saturate**, không còn signal để biết model nào thật sự giỏi hơn.

Tôi quyết định ngồi viết tay **64 câu test khó** — mỗi intent 8 câu, gồm:
- 2 câu sạch keyword rõ
- 2 câu **không dấu** (telex typo: "may gio thi an")
- 2 câu compound dài ("Cho em hỏi mai tập trung lúc mấy giờ và làm gì ạ")
- 2 câu **adversarial near-confusion** (vd "có gì ăn không em đói" — gần OUT_OF_SCOPE nhưng intent đúng là HOI_GIO_AN)

Lúc chạy lại trên test 64 câu này:
- **FastText: 25%** — thảm hại
- **LSTM: 95.3%** ⭐
- **Transformer: 18.8%**

Tôi sốc thật sự. Hóa ra FastText nó hoạt động bằng `mean_pool(embedding(các token))` — tức là cộng trung bình embedding của từng từ trong câu. Với câu đơn giản kiểu "Hôm nay có lịch gì" thì OK, nhưng với câu **"Có gì ăn không em đói"** thì các từ "có", "gì", "không" xuất hiện ở rất nhiều intent → mean-pool xong tín hiệu của "ăn" và "đói" bị làm loãng đi → model fall through về intent đông nhất trong vocab. **LSTM giữ thứ tự từ + bidirectional state**, biết rằng "ăn" và "đói" gần nhau là dấu hiệu HOI_GIO_AN cực mạnh, nên thắng.

Đó là lúc tôi hiểu vì sao kiến trúc tuần tự (LSTM) sẽ generalize tốt hơn bag-of-words (FastText) trên ngôn ngữ tự nhiên — không phải cứ model nhỏ là thắng.

→ **Quyết định**: đổi canonical từ FastText sang LSTM. Đồng thời rút kinh nghiệm: **test set có ý nghĩa = test set đủ khó để model phân biệt được nhau**. Sanity check 16 câu chỉ dùng để đảm bảo pipeline không vỡ, không phải eval thực.

### 2.5. Overnight loop v3.1 — train tự động qua đêm

Để tận dụng deadline 19:00 cùng ngày, tôi viết một script `overnight_loop.py` tự động lặp:

```
generate dataset 40k samples
→ train LSTM/FastText/Transformer cycle
→ eval real-world 64 câu
→ pick winner
→ save best ONNX vào deliverables/
→ tăng seed, lặp lại
```

Mỗi vòng lặp ~35 phút (trong đó Phase B chiếm 80% thời gian, sẽ kể sau). Trong ~10 tiếng tôi chạy được **27 vòng**. Phát hiện thêm:

- **LSTM saturate ngay từ iter 1**: 98.4% trên 64-câu test, các iter sau không cải thiện. Đó là vì kiến trúc đã đủ mạnh, data 40k đủ phong phú, vocab đủ phủ.
- **FastText cần lottery ticket**: phải tới iter 17 (seed 1016) FastText mới đạt 98.4% match được LSTM. Trước đó loanh quanh 95.3%. Kiến trúc nhỏ thì stochastic training quan trọng — phải retry nhiều seed.
- **Transformer kém hơn cả**: best 96.88% — kích thước nhỏ + data còn thiếu so với cái transformer cần để học position encoding tốt.

→ **Bài học quan trọng**: *Data scale > kiến trúc complexity* trong domain hẹp này. Đem 51K-param FastText train trên data đủ phong phú thì cũng match được LSTM 116K — không có "model thông minh hơn", chỉ có "model phù hợp với data hơn".

Cuối ngày tôi giao 3 file ONNX:
- `intent_classifier.onnx` = LSTM 98.44% (canonical)
- `fasttext_intent.onnx` = FastText 98.44%
- `transformer_intent.onnx` = Transformer 96.88%

### 2.6. Bug tích hợp Unity — 6 bugs sau khi load model thật vào game

Bài kiểm tra thật là khi **lôi model vào Unity và chạy**. Tôi gặp đúng 6 bugs, đáng kể nhất:

**Bug tokenization C# vs Python (bug nặng nhất):** vocab Python chứa các entry **đa từ có space** như `"ăn cơm"`, `"thủ trưởng"`, `"báo cáo"` (do tokenizer `underthesea` segment từ tiếng Việt thành multi-word token). Code C# của tôi ban đầu chỉ split whitespace đơn giản → cả `"ăn cơm"` thành 2 token riêng → cả 2 đều UNK → 30-50% token UNK → model fail.

Fix: viết **greedy longest-match** trong tokenizer C# — tại mỗi vị trí thử ghép N..1 từ liên tiếp, lấy match dài nhất có trong vocab.

**Bug Unity 6 Input System:** project tôi để `activeInputHandler: 1` (Input System Package only, không legacy). Hậu quả là `Input.GetKeyDown(KeyCode.Return)` silent fail — không có lỗi gì, nhưng nhấn Enter không submit. Tôi phải thay bằng `UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame` và đổi `StandaloneInputModule` thành `InputSystemUIInputModule`.

Còn 4 bug khác liên quan canvas UI, IMGUI vs Canvas-based Unity UI, scene builder Editor menu (1-click setup). Tôi viết một file `Assets/AI/Editor/AITestSceneBuilder.cs` để build sẵn scene Phase A test, scene Phase B test, scene combined — user click 1 nút trong menu **AI** là có scene đầy đủ trong Hierarchy luôn, không cần drag-drop thủ công.

### 2.7. User test thử → phát hiện AI vẫn yếu

Sau khi tích hợp xong, tôi tự test một số câu, thấy ổn. Nhưng khi user (người ngoài) gõ vào, tôi nhận feedback **"AI ngu, hỏi khác 1 tý là dính ngay, hỏi khu A mà cứ trả lời khu B"**.

Lúc đầu tôi không tin — model 98.4% trên test 64 câu mà. Tôi build một **test set khó hơn nhiều, 216 câu**, chia thành 10 nhóm:

| Nhóm | Số câu | Đặc điểm |
|---|---:|---|
| CLEAN | 55 | Tiếng Việt sạch chuẩn |
| NO_ACCENT | 24 | Không dấu (kiểu gõ vội) |
| TELEX_TYPOS | 12 | Lỗi gõ telex (ô→oo, đ→dd, lặp ký tự) |
| CODE_MIX | 24 | Trộn tiếng Anh ("Schedule mai sao", "Library ở đâu") |
| COMPOUND | 16 | Hỏi 2 thứ trong 1 câu |
| ELLIPSIS | 25 | Câu cụt ("Cantin", "Cuối tuần", "Còn mai thì sao") |
| SLANG | 15 | Lóng tuổi teen ("z", "v", "hum") |
| SYNONYM | 18 | Cùng intent nhưng từ khác (xơi/chén thay ăn) |
| COMPLAINT | 23 | Phàn nàn ngầm hỏi ("Đói quá", "Lạc đường rồi") |
| ADVERSARIAL | 4 | Câu near-confusion |

Chạy LSTM v1 trên test này: **38.0% (82/216)**. Đúng là user nói thật.

Test 64 câu cũ tôi dùng làm sanity, nhưng nó share template DNA với data training (vì cùng tôi viết) nên model "nhìn quen mắt". Test 216 câu mới mới lộ ra điểm yếu thật.

### 2.8. Phase A v2 — iterate dựa trên failure analysis

Tôi thấy có 2 vấn đề tách biệt:

**Vấn đề 1: Intent classifier yếu trên paraphrase.** Chỉ là vấn đề thiếu data đa dạng. Fix bằng cách scale dataset.

**Vấn đề 2: "Khu A → trả lời khu B5".** Vấn đề này thực ra **không phải intent sai** (model classify HOI_VI_TRI hoàn toàn đúng). Vấn đề là response template `"{place} nằm ở khu {block}. Có biển chỉ dẫn."` được fill bằng **DummyContext.Get("block") = "B5" cứng** — bất kể user hỏi khu nào, AI cũng kết luận "khu B5". Đây là vấn đề **tầng 2 (response substitution)**, không phải tầng 1 (model). Hai tầng phải fix riêng.

#### Iter 1 — bump scale (v4)

Generator v4:
- **240k câu** (vs 40k cũ — gấp 6 lần)
- **200-300 template/intent** (vs 50)
- Pool từ gấp 5x: 100+ TIMES, 130+ PLACES, 188 KNOWLEDGE topic, 100 REASONS
- Paraphrase patterns: ellipsis, complaint-as-question ("đói quá", "lạc đường rồi"), code-mixing 5%
- Augmentation rate **32%** (vs 10% cũ): drop_accent, telex_typo, char_swap, drop_filler, synonym_swap

LSTM tăng từ 250K params lên **707K params**, max_len 32→40, vocab 5K→10K. Train 15 epoch ~4 phút.

Kết quả hard test 216 câu: **38% → 92.6% (+54.6 điểm)**. Có 16 câu vẫn miss.

#### Iter 2 — gap-filling từ exact failures (v5)

Tôi chạy lệnh `eval_hardset.py --print_misses` để in ra **đúng 16 câu fail với prediction**. Phân loại theo cluster, mỗi cluster tôi viết 10-20 template targeted:

| Cluster fail | Số ca | Template tôi thêm |
|---|---:|---|
| "rảnh không / bận hay rảnh" → OOS sai | 2 | `{time} có rảnh không`, `{time} bận hay rảnh thế` |
| "Mai đi đâu" → HOI_VI_TRI sai (đúng phải HOI_LICH) | 1 | `{time} đi đâu`, `{time} đại đội đi đâu` (asking activity) |
| "Em đang sốt 39 độ" → TAM_BIET sai (đúng XIN_PHEP) | 3 | Pure-symptom: `em đang {reason}`, `em sốt 39 độ`, không cần keyword "xin phép" |
| Lone-place ellipsis: "Cantin" → TAM_BIET sai | 2 | `{place}` đứng một mình, `{place} đâu`, `{place} chỗ nào` |
| NO_ACCENT location: "san van dong o dau" | 1 | Templates no-accent rõ ràng |
| Compound OOS: "Trời đẹp và tớ đói" → HOI_GIO_AN sai | 1 | Mixed weather+hunger templates trong OUT_OF_SCOPE |
| Lone-time-word: "Cuối tuần" → HOI_LICH sai | 1 | OUT_OF_SCOPE templates với time-word casual |

Cộng với việc bump augmentation rate **32% → 45%** (cho NO_ACCENT, TELEX_TYPOS được training thường xuyên hơn). Train tiếp 15 epoch.

Kết quả: **92.6% → 96.8% (+4.2 điểm)**. Còn 7 câu miss.

#### Iter 3 — học bài học không thêm template quá tay (v6)

Tôi tham, muốn lên 98%+, viết thêm 70+ template (rảnh-form, English-place, academic-complaint OOS) và bump augmentation tới 50%. Kết quả: **96.3%** — TỆ HƠN v5 0.5 điểm.

Nhìn breakdown thì:
- COMPLAINT 96% → 100% (tốt lên)
- NO_ACCENT 96% → 100% (tốt lên)
- Nhưng CODE_MIX, SLANG, SYNONYM mỗi cái regress 1 ca → tổng net âm

Đó là lúc tôi hiểu rõ: **thêm template không phải lúc nào cũng tốt**. Khi đã ở 96%+, thêm template = đổi phân phối training, làm decision boundary chao đảo, các category đang OK có thể bị dạt sang miss. ROI âm. Tôi reject v6, ship v5.

### 2.9. EntityExtractor — fix "khu A → trả lời khu B5" (vấn đề tầng 2)

Việc model classify HOI_VI_TRI khi user hỏi "khu A" là đúng. Cái sai là **response không biết user vừa hỏi cái gì cụ thể**. Tôi viết một module C# **EntityExtractor** rule-based, không cần ML thêm:

1. Dump các pool PLACES/TIMES/KNOWLEDGE/MEALS/REASONS/REPORT_OBJ từ generator Python ra `slot_vocab.json` (715 phrase tổng)
2. Mỗi phrase được sort theo độ dài giảm dần (longest-first match)
3. Khi user gõ câu, scan toàn câu, với mỗi slot tìm phrase đầu tiên match như cả từ → trả về dict `{place: "khu a", time: "ngày mai", ...}`
4. Class `SmartRuntimeContext` wrap `IRuntimeContext` cũ — khi template hỏi `{place}`, ưu tiên trả về slot đã extract; chỉ fallback DummyContext khi không có

Đồng thời tôi viết lại **`responses_v2.json`** để tránh template tự xung đột. Template cũ `"{place} nằm ở khu {block}"` bị replaced bằng `"{place} ở phía {direction} doanh trại, đi thẳng {distance}m là tới"` — vì `{place}` đã là entity user hỏi, không cần `{block}` cứng nữa.

Kết quả: user hỏi `"Khu A ở đâu"`, AI trả lời `"khu a ở phía đông doanh trại, đi thẳng 100m là tới"` — đúng ngữ cảnh user hỏi.

### 2.10. So sánh trực tiếp V1 vs V2 trong Unity

Tôi giữ **toàn bộ V1 nguyên vẹn** trên đĩa (`intent_classifier.onnx`, `responses.json`, `NPCDialogueBrain.cs` với DummyContext) và deploy V2 song song:

```
Assets/AI/Models/intent_classifier.onnx       ← V1 LSTM 250K, 38% hard test
Assets/AI/Models/intent_classifier_v2.onnx    ← V2 LSTM 707K, 96.8% hard test

Assets/AI/Resources/responses.json            ← V1, có template tự xung đột
Assets/AI/Resources/responses_v2.json         ← V2, slot-aware

Assets/AI/Resources/slot_vocab.json           ← V2, EntityExtractor data

Assets/AI/Scripts/EntityExtractor.cs          ← V2 mới
Assets/AI/Scripts/SmartRuntimeContext.cs      ← V2 mới
Assets/AI/Scripts/PhaseACompareTester.cs      ← UI 2 cột compare V1 vs V2
Assets/AI/Editor/PhaseACompareSceneBuilder.cs ← Menu AI/4
```

Mở Unity, vào menu **AI → 4. Phase A — Compare V1 (Old) vs V2 (New)**, click Play, gõ tiếng Việt, cả V1 và V2 reply song song trong 2 cột. Đây là cách tôi tự kiểm tra và để hội đồng kiểm chứng cải thiện thật chứ không phải tôi nói suông.

### 2.11. Còn 7 câu V2 vẫn fail — và tôi biết vì sao

| Câu | V2 đoán | Đúng | Lý do |
|---|---|---|---|
| "Phoongg học oo đâu" | HOI_LICH | HOI_VI_TRI | Telex bất thường, mọi token UNK |
| "Bááoo cáo đủù quânnn" | OUT_OF_SCOPE | BAO_CAO | Telex multi-char duplication |
| "Emm chào thủủ trưởng emm đii" | OUT_OF_SCOPE | TAM_BIET | Telex extreme |
| "Library ở đâu thầy" | HOI_LICH | HOI_VI_TRI | "thầy" cuối câu lệch về schedule |
| "Bài tập về nhà nhiều quá" | HOI_VI_TRI | OUT_OF_SCOPE | Có "nhà" → confused về place |
| "Hôm nay rảnh không nhỉ" | OUT_OF_SCOPE | HOI_LICH | Form "rising-tone" thiếu trong template |
| "Cho em hoi sang van dong o dau" | OUT_OF_SCOPE | HOI_VI_TRI | NO_ACCENT borderline 50% confidence |

3 trên 7 câu là **telex extreme** — người thật không gõ kiểu "Phoongg" cả. Để fix triệt để cần **subword tokenizer (FastText char n-gram 3-5)** — model nhìn vào ký tự bên trong từ thay vì cả từ. Tôi đánh giá: 96.8% là sweet spot, nâng cấp subword cần ~1 ngày làm cả Python lẫn C# tokenizer, để dành cho phase nâng cấp sau.

---

## 3. Phase B — Lính NPC tự đi tránh chướng ngại

### 3.1. Quyết định không dùng ML-Agents

Lựa chọn đầu tiên là **phương pháp training**. Unity có sẵn `ML-Agents Toolkit` rất tiện — viết env trong Unity, train với Python backend, output ONNX. Nhưng có một ràng buộc cứng: **ML-Agents YÊU CẦU Unity Editor mở suốt training**. Train PPO 5 triệu step thường ~6-15 giờ. Nếu tôi muốn để máy chạy thông đêm, lock screen, đi ngủ — Unity Editor có thể sleep, freeze, ngắt connection với Python communicator → training vỡ.

Tôi chọn **standalone PyTorch + Stable-Baselines3 PPO + Gymnasium**. Chấp nhận trade-off:
- **Phải tự code Gymnasium env** mô phỏng raycast 2D bằng numpy (~200 dòng code)
- **Phải đảm bảo observation contract** trong Python KHỚP với raycast Unity (raycast direction, layer mask, normalize range)
- Mất khả năng visualize training trong Unity Editor — chỉ có log mean_reward dạng text

Đổi lại: **chạy nohup background được**, không phụ thuộc Unity, output cuối cùng vẫn là 1 file ONNX nạp vào Unity 6 qua `InferenceEngine` y hệt ML-Agents.

### 3.2. Thiết kế observation và action — contract giữa Python env và Unity

Đây là phần tôi mất nhiều thời gian nhất phase B. Vì 2 system phải khớp **chính xác từng float**, không lệch.

**Observation = 21 floats:**

```
[0..7]   8 ray distance normalize / 10m   — 8 hướng evenly 360°, index 0 = forward, CCW
[8..15]  8 ray hit-target one-hot         — 1.0 nếu ray trúng target
[16]     velocity_forward / max_speed     — vận tốc trong agent frame
[17]     velocity_lateral / max_speed
[18]     dir_to_target forward (cos)      — góc tới target trong agent frame
[19]     dir_to_target lateral (sin)
[20]     distance_to_target / arena_diag  — khoảng cách tương đối
```

**Action = 2 continuous float qua tanh:**

```
[0] thrust ∈ [-1, 1]  — forward, reverse chạy 0.5x
[1] turn   ∈ [-1, 1]  — phải dương, scale theo π rad/s
```

**Reward shaping:**

```
+1.0     đến target (terminate)
+0.5*Δd  progress (gần lại target)
-0.0005  per step (chống đứng yên)
-0.1     đụng obstacle (slide ra cạnh, không terminate)
-1.0     out-of-bounds (terminate)
-0.5     timeout 500 step
```

Mỗi episode random arena (15-25m), random số obstacle (3-12), random kích thước obstacle (1.0-2.5m). Cố tình random để policy không học pattern fix mà generalize ra nhiều layout.

### 3.3. V2 fixed env — baseline 6.126

Phiên bản đầu tôi để env fix 6 obstacle, train 30 lần × 200k step trên net `[64, 64]`. Best: **mean_reward 6.126** ở iter 8 (lottery seed sớm).

Nhìn loop chạy thì plateau quanh 5.5-6.1 suốt. Tôi gặp một **bug** mà sau này tôi tự đặt tên: **"loop spin near deadline"**. Khi đến gần deadline (19:00), một iter mới start nhưng không đủ thời gian chạy 200k step → start rồi crash → Python loop spin **493.000 vòng** trong 30 phút cuối, cố gắng restart liên tục. Fix bằng **30s sleep guard** + check thời gian còn lại trước khi launch.

### 3.4. V3 random env — generalize ngon hơn nhưng score thấp hơn

Tôi tăng độ khó env: random 3-12 obstacle, random arena 15-25m, net lên `[128, 128]`. Train 500k step/iter × 5 iter. Best: **6.05** — thấp hơn v2 fixed env (6.126).

Đây là một phát hiện thú vị: **score thấp hơn không có nghĩa là model dở hơn** — nó nói lên env mới khó hơn (nhiều obstacle hơn, range arena rộng hơn). So sánh trực tiếp 2 score giữa 2 env khác nhau là vô nghĩa. Tôi giữ cả 2 file ONNX:
- `soldier_v2_fixedenv.onnx` (best fixed env, dùng nếu Unity scene 6 obstacle cố định)
- `soldier.onnx` (random env, generalize tốt hơn cho scene đa dạng)

### 3.5. V3.1 — bump 1M step, chạy 2 máy song song

Đến giai đoạn cuối, deadline còn 10 tiếng. Tôi quyết định:
- Scale Phase B lên **1M step/iter** (gấp 2x v3, 5x v2)
- Tận dụng 2 máy: máy 1 chạy iter nhanh 3 archs cycle, **máy 2 chạy heavy hyperparameter grid**

#### Phân vai 2 máy

| | Máy 1 (overnight_loop.py) | Máy 2 (overnight_loop_machine2.py — HEAVY) |
|---|---|---|
| Phase A target | 5,000/intent (40k total) | **25,000/intent (200k — gấp 5x)** |
| Phase A archs | LSTM/FastText/Transformer cycle | LSTM only (focus winner) |
| Phase A epoch | 25 | 35 |
| Phase B step | 1M | **2M** |
| Phase B HP | fixed `[128,128]` ent=0.01 lr=3e-4 | **cycle 4 config:** |
| | | h1_baseline `[128,128]` ent=0.01 lr=3e-4 |
| | | h2_bigexplore `[256,128]` ent=0.02 lr=3e-4 |
| | | **h3_deepfocus `[128,128,64]` ent=0.005 lr=1e-4** ⭐ |
| | | h4_bigwide `[256,256]` ent=0.05 lr=5e-4 |
| Phase B seed | 2000+ | 7000+ |

Sync giữa 2 máy = git push/pull cuối ngày. Không real-time. Mỗi máy ghi vào folder riêng (`deliverables/` vs `deliverables_m2/`) tránh collision file. Cuối ngày tôi viết script `merge_machine_results.py` tự đọc 2 file state, so reward, copy ONNX winning vào canonical.

#### Kết quả cuối ngày

**Máy 1 (27 iter, 1M step mỗi iter):**

| Iter | Reward | Note |
|---|---|---|
| 1-13 | 5.5 → 6.05 | plateau |
| **14** | **6.569** ⭐ | **Lottery ticket — seed 2013** |
| 15-16 | 5.7 → 6.28 | back to plateau |

Iter 14 break plateau từ 6.0 lên 6.569 — đó là lúc tôi hiểu sâu hơn về **stochastic training**: PPO không deterministic, mỗi seed cho một path optimization khác nhau. Có những seed gặp được "lucky exploration" rất sớm, dẫn tới policy tốt hơn hẳn. Không thể dự đoán seed nào trúng — chỉ có thể chạy nhiều seed.

**Máy 2 (HP grid):**

| HP | Net | Ent | LR | Best Reward | Verdict |
|---|---|---|---|---|---|
| **h3_deepfocus** ⭐⭐ | [128,128,64] | 0.005 | 1e-4 | **6.572** | conservative thắng |
| h2_bigexplore | [256,128] | 0.02 | 3e-4 | 6.263 | tốt vừa |
| h1_baseline | [128,128] | 0.01 | 3e-4 | 6.236 | baseline |
| h4_bigwide | [256,256] | 0.05 | 5e-4 | 5.252 | flop — bigger net + high entropy hurt |

**Final winner: máy 2, h3_deepfocus, mean_reward = 6.572** (chỉ thua máy 1 best 0.003 — gần như tie).

### 3.6. Bài học từ HP grid

`h4_bigwide` thất bại nhiều nhất — net to ([256,256]) + entropy cao (0.05) + lr cao (5e-4). Tôi nghĩ ban đầu "to hơn = mạnh hơn", nhưng với env này:
- Net to mà data ít → overfit hành vi sớm
- Entropy cao → exploration quá nhiều → policy không converge
- LR cao → nhảy qua optimum

`h3_deepfocus` thắng — net sâu hơn ([128,128,64]) nhưng entropy thấp (0.005, exploration ít, exploitation nhiều) + lr thấp (1e-4). **"Conservative beats brute"** — quan trọng là pick HP phù hợp env, không phải đơn giản scale up.

### 3.7. Bug Unity integration Phase B — coord và collision

Khi nạp `soldier.onnx` vào Unity, agent di chuyển "lệch" — nhiều khi tăng tốc khi gần target (không đúng), slow down khi xa (sai bản năng). Sau khi debug:

**Bug 1: arenaDiagonal sai 25%.** Trong Python tôi dùng `arena_max * sqrt(2) = 25 * sqrt(2) = 35.36`, nhưng trong C# tôi để `28.28 = 20 * sqrt(2)` (kế thừa từ v2 fixed). Distance feature obs[20] normalize bằng arenaDiagonal → sai 25% → model nhận tín hiệu "đang gần bờ map" sai → quyết định turn/thrust sai.

Fix: `arenaDiagonal = 35.36f;`

**Bug 2: Collision = block fully thay vì slide.** Trong Python env, khi agent đụng obstacle, code tôi chiếu agent ra cạnh obstacle gần nhất — gọi là "slide projection". Trong Unity tôi viết đơn giản hơn:

```csharp
if (!Physics.CheckSphere(next, 0.5f, obstacleLayer))
    transform.position = next;
```

Nghĩa là nếu vị trí kế tiếp overlap obstacle → KHÔNG di chuyển. Vấn đề: policy có thể command thrust forward liên tục → next luôn overlap → kẹt forever. Trong Python policy biết "đụng → trượt" rồi học từ đó; trong Unity policy gặp behavior khác → vỡ.

Fix: port slide projection sang Unity:

```csharp
Collider[] hits = Physics.OverlapSphere(next, agentRadius, obstacleLayer);
if (hits.Length > 0)
{
    var ob = hits[0];
    Vector3 obCenter = ob.bounds.center;
    float halfX = ob.bounds.extents.x;
    float halfZ = ob.bounds.extents.z;
    float ox = next.x - obCenter.x;
    float oz = next.z - obCenter.z;
    if (Mathf.Abs(ox) > Mathf.Abs(oz))
        next.x = obCenter.x + Mathf.Sign(ox) * (halfX + agentRadius + 0.01f);
    else
        next.z = obCenter.z + Mathf.Sign(oz) * (halfZ + agentRadius + 0.01f);
}
transform.position = next;
```

**Bug 3: Raycast direction CCW (Python) vs CW (Unity).** Python tôi tính 8 hướng theo công thức `theta = i * 2π / 8` quay **counter-clockwise**. Unity left-handed coordinate system, quay **clockwise**. Hậu quả: policy rẽ trái thì agent rẽ phải, ngược lại. Rất confused. Fix: đảo dấu turn action HOẶC đảo dấu raycast index trong C#.

Sau khi fix 3 bug này, agent đi mượt, đến target ổn định trong scene 6-obstacle test.

---

## 4. Tích hợp cuối — workflow Unity test

Tôi gói tất cả vào **3 menu item** trong Unity Editor:

```
AI / 1. Phase A — Chat Test
AI / 2. Phase B — Movement Test
AI / 3. Both — Combined
AI / 4. Phase A — Compare V1 (Old) vs V2 (New)
```

Click 1 trong 4, Editor script tự build scene đầy đủ trong Hierarchy: cube xanh agent, cube đỏ target, 6 obstacle, Layers + Tags được set tự động qua `AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")`. Người dùng không cần biết Sentis là gì, chỉ click Play là xem được model hoạt động.

---

## 5. Bài học chung sau khi tự làm 2 con AI

Tôi tóm gọn những gì bản thân học được, không phải đọc trên giấy:

1. **Val_acc trên synthetic data không bao giờ là metric thật.** Phải build hard test set, ban đầu hand-crafted cũng được, sau khi user chơi thật thì append câu user gõ vào test set. Tôi đã đi đúng hướng đó: từ 16 câu → 64 câu → 216 câu, mỗi lần expose model yếu chỗ nào.

2. **Iter dựa trên failure cụ thể, không phải bump dataset mù.** V4 → V5 tôi tăng accuracy +4.2 điểm chỉ bằng việc đọc 16 câu fail của V4 và viết template targeted. So với việc bump dataset thêm 100k câu mà không biết đang đắp vào đâu thì hiệu quả gấp nhiều lần.

3. **Đôi khi thêm template làm tệ đi.** V6 chứng minh điều này. Khi đã ở 96%+ thì decision boundary nhạy, thêm template = đổi phân phối, có thể đẩy các category đang OK ra ngoài. ROI âm.

4. **Vấn đề user cảm nhận có thể không phải vấn đề model.** "Khu A → trả lời khu B5" thực ra là vấn đề tầng response template + DummyContext, không phải intent classification. Không phải lúc nào cứ thấy AI ngu là phải retrain — nhiều khi chỉ cần fix logic xung quanh.

5. **Data scale > kiến trúc complexity** với domain hẹp. FastText 51K param match được LSTM 116K khi data đủ. Đừng vội chọn model "thông minh hơn" — thường vấn đề là data không đủ phong phú, không phải model không đủ to.

6. **Stochastic training cần lottery ticket.** PPO Phase B break plateau ở seed 2013 (iter 14/16). FastText match LSTM ở seed 1016 (iter 17/27). Không có cách dự đoán seed thắng — chỉ có cách chạy nhiều seed.

7. **Conservative beats brute trong HP tuning.** h3_deepfocus (entropy thấp + lr thấp + net sâu nhỏ) thắng h4_bigwide (entropy cao + net to + lr cao) trên Phase B.

8. **Test integration sớm và đau.** 6 bug Unity tôi gặp đều do Python ↔ C# mismatch (tokenizer, coord, raycast direction, Input System). Nếu không thử nạp model thật vào Unity, tôi sẽ không biết những bug này.

9. **Architecture cycle + auto-pick winner an toàn hơn fix một arch.** Loop v3.1 chạy cả 3 archs, auto fall back nếu LSTM regress trên data mới. Tránh được kiểu "đổi data mới → model cũ broken → không có model deploy".

10. **Random env > fixed env cho generalization.** V2 fixed 6 obstacle score cao nhất nhưng yếu khi gặp scene khác. V3 random 3-12 obstacle score thấp hơn nhưng generalize tốt hơn. Trong production tôi sẽ chọn V3 dù số trên giấy tệ hơn.

---

## 6. Trạng thái cuối session — deliverables hoàn chỉnh

Đến thời điểm hiện tại tôi giao được:

**Phase A:**
- `Assets/AI/Models/intent_classifier.onnx` — V1 LSTM (giữ để compare)
- `Assets/AI/Models/intent_classifier_v2.onnx` — **V2 LSTM 707K, 96.8% hard test 216 câu** ⭐
- `Assets/AI/Resources/intent_classifier_v2_meta.json` — vocab + label map
- `Assets/AI/Resources/responses_v2.json` — 5-7 template/intent slot-aware
- `Assets/AI/Resources/slot_vocab.json` — 715 phrase cho EntityExtractor
- `Assets/AI/Scripts/{NPCDialogueBrain,EntityExtractor,SmartRuntimeContext,PhaseACompareTester}.cs`

**Phase B:**
- `AI_Training/deliverables_m2/soldier_m2.onnx` — **PPO h3_deepfocus mean_reward 6.572** ⭐
- `AI_Training/deliverables/soldier.onnx` — runner-up 6.569 (random env)
- `AI_Training/deliverables/soldier_v2_fixedenv.onnx` — backup fixed env 6.126
- `Assets/AI/Models/soldier.onnx` — copy canonical
- `Assets/AI/Scripts/MovementAgent.cs` — wrapper với fix coord, collision slide, raycast direction

**Test workflow:** Unity Editor → menu **AI → 1/2/3/4** → click Play.

**Báo cáo chi tiết iteration:** `AI_Training/PHASE_A_V2_REPORT.md` có đủ số liệu cho từng iter v3 → v4 → v5 → v6.

**Wiki nội bộ:** `.wiki/wiki/` chứa toàn bộ design decisions, bug log, system docs, claims với citation. Có thể navigate qua `index.md`.

---

## 7. Hướng nâng cấp tương lai (nếu có thời gian)

Tôi note lại những gì còn thiếu để hội đồng biết tôi đang đứng ở đâu trên đường hoàn chỉnh:

1. **Phase A subword tokenizer** — port FastText char n-gram 3-5 sang Python + C#. Sẽ fix 3/7 misses TELEX_TYPOS còn lại, dự kiến đẩy lên 98%+. Cost: ~1 ngày làm.

2. **Phase A test set 500+ câu** — hiện 216 còn ít cho ĐATN final. Cần record câu user thật khi chơi thử rồi append vào eval.

3. **Phase B 5M step** — hiện 1M (hoặc 2M trên máy 2). Plateau hiện tại 6.5-6.6 reward; expected ceiling ~10. Chạy 5M có thể break thêm 1-2 điểm.

4. **EntityExtractor → PhoBERT NER** — hiện rule-based, không generalize được phrase ngoài vocab. Nâng cấp sang NER ML model nếu vocab cần mở rộng. Vướng: PhoBERT chưa hỗ trợ Sentis (BERT tokenizer trên C# phức tạp).

5. **Multi-turn dialogue memory** — hiện model classify từng câu độc lập. "Cái đó ở đâu" không nhớ "cái đó" là gì từ turn trước. Cần stateful context.

---

*Hết.*

Toàn bộ source code, deliverables, wiki, báo cáo iteration đều ở repo `https://github.com/luzart-outsources/unity_train_ai`. Mọi quyết định lớn đều có decision record kèm context-options-trade-offs trong `.wiki/wiki/decisions/`. Mọi claim quan trọng có citation trong `.wiki/wiki/claims.md` để hội đồng có thể truy xét nguồn gốc.
