// Build noob-friendly docx — simpler vocabulary, less jargon, ~2000 words.
// Output: HANH_TRINH_TRAINING_2_CON_AI_DON_GIAN.docx

const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun,
  AlignmentType, HeadingLevel,
} = require("docx");

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

const children = [];

children.push(title("Hành trình huấn luyện 2 con AI cho đồ án tốt nghiệp"));
children.push(subtitle("Bản kể chuyện ngắn gọn, dễ đọc"));

// ─────────────────────────────────────────────
children.push(heading("1. Mở đầu — vì sao game cần đến 2 con AI", 1));
children.push(para(
  "ĐATN là 1 game mô phỏng học kỳ quân đội tại Học viện Kỹ thuật Mật mã, làm trên Unity. Trong game có 2 chỗ cần đến AI thật, không thể code tay theo kiểu \"nếu người chơi nhập câu này thì NPC trả lời câu kia\":"
));
children.push(para(
  "Chỗ thứ nhất là sĩ quan chỉ huy — người chơi gõ tiếng Việt vào để hỏi lịch, hỏi giờ ăn, xin phép. Người thật gõ chữ thì lệch dấu, viết tắt, chêm tiếng Anh => không có cách nào liệt kê hết câu hỏi để code tay. Phải có AI biết \"hiểu\" ý câu."
));
children.push(para(
  "Chỗ thứ hai là lính NPC — phải tự đi từ A → B trong doanh trại, tránh chướng ngại như bàn ghế, người đứng chắn. Nếu code đường đi cứng tay, nhìn vào là biết robot ngay. Phải có AI tự học cách đi."
));
children.push(para(
  "=> Tóm gọn quá trình làm 2 con AI này, từ lúc bắt đầu sai đến lúc đi đến kết quả dùng được."
));

