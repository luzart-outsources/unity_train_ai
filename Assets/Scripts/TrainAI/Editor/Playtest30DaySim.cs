using System;
using System.IO;
using System.Text;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.Services;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // Headless 30-day simulator. Bypasses scene/UI/play-mode entirely and
    // exercises the service layer directly: ServiceLocator.Bootstrap, then
    // for each day 1..30 advance the clock through each quest window,
    // auto-activate via Quests.Tick, complete via API, and AdvanceDay.
    //
    // Captures any exception thrown by any service into a markdown report.
    // Designed to run in -batchmode -executeMethod so we can loop fix→test
    // without launching a full editor session.
    public static class Playtest30DaySim
    {
        const string ReportPath = "Library/playtest_30day_sim.md";
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";

        [MenuItem("Tools/Build Game/Playtest - Simulate 30 Days (logic-only)", false, 251)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TrainAI 30-day simulation report");
            sb.AppendLine($"_run: {DateTime.Now:O}_");
            int totalErrors = 0;

            ServiceLocatorSO loc = null;
            try
            {
                loc = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
                if (loc == null)
                {
                    sb.AppendLine("FATAL: ServiceLocator not found at " + LocatorPath);
                    Finish(sb, ++totalErrors);
                    return;
                }

                // Reset persistent runtime SOs so each run starts clean.
                loc.playerState?.Reset();
                loc.clock?.Reset();
                loc.activeQuest?.Reset();
                loc.dayProgress?.Reset();

                if (loc.IsBootstrapped) loc.Shutdown();
                loc.Bootstrap();
                sb.AppendLine($"- Bootstrap: services online");
            }
            catch (Exception e)
            {
                totalErrors++;
                sb.AppendLine("ERR Bootstrap: " + e);
                Finish(sb, totalErrors);
                return;
            }

            int completed = 0, missed = 0, skipped = 0, expelCount = 0, quizRewards = 0;
            Action<ExpelTriggeredMsg> onExpel = _ => { expelCount++; sb.AppendLine("  EXPEL fired (renLuyen hit 0)"); };
            BroadcastService.Subscribe(onExpel);
            for (int day = 1; day <= 30; day++)
            {
                sb.AppendLine($"\n## Day {day} ({loc.clock.weekday})");
                try
                {
                    // Sync clock to start of day; call StartDay DIRECTLY to load
                    // today's quests. (Earlier version manually broadcast
                    // DayStartedMsg, which caused StartDay to re-enqueue an
                    // already-activated current quest — when the next SkipTo
                    // jumped past its deadline, the quest was unfairly missed,
                    // costing -5 renLuyen on day 2+. Calling StartDay directly
                    // sidesteps that double-init.)
                    loc.clock.day = day;
                    loc.Clock.SkipTo(5, 0);
                    loc.Quests.StartDay(day);
                    loc.Quests.Tick(0.1f);
                }
                catch (Exception e)
                {
                    totalErrors++;
                    sb.AppendLine("  ERR day-start: " + e.Message);
                    continue;
                }

                var daySO = loc.dayDB != null ? loc.dayDB.ByIndex(day) : null;
                if (daySO == null || daySO.quests == null)
                {
                    sb.AppendLine($"  WARN: no quests defined for day {day}");
                    continue;
                }

                foreach (var q in daySO.quests)
                {
                    if (q == null) { skipped++; sb.AppendLine("  SKIP null quest"); continue; }
                    try
                    {
                        // Move clock to quest start; Tick auto-activates.
                        loc.Clock.SkipTo(q.window.startHour, q.window.startMinute);
                        loc.Quests.Tick(0.1f);

                        if (loc.activeQuest.current != q)
                        {
                            // Try one more Tick — sometimes activation needs a beat.
                            loc.Quests.Tick(0.1f);
                        }

                        if (loc.activeQuest.current != q)
                        {
                            skipped++;
                            sb.AppendLine($"  SKIP {q.window.startHour:00}:{q.window.startMinute:00} '{q.title}' (not active; current={loc.activeQuest.current?.title ?? "null"})");
                            continue;
                        }

                        // Day-7 deliberate miss to exercise the failure path.
                        bool success = (day % 7) != 0;

                        // For Quiz quests in normal gameplay, the interaction
                        // path (OpenQuizInteractionSO) feeds quiz score to
                        // ScoreSystem.OnQuizResult, which awards hocTap. Since
                        // the simulator skips the interaction layer, we apply
                        // the equivalent quiz reward directly here so the
                        // economy is exercised end-to-end (80% correct).
                        if (success && q is QuizQuestSO qzq && qzq.quizSet != null && qzq.quizSet.subject != null)
                        {
                            int total = 10;
                            int correct = 8; // 80% — typical playthrough
                            loc.Score.OnQuizResult(correct, total, qzq.quizSet.subject);
                            quizRewards++;
                        }

                        loc.Quests.Complete(q, success);
                        if (success) completed++; else missed++;
                        sb.AppendLine($"  [{q.window.startHour:00}:{q.window.startMinute:00}] {q.title} -> {(success ? "OK" : "MISS")} (hocTap={loc.playerState.hocTap} renLuyen={loc.playerState.renLuyen})");
                    }
                    catch (Exception e)
                    {
                        totalErrors++;
                        sb.AppendLine($"  ERR '{q.title}': {e.GetType().Name}: {e.Message}");
                    }
                }

                try { loc.Clock.AdvanceDay(); }
                catch (Exception e) { totalErrors++; sb.AppendLine("  ERR AdvanceDay: " + e.Message); }
            }

            BroadcastService.Unsubscribe(onExpel);

            sb.AppendLine();
            sb.AppendLine($"## Summary");
            sb.AppendLine($"- Quests completed: {completed}");
            sb.AppendLine($"- Quests missed:    {missed}");
            sb.AppendLine($"- Quests skipped:   {skipped}");
            sb.AppendLine($"- Quiz rewards:     {quizRewards}");
            sb.AppendLine($"- Expel events:     {expelCount}");
            sb.AppendLine($"- Errors:           {totalErrors}");
            sb.AppendLine($"- Final hocTap:     {loc.playerState.hocTap}");
            sb.AppendLine($"- Final renLuyen:   {loc.playerState.renLuyen}");
            sb.AppendLine($"- Final day:        {loc.clock.day}");
            sb.AppendLine($"## RESULT: {(totalErrors == 0 && skipped == 0 ? "PASS" : (totalErrors == 0 ? "PASS-with-skips" : "FAIL"))}");

            Finish(sb, totalErrors);
        }

        static void Finish(StringBuilder sb, int errors)
        {
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[Playtest30DaySim] done. errors={errors}. Report -> {ReportPath}");
        }
    }
}
