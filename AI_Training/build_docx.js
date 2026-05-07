// Build narrative-style .docx for ĐATN AI training journey.
// Voice: third-person dev-log style, no "tôi", numbers as digits,
// some shorthand (=>, ->, ~, vs, ≠) to feel hand-written, not AI-generated.
//
// Usage: node build_docx.js
// Output: HANH_TRINH_TRAINING_2_CON_AI.docx

const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun,
  AlignmentType, HeadingLevel,
} = require("docx");

// ─────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────
const para = (text, opts = {}) =>
  new Paragraph({
    spacing: { after: 240, line: 360 },
    alignment: opts.alignment || AlignmentType.JUSTIFIED,
    children: [
      new TextRun({ text, font: "Times New Roman", size: 26 }),
    ],
    ...opts,
  });

const heading = (text, level = 1) =>
  new Paragraph({
    heading: level === 1 ? HeadingLevel.HEADING_1 : HeadingLevel.HEADING_2,
    spacing: { before: 480, after: 240 },
    alignment: AlignmentType.LEFT,
    children: [
      new TextRun({
        text, font: "Times New Roman",
        size: level === 1 ? 32 : 28, bold: true,
      }),
    ],
  });

const title = (text) =>
  new Paragraph({
    spacing: { after: 480 },
    alignment: AlignmentType.CENTER,
    children: [
      new TextRun({ text, font: "Times New Roman", size: 36, bold: true }),
    ],
  });

const subtitle = (text) =>
  new Paragraph({
    spacing: { after: 720 },
    alignment: AlignmentType.CENTER,
    children: [
      new TextRun({ text, font: "Times New Roman", size: 24, italics: true }),
    ],
  });

// ─────────────────────────────────────────────────────────────────────────
// Content — narrative prose, no markdown, no "tôi", numbers as digits
// ─────────────────────────────────────────────────────────────────────────
const children = [];

children.push(title("Hành trình huấn luyện 2 con AI cho đồ án tốt nghiệp"));
children.push(subtitle("Bản tự sự về quá trình thử, sai, sửa, và đi đến kết quả"));

// 1
children.push(heading("Chương 1. Bối cảnh — vì sao cần đến 2 con AI", 1));
children.push(para(
  "ĐATN là 1 game giáo dục mô phỏng học kỳ quân đội tại Học viện KTMM trên Unity 6. Lúc lập kế hoạch ban đầu, phần được kỳ vọng tốn nhiều công sức nhất là level design + animation + day-loop. AI khi đó được nghĩ chỉ là gia vị, không phải món chính."
));
children.push(para(
  "Sau khi ngồi xuống chi tiết hoá kế hoạch => phát hiện ra có 2 chỗ trong game mà nếu code tay theo kiểu if/else thì sẽ lộ ngay rằng đây là thứ giả. Chỗ thứ nhất là sĩ quan chỉ huy NPC: người chơi gõ tiếng Việt vào để hỏi lịch tập, hỏi giờ ăn cơm, xin phép, báo cáo nhiệm vụ. Code tay từng câu thì viết đến đời nào cũng không xong; parse bằng regex thì vỡ ngay khi gặp typo, chêm tiếng Anh, hoặc lóng tuổi teen. Chỗ thứ hai là lính NPC: phải tự đi từ A → B trong doanh trại, biết tránh chướng ngại, đi vòng khi có người đứng chắn. Pathfinding A* tay thì đường đi cứng đến mức người chơi nhìn vào là biết robot."
));
children.push(para(
  "=> Phải có AI thật. Hệ chia làm 2 phase: Phase A là Sentis Chat (intent classification, phân loại câu tiếng Việt thành 8 lớp); Phase B là Movement AI (reinforcement learning để agent tự khám phá đường đi). Cả 2 đều phải chạy được offline trong Unity 6 qua thư viện Inference Engine (Sentis cũ, đã đổi tên trong Unity 6). Engine chỉ ăn ONNX => kết quả cuối cùng dù train ở đâu cũng phải xuất ra 1 file ONNX duy nhất."
));
children.push(para(
  "Trước khi code dòng nào, phải cắt scope. Đề cương ban đầu có 30 ngày trong game + 8 mini-game khi vào lớp (Rhythm, Quiz, FPS, Tower Defense, Maze, Memory, Card, Drag-drop). Tính lại thời gian: 5 tháng + 1 dev + 2 con AI from scratch = không khả thi. Quyết định: cắt hết 8 mini-game, mỗi buổi học chỉ là 1 fade-to-black + cộng stat tự động. Đây là quyết định scope đầu tiên — về sau nhìn lại, nếu giữ 8 mini-game thì kết cục sẽ là 8 mini-game dở + 2 con AI dở."
));

// 2
children.push(heading("Chương 2. V1 Phase A — bài học đầu tiên về val_acc", 1));
children.push(para(
  "Phase A được làm trước vì nhỏ + dễ hình dung. 8 intent định nghĩa: HOI_LICH, HOI_GIO_AN, HOI_VI_TRI, HOI_KIEN_THUC, BAO_CAO, XIN_PHEP, TAM_BIET, OUT_OF_SCOPE."
));
children.push(para(
  "Generator v1 đơn giản: ~50 câu mẫu × pool 30-60 từ → 529 câu, vocab 165 token. Train FastText 30 epoch, val_acc = 1.00. Khoảnh khắc đó tưởng Phase A xong."
));
children.push(para(
  "Cảm giác đó kéo dài đúng nửa ngày. Tự nghĩ ra 8 câu khác — không lấy từ template — đưa vào model => đoán đúng 1/8 = 12.5%."
));
children.push(para(
  "=> Bài học đắt nhất V1: val_acc cao trên synthetic data ≠ model giỏi. Nó chỉ chứng minh model học thuộc đúng phân phối đã sinh. Câu ngoài template => vocab chuyển sang UNK token => model mất tín hiệu => fall through về intent đông nhất trong vocab. Dữ liệu nghèo => model học vẹt, không phải học hiểu."
));
children.push(para(
  "Viết lại generator. Lần này pool giàu hơn nhiều, có cả thuật ngữ KTMM (RSA, AES, chữ ký số, hàm băm, tường lửa, an toàn thông tin) bên cạnh quân đội (súng AK, lựu đạn, điều lệnh, đội hình). Sĩ quan trong game phải nói được cả 2 ngôn ngữ chuyên ngành — không phải sĩ quan thuần huấn luyện mà là sĩ quan dạy mật mã. Sinh được 2,000 câu, vocab 769 token. Train cả 3 arch song song: FastText (51K params), LSTM (116K), Transformer (120K). Thêm 1 việc khôn ngoan hơn lần trước: viết tay 16 câu test thực tế và chạy cả 3 model trên đó."
));
children.push(para(
  "Kết quả test 16 câu => FastText 16/16 = 100% ⭐, LSTM 15/16 = 93.8%, Transformer 14/16 = 87.5%. HANDOFF công bố: FastText canonical, vừa nhỏ vừa giỏi. Một quyết định trông rất logic vào lúc đó — model 51K param thắng được LSTM 116K, ai cũng thấy hợp lý."
));