// ─────────────────────────────────────────────
children.push(heading("2. Con AI thứ nhất — sĩ quan chỉ huy hiểu tiếng Việt", 1));
children.push(para(
  "Cách AI này làm việc khá đơn giản nếu giải thích bằng ví dụ. Người chơi gõ \"Mấy giờ ăn cơm?\", AI nhìn câu này và quyết định đây là loại \"hỏi giờ ăn\" (1 trong 8 loại đã định trước: hỏi lịch, hỏi giờ ăn, hỏi vị trí, xin phép, báo cáo, hỏi kiến thức, tạm biệt, nói chuyện linh tinh). Sau đó tra trong danh sách câu trả lời mẫu cho loại này => NPC trả lời."
));
children.push(para(
  "Để AI biết phân loại được, phải cho nó học. Cách học: viết khoảng 500 câu mẫu (mỗi loại ~60 câu), cho AI đọc đi đọc lại nhiều lần. Lần đầu thử nhỏ vậy thôi: 500 câu, từ vựng còn nghèo. Train xong AI báo \"đã học chính xác 100%\". Vui mừng vì tưởng xong sớm."
));
children.push(para(
  "Cảm giác đó kéo dài đúng nửa ngày. Tự nghĩ ra 8 câu khác — chưa từng cho AI thấy bao giờ — đưa vào để thử => AI đoán đúng đúng 1 trên 8 câu, tức 12.5%. Một trên tám."
));
children.push(para(
  "=> Bài học đắt nhất ngày đầu: AI báo \"học chính xác 100%\" không có nghĩa là AI giỏi. Nó chỉ chứng minh AI đã \"thuộc lòng\" đúng những câu đã được cho thấy. Câu mới hơi khác một chút thôi là AI bí. Lỗi này tên kỹ thuật là overfit, nhưng dễ hiểu hơn nếu nghĩ như học sinh học vẹt — thuộc đề thi mẫu thì 10 điểm, đề mới khác chút là 0 điểm."
));
children.push(para(
  "Sửa lại từ đầu. Viết 1 cái máy tự sinh câu (template generator) — đưa cho nó vài chục cấu trúc câu + vài trăm từ trong các pool về thời gian, địa điểm, đồ ăn, vũ khí, kiến thức. Cho máy chạy => sinh ra 240 nghìn câu, gấp 500 lần ban đầu, từ vựng phong phú hơn nhiều. Có cả thuật ngữ KTMM (RSA, AES, mật mã, chữ ký số) bên cạnh quân đội (súng AK, lựu đạn, điều lệnh). Vì sĩ quan trong game này không phải sĩ quan thuần huấn luyện mà là sĩ quan dạy mật mã."
));
children.push(para(
  "Train xong, đo lại bằng 1 test khó hơn nhiều: 216 câu tự viết tay, gồm câu không dấu (kiểu gõ vội \"may gio thi an\"), câu chêm tiếng Anh (\"Schedule mai sao\"), câu cụt 1 chữ (\"Cantin\"), câu phàn nàn ngầm hỏi (\"Đói quá\"), câu lóng tuổi teen (\"z\", \"hum\")."
));
children.push(para(
  "AI ban đầu (train với 500 câu) đạt 38% trên test khó. AI mới (train với 240 nghìn câu + đa dạng cách diễn đạt) lên 92.6%. Tăng được hơn 50 điểm."
));
children.push(para(
  "Vẫn còn 16 câu sai. Đến đây học được kỹ thuật quan trọng nhất: thay vì cứ tiếp tục đổ thêm dữ liệu mù, ngồi xuống đọc 16 câu sai đó xem AI đang fail kiểu gì. Mỗi câu sai là 1 manh mối về điểm yếu cụ thể. Ví dụ: AI fail câu \"Em đang sốt 39 độ\" — vì câu này chỉ phàn nàn triệu chứng, không có chữ \"xin phép\" rõ ràng. Vậy phải thêm các câu kiểu \"em đang [triệu chứng]\" vào dữ liệu train. Sau khi sửa đúng các điểm yếu này, AI lên 96.8%. Còn 7 câu sai, đa số là kiểu telex bất thường \"Phoongg học oo đâu\" mà người thật ít khi gõ vậy."
));
children.push(para(
  "=> Bài học: lặp dựa trên failure cụ thể >> tăng dữ liệu mù. Đọc kỹ AI fail chỗ nào, viết thêm vài chục câu fix đúng chỗ đó, train lại. Hiệu quả gấp nhiều lần đổ thêm 100 nghìn câu ngẫu nhiên."
));

// ─────────────────────────────────────────────
children.push(heading("Vấn đề khác: \"hỏi khu A mà cứ trả lời khu B\"", 2));
children.push(para(
  "User test thử và phản hồi: AI ngu, hỏi khu A mà cứ trả lời khu B. Phản xạ đầu tiên: bực, chắc model lại dở rồi. Nhưng ngồi đọc kỹ thì phát hiện: AI thật ra phân loại ĐÚNG (biết user đang hỏi về vị trí). Sai là ở khâu sau."
));
children.push(para(
  "Câu trả lời mẫu của loại \"hỏi vị trí\" có dạng: \"{place} nằm ở khu {block}, có biển chỉ dẫn rồi.\" Trong đó {place} và {block} là 2 ô trống cần điền. Code cũ điền {block} bằng giá trị cứng \"B5\" — bất kể user hỏi khu nào, AI cũng kết luận \"khu B5\". User hỏi \"khu A ở đâu\" => câu trả lời thành \"khu A nằm ở khu B5\" — vô nghĩa."
));
children.push(para(
  "Đây không phải lỗi AI. Đây là lỗi code logic xung quanh AI. Sửa: viết 1 module nhỏ tự đọc câu user, tìm xem user đang nhắc đến địa điểm cụ thể nào (khu A, khu C, phòng 305...), rồi điền vào ô trống thay vì điền cứng. Module này không cần train — chỉ cần 1 danh sách các địa điểm và logic so sánh chuỗi. Sau khi fix, user hỏi khu A => AI trả lời \"khu A ở phía đông doanh trại, đi thẳng 100m là tới\". Đúng cái user đang hỏi."
));
children.push(para(
  "=> Bài học: vấn đề user cảm nhận không phải lúc nào cũng là vấn đề AI. Đôi khi chỉ là logic xung quanh viết cứng. Trước khi train lại AI => phải biết chính xác chỗ nào đang sai. Sửa code logic dễ hơn train lại AI rất nhiều."
));

