using System.Collections.Generic;
using TrainAI.Configs;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    public static class SOInstanceCreator
    {
        private const string Root = "Assets/Configs/TrainAI";

        public static GameDatabaseSO CreateAll()
        {
            EnsureFolders();

            var time = CreateOrLoad<TimeConfigSO>($"{Root}/Time/TimeConfig.asset", t =>
            {
                t.secondsPerGameHour = 180f;
                t.tickEveryGameMinutes = 1;
                t.firstDay = 1;
                t.totalDays = 30;
                t.skipWeekend = true;
                t.weekStart = System.DayOfWeek.Monday;
                t.dayStartOffsetMinutesBeforeFirstQuest = 30;
                t.lateAfterMinutes = 15;
                t.defaultDayStartHour = 5;
                t.defaultDayStartMinute = 0;
            });

            var score = CreateOrLoad<ScoreConfigSO>($"{Root}/Score/ScoreConfig.asset", s =>
            {
                s.startingDiscipline = 100;
                s.startingAcademic = 0;
                s.maxDiscipline = 100;
                s.maxAcademic = 480;
                s.pointsPerQuizQuestion = 1f;
                s.penaltyLate = 5;
                s.penaltyMissed = 5;
                s.kickOutThreshold = 0;
                s.disciplineGrades = new List<GradeThreshold>
                {
                    new GradeThreshold { label = "Xuat sac", minScoreInclusive = 90,  maxScoreInclusive = 100, uiColor = new Color(0.2f,0.8f,0.2f) },
                    new GradeThreshold { label = "Tot",      minScoreInclusive = 60,  maxScoreInclusive = 89,  uiColor = new Color(0.2f,0.6f,1f)   },
                    new GradeThreshold { label = "Trung binh", minScoreInclusive = 0, maxScoreInclusive = 59,  uiColor = new Color(0.9f,0.6f,0.2f) },
                };
                s.academicGrades = new List<GradeThreshold>
                {
                    new GradeThreshold { label = "Xuat sac", minScoreInclusive = 432, maxScoreInclusive = 480, uiColor = new Color(0.2f,0.8f,0.2f) },
                    new GradeThreshold { label = "Tot",      minScoreInclusive = 288, maxScoreInclusive = 431, uiColor = new Color(0.2f,0.6f,1f)   },
                    new GradeThreshold { label = "Trung binh", minScoreInclusive = 0, maxScoreInclusive = 287, uiColor = new Color(0.9f,0.6f,0.2f) },
                };
            });

            // Subjects + QuizSets.
            var subjLichSu = CreateSubject("S_LichSu", "Lich Su Viet Nam");
            var subjQuocPhong = CreateSubject("S_QuocPhong", "Giao duc quoc phong");

            var qs1 = CreateQuizSet("QS_LichSu_Bai01", "Lich Su - Buoi 1");
            var qs2 = CreateQuizSet("QS_LichSu_Bai02", "Lich Su - Buoi 2");
            var qs3 = CreateQuizSet("QS_QuocPhong_Bai01", "QP - Buoi 1");
            var qs4 = CreateQuizSet("QS_QuocPhong_Bai02", "QP - Buoi 2");
            subjLichSu.sessions = new List<QuizSetSO> { qs1, qs2 };
            subjQuocPhong.sessions = new List<QuizSetSO> { qs3, qs4 };

            // Routes.
            var rWorld = CreateRoute("R_World", "World", "Dang vao trai...", false);
            var rTitle = CreateRoute("R_Title", "Title", "Dang khoi dong...", false);
            var rClassroom = CreateRoute("R_Classroom", "Classroom", "Dang vao lop hoc...", true);
            var rDormitory = CreateRoute("R_Dormitory", "Dormitory", "Dang vao ky tuc xa...", true);
            var rCafeteria = CreateRoute("R_Cafeteria", "Cafeteria", "Dang vao nha an...", true);

            // Interactables (key match QuestDef.interactableLocationKey va InteractableTrigger gan trong scene).
            var iSan = CreateInteractable("I_SanVanDong", "SanVanDong", "San van dong", InteractableKind.QuestPoint);
            var iVesinh = CreateInteractable("I_DonVeSinh", "DonVeSinh", "Don ve sinh", InteractableKind.QuestPoint);
            var iCuaLop = CreateInteractable("I_CuaLopHoc", "CuaLopHoc", "Cua lop hoc", InteractableKind.SceneDoor, doorTarget: rClassroom);
            var iCuaKtx = CreateInteractable("I_CuaKyTucXa", "CuaKyTucXa", "Cua ky tuc xa", InteractableKind.SceneDoor, doorTarget: rDormitory);
            var iCuaNa = CreateInteractable("I_CuaNhaAn", "CuaNhaAn", "Cua nha an", InteractableKind.SceneDoor, doorTarget: rCafeteria);
            var iBanLop = CreateInteractable("I_BanHoc", "BanHoc", "Ban hoc", InteractableKind.QuestPoint);
            var iBanAn = CreateInteractable("I_BanAn", "BanAn", "Ban an", InteractableKind.QuestPoint);
            var iGiuong = CreateInteractable("I_Giuong", "Giuong", "Giuong ngu", InteractableKind.QuestPoint);

            // NPC profiles.
            var npcDaiDoi = CreateOrLoad<NPCProfileSO>($"{Root}/NPCs/NPC_DaiDoiTruong.asset", n =>
            {
                n.id = "NPC_DaiDoiTruong";
                n.displayName = "Dai doi truong";
                n.aiMode = NPCAIMode.SentisChat;
                n.greetingTemplate = "Chao {playerName}, co chuyen gi vay?";
                n.fallbackResponses = new List<string> {
                    "Co gang hoan thanh nhiem vu nhe!",
                    "Em ve di nghi cho khoe.",
                    "Ngay mai con nhieu viec, gang len.",
                };
            });
            var iNpcDaiDoi = CreateInteractable("I_NPC_DaiDoiTruong", "NPC_DaiDoiTruong", "Dai doi truong", InteractableKind.NPC, npcProfile: npcDaiDoi);

            // Quests (demo theo GDD: 5h tap, 6h ve sinh, 7h an, 7h30 hoc, 14h hoc, 18h30 ngu).
            var qExercise = CreateQuest("Q_Exercise_0500", "Tap the duc", QuestType.Exercise, 5, 0, 5, 15, 6, 0,
                "Ban dang tap the duc", "OK", "SanVanDong", null);
            var qCleaning = CreateQuest("Q_Cleaning_0600", "Don ve sinh", QuestType.Cleaning, 6, 0, 6, 30, 7, 0,
                "Ban dang don ve sinh", "OK", "DonVeSinh", null);
            var qBreakfast = CreateQuest("Q_Eat_0700", "An sang", QuestType.Eat, 7, 0, 7, 30, 7, 30,
                "Ban dang an sang", "OK", "BanAn", rCafeteria);
            var qStudyAm = CreateQuest("Q_StudyAm_0730", "Hoc tap buoi sang", QuestType.StudyMorning, 7, 30, 8, 0, 11, 30,
                null, null, "BanHoc", rClassroom, qs1, subjLichSu);
            var qLunch = CreateQuest("Q_Eat_1130", "An trua", QuestType.Eat, 11, 30, 12, 30, 14, 0,
                "Ban dang an trua", "OK", "BanAn", rCafeteria);
            var qStudyPm = CreateQuest("Q_StudyPm_1400", "Hoc tap buoi chieu", QuestType.StudyAfternoon, 14, 0, 14, 30, 17, 0,
                null, null, "BanHoc", rClassroom, qs3, subjQuocPhong);
            var qDinner = CreateQuest("Q_Eat_1730", "An toi", QuestType.Eat, 17, 30, 18, 0, 18, 30,
                "Ban dang an toi", "OK", "BanAn", rCafeteria);
            var qSleep = CreateQuest("Q_Sleep_1830", "Di ngu", QuestType.Sleep, 18, 30, 21, 0, 5, 0,
                "Den gio di ngu", "Di ngu", "Giuong", rDormitory);

            // Day plans (demo 7 ngay).
            var days = new List<DayPlanSO>();
            for (int d = 1; d <= 7; d++)
            {
                var plan = CreateOrLoad<DayPlanSO>($"{Root}/DayPlans/Day{d:D2}.asset", p =>
                {
                    p.dayNumber = d;
                    p.label = $"Ngay {d}";
                    p.quests = new List<QuestDefSO> {
                        qExercise, qCleaning, qBreakfast, qStudyAm, qLunch, qStudyPm, qDinner, qSleep
                    };
                });
                days.Add(plan);
            }

            var dayCycle = CreateOrLoad<DayCycleConfigSO>($"{Root}/DayCycle.asset", dc =>
            {
                dc.dayPlans = days;
            });

            // Audio bank.
            var audio = CreateOrLoad<AudioBankSO>($"{Root}/Audio/AudioBank.asset", a =>
            {
                a.cues = CreateAllAudioCues();
            });

            // UI text.
            var uiText = CreateOrLoad<UITextSO>($"{Root}/UIText.asset", u =>
            {
                u.entries = new List<UITextSO.Entry>
                {
                    new UITextSO.Entry { key = "EndDay", text = "Het ngay, dang chuyen sang ngay tiep theo..." },
                    new UITextSO.Entry { key = "EnterClass", text = "Dang vao lop hoc..." },
                    new UITextSO.Entry { key = "EnterDorm", text = "Dang vao ky tuc xa..." },
                    new UITextSO.Entry { key = "EnterCafe", text = "Dang vao nha an..." },
                    new UITextSO.Entry { key = "MainMenu_NewGame", text = "Bat dau moi" },
                    new UITextSO.Entry { key = "MainMenu_Continue", text = "Tiep tuc" },
                    new UITextSO.Entry { key = "MainMenu_Quit", text = "Thoat" },
                };
            });

            // Database root.
            var db = CreateOrLoad<GameDatabaseSO>($"{Root}/_GameDatabase.asset", d =>
            {
                d.timeConfig = time;
                d.scoreConfig = score;
                d.dayCycle = dayCycle;
                d.uiText = uiText;
                d.subjects = new List<SubjectSO> { subjLichSu, subjQuocPhong };
                d.npcs = new List<NPCProfileSO> { npcDaiDoi };
                d.scenes = new List<SceneRouteSO> { rTitle, rWorld, rClassroom, rDormitory, rCafeteria };
                d.interactables = new List<InteractableSO> {
                    iSan, iVesinh, iCuaLop, iCuaKtx, iCuaNa, iBanLop, iBanAn, iGiuong, iNpcDaiDoi
                };
                d.audioBank = audio;
                d.playerNameDefault = "Hoc vien";
                d.autosaveIntervalMinutes = 5f;
            });

            AssetDatabase.SaveAssets();
            return db;
        }

        private static List<AudioCueSO> CreateAllAudioCues()
        {
            var ids = (AudioCueId[])System.Enum.GetValues(typeof(AudioCueId));
            var list = new List<AudioCueSO>();
            foreach (var id in ids)
            {
                if (id == AudioCueId.None) continue;
                var path = $"{Root}/Audio/AC_{id}.asset";
                var cue = CreateOrLoad<AudioCueSO>(path, c =>
                {
                    c.id = id;
                    c.channel = ChannelFromId(id);
                    c.volume = 1f;
                    c.pitchRange = new Vector2(1f, 1f);
                    c.loop = c.channel == AudioChannel.Music || c.channel == AudioChannel.Ambient;
                });
                list.Add(cue);
            }
            return list;
        }

        private static AudioChannel ChannelFromId(AudioCueId id)
        {
            int i = (int)id;
            if (i >= 1000 && i < 2000) return AudioChannel.UI;
            if (i >= 6000 && i < 7000) return AudioChannel.Music;
            if (i >= 7000 && i < 8000) return AudioChannel.Ambient;
            return AudioChannel.SFX;
        }

        private static SubjectSO CreateSubject(string id, string display)
        {
            return CreateOrLoad<SubjectSO>($"{Root}/Subjects/{id}.asset", s =>
            {
                s.id = id; s.displayName = display;
            });
        }

        private static QuizSetSO CreateQuizSet(string id, string title)
        {
            return CreateOrLoad<QuizSetSO>($"{Root}/QuizSets/{id}.asset", set =>
            {
                set.id = id; set.title = title; set.secondsPerQuestion = 15f;
                set.questions = new List<QuestionSO>();
                // Tao 10 cau placeholder.
                for (int i = 0; i < 10; i++)
                {
                    var q = CreateOrLoad<QuestionSO>($"{Root}/QuizSets/{id}_Q{i + 1:D2}.asset", qq =>
                    {
                        qq.stem = $"Cau hoi {i + 1} cua {title}? (Quyen sua noi dung sau)";
                        qq.options = new[] { "Dap an A", "Dap an B", "Dap an C", "Dap an D" };
                        qq.correctIndex = i % 4;
                        qq.explanation = "";
                    });
                    set.questions.Add(q);
                }
            });
        }

        private static SceneRouteSO CreateRoute(string id, string sceneName, string loadingText, bool freeze)
        {
            return CreateOrLoad<SceneRouteSO>($"{Root}/Scenes/{id}.asset", r =>
            {
                r.id = id;
                r.sceneName = sceneName;
                r.loadingText = loadingText;
                r.fadeOutSeconds = 0.4f;
                r.fadeInSeconds = 0.4f;
                r.minLoadingScreenSeconds = 3f;
                r.freezeTimeWhileLoaded = freeze;
                if (sceneName == "World") r.backgroundMusic = AudioCueId.Music_World;
                if (sceneName == "Classroom") r.backgroundMusic = AudioCueId.Music_Classroom;
            });
        }

        private static InteractableSO CreateInteractable(string fileName, string key, string label,
            InteractableKind kind, SceneRouteSO doorTarget = null, NPCProfileSO npcProfile = null)
        {
            return CreateOrLoad<InteractableSO>($"{Root}/Interactables/{fileName}.asset", i =>
            {
                i.key = key;
                i.displayLabel = label;
                i.kind = kind;
                i.doorTarget = doorTarget;
                i.npcProfile = npcProfile;
                i.aimDotThreshold = 0.5f;
                i.interactRadius = 2f;
            });
        }

        private static QuestDefSO CreateQuest(string id, string title, QuestType type,
            int sh, int sm, int dh, int dm, int kh, int km,
            string confirmText, string okBtn, string locationKey, SceneRouteSO targetScene,
            QuizSetSO quizSet = null, SubjectSO subject = null)
        {
            return CreateOrLoad<QuestDefSO>($"{Root}/Quests/{id}.asset", q =>
            {
                q.id = id;
                q.title = title;
                q.descriptionTemplate = $"{title} ({{deadline}})";
                q.type = type;
                q.startHour = sh; q.startMinute = sm;
                q.deadlineHour = dh; q.deadlineMinute = dm;
                q.skipToHour = kh; q.skipToMinute = km;
                q.targetScene = targetScene;
                q.interactableLocationKey = locationKey;
                q.confirmText = confirmText ?? "";
                q.okButtonText = okBtn ?? "OK";
                q.subject = subject;
                q.quizSet = quizSet;
                q.penaltyOnLate = 5;
                q.penaltyOnMissed = 5;
                q.startSound = AudioCueId.Quest_Started;
                q.completeSound = AudioCueId.Quest_Completed;
            });
        }

        private static T CreateOrLoad<T>(string path, System.Action<T> populate) where T : ScriptableObject
        {
            AssetScanner.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                populate(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var inst = ScriptableObject.CreateInstance<T>();
            populate(inst);
            AssetDatabase.CreateAsset(inst, path);
            return inst;
        }

        private static void EnsureFolders()
        {
            AssetScanner.EnsureFolder(Root);
            AssetScanner.EnsureFolder($"{Root}/Time");
            AssetScanner.EnsureFolder($"{Root}/Score");
            AssetScanner.EnsureFolder($"{Root}/DayPlans");
            AssetScanner.EnsureFolder($"{Root}/Quests");
            AssetScanner.EnsureFolder($"{Root}/Subjects");
            AssetScanner.EnsureFolder($"{Root}/QuizSets");
            AssetScanner.EnsureFolder($"{Root}/NPCs");
            AssetScanner.EnsureFolder($"{Root}/Scenes");
            AssetScanner.EnsureFolder($"{Root}/Interactables");
            AssetScanner.EnsureFolder($"{Root}/Audio");
        }
    }
}