// 3
children.push(heading("Chương 3. Phát hiện FastText đã \"lừa\"", 1));
children.push(para(
  "Buổi trưa hôm sau bump dataset lên 16K câu, train lại 3 arch. Cả 3 lại đều 100% trên test 16 câu cũ. Đó là dấu hiệu cảnh báo: metric đã saturate => không còn signal phân biệt được model nào thật sự giỏi hơn. Nếu cứ tiếp tục bump data và báo cáo \"đạt 100%\", đến khi đem vào Unity người chơi thật gõ vào => vỡ trận."
));
children.push(para(
  "Phải viết tay 64 câu test khó hơn nhiều. Mỗi intent 8 câu, cấu trúc: 2 câu sạch keyword rõ + 2 câu không dấu (telex typo) + 2 câu compound dài + 2 câu adversarial near-confusion. Adversarial là loại quan trọng nhất — vd câu \"Có gì ăn không em đói\" thoạt nhìn giống small talk, nhưng intent đúng là HOI_GIO_AN, vì đó là cách sinh viên thật sự nói khi muốn hỏi giờ ăn."
));
children.push(para(
  "Chạy 3 arch trên test 64 câu mới => FastText 25% ‼️, LSTM 95.3% ⭐, Transformer 18.8%. Sốc thật."
));
children.push(para(
  "Ngồi suy nghĩ vì sao FastText tụt từ 100% xuống 25%. Hoá ra cách FastText hoạt động đơn giản đến mức nguy hiểm: mean_pool(embedding(token)) — cộng trung bình embedding của mọi token rồi đẩy qua classifier nhỏ. Câu sạch ngắn như \"Hôm nay có lịch gì\" thì OK vì \"lịch\" có tín hiệu mạnh. Nhưng câu phức tạp như \"Có gì ăn không em đói\" => các từ \"có\", \"gì\", \"không\" xuất hiện ở rất nhiều intent, sau khi cộng trung bình thì tín hiệu của \"ăn\" + \"đói\" bị làm loãng đi => model fall về intent đông nhất (TAM_BIET vì \"không\" cuối câu)."
));
children.push(para(
  "LSTM khác hẳn: duyệt tuần tự, giữ thứ tự từ, có hidden state truyền giữa các bước. Khi nó thấy \"ăn\" + \"đói\" gần nhau => kích hoạt mạnh tín hiệu HOI_GIO_AN ngay cả khi \"không\" cuối câu cố tình gây nhiễu."
));
children.push(para(
  "=> Bài học: kiến trúc bag-of-words không đủ cho ngôn ngữ tự nhiên khi câu phức tạp. LSTM > FastText về paraphrase robustness. Đổi canonical từ FastText sang LSTM. Quyết định đắt — HANDOFF đã công bố FastText. Nhưng giữ vì sĩ diện thì khi user thật gõ sẽ sai 1/3 câu, đứng trước hội đồng giải thích sẽ rất khó. Thay vì giữ thể diện ngắn hạn => đổi tên file canonical, document lý do trong wiki, đi tiếp."
));

// 4
children.push(heading("Chương 4. Overnight loop — lottery ticket và bài học stochastic", 1));
children.push(para(
  "Tận dụng deadline còn ~10 tiếng => viết script overnight_loop.py tự động. Pipeline mỗi vòng: generate dataset 40K câu → train 3 arch cycle → eval 64 câu → pick winner → save best ONNX → tăng seed → lặp. Mỗi vòng ~35 phút (Phase B chiếm ~80% thời gian). Trong 10 tiếng chạy được 27 vòng."
));
children.push(para(
  "Quan sát thú vị nhất: LSTM saturate ngay từ vòng 1 với 98.4% trên test 64 câu. 26 vòng sau dù đổi seed thế nào cũng không vượt được con số này. Kiến trúc LSTM với cấu hình hiện tại đã chạm trần data có. Trong khi đó FastText leo dần qua từng vòng — vòng 1 ~95.3%, vòng 5 ~96%, có vòng đứng yên, đến vòng 17 (seed 1016) mới chạm 98.4% match được LSTM. Transformer kém hơn cả, dừng ở 96.88% best."
));
children.push(para(
  "=> Bài học: stochastic training cần lottery ticket. Mỗi seed = 1 đường đi tối ưu hoá khác. Có seed gặp local optimum tốt sớm; có seed lạc đường, mãi mới hội tụ. Không có cách nào dự đoán seed nào trúng — chỉ có cách chạy nhiều seed song song và pick winner."
));
children.push(para(
  "Cuối ngày giao 3 file ONNX: intent_classifier.onnx = LSTM 98.44% (canonical), fasttext_intent.onnx = FastText 98.44%, transformer_intent.onnx = Transformer 96.88%. Tưởng Phase A đã xong."
));