// ─────────────────────────────────────────────
children.push(heading("3. Con AI thứ hai — lính tự đi tránh chướng ngại", 1));
children.push(para(
  "Cách AI này hoạt động khác hẳn AI hiểu câu. Không có dữ liệu mẫu để học. Thay vào đó: thả agent (con lính ảo) vào 1 sân vuông có target ở vị trí random + vài chướng ngại random => agent thử di chuyển. Đi đúng đến target => được điểm thưởng. Đụng chướng ngại => bị trừ điểm. Sau hàng triệu lần thử-và-sai như vậy, agent học được cách đi hiệu quả nhất. Đây là kiểu học gọi là reinforcement learning — học bằng phản hồi thưởng phạt."
));
children.push(para(
  "Unity có sẵn công cụ làm việc này gọi là ML-Agents — viết environment ngay trong Unity scene, kết nối với Python để train, xong xuất ra file model. Tiện và phổ biến. Nhưng có 1 ràng buộc lớn: phải mở Unity Editor suốt quá trình train (5-15 tiếng). Nếu máy ngủ, freeze, hoặc Unity Editor mất kết nối với Python giữa chừng => training vỡ, mất hết tiến độ."
));
children.push(para(
  "Trong dự án này, training cần chạy thông đêm + chạy 2 máy song song để xong nhanh trước deadline. Mở 2 Unity Editor 24/7 trên 2 máy = không thực tế. Phải tìm cách nhanh hơn nhưng vẫn giữ được mọi tinh hoa của ML-Agents."
));

// ─────────────────────────────────────────────
children.push(heading("Cách thay thế: tách phần training ra ngoài Unity", 2));
children.push(para(
  "ML-Agents thật ra gồm 2 phần ghép lại: phần \"environment\" (sân + agent + chướng ngại trong Unity scene) và phần \"trainer\" (thuật toán PPO chạy bằng Python ở phía sau). Ý tưởng giải pháp: thay phần environment Unity bằng 1 environment Python tự code (mô phỏng cùng 1 sân, cùng 1 agent, cùng raycast, cùng luật collision). Phần trainer giữ nguyên — vẫn dùng PPO chuẩn, vẫn cùng cấu hình. Output cuối cùng vẫn là 1 file model định dạng ONNX, Unity load được như mọi model ML-Agents thông thường."
));
children.push(para(
  "Nhờ vậy mọi tinh hoa của ML-Agents giữ trọn:"
));
children.push(para(
  "Cùng thuật toán: PPO chuẩn (cũng là thuật toán mặc định ML-Agents dùng). Cùng cách thiết kế observation (8 hướng raycast quanh agent, vị trí target, vận tốc) — copy đúng layout của ML-Agents RayPerceptionSensor. Cùng action (2 giá trị: tiến/lùi và rẽ trái/phải). Cùng cách thưởng/phạt. Cùng định dạng xuất file (ONNX). Unity load model ra cũng giống y hệt cách load 1 model ML-Agents. Khác duy nhất: training chạy thông qua Python độc lập, không cần Unity Editor mở."
));
children.push(para(
  "Lợi ích: training nhanh hơn nhiều lần (không có overhead truyền dữ liệu giữa Unity và Python qua TCP, có thể chạy nhiều môi trường song song trong 1 Python process). Chạy được background nohup, lock screen đi ngủ thoải mái. 2 máy chạy song song không bị Unity license block."
));

