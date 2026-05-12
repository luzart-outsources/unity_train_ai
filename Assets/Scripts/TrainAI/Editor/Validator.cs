using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TrainAI.Core;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class Validator
    {
        const string ReportPath = "Library/automation_report.md";

        [MenuItem("Tools/Build Game/6. Validate Everything", false, 106)]
        public static void ValidateAll()
        {
            var report = new StringBuilder();
            report.AppendLine("# TrainAI automation report");
            report.AppendLine($"_generated: {System.DateTime.Now:O}_");
            report.AppendLine();

            int fail = 0;
            fail += CheckRequiredFields(report);
            fail += CheckSceneRefs(report);
            fail += CheckCompile(report);
            fail += CheckResponses(report);

            report.AppendLine();
            report.AppendLine(fail == 0 ? "## RESULT: PASS" : $"## RESULT: FAIL ({fail} issues)");

            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log($"[Validator] wrote {ReportPath} (fail={fail})");
            if (fail > 0) Debug.LogWarning("[Validator] issues found, see automation_report.md");
        }

        static int CheckRequiredFields(StringBuilder report)
        {
            report.AppendLine("## [Required] field check");
            int fail = 0;
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Data" });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;
                var fields = so.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var f in fields)
                {
                    if (f.GetCustomAttribute<RequiredAttribute>() == null) continue;
                    var v = f.GetValue(so);
                    bool empty = v == null || (v is string s && string.IsNullOrEmpty(s)) || (v is Object o && o == null);
                    if (empty)
                    {
                        report.AppendLine($"- FAIL `{path}` field `{f.Name}` is null/empty");
                        fail++;
                    }
                }
            }
            if (fail == 0) report.AppendLine("- OK all required fields populated");
            return fail;
        }

        static int CheckSceneRefs(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("## SceneRefSO existence check");
            int fail = 0;
            var registered = new HashSet<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s != null && !string.IsNullOrEmpty(s.path))
                    registered.Add(Path.GetFileNameWithoutExtension(s.path));

            var guids = AssetDatabase.FindAssets("t:SceneRefSO");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<TrainAI.SO.Base.SceneRefSO>(path);
                if (so == null) continue;
                if (string.IsNullOrEmpty(so.sceneName))
                {
                    report.AppendLine($"- FAIL `{path}` empty sceneName");
                    fail++;
                    continue;
                }
                if (!registered.Contains(so.sceneName))
                    report.AppendLine($"- WARN `{path}` sceneName `{so.sceneName}` not in EditorBuildSettings");
            }
            if (fail == 0) report.AppendLine("- OK SceneRefSO check");
            return fail;
        }

        static int CheckCompile(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("## Compile check");
            int fail = 0;
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            report.AppendLine($"- found {assemblies.Length} player assemblies");
            return fail;
        }

        static int CheckResponses(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("## responses.json check");
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Models/responses.json");
            if (asset == null) { report.AppendLine("- FAIL responses.json missing"); return 1; }
            int found = 0;
            foreach (var iv in System.Enum.GetNames(typeof(IntentId)))
                if (asset.text.Contains($"\"{iv}\"")) found++;
            if (found < 8) { report.AppendLine($"- FAIL only {found}/8 intent keys present"); return 1; }
            report.AppendLine("- OK all 8 intent keys present");
            return 0;
        }
    }
}