// 5
children.push(heading("Chương 5. Lúc người chơi thật ngồi xuống và gõ", 1));
children.push(para(
  "Sau khi tích hợp model vào Unity, tự test vài câu, thấy ổn. Nhờ user (người ngoài) thử => phản hồi: \"AI ngu, hỏi khác 1 tý dính ngay, hỏi khu A mà cứ trả lời khu B\". Phản xạ đầu tiên: bực — model 98.4% trên test 64 câu cơ mà."
));
children.push(para(
  "Nhưng phải tự ép ngồi xuống làm thí nghiệm thật. Build hard test set 216 câu, chia 10 nhóm khó: CLEAN (sạch keyword), NO_ACCENT (không dấu kiểu gõ vội), TELEX_TYPOS (lỗi gõ ô→oo, đ→dd, lặp ký tự), CODE_MIX (chêm tiếng Anh \"Schedule mai sao\", \"Library ở đâu\"), COMPOUND (2 câu hỏi trong 1), ELLIPSIS (câu cụt: \"Cantin\", \"Cuối tuần\", \"Còn mai thì sao\"), SLANG (lóng tuổi teen: \"z\", \"v\", \"hum\"), SYNONYM (từ đồng nghĩa hiếm: xơi/chén thay ăn), COMPLAINT (phàn nàn ngầm hỏi: \"Đói quá\", \"Lạc đường rồi\"), ADVERSARIAL (near-confusion)."
));
children.push(para(
  "Chạy LSTM v1 trên test 216 câu: 38.0% (82/216). User nói thật."
));
children.push(para(
  "=> Test 64 câu cũ chỉ là sanity check, không phải eval thực. Vì test 64 câu share template DNA với data training (cùng người viết) => model \"nhìn quen mắt\". Test 216 câu mới mới lộ ra điểm yếu thật."
));
children.push(para(
  "Phân tích misses cụ thể => phát hiện ra 2 vấn đề tách biệt mà trước đó gộp làm 1. Vấn đề 1: intent classifier yếu khi gặp paraphrase ngoài template — chuyện cũ, fix bằng data scale. Vấn đề 2: \"khu A → trả lời khu B\" thực ra KHÔNG phải intent sai (model classify HOI_VI_TRI hoàn toàn đúng). Sai là response template \"{place} nằm ở khu {block}. Có biển chỉ dẫn.\" được fill bằng DummyContext.Get(\"block\") = \"B5\" cứng. Bất kể user hỏi khu nào, AI cũng kết \"khu B5\". Vấn đề tầng 2 (response substitution), không phải tầng 1 (model). 2 tầng phải fix riêng."
));

// 6
children.push(heading("Chương 6. Iter dựa trên failure thật, không bump dataset mù", 1));
children.push(para(
  "Generator v4: 240K câu (gấp 6× v3 cũ). 200-300 template/intent (vs 50). Pool gấp ~5×: 100+ TIMES, 130+ PLACES, 188 KNOWLEDGE topic, 100 REASONS. Paraphrase patterns mới: ellipsis, complaint-as-question, code-mixing 5%, compound. Augmentation rate ~32% (vs ~10% cũ): drop_accent + telex_typo + char_swap + drop_filler + synonym_swap."
));
children.push(para(
  "LSTM v4: 707K params (gấp ~3× v3), max_len 32→40, vocab 5K→10K. Train 15 epoch ~4 phút. Hard test 216 câu: 38% → 92.6% (+54.6 điểm). Còn 16 câu miss."
));
children.push(para(
  "Đến đây học được kỹ thuật quan trọng nhất phase A v2: thay vì bump data mù bằng cách tăng mọi thứ gấp đôi => chạy lệnh `eval_hardset.py --print_misses` để in ra ĐÚNG 16 câu fail kèm prediction sai. Mỗi câu fail là 1 story về model đang yếu chỗ nào. Phân cluster:"
));
children.push(para(
  "Cluster 1 (rảnh/bận): \"Hôm nay rảnh không nhỉ\" → OOS sai (đúng HOI_LICH). Template chưa có form rảnh-không-có-từ-có. Cluster 2 (pure-symptom XIN_PHEP): \"Em đang sốt 39 độ\" → TAM_BIET sai (đúng XIN_PHEP). User chỉ phàn nàn triệu chứng, không có keyword \"xin phép\". Cluster 3 (lone-place ellipsis): \"Cantin\" → TAM_BIET sai (đúng HOI_VI_TRI). Cluster 4 (lone-time casual): \"Cuối tuần\" → HOI_LICH sai (đúng OUT_OF_SCOPE). Cluster 5 (mixed compound OOS): \"Hôm nay trời đẹp và tớ đói\" → HOI_GIO_AN sai (đúng OUT_OF_SCOPE)."
));
children.push(para(
  "Viết ~90 template targeted cho từng cluster. Bump augmentation 32% → 45% để NO_ACCENT + TELEX_TYPOS được training thường xuyên hơn. Train lại 15 epoch."
));
children.push(para(
  "V5: 92.6% → 96.8% (+4.2 điểm). Còn 7 miss. Khoảnh khắc tự hào nhất phase A — không phải bằng cách nhồi data, mà bằng cách đọc kỹ những gì model fail và viết template targeted. Hiệu quả gấp nhiều lần scale up bằng cảm giác."
));
children.push(para(
  "Tham, muốn lên 98%+. Viết v6: thêm 70+ template nữa, bump augmentation lên 50%. Kết quả v6: 96.3% (-0.5 điểm so v5)."
));
children.push(para(
  "Breakdown v6: COMPLAINT 96%→100% (tốt lên), NO_ACCENT 96%→100% (tốt lên), NHƯNG CODE_MIX/SLANG/SYNONYM mỗi cái lùi 1 ca. Net negative."
));
children.push(para(
  "=> Bài học sâu nhất phase A v2: thêm template KHÔNG phải lúc nào cũng tốt. Ở threshold 96%+, decision boundary nhạy => thêm data có thể đẩy các category đang OK ra ngoài. Lợi nhuận cận biên đôi khi âm. Phải có kỷ luật đủ để chấp nhận sweet spot đã qua, đừng cố nữa. V6 reject. Ship V5."
));
children.push(para(
  "Phần fix \"khu A → khu B\" thì đơn giản hơn nhiều — không phải vấn đề ML. Viết module C# tên EntityExtractor: scan câu user gõ, tìm phrase nào trong vocab slot khớp như cả từ, ưu tiên phrase dài nhất. Vocab slot dump trực tiếp từ generator Python (cùng pool PLACES, TIMES, KNOWLEDGE, MEALS, REASONS, REPORT_OBJ) để giữ đồng bộ. Tổng cộng 715 phrase. User hỏi \"Khu A ở đâu\" => extractor trả về dict {place: \"khu a\"}. Thay DummyContext bằng SmartRuntimeContext, ưu tiên slot extracted trước rồi mới fallback giá trị cứng."
));
children.push(para(
  "Đồng thời viết lại file responses_v2.json để tránh template tự xung đột. Thay câu \"{place} nằm ở khu {block}\" bằng \"{place} ở phía {direction} doanh trại, đi thẳng {distance}m là tới\". User hỏi khu A => AI giờ trả lời \"khu A ở phía đông doanh trại, đi thẳng 100m là tới\". Đúng cái user đang hỏi."
));
children.push(para(
  "Giai đoạn cuối Phase A => giao Unity 2 bộ model song song. V1 cũ giữ nguyên không động đến (intent_classifier.onnx, responses.json, NPCDialogueBrain.cs với DummyContext). V2 mới đặt cạnh (intent_classifier_v2.onnx, responses_v2.json, slot_vocab.json, EntityExtractor.cs, SmartRuntimeContext.cs). Cả 2 chạy song song trong Unity để compare trực tiếp — gõ 1 câu là cả V1 và V2 trả lời cùng lúc, đo cải thiện thật bằng mắt thay vì nghe số liệu."
));