// ─────────────────────────────────────────────
children.push(heading("Train xong, gặp \"lottery ticket\"", 2));
children.push(para(
  "Train 1 triệu lượt thử mỗi vòng, qua 27 vòng trên máy 1, điểm thưởng plateau quanh 5.5-6.0 (thang -5 đến +10). Đến vòng thứ 14 (random seed 2013), điểm bỗng nhảy lên 6.57. Plateau bị break. Vòng 15-16 lại tụt về plateau cũ."
));
children.push(para(
  "=> Bài học: training kiểu này có yếu tố may mắn. Mỗi lần train với 1 random seed khác là 1 đường đi tối ưu khác. Có những seed gặp \"jackpot\" sớm, có seed lạc đường mãi không hội tụ. Không có cách dự đoán seed nào trúng — chỉ có cách thử nhiều seed song song và pick winner."
));
children.push(para(
  "Trên máy 2 thử 4 cấu hình khác nhau song song: net to/nhỏ, exploration cao/thấp, learning rate cao/thấp. Cấu hình thắng đậm: net sâu nhỏ + exploration thấp + lr thấp = 6.57 điểm. Cấu hình thua: net to + exploration cao + lr cao = 5.25 điểm. Bài học: cấu hình \"bảo thủ\" thường thắng cấu hình \"to mạnh\" khi data ít. Không phải cứ scale up tham số là tốt."
));

// ─────────────────────────────────────────────
children.push(heading("4. Tích hợp Unity — vài cú vấp về toạ độ", 1));
children.push(para(
  "Khi đem 2 model vào Unity test thật, gặp vài bug khó vì không có error message rõ ràng — chỉ là behavior sai. Đáng kể nhất 3 cái:"
));
children.push(para(
  "Bug 1 — đường chéo arena tính sai: trong Python tính cho arena 25m, đường chéo = 35.36. Trong Unity hardcode 28.28 (giá trị cũ tính cho arena 20m). Sai 25%. Hậu quả: agent nhận tín hiệu sai về việc đang gần rìa map => quyết định di chuyển sai. Fix: sửa 1 con số. Mất nửa ngày debug mới phát hiện."
));
children.push(para(
  "Bug 2 — chiều quay tia raycast: trong Python tính 8 hướng quay ngược chiều kim đồng hồ (vì hệ toạ độ toán học mặc định). Unity là hệ left-handed, quay theo chiều kim đồng hồ. Hậu quả: AI ra lệnh \"rẽ trái\" thì agent ở Unity rẽ phải, ngược lại. Bug rất nhức đầu vì agent vẫn di chuyển, vẫn có vẻ đang try-to-navigate, chỉ là đi sai hướng và đụng chướng ngại ngay. Fix: đảo dấu."
));
children.push(para(
  "Bug 3 — tách từ tiếng Việt: AI hiểu câu được train với cách tách câu kiểu \"ăn cơm\" là 1 từ ghép, \"thủ trưởng\" là 1 từ ghép. Code Unity ban đầu tách câu theo dấu cách đơn giản => mỗi từ ghép bị tách thành 2 từ riêng, AI không hiểu, ~30-50% từ trong câu trở thành \"không có trong từ điển\". Fix: tách câu theo cách ưu tiên ghép từ dài nhất tìm thấy được trong từ điển."
));
children.push(para(
  "3 bug còn lại liên quan UI Unity (Input System, IMGUI vs Canvas) — đều fix được sau vài giờ debug."
));
children.push(para(
  "=> Bài học: khi pipeline có 2 môi trường khác nhau (Python + Unity), bug hay xảy ra ở chỗ contract giữa 2 bên không khớp — toạ độ, chiều quay, cách tách từ, cách normalize số. Phải verify từng feature một, không tin tưởng."
));

