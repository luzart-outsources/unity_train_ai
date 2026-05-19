using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // Per user request: "loop fix-test-review tầm 15 lần" for the NPC change.
    // Runs StressSim + 30DaySim N times and aggregates results. Designed for
    // -executeMethod TrainAI.Editor.MultiLoopRunner.Run15
    //
    // Output: Library/multi_loop_report.md with per-iteration PASS/FAIL
    // and a summary. Any iteration that hits errors marks the whole suite
    // as RED.
    public static class MultiLoopRunner
    {
        const string ReportPath = "Library/multi_loop_report.md";

        [MenuItem("Tools/Build Game/Playtest - Multi-Loop (15 iterations)", false, 254)]
        public static void Run15()
        {
            RunN(15);
            EditorApplication.Exit(0);
        }

        public static void RunN(int n)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Multi-loop regression — {n} iterations");
            sb.AppendLine($"_started: {DateTime.Now:O}_");

            int stressPass = 0, stressFail = 0;
            int dayPass = 0, dayFail = 0;
            for (int i = 1; i <= n; i++)
            {
                sb.AppendLine($"\n## Iteration {i}/{n}");
                try
                {
                    PlaytestStressSim.Run();
                    string stressMd = File.Exists("Library/playtest_stress_sim.md") ? File.ReadAllText("Library/playtest_stress_sim.md") : "";
                    bool stressOk = stressMd.Contains("RESULT: PASS");
                    if (stressOk) stressPass++; else stressFail++;
                    sb.AppendLine($"- StressSim: {(stressOk ? "PASS" : "FAIL")}");

                    Playtest30DaySim.Run();
                    string dayMd = File.Exists("Library/playtest_30day_sim.md") ? File.ReadAllText("Library/playtest_30day_sim.md") : "";
                    bool dayOk = dayMd.Contains("RESULT: PASS");
                    if (dayOk) dayPass++; else dayFail++;
                    sb.AppendLine($"- 30DaySim:   {(dayOk ? "PASS" : "FAIL")}");
                }
                catch (Exception e)
                {
                    sb.AppendLine($"- ERR: {e.Message}");
                    stressFail++;
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Aggregate");
            sb.AppendLine($"- StressSim: {stressPass}/{n} PASS");
            sb.AppendLine($"- 30DaySim:  {dayPass}/{n} PASS");
            sb.AppendLine($"## RESULT: {(stressFail == 0 && dayFail == 0 ? "PASS" : "FAIL")}");

            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[MultiLoopRunner] {n} iterations done. stress={stressPass}/{n} day={dayPass}/{n}. Report: {ReportPath}");
        }
    }
}