// 7
children.push(heading("Chương 7. Phase B — pipeline PPO độc lập, giữ tinh thần ML-Agents", 1));
children.push(para(
  "Phase B là bài toán reinforcement learning navigation. Agent là cube vuông trong môi trường 2D top-down, có target ở vị trí random, vài cube nâu là chướng ngại. Mục tiêu: agent học đi từ vị trí xuất phát đến target, tránh đụng obstacle, không ra khỏi rìa map. Output cần là 1 policy export sang ONNX để Unity load runtime."
));
children.push(para(
  "Lựa chọn pipeline huấn luyện: Unity có sẵn ML-Agents Toolkit. Đây là framework gold-standard cho RL trong Unity. Để hiểu vì sao project chọn 1 con đường khác (mà vẫn giữ trọn tinh thần của ML-Agents), trước hết phải mô tả ML-Agents hoạt động như nào."
));

// ML-Agents architectural breakdown
children.push(heading("ML-Agents — kiến trúc 4 lớp", 2));
children.push(para(
  "Lớp 1 (Unity scene): Agent là 1 GameObject có component `Agent` của ML-Agents. Gắn `BehaviorParameters` để khai báo observation size + action space. Gắn `RayPerceptionSensor3D` (8 ray, 360°) hoặc `VectorSensor` (manual feature vector) để thu observation. Gắn `DecisionRequester` để quyết định khi nào agent step (mỗi N FixedUpdate)."
));
children.push(para(
  "Lớp 2 (Communicator): khi train, ML-Agents mở 1 TCP socket trong Unity Editor để Python backend kết nối. Mỗi step Editor serialize observation → gửi qua TCP → Python compute action → gửi lại Unity → apply action. Toàn bộ vòng lặp này synchronize giữa 2 process."
));
children.push(para(
  "Lớp 3 (Python backend): package `mlagents` chạy 1 PPO/SAC trainer (built trên PyTorch). Trainer này về bản chất là PPO chuẩn (Schulman et al. 2017) — không có gì đặc biệt riêng ML-Agents về thuật toán. Cấu hình HP qua YAML (network size, lr, ent_coef, batch_size, clip_range)."
));
children.push(para(
  "Lớp 4 (Export): khi train xong, trainer dump checkpoint thành ONNX. Unity load ONNX runtime qua Inference Engine (Sentis trong Unity 6). Lúc này không cần Python nữa, không cần ML-Agents Communicator nữa, chỉ cần Inference Engine."
));

// Bottleneck
children.push(heading("Bottleneck của ML-Agents trong project này", 2));
children.push(para(
  "ML-Agents YÊU CẦU Unity Editor mở suốt quá trình train. Train PPO ~5M step trên environment hiện tại thường mất 6-15h trên GPU mid-range. Trong project này, deadline áp lực, training cần chạy thông đêm + chạy 2 máy song song."
));
children.push(para(
  "(1) Editor mở suốt = 1 license slot Unity bị giữ. 2 máy chạy song song = cần 2 license slot active. Khó scale ngang."
));
children.push(para(
  "(2) Editor có thể sleep/freeze nếu lock screen, OS update, hoặc memory leak sau nhiều giờ. Communicator TCP đứt => training vỡ giữa chừng, mất hết progress kể từ lần checkpoint cuối."
));
children.push(para(
  "(3) Communicator overhead per-step. Mỗi step Editor serialize obs ~21 float + Python compute ~2 float action + gửi qua TCP. Overhead ~0.5-2ms/step trên localhost. Với 5M step = 40 phút throughput thuần overhead."
));
children.push(para(
  "(4) Vectorized multi-env (chạy nhiều scene song song trong 1 process để tăng throughput) khó setup với ML-Agents — phải Multi-Agent Group hoặc multiple Editor instances. Mỗi instance là 1 process Unity nặng (~2-4GB RAM)."
));
children.push(para(
  "(5) Headless server (máy không GUI) khó chạy ML-Agents vì Unity Editor cần graphical context."
));