// ─────────────────────────────────────────────
children.push(heading("5. Năm bài học rút ra", 1));
children.push(para(
  "(1) AI báo \"học chính xác 100%\" trên dữ liệu đã train không có nghĩa AI giỏi. Phải tự build test set khó hơn (paraphrase, không dấu, chêm tiếng Anh, ngắn cụt) để đo thật. Đừng tin số liệu val_acc trên dữ liệu cùng nguồn với training."
));
children.push(para(
  "(2) Tăng dữ liệu mù không bằng đọc kỹ AI đang fail chỗ nào rồi viết thêm câu fix đúng chỗ đó. Cách này hiệu quả gấp nhiều lần. Mỗi câu fail là 1 manh mối quý giá."
));
children.push(para(
  "(3) Đến lúc nào đó, thêm dữ liệu sẽ làm AI tệ đi (vì xáo trộn ranh giới phân loại). Khi đã ở ngưỡng cao 96%+ phải biết khi nào dừng. Cố thêm có thể đẩy các phần đang đúng sang sai."
));
children.push(para(
  "(4) Vấn đề user cảm nhận có thể không phải vấn đề AI. \"Hỏi khu A trả lời khu B\" thực ra là lỗi logic xung quanh, không phải AI sai. Đôi khi sửa code đơn giản hơn rất nhiều so với train lại model. Trước khi sửa => phải biết chính xác chỗ nào đang sai."
));
children.push(para(
  "(5) Khi pipeline có 2 môi trường (Python train + Unity inference), bug hay xảy ra ở chỗ contract giữa 2 bên không khớp. Phải verify từng feature, không tin mặc định."
));

// ─────────────────────────────────────────────
children.push(heading("6. Kết quả cuối", 1));
children.push(para(
  "Sĩ quan chỉ huy hiểu tiếng Việt: 96.8% chính xác trên test khó 216 câu. Có thể trả lời đúng phần lớn cách user thật gõ — kể cả không dấu, chêm tiếng Anh, lóng, câu cụt. Có module nhận biết được user đang hỏi về địa điểm/thời gian cụ thể nào để điền vào câu trả lời đúng ngữ cảnh."
));
children.push(para(
  "Lính tự đi: điểm thưởng trung bình 6.57/10 (thang -5 đến +10) sau khi test trên scene đa dạng. Đến target ổn định, biết tránh chướng ngại, đi vòng khi đường thẳng bị chặn."
));
children.push(para(
  "Cả 2 model nhỏ gọn (vài MB), chạy offline trong Unity, không cần internet hay server backend. Unity load runtime, NPC tự nói + tự đi không cần can thiệp tay."
));

// ─────────────────────────────────────────────
children.push(heading("Lời kết", 1));
children.push(para(
  "AI thực ra không khó như mọi người hay nghĩ. Phần khó nhất không phải ở chỗ chọn kiểu mạng neural nào hoành tráng, mà ở 3 câu hỏi đơn giản: dữ liệu có đủ phong phú không, có cách nào đo AI thật giỏi hay không, và khi user kêu \"AI ngu\" có biết phân biệt lỗi AI vs lỗi logic xung quanh không."
));
children.push(para(
  "Trong dự án này, cả 2 model cuối cùng đều không phải state-of-the-art. AI hiểu câu là LSTM (kiến trúc 2014, có ở mọi tutorial cơ bản). AI tự đi là PPO (thuật toán 2017, cũng phổ biến). Cái khó nhất không nằm ở model, mà ở việc design dữ liệu, design test, design quy trình lặp — đó mới là phần học được nhiều nhất từ ĐATN này."
));
children.push(para(
  "Sẵn sàng trả lời mọi câu hỏi về quyết định trong các phần trên."
));

// ─────────────────────────────────────────────
const doc = new Document({
  creator: "Quyen — KTMM",
  title: "Hành trình huấn luyện 2 con AI cho ĐATN — bản đơn giản",
  styles: {
    default: { document: { run: { font: "Times New Roman", size: 26 } } },
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 },
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 },
      },
    },
    children,
  }],
});

Packer.toBuffer(doc).then((buf) => {
  const final = "HANH_TRINH_TRAINING_2_CON_AI_DON_GIAN.docx";
  try {
    fs.writeFileSync(final, buf);
    console.log("[done] wrote", final, "bytes:", buf.length);
  } catch (e) {
    if (e.code === "EBUSY" || e.code === "EPERM") {
      const tmp = final.replace(/\.docx$/, ".new.docx");
      fs.writeFileSync(tmp, buf);
      console.log("[note] file locked; wrote", tmp);
    } else throw e;
  }
}).catch((e) => {
  console.error("[fail]", e);
  process.exit(1);
});
