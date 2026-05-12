using System;
using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class SOGenerator
    {
        const string AreaFolder = "Assets/_Data/Areas";
        const string SceneFolder = "Assets/_Data/Scenes";
        const string SubjectFolder = "Assets/_Data/Subjects";
        const string QuizFolder = "Assets/_Data/Quizzes";
        const string QuestFolder = "Assets/_Data/Quests";
        const string DayFolder = "Assets/_Data/Days";
        const string NpcFolder = "Assets/_Data/NPCs";
        const string DbFolder = "Assets/_Data/Config";

        [MenuItem("Tools/Build Game/1. Generate Data SO", false, 101)]
        public static void GenerateAll()
        {
            var bp = BlueprintLoader.Load();
            if (bp == null) return;

            EnsureFolders();

            var sceneRefs = GenerateSceneRefs(bp);
            var areas = GenerateAreas(bp, sceneRefs);
            var subjects = GenerateSubjects(bp);
            var quizzes = GenerateQuizzes(bp, subjects);
            GenerateNPCs(bp, areas);
            GenerateDays(bp, areas, quizzes, sceneRefs);
            GenerateInteractables(bp, areas, sceneRefs);

            AutoPopulateDBs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SOGenerator] generated {areas.Count} areas, {subjects.Count} subjects, {quizzes.Count} quizzes, {bp.npcs.Count} NPCs, {bp.days.Count} days");
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { AreaFolder, SceneFolder, SubjectFolder, QuizFolder, QuestFolder, DayFolder, NpcFolder, DbFolder })
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = System.IO.Path.GetDirectoryName(f).Replace('\\', '/');
                    string leaf = System.IO.Path.GetFileName(f);
                    if (!AssetDatabase.IsValidFolder(parent))
                        AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(parent).Replace('\\','/'),
                                                  System.IO.Path.GetFileName(parent));
                    AssetDatabase.CreateFolder(parent, leaf);
                }
        }

        static Dictionary<string, SceneRefSO> GenerateSceneRefs(WorldBlueprint bp)
        {
            var byName = new Dictionary<string, SceneRefSO>();
            var sceneNames = new HashSet<string> { "00_Bootstrap","01_MainMenu","02_CutScene","03_CreateChar","10_World","11_LopHoc","12_NhaAn","13_KyTucXa","99_Ending" };
            foreach (var a in bp.areas)
            {
                if (!string.IsNullOrEmpty(a.scene)) sceneNames.Add(a.scene);
                if (!string.IsNullOrEmpty(a.sub)) sceneNames.Add(a.sub);
            }
            foreach (var sn in sceneNames)
            {
                var sr = BlueprintLoader.CreateOrLoad<SceneRefSO>($"{SceneFolder}/SceneRef_{sn}.asset");
                sr.sceneName = sn;
                sr.mode = sn == "00_Bootstrap" || sn == "01_MainMenu" || sn == "99_Ending"
                    ? SceneRefSO.LoadMode.Single : SceneRefSO.LoadMode.Additive;
                EditorUtility.SetDirty(sr);
                byName[sn] = sr;
            }
            return byName;
        }

        static Dictionary<string, AreaSO> GenerateAreas(WorldBlueprint bp, Dictionary<string, SceneRefSO> sceneRefs)
        {
            var byId = new Dictionary<string, AreaSO>();
            foreach (var a in bp.areas)
            {
                var area = BlueprintLoader.CreateOrLoad<AreaSO>($"{AreaFolder}/Area_{a.id}.asset");
                area.id = a.id;
                area.worldPos = ToVec(a.pos);
                area.size = ToVec(a.size);
                if (a.scene != null) sceneRefs.TryGetValue(a.scene, out var sc);
                if (!string.IsNullOrEmpty(a.scene)) area.scene = sceneRefs[a.scene];
                if (!string.IsNullOrEmpty(a.sub) && sceneRefs.TryGetValue(a.sub, out var sub)) area.subScene = sub;
                EditorUtility.SetDirty(area);
                byId[a.id] = area;
            }
            return byId;
        }

        static Dictionary<string, SubjectSO> GenerateSubjects(WorldBlueprint bp)
        {
            var byId = new Dictionary<string, SubjectSO>();
            foreach (var s in bp.subjects)
            {
                var subj = BlueprintLoader.CreateOrLoad<SubjectSO>($"{SubjectFolder}/Subject_{s.id}.asset");
                subj.id = s.id;
                subj.displayName = s.displayName;
                subj.maxPointsPerLesson = s.maxPerLesson;
                EditorUtility.SetDirty(subj);
                byId[s.id] = subj;
            }
            return byId;
        }

        static Dictionary<string, QuizSetSO> GenerateQuizzes(WorldBlueprint bp, Dictionary<string, SubjectSO> subjects)
        {
            var byKey = new Dictionary<string, QuizSetSO>();
            const int QuestionsPerLesson = 10;
            foreach (var sBp in bp.subjects)
            {
                if (!subjects.TryGetValue(sBp.id, out var subj)) continue;
                int lessons = Mathf.Max(1, sBp.lessons);
                for (int i = 1; i <= lessons; i++)
                {
                    string key = $"{subj.id}_{i}";
                    var qs = BlueprintLoader.CreateOrLoad<QuizSetSO>($"{QuizFolder}/QuizSet_{key}.asset");
                    qs.subject = subj;
                    qs.perQuestionSec = 15f;
                    if (qs.questions == null) qs.questions = new List<QuizQuestionSO>();
                    if (qs.questions.Count == 0)
                    {
                        for (int qi = 1; qi <= QuestionsPerLesson; qi++)
                        {
                            string qPath = $"{QuizFolder}/Q_{key}_{qi:00}.asset";
                            var q = BlueprintLoader.CreateOrLoad<QuizQuestionSO>(qPath);
                            q.question = $"[{subj.displayName} - bai {i}] Cau hoi mau {qi}?";
                            q.answers = new[] {
                                "Dap an A (placeholder)",
                                "Dap an B (placeholder)",
                                "Dap an C (placeholder)",
                                "Dap an D (placeholder)"
                            };
                            q.correctIndex = (qi - 1) % 4;
                            EditorUtility.SetDirty(q);
                            qs.questions.Add(q);
                        }
                    }
                    EditorUtility.SetDirty(qs);
                    byKey[key] = qs;
                }
            }
            return byKey;
        }

        static void GenerateNPCs(WorldBlueprint bp, Dictionary<string, AreaSO> areas)
        {
            foreach (var n in bp.npcs)
            {
                var npc = BlueprintLoader.CreateOrLoad<NPCSO>($"{NpcFolder}/NPC_{n.id}.asset");
                npc.id = n.id;
                npc.displayName = string.IsNullOrEmpty(n.displayName) ? n.id : n.displayName;
                npc.spawnPos = ToVec(n.pos);

                npc.movement = LoadStrategy<MovementStrategySO>(n.movement);
                npc.dialogue = LoadStrategy<DialogueStrategySO>(n.dialogue);

                if (n.schedule != null && n.schedule.Count > 0)
                {
                    var schedulePath = $"{NpcFolder}/Schedule_{n.id}.asset";
                    var sched = BlueprintLoader.CreateOrLoad<ScheduleSO>(schedulePath);
                    sched.entries.Clear();
                    foreach (var e in n.schedule)
                    {
                        if (!ParseTime(e.time, out int h, out int m)) continue;
                        if (!areas.TryGetValue(e.area, out var area)) continue;
                        sched.entries.Add(new ScheduleEntry { hour = h, minute = m, target = area });
                    }
                    EditorUtility.SetDirty(sched);
                    npc.schedule = sched;
                }
                EditorUtility.SetDirty(npc);
            }
        }

        static void GenerateDays(WorldBlueprint bp, Dictionary<string, AreaSO> areas,
                                 Dictionary<string, QuizSetSO> quizzes,
                                 Dictionary<string, SceneRefSO> sceneRefs)
        {
            foreach (var d in bp.days)
            {
                var day = BlueprintLoader.CreateOrLoad<DaySO>($"{DayFolder}/Day_{d.day:00}.asset");
                day.dayIndex = d.day;
                if (Enum.TryParse<Weekday>(d.weekday, out var wd)) day.weekday = wd;
                day.quests.Clear();

                int qi = 0;
                foreach (var q in d.quests)
                {
                    qi++;
                    QuestSO quest = q.type switch
                    {
                        "GotoConfirm" => BuildGotoConfirm(q, d.day, qi, areas),
                        "Quiz" => BuildQuiz(q, d.day, qi, areas, quizzes, sceneRefs),
                        "SceneTransition" => BuildSceneTransition(q, d.day, qi, areas, sceneRefs),
                        "Sleep" => BuildSleep(q, d.day, qi, areas),
                        "FreeRoam" => BuildFreeRoam(q, d.day, qi, areas),
                        _ => BuildGotoConfirm(q, d.day, qi, areas)
                    };
                    if (quest != null) day.quests.Add(quest);
                }
                EditorUtility.SetDirty(day);
            }
        }

        static QuestSO BuildQuiz(QuestBlueprint q, int day, int qi,
                                 Dictionary<string, AreaSO> areas,
                                 Dictionary<string, QuizSetSO> quizzes,
                                 Dictionary<string, SceneRefSO> sceneRefs)
        {
            var path = $"{QuestFolder}/Quest_{day:00}_{qi:00}_{Safe(q.title)}.asset";
            var asset = BlueprintLoader.CreateOrLoad<QuizQuestSO>(path);
            ApplyCommon(asset, q, day, qi, areas);
            if (!string.IsNullOrEmpty(q.quizSet) && quizzes.TryGetValue(q.quizSet, out var qs))
                asset.quizSet = qs;
            if (sceneRefs.TryGetValue("11_LopHoc", out var lop)) asset.classroomScene = lop;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static QuestSO BuildSceneTransition(QuestBlueprint q, int day, int qi,
                                            Dictionary<string, AreaSO> areas,
                                            Dictionary<string, SceneRefSO> sceneRefs)
        {
            var path = $"{QuestFolder}/Quest_{day:00}_{qi:00}_{Safe(q.title)}.asset";
            var asset = BlueprintLoader.CreateOrLoad<SceneTransitionQuestSO>(path);
            ApplyCommon(asset, q, day, qi, areas);
            if (!string.IsNullOrEmpty(q.subscene) && sceneRefs.TryGetValue(q.subscene, out var sub))
                asset.subScene = sub;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static QuestSO BuildSleep(QuestBlueprint q, int day, int qi, Dictionary<string, AreaSO> areas)
        {
            var path = $"{QuestFolder}/Quest_{day:00}_{qi:00}_{Safe(q.title)}.asset";
            var asset = BlueprintLoader.CreateOrLoad<SleepQuestSO>(path);
            ApplyCommon(asset, q, day, qi, areas);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static QuestSO BuildFreeRoam(QuestBlueprint q, int day, int qi, Dictionary<string, AreaSO> areas)
        {
            var path = $"{QuestFolder}/Quest_{day:00}_{qi:00}_{Safe(q.title)}.asset";
            var asset = BlueprintLoader.CreateOrLoad<FreeRoamQuestSO>(path);
            ApplyCommon(asset, q, day, qi, areas);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void ApplyCommon(QuestSO asset, QuestBlueprint q, int day, int qi,
                                Dictionary<string, AreaSO> areas)
        {
            asset.id = $"Q_d{day:00}_{qi:00}";
            asset.title = q.title ?? "";
            asset.latePenalty = 5;
            if (!string.IsNullOrEmpty(q.area) && areas.TryGetValue(q.area, out var area)) asset.area = area;
            if (ParseTime(q.start, out int sh, out int sm) && ParseTime(q.deadline, out int eh, out int em))
                asset.window = new TimeRange(sh, sm, eh, em);
        }

        static string AreaConfirmText(string areaId) => areaId switch
        {
            "SanVanDong" => "Ban dang tap the duc. Bam OK de hoan thanh.",
            "DonVeSinh"  => "Ban dang don ve sinh. Bam OK de hoan thanh.",
            "FreeArea"   => "Khu vuc tu do.",
            _            => $"Ban dang o khu {areaId}."
        };

        static string AreaActionVerb(string areaId) => areaId switch
        {
            "SanVanDong" => "tap the duc",
            "DonVeSinh"  => "don ve sinh",
            "FreeArea"   => "kham pha",
            _            => $"tuong tac"
        };

        static (int hour, int minute) AreaSkipTo(string areaId) => areaId switch
        {
            "SanVanDong" => (5, 30),
            "DonVeSinh"  => (6, 45),
            _            => (-1, 0)
        };

        const string InteractFolder = "Assets/_Data/Interactables";

        static void GenerateInteractables(WorldBlueprint bp, Dictionary<string, AreaSO> areas,
                                          Dictionary<string, SceneRefSO> sceneRefs)
        {
            if (!AssetDatabase.IsValidFolder(InteractFolder))
                AssetDatabase.CreateFolder("Assets/_Data", "Interactables");

            foreach (var a in bp.areas)
            {
                if (!areas.TryGetValue(a.id, out var area)) continue;
                var path = $"{InteractFolder}/Interactable_{a.id}.asset";
                var inter = BlueprintLoader.CreateOrLoad<InteractableSO>(path);
                inter.id = $"I_{a.id}";
                inter.area = area;

                InteractionSO action = null;
                if (a.id == "KTX_Door")
                {
                    var actPath = "Assets/_Data/Strategies/Interact_Sleep_KTX.asset";
                    var sl = BlueprintLoader.CreateOrLoad<SleepInteractionSO>(actPath);
                    sl.confirmText = "Di ngu?";
                    sl.loadingText = "Sang ngay hom sau...";
                    sl.loadingSeconds = 2f;
                    sl.promptText = "Bam E de di ngu";
                    EditorUtility.SetDirty(sl);
                    action = sl;
                }
                else if (!string.IsNullOrEmpty(a.sub) && sceneRefs.TryGetValue(a.sub, out var subScene))
                {
                    var actPath = $"Assets/_Data/Strategies/Interact_SceneTo_{a.sub}.asset";
                    var st = BlueprintLoader.CreateOrLoad<SceneTransitionInteractionSO>(actPath);
                    st.targetScene = subScene;
                    st.transitionText = $"Dang vao {a.sub}...";
                    st.promptText = $"Bam E de vao {a.id}";
                    EditorUtility.SetDirty(st);
                    action = st;
                }
                else
                {
                    var actPath = $"Assets/_Data/Strategies/Interact_Confirm_{a.id}.asset";
                    var cf = BlueprintLoader.CreateOrLoad<OpenConfirmInteractionSO>(actPath);
                    cf.confirmText = AreaConfirmText(a.id);
                    cf.promptText = $"Bam E de {AreaActionVerb(a.id)}";
                    cf.skipTime = a.id != "FreeArea";
                    var (sh, sm) = AreaSkipTo(a.id);
                    cf.skipToHour = sh; cf.skipToMinute = sm;
                    cf.completeQuestOnConfirm = a.id != "FreeArea";
                    EditorUtility.SetDirty(cf);
                    action = cf;
                }
                inter.onInteract = action;
                EditorUtility.SetDirty(inter);
            }
        }

        static QuestSO BuildGotoConfirm(QuestBlueprint q, int day, int qi, Dictionary<string, AreaSO> areas)
        {
            var path = $"{QuestFolder}/Quest_{day:00}_{qi:00}_{Safe(q.title)}.asset";
            var asset = BlueprintLoader.CreateOrLoad<GotoConfirmQuestSO>(path);
            asset.id = $"Q_d{day:00}_{qi:00}";
            asset.title = q.title ?? "";
            asset.latePenalty = 5;
            if (!string.IsNullOrEmpty(q.area) && areas.TryGetValue(q.area, out var area)) asset.area = area;
            if (ParseTime(q.start, out int sh, out int sm) && ParseTime(q.deadline, out int eh, out int em))
                asset.window = new TimeRange(sh, sm, eh, em);
            if (!string.IsNullOrEmpty(q.confirmText)) asset.confirmText = q.confirmText;
            asset.skipToHour = ParseTime(q.deadline, out int dh, out int dm) ? dh : sh + 1;
            asset.skipToMinute = ParseTime(q.deadline, out _, out int dmm) ? dmm : 0;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static T LoadStrategy<T>(string key) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(key)) return null;
            string path = $"Assets/_Data/Strategies/";
            string fn = typeof(T) == typeof(MovementStrategySO) ? $"Movement_{key}.asset"
                       : typeof(T) == typeof(DialogueStrategySO) ? $"Dialogue_{key}.asset"
                       : null;
            if (fn == null) return null;
            return AssetDatabase.LoadAssetAtPath<T>(path + fn);
        }

        static void AutoPopulateDBs()
        {
            CreateAndPopulate<QuestDB>($"{DbFolder}/QuestDB.asset", $"{QuestFolder}");
            CreateAndPopulate<NPCDB>($"{DbFolder}/NPCDB.asset", $"{NpcFolder}");
            CreateAndPopulate<DayDB>($"{DbFolder}/DayDB.asset", $"{DayFolder}");
            CreateAndPopulate<QuizDB>($"{DbFolder}/QuizDB.asset", $"{QuizFolder}");
            CreateAndPopulate<AreaDB>($"{DbFolder}/AreaDB.asset", $"{AreaFolder}");
            CreateAndPopulate<SubjectDB>($"{DbFolder}/SubjectDB.asset", $"{SubjectFolder}");
            CreateAndPopulate<InteractableDB>($"{DbFolder}/InteractableDB.asset", "Assets/_Data/Interactables");
        }

        static void CreateAndPopulate<T>(string assetPath, string searchFolder) where T : ScriptableObject
        {
            var db = BlueprintLoader.CreateOrLoad<T>(assetPath);
            var type = typeof(T);
            var listField = type.GetField("all");
            if (listField == null) return;
            string elementTypeName = type.Name.Replace("DB", "SO");
            var guids = AssetDatabase.FindAssets($"t:{elementTypeName}", new[] { searchFolder });
            var list = (System.Collections.IList)System.Activator.CreateInstance(listField.FieldType);
            foreach (var g in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g),
                    System.Type.GetType($"TrainAI.SO.Base.{elementTypeName}, TrainAI.SO.Base"));
                if (asset != null) list.Add(asset);
            }
            listField.SetValue(db, list);
            EditorUtility.SetDirty(db);
        }

        static Vector3 ToVec(VectorBlueprint v) => v == null ? Vector3.zero : new Vector3(v.x, v.y, v.z);

        static bool ParseTime(string s, out int h, out int m)
        {
            h = 0; m = 0;
            if (string.IsNullOrEmpty(s)) return false;
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            return int.TryParse(parts[0], out h) && int.TryParse(parts[1], out m);
        }

        static string Safe(string s) => string.IsNullOrEmpty(s) ? "x" : s.Replace(' ', '_').Replace('/', '_');
    }
}