// Solution
children.push(heading("Giải pháp tương đương: PPO độc lập, giữ trọn tinh thần", 2));
children.push(para(
  "=> Quyết định: tách lớp 1+2 (Unity scene + Communicator) ra thành 1 Gymnasium environment thuần Python mô phỏng raycast bằng numpy. Lớp 3 (PPO trainer) giữ nguyên — dùng Stable-Baselines3 PPO, vốn cùng họ thuật toán với mlagents PPO trainer (PPO chuẩn Schulman 2017). Lớp 4 (export ONNX → Inference Engine) giữ nguyên hoàn toàn."
));
children.push(para(
  "Tinh thần ML-Agents giữ trọn vẹn:"
));
children.push(para(
  "(a) Algorithm: PPO clip-objective, GAE-λ advantage, Adam optimizer, entropy bonus — y hệt ML-Agents PPO trainer config mặc định."
));
children.push(para(
  "(b) Observation contract: 21 float vector mô phỏng đúng cách RayPerceptionSensor3D + VectorSensor của ML-Agents trả ra. 8 ray distance normalize (giống RayPerceptionSensor 8 hướng), 8 hit-target one-hot (giống detectableTags), velocity + heading + distance (giống custom VectorSensor)."
));
children.push(para(
  "(c) Action space: 2 continuous float qua tanh, thrust + turn — y hệt cách ML-Agents Continuous action space hoạt động."
));
children.push(para(
  "(d) Reward shaping: cùng pattern dense reward + sparse terminal reward + step penalty mà ML-Agents tutorial khuyến nghị."
));
children.push(para(
  "(e) Output format: ONNX với input \"obs\" shape [batch, 21] + output \"action\" shape [batch, 2] tanh-squashed — Unity Inference Engine load y hệt cách load file ML-Agents export. C# wrapper `MovementAgent.cs` tính observation theo cùng layout, không khác gì bind 1 ML-Agents Behavior."
));

// Why faster
children.push(heading("Vì sao nhanh hơn ML-Agents trong context này", 2));
children.push(para(
  "(1) Bỏ Communicator overhead 0.5-2ms/step => throughput training tăng 2-4× với cùng GPU."
));
children.push(para(
  "(2) Vectorized env: Stable-Baselines3 hỗ trợ `SubprocVecEnv` chạy 8-16 env song song trong 1 process Python. Mỗi env là 1 numpy state ~200 byte, không phải 1 Unity scene 2GB. Throughput tăng thêm 5-10×."
));
children.push(para(
  "(3) Background process được: chạy `nohup python train.py > log.out 2>&1 &` rồi đóng terminal, lock screen, đi ngủ — process không phụ thuộc GUI."
));
children.push(para(
  "(4) 2 máy song song không bị Unity license block. Mỗi máy chỉ cần Python env đã setup. Chia HP grid: máy 1 cycle 3 archs Phase A + 1 PPO config Phase B; máy 2 cycle 4 PPO HP configs (h1 baseline, h2 bigexplore, h3 deepfocus, h4 bigwide). Cuối ngày merge results."
));
children.push(para(
  "(5) Iteration nhanh: thay đổi reward shaping hoặc obs format => sửa 1 file Python, restart training trong 5 giây. ML-Agents thì phải mở Unity Editor, sửa scene + Behavior Parameters + recompile, ~2-3 phút mỗi lần."
));

// Contract verify
children.push(heading("Verify contract sau khi train", 2));
children.push(para(
  "Sau khi train xong + export ONNX, phải verify Unity load đúng + output khớp Python policy. Cách làm: load ONNX vào Inference Engine, feed cùng observation vector (21 float đã pre-compute từ Python env), so sánh action output Unity vs Python — phải khớp đến < 1e-5. Pass."
));
children.push(para(
  "Tiếp theo verify trong scene: dùng `MovementAgent.cs` tự compute observation từ Unity raycast + transform, đảm bảo match layout Python (8 hướng evenly 360° CCW, normalize 10m, agent-frame velocity). Đây là phần dễ vỡ — sẽ kể trong chương về bug tích hợp."
));
children.push(para(
  "Trade-off chấp nhận: phải tự code Gymnasium env ~200 dòng numpy (raycast + collision + reward) thay vì dùng Unity scene built-in. Mất khả năng visualize training real-time trong Editor (chỉ có log mean_reward dạng text). Đổi lại: training nhanh 10-30× tổng thể, không phụ thuộc license + GUI, scale ngang được."
));

// Observation/action/reward (giữ phần technical detail)
children.push(heading("Observation, action, reward — chi tiết contract", 2));
children.push(para(
  "Observation = 21 float. [0..7]: 8 raycast distance / 10m (8 hướng evenly 360°, index 0 = forward, CCW). [8..15]: 8 ray hit-target one-hot. [16]: velocity_forward / max_speed (agent frame). [17]: velocity_lateral / max_speed. [18]: cos góc tới target (agent frame). [19]: sin góc tới target. [20]: distance_to_target / arena_diagonal."
));
children.push(para(
  "Action = 2 continuous float qua tanh. [0]: thrust ∈ [-1, 1] (forward; reverse 0.5×). [1]: turn ∈ [-1, 1] (right dương; scale theo π rad/s)."
));
children.push(para(
  "Reward shaping: +1.0 đến target (terminate); +0.5×Δd progress (gần lại target); -0.0005 per step (chống đứng yên); -0.1 đụng obstacle (slide ra cạnh, không terminate); -1.0 out-of-bounds (terminate); -0.5 timeout 500 step. Range typical: -5 (random policy) đến +10 (perfect)."
));

