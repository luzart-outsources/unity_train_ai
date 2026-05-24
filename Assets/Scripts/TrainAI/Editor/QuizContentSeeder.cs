using TrainAI.SO.Base;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // One-shot populator that overwrites every QuizQuestionSO asset under
    // Assets/_Data/Quizzes/ with real Vietnamese content for the 3 subjects:
    // Chính trị, Giáo dục Quốc phòng, Lịch sử (6 lessons × 10 questions each).
    //
    // Source content was authored for in-game placeholder readability — not
    // an authoritative curriculum reference. Edit individual QuizQuestionSO
    // assets in the Inspector for any per-question polish.
    //
    // Why one big file: each question is 5 lines (question + 4 answers +
    // correct index). 180 questions = 1080 lines of data. Putting them in
    // a single static array keeps the populate menu self-contained and lets
    // a designer re-run after asset re-create without losing content.
    public static class QuizContentSeeder
    {
        const string QuizFolder = "Assets/_Data/Quizzes";

        struct Q
        {
            public string subject;  // "ChinhTri" / "GDQuocPhong" / "LichSu"
            public int lesson;      // 1..6
            public int index;       // 0..9
            public string question;
            public string a0, a1, a2, a3;
            public int correctIdx;  // 0..3
        }

        [MenuItem("Tools/Build Game/Quizzes/Populate Vietnamese Content (180 questions)", false, 250)]
        public static void Populate()
        {
            var bank = BuildBank();
            int populated = 0, missing = 0;
            foreach (var q in bank)
            {
                string setName = $"QuizSet_{q.subject}_{q.lesson}";
                string setPath = $"{QuizFolder}/{setName}.asset";
                var qs = AssetDatabase.LoadAssetAtPath<QuizSetSO>(setPath);
                if (qs == null || qs.questions == null || q.index >= qs.questions.Count)
                {
                    missing++;
                    Debug.LogWarning($"[QuizSeeder] {setName} [{q.index}] missing — skipped");
                    continue;
                }
                var question = qs.questions[q.index];
                if (question == null) { missing++; continue; }
                question.question = q.question;
                question.answers = new[] { q.a0, q.a1, q.a2, q.a3 };
                question.correctIndex = q.correctIdx;
                EditorUtility.SetDirty(question);
                populated++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[QuizSeeder] populated {populated} questions, {missing} missing/skipped across 18 QuizSets.");
        }

        static System.Collections.Generic.List<Q> BuildBank()
        {
            var list = new System.Collections.Generic.List<Q>(180);
            // === Chính trị (Politics) — 6 lessons × 10 questions ===
            AddChinhTri(list);
            // === GD Quốc phòng (National Defense Education) — 6 × 10 ===
            AddGDQuocPhong(list);
            // === Lịch sử (History) — 6 × 10 ===
            AddLichSu(list);
            return list;
        }

        static void Add(System.Collections.Generic.List<Q> list, string subject, int lesson, int index,
                        string question, string a0, string a1, string a2, string a3, int correct)
        {
            list.Add(new Q { subject = subject, lesson = lesson, index = index,
                             question = question, a0 = a0, a1 = a1, a2 = a2, a3 = a3, correctIdx = correct });
        }

        // ============================================================
        // CHÍNH TRỊ — Marxism-Leninism, Ho Chi Minh thought, Party,
        // State, Development path, National defense.
        // ============================================================
        static void AddChinhTri(System.Collections.Generic.List<Q> L)
        {
            // --- Bài 1: Chủ nghĩa Mác-Lênin ---
            Add(L,"ChinhTri",1,0,"Ba bộ phận cấu thành chủ nghĩa Mác-Lênin là gì?",
                "Triết học, Kinh tế chính trị, Chủ nghĩa xã hội khoa học",
                "Triết học, Toán học, Vật lý học",
                "Văn học, Lịch sử, Triết học",
                "Đạo đức học, Mỹ học, Logic học",0);
            Add(L,"ChinhTri",1,1,"Người sáng lập chủ nghĩa Mác là ai?",
                "Lê-nin","Các Mác và Ph. Ăng-ghen","Stalin","Hồ Chí Minh",1);
            Add(L,"ChinhTri",1,2,"Tác phẩm 'Tuyên ngôn của Đảng Cộng sản' ra đời năm nào?",
                "1845","1846","1848","1850",2);
            Add(L,"ChinhTri",1,3,"Chủ nghĩa duy vật biện chứng là cơ sở triết học của:",
                "Chủ nghĩa Mác-Lênin","Chủ nghĩa duy tâm","Chủ nghĩa tự do","Chủ nghĩa thực chứng",0);
            Add(L,"ChinhTri",1,4,"Quy luật cơ bản của phép biện chứng duy vật KHÔNG bao gồm:",
                "Quy luật mâu thuẫn","Quy luật lượng - chất","Quy luật phủ định của phủ định","Quy luật bảo toàn năng lượng",3);
            Add(L,"ChinhTri",1,5,"Lê-nin phát triển chủ nghĩa Mác trong điều kiện:",
                "Chủ nghĩa tư bản tự do cạnh tranh","Chủ nghĩa đế quốc","Chủ nghĩa phong kiến","Xã hội nguyên thuỷ",1);
            Add(L,"ChinhTri",1,6,"Hình thái kinh tế - xã hội cao nhất theo Mác là:",
                "Tư bản chủ nghĩa","Phong kiến","Cộng sản chủ nghĩa","Nô lệ",2);
            Add(L,"ChinhTri",1,7,"Vật chất theo định nghĩa của Lê-nin là:",
                "Cái có sẵn trong tự nhiên","Phạm trù triết học chỉ thực tại khách quan tồn tại độc lập với ý thức",
                "Cái do con người tạo ra","Cái không nhìn thấy được",1);
            Add(L,"ChinhTri",1,8,"Nguồn gốc và động lực phát triển xã hội là:",
                "Đấu tranh giai cấp","Tôn giáo","Khí hậu","Địa lý",0);
            Add(L,"ChinhTri",1,9,"Chủ nghĩa xã hội khoa học khác chủ nghĩa xã hội không tưởng ở chỗ:",
                "Có cơ sở khoa học và chỉ ra giai cấp công nhân là lực lượng cách mạng",
                "Mong muốn xã hội công bằng","Phê phán bất công xã hội","Đề cao đạo đức",0);

            // --- Bài 2: Tư tưởng Hồ Chí Minh ---
            Add(L,"ChinhTri",2,0,"Tư tưởng Hồ Chí Minh là gì?",
                "Hệ thống quan điểm toàn diện và sâu sắc về cách mạng Việt Nam",
                "Một học thuyết triết học","Một trường phái văn học","Một tôn giáo",0);
            Add(L,"ChinhTri",2,1,"Hồ Chí Minh tìm thấy con đường cứu nước qua tác phẩm nào?",
                "Tuyên ngôn Đảng Cộng sản","Sơ thảo Luận cương về vấn đề dân tộc và thuộc địa của Lê-nin",
                "Tư bản luận","Nhà nước và cách mạng",1);
            Add(L,"ChinhTri",2,2,"Bác Hồ ra đi tìm đường cứu nước năm nào?",
                "1905","1911","1920","1930",1);
            Add(L,"ChinhTri",2,3,"Bác Hồ đọc Tuyên ngôn Độc lập ngày nào?",
                "19-8-1945","2-9-1945","7-5-1954","30-4-1975",1);
            Add(L,"ChinhTri",2,4,"Phẩm chất cốt lõi của người cán bộ theo Bác Hồ là:",
                "Có tài năng","Cần - Kiệm - Liêm - Chính","Có học vị cao","Có quan hệ rộng",1);
            Add(L,"ChinhTri",2,5,"Bác Hồ nói 'Vì lợi ích mười năm trồng cây, vì lợi ích trăm năm':",
                "Trồng rừng","Trồng người","Xây nhà","Xây nước",1);
            Add(L,"ChinhTri",2,6,"Tư tưởng đại đoàn kết của Hồ Chí Minh nhằm:",
                "Đoàn kết toàn dân tộc làm sức mạnh đánh thắng kẻ thù",
                "Chỉ đoàn kết người cùng tôn giáo","Chỉ đoàn kết người cùng giai cấp","Đoàn kết theo vùng miền",0);
            Add(L,"ChinhTri",2,7,"Quan điểm 'Không có gì quý hơn độc lập tự do' được Bác nêu năm nào?",
                "1945","1954","1966","1969",2);
            Add(L,"ChinhTri",2,8,"Trong Di chúc, Bác Hồ căn dặn về Đảng:",
                "Phải đoàn kết, trong sạch, vững mạnh",
                "Phải mở rộng đảng viên ồ ạt","Phải tách thành nhiều đảng nhỏ","Phải dùng vũ lực",0);
            Add(L,"ChinhTri",2,9,"Hồ Chí Minh được UNESCO vinh danh là:",
                "Anh hùng giải phóng dân tộc, Nhà văn hoá kiệt xuất","Nhà toán học","Nhà phát minh","Nhà ngoại giao thế giới",0);

            // --- Bài 3: Đảng Cộng sản Việt Nam ---
            Add(L,"ChinhTri",3,0,"Đảng Cộng sản Việt Nam thành lập ngày nào?",
                "19-8-1930","3-2-1930","2-9-1945","7-5-1954",1);
            Add(L,"ChinhTri",3,1,"Ai chủ trì hội nghị hợp nhất ba tổ chức cộng sản năm 1930?",
                "Trường Chinh","Lê Duẩn","Nguyễn Ái Quốc","Phan Đăng Lưu",2);
            Add(L,"ChinhTri",3,2,"Đại hội đại biểu toàn quốc lần thứ nhất của Đảng tổ chức ở:",
                "Hà Nội","Sài Gòn","Ma Cao","Pác Bó",2);
            Add(L,"ChinhTri",3,3,"Cương lĩnh chính trị đầu tiên do ai soạn thảo?",
                "Trần Phú","Nguyễn Ái Quốc","Lê Hồng Phong","Hà Huy Tập",1);
            Add(L,"ChinhTri",3,4,"Mục tiêu của Đảng hiện nay là:",
                "Xây dựng nước Việt Nam dân giàu, nước mạnh, dân chủ, công bằng, văn minh",
                "Bành trướng lãnh thổ","Trở thành cường quốc quân sự","Đóng cửa với thế giới",0);
            Add(L,"ChinhTri",3,5,"Đảng Cộng sản Việt Nam là Đảng cầm quyền, hoạt động theo nguyên tắc:",
                "Tập trung dân chủ","Đa đảng","Quân chủ","Tự do tuyệt đối",0);
            Add(L,"ChinhTri",3,6,"Tổng Bí thư đầu tiên của Đảng là:",
                "Hồ Chí Minh","Trần Phú","Lê Hồng Phong","Hà Huy Tập",1);
            Add(L,"ChinhTri",3,7,"Đại hội VI (1986) đề ra đường lối:",
                "Cải cách kinh tế","Đổi mới toàn diện","Đóng cửa nền kinh tế","Quốc hữu hoá",1);
            Add(L,"ChinhTri",3,8,"Nền tảng tư tưởng của Đảng là:",
                "Chủ nghĩa Mác-Lênin và tư tưởng Hồ Chí Minh","Chủ nghĩa duy tâm","Chủ nghĩa tự do","Chủ nghĩa dân tộc cực đoan",0);
            Add(L,"ChinhTri",3,9,"Vai trò lãnh đạo của Đảng được khẳng định trong:",
                "Hiến pháp","Bộ luật Dân sự","Luật Đất đai","Luật Doanh nghiệp",0);

            // --- Bài 4: Nhà nước pháp quyền XHCN ---
            Add(L,"ChinhTri",4,0,"Nhà nước CHXHCN Việt Nam là nhà nước:",
                "Pháp quyền xã hội chủ nghĩa của dân, do dân, vì dân",
                "Quân chủ lập hiến","Cộng hoà tổng thống","Liên bang",0);
            Add(L,"ChinhTri",4,1,"Quyền lực nhà nước ở Việt Nam thuộc về:",
                "Nhân dân","Quân đội","Doanh nhân","Trí thức",0);
            Add(L,"ChinhTri",4,2,"Cơ quan quyền lực cao nhất của nhà nước là:",
                "Chính phủ","Quốc hội","Toà án nhân dân tối cao","Viện kiểm sát nhân dân tối cao",1);
            Add(L,"ChinhTri",4,3,"Hiến pháp hiện hành của Việt Nam là Hiến pháp năm:",
                "1980","1992","2013","1946",2);
            Add(L,"ChinhTri",4,4,"Người đứng đầu Chính phủ là:",
                "Chủ tịch nước","Thủ tướng","Tổng Bí thư","Chủ tịch Quốc hội",1);
            Add(L,"ChinhTri",4,5,"Nguyên tắc tổ chức quyền lực nhà nước là:",
                "Tam quyền phân lập tuyệt đối","Phân công, phối hợp và kiểm soát giữa các cơ quan",
                "Tập trung tuyệt đối","Phân quyền theo địa phương",1);
            Add(L,"ChinhTri",4,6,"Toà án nhân dân thực hiện quyền:",
                "Lập pháp","Hành pháp","Tư pháp","Giám sát tối cao",2);
            Add(L,"ChinhTri",4,7,"Mặt trận Tổ quốc Việt Nam là tổ chức:",
                "Chính trị","Liên minh chính trị, liên hiệp tự nguyện",
                "Quân sự","Tôn giáo",1);
            Add(L,"ChinhTri",4,8,"Quyền cơ bản của công dân bao gồm:",
                "Chỉ quyền chính trị","Quyền kinh tế, văn hoá, xã hội và quyền chính trị",
                "Chỉ quyền kinh tế","Chỉ quyền tự do ngôn luận",1);
            Add(L,"ChinhTri",4,9,"Nghĩa vụ cơ bản của công dân Việt Nam KHÔNG bao gồm:",
                "Bảo vệ Tổ quốc","Tuân theo Hiến pháp và pháp luật",
                "Đóng thuế","Tham gia tôn giáo bắt buộc",3);

            // --- Bài 5: Đường lối phát triển KT-XH ---
            Add(L,"ChinhTri",5,0,"Nền kinh tế Việt Nam hiện nay là:",
                "Kinh tế thị trường định hướng xã hội chủ nghĩa",
                "Kinh tế kế hoạch hoá tập trung","Kinh tế tư bản thuần tuý","Kinh tế tự cung tự cấp",0);
            Add(L,"ChinhTri",5,1,"Thành phần kinh tế giữ vai trò chủ đạo là:",
                "Kinh tế tư nhân","Kinh tế nhà nước","Kinh tế tập thể","Kinh tế có vốn đầu tư nước ngoài",1);
            Add(L,"ChinhTri",5,2,"Mục tiêu đến năm 2030 Việt Nam trở thành:",
                "Nước phát triển có thu nhập cao","Nước đang phát triển có công nghiệp hiện đại, thu nhập trung bình cao",
                "Nước nông nghiệp","Nước có thu nhập thấp",1);
            Add(L,"ChinhTri",5,3,"Ba khâu đột phá chiến lược là:",
                "Thể chế - Hạ tầng - Nhân lực","Vốn - Đất - Lao động",
                "Quân sự - Ngoại giao - Kinh tế","Tài chính - Công nghệ - Thương mại",0);
            Add(L,"ChinhTri",5,4,"Công nghiệp hoá, hiện đại hoá gắn với:",
                "Phát triển kinh tế tri thức","Phát triển nông nghiệp truyền thống",
                "Hạn chế xuất khẩu","Đóng cửa nền kinh tế",0);
            Add(L,"ChinhTri",5,5,"Mục tiêu phát triển bền vững bao gồm các trụ cột:",
                "Kinh tế - Quân sự - Văn hoá","Kinh tế - Xã hội - Môi trường",
                "Chính trị - Kinh tế - Pháp luật","Đạo đức - Tôn giáo - Khoa học",1);
            Add(L,"ChinhTri",5,6,"Hội nhập quốc tế của Việt Nam dựa trên:",
                "Độc lập tự chủ và đa phương hoá quan hệ",
                "Phụ thuộc một cường quốc","Đóng cửa","Đối đầu với mọi nước",0);
            Add(L,"ChinhTri",5,7,"Cách mạng công nghiệp 4.0 đặc trưng bởi:",
                "Hơi nước","Điện","Máy tính","Trí tuệ nhân tạo, IoT, dữ liệu lớn",3);
            Add(L,"ChinhTri",5,8,"Văn hoá là:",
                "Nền tảng tinh thần của xã hội","Phụ thuộc kinh tế",
                "Không quan trọng","Chỉ giải trí",0);
            Add(L,"ChinhTri",5,9,"Chính sách dân tộc của Đảng là:",
                "Đoàn kết, bình đẳng, tương trợ cùng phát triển",
                "Đồng hoá","Phân biệt đối xử","Tự trị hoàn toàn",0);

            // --- Bài 6: Quốc phòng toàn dân, an ninh nhân dân ---
            Add(L,"ChinhTri",6,0,"Bản chất nền quốc phòng Việt Nam là:",
                "Quốc phòng toàn dân","Quốc phòng nhà nghề","Liên minh quân sự","Trung lập",0);
            Add(L,"ChinhTri",6,1,"Mục tiêu của quốc phòng toàn dân là:",
                "Bảo vệ vững chắc Tổ quốc","Bành trướng lãnh thổ","Tham chiến nước ngoài","Tích trữ vũ khí",0);
            Add(L,"ChinhTri",6,2,"Lực lượng nòng cốt của nền quốc phòng là:",
                "Quân đội nhân dân và Công an nhân dân","Dân quân tự vệ","Học sinh, sinh viên","Cán bộ nhà nước",0);
            Add(L,"ChinhTri",6,3,"Thế trận quốc phòng toàn dân kết hợp với:",
                "Thế trận an ninh nhân dân","Thế trận chiến lược nước ngoài",
                "Thế trận đối ngoại","Thế trận thương mại",0);
            Add(L,"ChinhTri",6,4,"Đối tượng tác chiến của quân đội ta là:",
                "Bất kỳ ai chống đối","Các thế lực xâm lược và phản động chống phá đất nước",
                "Người nước ngoài","Người không cùng tôn giáo",1);
            Add(L,"ChinhTri",6,5,"Chiến lược 'Diễn biến hoà bình' của các thế lực thù địch nhằm:",
                "Xoá bỏ chế độ XHCN ở Việt Nam","Phát triển kinh tế Việt Nam",
                "Giúp Việt Nam hội nhập","Tăng cường hữu nghị",0);
            Add(L,"ChinhTri",6,6,"Bảo vệ Tổ quốc là nhiệm vụ của:",
                "Quân đội","Toàn dân, toàn diện, dựa vào sức mình là chính",
                "Công an","Bộ Quốc phòng",1);
            Add(L,"ChinhTri",6,7,"Nguyên tắc xây dựng quân đội ta là:",
                "Đảng lãnh đạo tuyệt đối, trực tiếp về mọi mặt",
                "Quân đội phi chính trị","Quân đội đánh thuê","Tự do hoạt động",0);
            Add(L,"ChinhTri",6,8,"Bảo vệ chủ quyền biển đảo bằng biện pháp:",
                "Quân sự là chủ yếu","Hoà bình, đối thoại trên cơ sở luật pháp quốc tế",
                "Ngoại giao đơn phương","Bỏ mặc",1);
            Add(L,"ChinhTri",6,9,"Trách nhiệm của sinh viên với Tổ quốc là:",
                "Học tập tốt, rèn luyện tốt, sẵn sàng bảo vệ Tổ quốc",
                "Chỉ học tập","Không quan tâm chính trị","Tham gia mạng xã hội",0);
        }

        // ============================================================
        // GIÁO DỤC QUỐC PHÒNG — Drill, AK rifle, grenade, formations,
        // shooting, military etiquette.
        // ============================================================
        static void AddGDQuocPhong(System.Collections.Generic.List<Q> L)
        {
            // Bài 1: Điều lệnh đội ngũ
            Add(L,"GDQuocPhong",1,0,"Khẩu lệnh trong đội ngũ gồm mấy phần?","2 phần (dự lệnh và động lệnh)","1 phần","3 phần","4 phần",0);
            Add(L,"GDQuocPhong",1,1,"Tư thế nghiêm: hai gót chân:","Cách nhau 20cm","Chụm lại, hai bàn chân mở rộng 45°","Cách nhau 40cm","Đặt song song",1);
            Add(L,"GDQuocPhong",1,2,"Khi hô 'Nghỉ', người ở tư thế:","Đứng yên","Trùng gối trái, dồn trọng tâm chân phải","Ngồi xuống","Quỳ một chân",1);
            Add(L,"GDQuocPhong",1,3,"Động tác chào kiểu nhà binh thực hiện bằng tay:","Trái","Phải","Cả hai tay","Không dùng tay",1);
            Add(L,"GDQuocPhong",1,4,"Quay bên phải, gót chân làm trụ là:","Gót phải","Gót trái","Cả hai","Mũi chân phải",0);
            Add(L,"GDQuocPhong",1,5,"Đi đều bước chân trái trước cùng tay nào đưa ra phía sau?","Tay trái","Tay phải","Không đưa tay","Cả hai tay",1);
            Add(L,"GDQuocPhong",1,6,"Khoảng cách hàng dọc giữa hai người là:","1 cánh tay","1 bước","2 bước","Không quy định",0);
            Add(L,"GDQuocPhong",1,7,"Tốc độ đi đều trung bình:","60-80 bước/phút","100-110 bước/phút","160 bước/phút","200 bước/phút",1);
            Add(L,"GDQuocPhong",1,8,"Khẩu lệnh tập hợp đội hình hàng ngang:","'Hàng dọc - tập hợp'","'Hàng ngang - tập hợp'","'Đứng tại chỗ'","'Sẵn sàng'",1);
            Add(L,"GDQuocPhong",1,9,"Điều lệnh đội ngũ rèn luyện cho người chiến sĩ:","Kỷ luật, ý thức tập thể","Trí thông minh","Sức khoẻ thể lực","Khả năng học tập",0);

            // Bài 2: Súng tiểu liên AK
            Add(L,"GDQuocPhong",2,0,"Súng tiểu liên AK do nước nào sản xuất đầu tiên?","Trung Quốc","Liên Xô","Mỹ","Việt Nam",1);
            Add(L,"GDQuocPhong",2,1,"Người thiết kế súng AK là:","M. Kalashnikov","Browning","Stoner","Glock",0);
            Add(L,"GDQuocPhong",2,2,"Cỡ đạn súng AK-47:","5.56mm","7.62mm","9mm","12.7mm",1);
            Add(L,"GDQuocPhong",2,3,"Hộp tiếp đạn AK chứa:","20 viên","25 viên","30 viên","45 viên",2);
            Add(L,"GDQuocPhong",2,4,"Tầm bắn ghi trên thước ngắm AK xa nhất:","500m","800m","1000m","1500m",1);
            Add(L,"GDQuocPhong",2,5,"Tốc độ bắn liên thanh AK lý thuyết:","100 viên/phút","400 viên/phút","600 viên/phút","900 viên/phút",2);
            Add(L,"GDQuocPhong",2,6,"Súng AK gồm bao nhiêu bộ phận chính?","5","7","9","11",1);
            Add(L,"GDQuocPhong",2,7,"Khoá an toàn AK gạt lên là:","Khoá an toàn","Bắn phát một","Bắn liên thanh","Mở nắp hộp khoá nòng",0);
            Add(L,"GDQuocPhong",2,8,"Nguyên lý hoạt động AK:","Khoá nòng then xoay, trích khí","Lùi tự do","Quay nòng","Điện tử",0);
            Add(L,"GDQuocPhong",2,9,"Bảo quản súng cần thường xuyên:","Lau chùi, tra dầu mỡ","Cất kỹ không dùng","Phơi nắng","Ngâm nước",0);

            // Bài 3: Lựu đạn
            Add(L,"GDQuocPhong",3,0,"Lựu đạn cấu tạo gồm:","Vỏ, thuốc nổ, bộ phận gây nổ","Chỉ vỏ và thuốc nổ","Vỏ và kíp","Thuốc nổ và kíp",0);
            Add(L,"GDQuocPhong",3,1,"Lựu đạn F-1 nặng khoảng:","300g","600g","1kg","200g",1);
            Add(L,"GDQuocPhong",3,2,"Bán kính sát thương lựu đạn F-1:","5m","15m","30m","100m",2);
            Add(L,"GDQuocPhong",3,3,"Thời gian cháy chậm sau khi rút chốt lựu đạn:","1-2 giây","3-4 giây","5-7 giây","10 giây",2);
            Add(L,"GDQuocPhong",3,4,"Khi ném lựu đạn, tay cầm:","Đặt chốt vào lòng bàn tay","Bóp chặt mỏ vịt cho đến khi ném đi","Cầm hờ","Đặt trong túi",1);
            Add(L,"GDQuocPhong",3,5,"Trước khi ném lựu đạn cần:","Đếm thật to","Rút chốt an toàn","Mở vỏ","Vẽ dấu",1);
            Add(L,"GDQuocPhong",3,6,"Tư thế ném lựu đạn đứng phù hợp khi:","Cách mục tiêu xa, không có vật che","Mục tiêu gần","Bị thương","Trong hầm",0);
            Add(L,"GDQuocPhong",3,7,"Sau khi ném lựu đạn cần:","Đứng quan sát","Nằm xuống tránh mảnh","Chạy về phía mục tiêu","Hô lớn",1);
            Add(L,"GDQuocPhong",3,8,"Lựu đạn KHÔNG sử dụng để:","Tấn công công sự","Phòng ngự","Ném trong phòng đông người không có chiến sự","Phục kích",2);
            Add(L,"GDQuocPhong",3,9,"Sai lầm cần tránh khi ném lựu đạn:","Ném mạnh hết sức","Rút chốt rồi cầm quá lâu","Ném thấp","Ném khi nằm",1);

            // Bài 4: Đội hình chiến đấu / từng người không có súng
            Add(L,"GDQuocPhong",4,0,"Đội hình tổ bộ binh có mấy hình thức cơ bản?","2","3","4","5",1);
            Add(L,"GDQuocPhong",4,1,"Khoảng cách giữa các chiến sĩ trong đội hình tiến công:","1m","2-3m","5-7m","20m",2);
            Add(L,"GDQuocPhong",4,2,"Động tác đi khom thấp dùng khi:","Đường bằng","Đường có vật che khuất thấp","Trong nhà","Trên cao",1);
            Add(L,"GDQuocPhong",4,3,"Bò cao hai chân, hai tay sử dụng khi:","Trên cát","Vướng vật cản, đêm tối","Đường nhựa","Đường dốc",1);
            Add(L,"GDQuocPhong",4,4,"Lăn dài dùng khi:","Cần vượt nhanh đoạn ngắn dưới hoả lực","Đi tuần tra","Tập luyện","Diễu hành",0);
            Add(L,"GDQuocPhong",4,5,"Khi vận động, chiến sĩ phải:","Cúi đầu nhìn xuống","Quan sát địa hình, mục tiêu, đồng đội","Bịt tai","Chạy hết sức",1);
            Add(L,"GDQuocPhong",4,6,"Ngụy trang giúp:","Trang trí","Che mắt địch, giảm thương vong","Tăng trọng lượng","Bảo vệ vũ khí",1);
            Add(L,"GDQuocPhong",4,7,"Khẩu lệnh 'Tiến!' người chiến sĩ:","Đứng yên","Vận động về phía trước","Quay sau","Ngồi xuống",1);
            Add(L,"GDQuocPhong",4,8,"Đội hình hàng dọc thuận lợi cho:","Quan sát rộng","Hành quân, vượt cầu hẹp","Phòng ngự diện rộng","Diễu hành",1);
            Add(L,"GDQuocPhong",4,9,"Người chỉ huy tổ chỉ huy bằng:","Tiếng nói, ký hiệu, tín hiệu","Chỉ tiếng nói","Chỉ điện thoại","Văn bản",0);

            // Bài 5: Kỹ thuật bắn súng tiểu liên AK
            Add(L,"GDQuocPhong",5,0,"Yếu lĩnh bắn súng gồm:","Lấy đường ngắm và bóp cò","Chuẩn bị bắn, ngắm, bóp cò, giữ súng",
                "Chỉ ngắm","Chỉ bóp cò",1);
            Add(L,"GDQuocPhong",5,1,"Đường ngắm cơ bản là:","Đường thẳng từ mắt qua khe ngắm tới đầu ruồi đặt vào điểm ngắm",
                "Đường từ mắt tới đích","Đường thẳng song song","Đường vuông góc nòng súng",0);
            Add(L,"GDQuocPhong",5,2,"Khi bóp cò, cần:","Bóp giật","Bóp êm, đều, dứt khoát","Bóp nhanh","Đẩy cò",1);
            Add(L,"GDQuocPhong",5,3,"Tư thế bắn cơ bản nhất với AK:","Đứng","Nằm bắn","Quỳ","Ngồi",1);
            Add(L,"GDQuocPhong",5,4,"Khi nằm bắn, hai khuỷu tay tạo thành:","Hình tròn","Tam giác vững chắc","Đường thẳng","Hình vuông",1);
            Add(L,"GDQuocPhong",5,5,"Đầu ruồi cao hơn khe ngắm sẽ:","Bắn cao","Bắn thấp","Bắn trúng","Không bắn được",0);
            Add(L,"GDQuocPhong",5,6,"Đầu ruồi nghiêng trái sẽ:","Đạn lệch trái","Đạn lệch phải","Đạn lên cao","Đạn xuống thấp",0);
            Add(L,"GDQuocPhong",5,7,"Giật cò mạnh làm:","Súng giật mạnh, đạn lệch","Bắn chính xác","Tăng tầm xa","Không ảnh hưởng",0);
            Add(L,"GDQuocPhong",5,8,"Nín thở khi bóp cò để:","Giữ ổn định, không rung súng","Tập trung","Đỡ mỏi","Tiết kiệm sức",0);
            Add(L,"GDQuocPhong",5,9,"Bài bắn số 1 AK thường bắn ở cự ly:","50m","100m","200m","300m",1);

            // Bài 6: Lễ tiết tác phong quân nhân
            Add(L,"GDQuocPhong",6,0,"Khi gặp cấp trên, quân nhân phải:","Nói chuyện thoải mái","Chào theo điều lệnh","Bắt tay trước","Quay đi",1);
            Add(L,"GDQuocPhong",6,1,"Quân phục mặc phải:","Đúng quy định, gọn gàng","Tuỳ thích","Pha trộn","Tự thiết kế",0);
            Add(L,"GDQuocPhong",6,2,"Khi nhận mệnh lệnh, quân nhân phải:","Nói 'Để tôi xem'","Đáp 'Rõ' và chấp hành","Bàn cãi","Im lặng",1);
            Add(L,"GDQuocPhong",6,3,"Quan hệ giữa quân nhân các cấp dựa trên:","Tình bạn","Cấp bậc, chức vụ và đồng chí","Quyền lực","Tiền bạc",1);
            Add(L,"GDQuocPhong",6,4,"Khi ăn trong tập thể, quân nhân:","Ăn vội","Có nề nếp, văn hoá","Ăn riêng lẻ","Tuỳ tiện",1);
            Add(L,"GDQuocPhong",6,5,"Trật tự nội vụ rèn luyện tính:","Tự do","Ngăn nắp, kỷ luật","Phóng khoáng","Lười nhác",1);
            Add(L,"GDQuocPhong",6,6,"Khi ra ngoài đơn vị, quân nhân phải:","Tự ý đi","Báo cáo và được phép","Không cần báo","Tuỳ thích",1);
            Add(L,"GDQuocPhong",6,7,"Đối với nhân dân, quân nhân phải:","Xa lánh","Tôn trọng, giúp đỡ, gắn bó","Tận thu","Không quan tâm",1);
            Add(L,"GDQuocPhong",6,8,"Tham gia học tập chính trị là:","Tự nguyện","Bắt buộc với mọi quân nhân","Tự chọn","Chỉ với cán bộ",1);
            Add(L,"GDQuocPhong",6,9,"Phẩm chất 'Bộ đội Cụ Hồ' bao gồm:","Trung thành, dũng cảm, sáng tạo, đoàn kết","Chỉ dũng cảm","Chỉ trung thành","Chỉ có sức khoẻ",0);
        }

        // ============================================================
        // LỊCH SỬ — Vietnamese history milestones.
        // ============================================================
        static void AddLichSu(System.Collections.Generic.List<Q> L)
        {
            // Bài 1: Văn Lang - Âu Lạc
            Add(L,"LichSu",1,0,"Nhà nước Văn Lang ra đời vào khoảng:","TK VII TCN","TK V TCN","TK III TCN","TK I",0);
            Add(L,"LichSu",1,1,"Vua đầu tiên của nước Văn Lang là:","Hùng Vương","An Dương Vương","Trưng Trắc","Đinh Bộ Lĩnh",0);
            Add(L,"LichSu",1,2,"Kinh đô nước Văn Lang đặt tại:","Cổ Loa","Phong Châu","Hoa Lư","Thăng Long",1);
            Add(L,"LichSu",1,3,"Nhà nước Âu Lạc do ai lập?","Hùng Vương","Thục Phán An Dương Vương","Triệu Đà","Lý Bí",1);
            Add(L,"LichSu",1,4,"Thành Cổ Loa được xây dựng dưới thời:","Văn Lang","Âu Lạc","Nhà Triệu","Nhà Đinh",1);
            Add(L,"LichSu",1,5,"Truyền thuyết 'Sơn Tinh Thuỷ Tinh' phản ánh:","Chống ngoại xâm","Đắp đê chống lũ lụt","Trị bệnh","Khai phá đất",1);
            Add(L,"LichSu",1,6,"Trống đồng Đông Sơn là biểu tượng văn hoá thời:","Hậu Lê","Văn Lang - Âu Lạc","Nhà Lý","Nhà Trần",1);
            Add(L,"LichSu",1,7,"Cuộc kháng chiến chống Triệu Đà của An Dương Vương thất bại do:","Quân yếu","Mất cảnh giác","Thiếu vũ khí","Bị phản bội bởi nội gián (Trọng Thuỷ)",3);
            Add(L,"LichSu",1,8,"Sản phẩm tiêu biểu của văn minh Đông Sơn:","Đồ sắt","Đồ đồng","Đồ gốm","Vũ khí đá",1);
            Add(L,"LichSu",1,9,"Hùng Vương có bao nhiêu đời theo truyền thuyết?","10","15","18","20",2);

            // Bài 2: Hai Bà Trưng - kháng Bắc thuộc
            Add(L,"LichSu",2,0,"Khởi nghĩa Hai Bà Trưng nổ ra năm:","40","43","542","938",0);
            Add(L,"LichSu",2,1,"Hai Bà Trưng quê ở:","Mê Linh","Cổ Loa","Hoa Lư","Thăng Long",0);
            Add(L,"LichSu",2,2,"Hai Bà Trưng khởi nghĩa chống lại nhà:","Hán","Đường","Tống","Nguyên",0);
            Add(L,"LichSu",2,3,"Người chồng của Trưng Trắc tên là:","Thi Sách","Đặng Tất","Trần Quốc Tuấn","Phạm Ngũ Lão",0);
            Add(L,"LichSu",2,4,"Cuộc khởi nghĩa của Bà Triệu năm:","40","248","542","939",1);
            Add(L,"LichSu",2,5,"Câu nói nổi tiếng của Bà Triệu:","'Tôi muốn cưỡi gió mạnh, đạp luồng sóng dữ...'","'Đánh cho để dài tóc'","'Đánh cho sử tri Nam quốc anh hùng'","'Sát Thát'",0);
            Add(L,"LichSu",2,6,"Lý Bí khởi nghĩa thắng lợi, lập ra nước:","Vạn Xuân","Đại Việt","Đại Cồ Việt","Đại Ngu",0);
            Add(L,"LichSu",2,7,"Mai Thúc Loan dựng cờ khởi nghĩa năm:","248","542","722","938",2);
            Add(L,"LichSu",2,8,"Phùng Hưng được nhân dân tôn xưng là:","Vua Hùng","Bố Cái Đại Vương","Đại Việt Vương","An Nam Vương",1);
            Add(L,"LichSu",2,9,"Khúc Thừa Dụ giành quyền tự chủ năm:","905","938","981","1010",0);

            // Bài 3: Ngô-Đinh-Tiền Lê / Lý Thường Kiệt kháng Tống
            Add(L,"LichSu",3,0,"Ngô Quyền đánh bại quân Nam Hán năm 938 trên sông:","Như Nguyệt","Bạch Đằng","Lô","Đáy",1);
            Add(L,"LichSu",3,1,"Đinh Bộ Lĩnh dẹp loạn 12 sứ quân, lập nước:","Vạn Xuân","Đại Cồ Việt","Đại Việt","Đại Nam",1);
            Add(L,"LichSu",3,2,"Kinh đô của nhà Đinh đặt tại:","Cổ Loa","Hoa Lư","Thăng Long","Phú Xuân",1);
            Add(L,"LichSu",3,3,"Người chỉ huy kháng chiến chống Tống lần 2 (1075-1077):","Đinh Bộ Lĩnh","Lý Thường Kiệt","Trần Hưng Đạo","Ngô Quyền",1);
            Add(L,"LichSu",3,4,"Bài thơ thần 'Nam quốc sơn hà' được đọc trong cuộc kháng chiến nào?","Kháng Tống thời Lý","Kháng Mông thời Trần","Kháng Minh thời Lê","Kháng Thanh thời Nguyễn",0);
            Add(L,"LichSu",3,5,"Trận Như Nguyệt diễn ra trên sông:","Bạch Đằng","Hồng","Cầu (Như Nguyệt)","Đáy",2);
            Add(L,"LichSu",3,6,"Chủ trương 'Tiên phát chế nhân' của Lý Thường Kiệt nghĩa là:","Đánh trước để khống chế đối phương","Phòng thủ","Hoà hoãn","Lui binh",0);
            Add(L,"LichSu",3,7,"Lý Công Uẩn dời đô về Thăng Long năm:","939","968","1010","1075",2);
            Add(L,"LichSu",3,8,"Trường đại học đầu tiên của Việt Nam là:","Quốc Tử Giám","Văn Miếu","Đông Kinh Nghĩa Thục","Nho học viện",0);
            Add(L,"LichSu",3,9,"Nhà Lý kéo dài bao nhiêu năm?","100","200","216","300",2);

            // Bài 4: Trần Hưng Đạo - kháng Mông Nguyên
            Add(L,"LichSu",4,0,"Nhà Trần ba lần đánh thắng quân:","Tống","Mông - Nguyên","Minh","Thanh",1);
            Add(L,"LichSu",4,1,"Hội nghị Diên Hồng diễn ra năm:","1257","1284","1288","1400",1);
            Add(L,"LichSu",4,2,"Trận Bạch Đằng năm 1288 do ai chỉ huy?","Lý Thường Kiệt","Trần Hưng Đạo","Trần Khánh Dư","Phạm Ngũ Lão",1);
            Add(L,"LichSu",4,3,"Tác phẩm 'Hịch tướng sĩ' do ai viết?","Trần Quang Khải","Trần Hưng Đạo","Lê Lợi","Nguyễn Trãi",1);
            Add(L,"LichSu",4,4,"Hai chữ 'Sát Thát' nghĩa là:","Giết giặc Mông Cổ","Bảo vệ vua","Cứu nước","Đoàn kết",0);
            Add(L,"LichSu",4,5,"Trần Quốc Tuấn dùng kế gì với cọc gỗ ở Bạch Đằng?","Thuỷ lôi","Cọc nhọn đầu bịt sắt cắm dưới nước","Đắp đập","Đóng cọc đỡ thuyền",1);
            Add(L,"LichSu",4,6,"Người trẻ tuổi nhất tham gia Hội nghị Bình Than:","Trần Quốc Toản","Trần Bình Trọng","Phạm Ngũ Lão","Trần Khánh Dư",0);
            Add(L,"LichSu",4,7,"Câu nói của Trần Bình Trọng khi bị giặc Nguyên bắt:","'Ta thà làm quỷ nước Nam'","'Phải đánh'","'Hàng giặc'","'Trốn đi'",0);
            Add(L,"LichSu",4,8,"Nhà Trần được thành lập năm:","1010","1225","1400","1428",1);
            Add(L,"LichSu",4,9,"Thái sư đầu tiên triều Trần là:","Trần Cảnh","Trần Thủ Độ","Trần Quang Khải","Trần Nhật Duật",1);

            // Bài 5: Quang Trung - đại phá quân Thanh
            Add(L,"LichSu",5,0,"Phong trào nông dân Tây Sơn nổ ra năm:","1771","1789","1802","1858",0);
            Add(L,"LichSu",5,1,"Lãnh tụ phong trào Tây Sơn là ba anh em:","Nguyễn Nhạc, Nguyễn Huệ, Nguyễn Lữ","Trần Hưng Đạo, Trần Quang Khải, Trần Nhật Duật","Lê Lai, Lê Lợi, Nguyễn Trãi","Đinh Bộ Lĩnh, Đinh Liễn, Đinh Toàn",0);
            Add(L,"LichSu",5,2,"Quang Trung đại phá quân Thanh vào dịp Tết:","Kỷ Dậu 1789","Mậu Thân 1968","Bính Tý 1996","Tân Mão 2011",0);
            Add(L,"LichSu",5,3,"Trận đánh tiêu biểu trong chiến thắng quân Thanh:","Đống Đa - Ngọc Hồi","Bạch Đằng","Chi Lăng","Điện Biên Phủ",0);
            Add(L,"LichSu",5,4,"Quang Trung từ Phú Xuân kéo quân ra Bắc mất bao nhiêu ngày?","5 ngày","Khoảng 1 tháng (35-40 ngày)","100 ngày","1 năm",1);
            Add(L,"LichSu",5,5,"Quân Thanh do tướng nào chỉ huy?","Tôn Sĩ Nghị","Sầm Nghi Đống","Hứa Thế Hanh","Cả 3 ý trên",3);
            Add(L,"LichSu",5,6,"Vua Lê cầu viện nhà Thanh là:","Lê Chiêu Thống","Lê Hiển Tông","Lê Lợi","Lê Thái Tổ",0);
            Add(L,"LichSu",5,7,"Câu nói của Quang Trung 'Đánh cho để dài tóc, đánh cho...':","'...để đen răng'","'...nước nhà yên'","'...giặc tan'","'...vua sống lâu'",0);
            Add(L,"LichSu",5,8,"Nguyễn Huệ lên ngôi Hoàng đế lấy hiệu là:","Quang Trung","Gia Long","Minh Mạng","Tự Đức",0);
            Add(L,"LichSu",5,9,"Quang Trung mất năm:","1789","1792","1802","1820",1);

            // Bài 6: Cách mạng tháng Tám và xây dựng đất nước
            Add(L,"LichSu",6,0,"Cách mạng Tháng Tám thành công ngày:","19-8-1945","2-9-1945","23-9-1945","19-12-1946",0);
            Add(L,"LichSu",6,1,"Nước Việt Nam Dân chủ Cộng hoà ra đời ngày:","2-9-1945","19-8-1945","30-4-1975","7-5-1954",0);
            Add(L,"LichSu",6,2,"Chiến dịch Điện Biên Phủ kết thúc ngày:","7-5-1954","2-9-1945","30-4-1975","27-1-1973",0);
            Add(L,"LichSu",6,3,"Người chỉ huy chiến dịch Điện Biên Phủ là:","Võ Nguyên Giáp","Văn Tiến Dũng","Nguyễn Chí Thanh","Lê Trọng Tấn",0);
            Add(L,"LichSu",6,4,"Hiệp định Geneva ký năm:","1945","1954","1973","1975",1);
            Add(L,"LichSu",6,5,"Hiệp định Paris về Việt Nam ký năm:","1954","1968","1973","1975",2);
            Add(L,"LichSu",6,6,"Chiến dịch Hồ Chí Minh giải phóng Sài Gòn ngày:","27-1-1973","30-4-1975","2-7-1976","19-5-1975",1);
            Add(L,"LichSu",6,7,"Quốc hội khoá VI quyết định đổi tên nước thành CHXHCN Việt Nam năm:","1975","1976","1980","1992",1);
            Add(L,"LichSu",6,8,"Đại hội Đảng đề ra đường lối Đổi mới năm:","1976","1982","1986","1991",2);
            Add(L,"LichSu",6,9,"Việt Nam gia nhập ASEAN năm:","1986","1995","2000","2007",1);
        }
    }
}