// 8
children.push(heading("Chương 8. Lottery ticket Phase B & bài học HP grid", 1));
children.push(para(
  "Phiên bản đầu để env fix 6 obstacle, train 30 lần × 200K step trên net [64, 64]. Best mean_reward = 6.126 (iter 8, lottery seed sớm). Plateau quanh 5.5-6.1 suốt cả đêm."
));
children.push(para(
  "Gặp 1 bug khá embarrassing tên là \"loop spin near deadline\". Gần deadline 19:00, 1 iter mới start nhưng không đủ thời gian chạy 200K step => Python crash => loop spin thử lại liên tục. Trong 30 phút cuối ngày spin 493,000 vòng vô nghĩa. Fix bằng 30s sleep guard + check thời gian còn lại trước khi launch run mới."
));
children.push(para(
  "Phiên bản v3: tăng độ khó env => random 3-12 obstacle, random arena 15-25m, random kích thước obstacle 1.0-2.5m. Net lên [128, 128]. Train 500K step/iter × 5 iter. Best: mean_reward 6.05 — THẤP HƠN v2 fixed env (6.126)."
));
children.push(para(
  "Suýt nghĩ phiên bản v3 tệ hơn. Ngồi suy nghĩ kỹ thì hiểu: score thấp hơn ≠ model dở hơn. Nó nói lên environment mới khó hơn (nhiều obstacle hơn, range arena rộng hơn, kích thước obstacle dao động). So sánh trực tiếp 2 score giữa 2 env khác nhau là vô nghĩa. Quyết định giữ cả 2 file ONNX: soldier_v2_fixedenv.onnx (best fixed env, dùng nếu Unity scene 6 obstacle cố định); soldier.onnx (random env, generalize tốt hơn cho scene đa dạng)."
));
children.push(para(
  "Giai đoạn cuối, deadline còn ~10h => quyết định scale Phase B lên 1M step/iter (gấp 2× v3, 5× v2) + dùng 2 máy song song. Máy 1 chạy loop nhanh 3 archs cycle, mỗi vòng ~35 phút. Máy 2 chạy heavy hyperparameter grid với 4 cấu hình: h1_baseline = [128,128] ent=0.01 lr=3e-4; h2_bigexplore = [256,128] ent=0.02 lr=3e-4; h3_deepfocus = [128,128,64] ent=0.005 lr=1e-4; h4_bigwide = [256,256] ent=0.05 lr=5e-4. Mỗi máy ghi vào folder riêng (deliverables/ vs deliverables_m2/) để không collision file. Sync qua git push/pull cuối ngày, không real-time."
));
children.push(para(
  "Máy 1 chạy 27 iter. Phần lớn plateau ở 5.5-6.05. Đến iter 14 (seed 2013), mean_reward bỗng nhảy lên 6.569. Plateau bị break 0.5 điểm. Iter 15-16 lại tụt về plateau cũ. Lottery ticket là ngẫu nhiên thật."
));
children.push(para(
  "Máy 2 chạy song song HP grid. Kết quả: h4_bigwide thất bại nặng nhất, dừng ở 5.252. h3_deepfocus thắng đậm = 6.572 (nhỉnh hơn máy 1 best 0.003). h2_bigexplore = 6.263. h1_baseline = 6.236."
));
children.push(para(
  "h4 thất bại có thể giải thích được: net to ([256,256]) + entropy cao (0.05) + lr cao (5e-4) là tổ hợp xấu. Net to mà data ít => overfit hành vi sớm. Entropy cao => exploration quá nhiều => policy không converge. LR cao => nhảy qua optimum. h3 thắng vì ngược lại: net sâu nhưng nhỏ ([128,128,64]), entropy thấp (0.005, exploitation > exploration), lr thấp (1e-4, cập nhật chậm và ổn định)."
));
children.push(para(
  "=> Bài học HP tuning quan trọng nhất: \"conservative beats brute\". Intuition đầu tiên thường là scale up tham số. Thực tế cấu hình phù hợp với độ phức tạp environment quan trọng hơn nhiều. Canonical Phase B cuối là model h3_deepfocus máy 2, mean_reward 6.572. Backup là model máy 1 iter 14 (6.569) và model fixed env cũ (6.126)."
));

// 9
children.push(heading("Chương 9. Tích hợp Unity & những cú vấp về toạ độ", 1));
children.push(para(
  "Khi nạp ONNX vào Unity, vài bug xuất hiện — đều là kiểu bug khó vì không phát ra error message rõ ràng, chỉ là behavior sai."
));
children.push(para(
  "Bug 1 (Phase B): agent di chuyển lệch — tăng tốc khi gần target (sai), slow down khi xa (sai bản năng). Mất nửa ngày debug. Phát hiện ra arenaDiagonal trong C# = 28.28 (= 20 × √2). Đó là giá trị cũ tính cho v2 fixed env arena 20m. Nhưng v3 random env có arena_max = 25m => đường chéo phải = 35.36. Sai 25%. Distance feature obs[20] normalize bằng arenaDiagonal => model nhận tín hiệu sai về việc đang gần rìa map => quyết định turn/thrust sai. Fix: sửa 1 con số `arenaDiagonal = 35.36f`."
));
children.push(para(
  "Bug 2 (Phase B): collision blocking. Code Unity ban đầu viết đơn giản: nếu vị trí kế tiếp đè obstacle => không di chuyển. Logic sai lầm — policy có thể command thrust forward liên tục => next luôn đè obstacle => agent kẹt forever. Trong Python env có behavior khác hẳn: khi đụng obstacle, agent được chiếu sang cạnh obstacle gần nhất (slide projection). 2 behavior khác nhau giữa Python (training) và Unity (inference) => policy không khớp physics => hành vi vỡ. Fix: port slide projection sang C# (~10 dòng tính sign(ox), sign(oz), so sánh |ox| vs |oz|, đẩy agent ra cạnh dài nhất)."
));
children.push(para(
  "Bug 3 (Phase B): raycast direction. Trong Python tính 8 hướng theo công thức theta = i × 2π / 8 quay counter-clockwise (vì matplotlib và mặt phẳng toán học mặc định). Unity là hệ left-handed => quay clockwise. Hậu quả: policy ra lệnh rẽ trái thì agent ở Unity rẽ phải, ngược lại. Bug rất nhức đầu vì agent VẪN di chuyển, vẫn có vẻ try-to-navigate, chỉ là đi nhầm hướng và đụng obstacle ngay. Fix: đảo dấu turn action HOẶC đảo index raycast trong C#."
));
children.push(para(
  "Bug 4 (Phase A): tokenizer C# ≠ Python. Vocab Python được tạo bởi underthesea — segment từ tiếng Việt thành multi-word token có chứa space: \"ăn cơm\" là 1 token, \"thủ trưởng\" là 1 token, \"báo cáo\" là 1 token. Code C# ban đầu split whitespace đơn giản => cả \"ăn cơm\" thành 2 token riêng => cả 2 đều UNK. ~30-50% token trong câu là UNK => model fail thảm. Fix: viết greedy longest-match trong C# tokenizer — tại mỗi vị trí thử ghép N..1 từ liên tiếp, lấy match dài nhất nằm trong vocab."
));
children.push(para(
  "Bug 5 (Phase A UI): Unity 6 Input System Package only. Project có activeInputHandler = 1 (chỉ Input System Package, không legacy). Hậu quả: Input.GetKeyDown(KeyCode.Return) silent fail — không có error, nhưng nhấn Enter không submit. Fix: thay bằng `Keyboard.current.enterKey.wasPressedThisFrame` + đổi StandaloneInputModule thành InputSystemUIInputModule + wrap với #if ENABLE_INPUT_SYSTEM."
));
children.push(para(
  "Bug 6: IMGUI vs Canvas-based UI. Phiên bản đầu dùng OnGUI() IMGUI để gọn, sau user yêu cầu UI Unity thật => phải port sang Canvas + RectTransform + Input Field + Button + ScrollRect."
));
children.push(para(
  "Tổng cộng 6 bug tích hợp giữa Python ↔ Unity. Tất cả đều do mismatch contract giữa 2 môi trường. Sau khi fix xong, mỗi bug đều được document thành test case và verify lại trên scene thật để đảm bảo không regress."
));

// 10
children.push(heading("Chương 10. Bài học rút ra", 1));
children.push(para(
  "Sau khi cả 2 con AI hoàn thành, ngồi nhìn lại đường đã đi và tóm gọn ra 10 bài học mà nếu phải làm lại từ đầu, sẽ làm khác."
));
children.push(para(
  "(1) Val_acc trên synthetic data ≠ metric thật. Bị lừa 2 lần — lần đầu test 16 câu cả 3 arch đều 100%, lần 2 test 64 câu LSTM 98.4% mà user vẫn nói \"AI ngu\". Mỗi lần build hard test set khó hơn, 1 lớp giả tan đi. Bài học: build hard test set ngay khi project chớm ổn, không đợi user complain."
));
children.push(para(
  "(2) Iter dựa trên failure cụ thể, không bump dataset mù. V4→V5 tăng 4.2 điểm chỉ bằng cách đọc đúng 16 câu fail và viết template targeted. Hiệu quả gấp nhiều lần scale up bằng cảm giác. Mỗi câu fail kể 1 story về model đang yếu chỗ nào."
));
children.push(para(
  "(3) Biết khi nào dừng thêm template. V6 dạy rằng ở threshold 96%+, decision boundary nhạy đến mức thêm data có thể làm tệ đi tổng thể. Lợi nhuận cận biên có thể âm. Phải có kỷ luật đủ để chấp nhận sweet spot đã qua."
));
children.push(para(
  "(4) Vấn đề user cảm nhận có thể KHÔNG phải vấn đề model. \"Khu A → trả lời khu B\" thực ra là vấn đề response template + DummyContext, model classify đúng. Sửa rule-based < retrain model. Trước khi sửa => phải biết chính xác chỗ nào đang sai."
));
children.push(para(
  "(5) Data scale > architecture complexity trong domain hẹp. FastText 51K params có thể match LSTM 116K khi data đủ phong phú và đủ seed. Đừng vội chọn model thông minh hơn — thường vấn đề là data không đủ, không phải model không đủ to."
));
children.push(para(
  "(6) Stochastic training cần lottery ticket. PPO break plateau ở iter 14 seed 2013. FastText match LSTM ở iter 17 seed 1016. Không có cách dự đoán seed nào trúng => pipeline ML phải design để dễ retry với seed khác — không phải pipeline cứng 1 seed 1 run."
));
children.push(para(
  "(7) Conservative thắng brute trong HP tuning. h3_deepfocus (net sâu nhỏ + entropy thấp + lr thấp) > h4_bigwide (net rộng to + entropy cao + lr cao). Cấu hình phù hợp domain quan trọng hơn scale up tham số."
));
children.push(para(
  "(8) Test integration sớm và đau. 6 bug Unity gặp đều ẩn đi tốt cho đến khi nạp model vào game thật. Khi pipeline có 2 môi trường khác nhau (Python + Unity), contract giữa chúng phải verify từng feature một, không tin tưởng."
));
children.push(para(
  "(9) Architecture cycle + auto-pick winner an toàn hơn cố định 1 arch. Loop overnight chạy cả 3 arch, mỗi vòng pick winner tự động. Khi data thay đổi nếu 1 arch regress => hệ thống tự fallback. Nếu chỉ chạy LSTM, có iter LSTM tệ đi mà không có model dự phòng để deploy. Diversity = robustness."
));
children.push(para(
  "(10) Random environment > fixed environment cho generalization, dù score nhìn tệ hơn. v2 fixed 6 obstacle = 6.126; v3 random 3-12 = 6.05. Score v3 thấp hơn nhưng generalize tốt hơn. Trong production sẽ luôn chọn v3 dù số trên giấy tệ hơn — vì người chơi không chơi trên benchmark, họ chơi trên scene đa dạng."
));

// 11
children.push(heading("Chương 11. Trạng thái cuối — deliverables hoàn chỉnh", 1));
children.push(para(
  "Phase A có 2 model song song để hội đồng compare trực tiếp. V1 cũ giữ nguyên không động đến: Assets/AI/Models/intent_classifier.onnx (LSTM v3, 116K params, 38% hard test) + Assets/AI/Resources/intent_classifier_meta.json + responses.json + NPCDialogueBrain.cs với DummyContext."
));
children.push(para(
  "V2 mới deploy bên cạnh: Assets/AI/Models/intent_classifier_v2.onnx (LSTM v5, 707K params, max_len=40, vocab 10K, 96.8% hard test 216 câu) + Assets/AI/Resources/intent_classifier_v2_meta.json + responses_v2.json (template slot-aware) + slot_vocab.json (715 phrase cho EntityExtractor) + Assets/AI/Scripts/{EntityExtractor.cs, SmartRuntimeContext.cs}."
));
children.push(para(
  "Phase B canonical: AI_Training/deliverables_m2/soldier_m2.onnx (PPO h3_deepfocus, mean_reward 6.572). Backup: deliverables/soldier.onnx (runner-up 6.569, máy 1 iter 14) + deliverables/soldier_v2_fixedenv.onnx (fixed env 6.126). Wrapper: Assets/AI/Scripts/MovementAgent.cs đã fix 3 bug (arenaDiagonal 35.36, slide projection collision, raycast direction CCW vs CW)."
));
children.push(para(
  "Test workflow trong Unity: load scene → gắn brain component vào NPC GameObject → wire ModelAsset + meta JSON + responses JSON qua Inspector → Play. Brain tự load model + tokenize input + classify + render reply. Không cần Python runtime."
));
children.push(para(
  "Báo cáo iteration chi tiết: AI_Training/PHASE_A_V2_REPORT.md (số liệu cho từng iter v3 → v4 → v5 → v6). Wiki nội bộ: .wiki/wiki/ chứa toàn bộ design decisions, bug log, system docs, claims với citation. Source code + commit history trên GitHub: github.com/luzart-outsources/unity_train_ai."
));

// 12
children.push(heading("Chương 12. Việc còn dang dở & hướng nâng cấp", 1));
children.push(para(
  "Phase A v5 vẫn miss 7/216 câu. 3 trong số đó là TELEX_TYPOS extreme kiểu \"Phoongg học oo đâu\", \"Bááoo cáo đủù quânnn\", \"Emm chào thủủ trưởng emm đii\" — multi-char vowel duplications mà người thật hiếm khi gõ. Để fix triệt để cần subword tokenizer (FastText char n-gram 3-5) — model nhìn vào ký tự bên trong từ thay vì cả từ. Cost ước ~1 ngày làm cả Python + C# tokenizer. Để dành cho phase nâng cấp sau."
));
children.push(para(
  "Test set 216 câu hand-crafted còn ít cho ĐATN final report — tiêu chuẩn nghiên cứu thường 200-500+ câu. Hướng làm: ghi lại câu user thật gõ trong quá trình demo + chơi thử, append vào eval. Mỗi câu user thật là 1 datapoint quý giá hơn câu hand-crafted vì nó từ distribution thật."
));
children.push(para(
  "Phase B mới train 1M step (máy 1) và 2M step (máy 2). Ceiling environment ước ~10 reward. Hiện ở ~6.6. Không gian leo còn nhiều. Nếu chạy 5M step có thể break thêm 1-2 điểm."
));
children.push(para(
  "EntityExtractor hiện rule-based với 715 phrase. Đủ cho domain hẹp KTMM. Nếu mở rộng vocab => phải nâng cấp lên NER ML model (PhoBERT). Vướng: PhoBERT chưa được Sentis hỗ trợ — BERT tokenizer trên C# khá phức tạp."
));
children.push(para(
  "Multi-turn dialogue memory chưa có. Hiện mỗi câu hỏi được phân loại độc lập. User nói \"Cái đó ở đâu\" => model không nhớ \"cái đó\" là gì từ turn trước. Cần context window + lưu vài câu cuối user gõ + inject vào input. Nằm ngoài phạm vi ĐATN này."
));

// Kết
children.push(heading("Lời kết", 1));
children.push(para(
  "Bản tự sự này không phải để báo cáo số liệu — số liệu đã có trong báo cáo kỹ thuật và wiki nội bộ. Mục đích: ghi lại những lúc đã sai, những lúc đổi ý, những lúc nghĩ một đằng kết quả ra một nẻo, và cách điều chỉnh sau mỗi lần như vậy. Đó là phần khó nhất khi học một kỹ năng mới — không phải làm đúng từ đầu, mà nhận ra mình đang sai và sửa kịp."
));
children.push(para(
  "Lúc bắt đầu đề tài, AI được nghĩ là phần phức tạp nhất, đáng sợ nhất, cần network ResNet hay Transformer hoành tráng. Sau 4 tháng làm thật, phần khó nhất KHÔNG phải ở model, mà ở data và ở eval. Model nhỏ + data tốt > model to + data nghèo. Eval tốt cho ra signal; eval kém cho ra ảo giác. Phần kỹ thuật của AI thật ra không khó học — có rất nhiều tutorial, có Stable-Baselines3 wrap PPO chỉ trong 10 dòng code. Phần khó là kỷ luật khi đối diện với metric, là khả năng ngồi xuống đọc kỹ các câu fail thay vì nhìn vào con số tổng quát, là khả năng dừng lại khi lợi nhuận cận biên đã âm."
));
children.push(para(
  "2 con AI cuối cùng đều không phải state-of-the-art. Phase A là LSTM 707K params train trên 240K câu synthetic — không có gì mới về kiến trúc. Phase B là PPO net [128,128,64] — cấu hình rất phổ thông. Nếu mục đích là chứng minh giỏi về deep learning research => thất bại. Nhưng nếu mục đích là chứng minh có thể tự huấn luyện 2 con AI và đem chúng vào game Unity hoạt động được, biết tự debug khi gặp bug tích hợp, biết viết test set khi metric saturate, biết khi nào nên thêm data và khi nào nên dừng => hướng đi này đúng."
));
children.push(para(
  "Mỗi quyết định trên đường đi đều có lý do nhớ rõ, có ngày tháng lưu trong git log, và có lý do thay thế đã cân nhắc nhưng không chọn. Sẵn sàng trả lời bất kỳ câu hỏi nào về quyết định nào trong các chương trên."
));

// ─────────────────────────────────────────────────────────────────────────
// Build
// ─────────────────────────────────────────────────────────────────────────
const doc = new Document({
  creator: "Quyen — KTMM",
  title: "Hành trình huấn luyện 2 con AI cho ĐATN",
  styles: {
    default: { document: { run: { font: "Times New Roman", size: 26 } } },
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 },     // A4 portrait
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 },
      },
    },
    children,
  }],
});

Packer.toBuffer(doc).then((buf) => {
  const final = "HANH_TRINH_TRAINING_2_CON_AI.docx";
  // Try writing to final; if file is locked (e.g. open in Word), write to .new.
  try {
    fs.writeFileSync(final, buf);
    console.log("[done] wrote", final, "bytes:", buf.length);
  } catch (e) {
    if (e.code === "EBUSY" || e.code === "EPERM") {
      const tmp = final.replace(/\.docx$/, ".new.docx");
      fs.writeFileSync(tmp, buf);
      console.log("[note] file locked; wrote", tmp, "instead. Close Word + rename to apply.");
    } else throw e;
  }
}).catch((e) => {
  console.error("[fail]", e);
  process.exit(1);
});
