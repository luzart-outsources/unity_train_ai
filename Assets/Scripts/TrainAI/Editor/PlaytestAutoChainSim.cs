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
    // Auto-chain regression sim.
    //
    // The original Playtest30DaySim cheats by calling Clock.SkipTo(quest.startHour,
    // quest.startMinute) BEFORE completing each quest — that masks the real-gameplay
    // bug where finishing one quest early left the player idling for the next.
    //
    // This sim does NOT cheat. It only seeds the clock at 04:30 once at day start
    // (matching what InteractionRouter.AdvanceDayAndReset does after sleep) and then
    // lets QuestRouter.Complete -> AutoAdvanceClockToNextQuest skip the clock for it.
    //
    // If the chain works, every quest of every day should auto-activate without a
    // manual SkipTo. If the chain is broken, quests will SKIP (current != q) and
    // the report flags them.
    public static class PlaytestAutoChainSim
    {
        const string ReportPath = "Library/playtest_autochain_sim.md";
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";

        [MenuItem("Tools/Build Game/Playtest - Auto-Chain Sim (no cheat SkipTo)", false, 252)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TrainAI Auto-Chain 30-day simulation");
            sb.AppendLine($"_run: {DateTime.Now:O}_");
            sb.AppendLine("_intent: verify QuestRouter auto-advances clock without per-quest SkipTo_");
            int totalErrors = 0;

            var loc = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
            if (loc == null)
            {
                sb.AppendLine("FATAL: ServiceLocator not found at " + LocatorPath);
                Finish(sb, ++totalErrors);
                return;
            }

            try
            {
                loc.playerState?.Reset();
                loc.clock?.Reset();
                loc.activeQuest?.Reset();
                loc.dayProgress?.Reset();
                if (loc.IsBootstrapped) loc.Shutdown();
                loc.Bootstrap();
                sb.AppendLine("- Bootstrap: services online");
            }
            catch (Exception e)
            {
                sb.AppendLine("ERR Bootstrap: " + e);
                Finish(sb, ++totalErrors);
                return;
            }

            int completed = 0, missed = 0, skipped = 0, expelCount = 0, quizRewards = 0, gapChainOk = 0;
            Action<ExpelTriggeredMsg> onExpel = _ => { expelCount++; sb.AppendLine("  EXPEL fired"); };
            BroadcastService.Subscribe(onExpel);

            for (int day = 1; day <= 30; day++)
            {
                sb.AppendLine($"\n## Day {day} ({loc.clock.weekday})");
                try
                {
                    // Match InteractionRouter.AdvanceDayAndReset day-start positioning.
                    loc.clock.day = day;
                    loc.Clock.SkipTo(4, 30);
                    loc.Quests.StartDay(day);
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
                    sb.AppendLine($"  WARN: no quests defined");
                    continue;
                }

                foreach (var q in daySO.quests)
                {
                    if (q == null) { skipped++; sb.AppendLine("  SKIP null"); continue; }
                    try
                    {
                        // Capture clock BEFORE Tick. If auto-advance is working, the
                        // previous Complete should have already skipped the clock to
                        // q.startTime, so a single Tick will pick it up.
                        int beforeMin = loc.clock.hour * 60 + loc.clock.minute;
                        int expectStart = q.window.startHour * 60 + q.window.startMinute;

                        loc.Quests.Tick(0.1f);

                        if (loc.activeQuest.current != q)
                        {
                            skipped++;
                            sb.AppendLine($"  SKIP {q.window.startHour:00}:{q.window.startMinute:00} '{q.title}' (auto-advance failed; clock={loc.clock.hour:00}:{loc.clock.minute:00}, expected>={q.window.startHour:00}:{q.window.startMinute:00}; current={loc.activeQuest.current?.title ?? "null"})");
                            // Recover with manual SkipTo so the rest of the day still runs.
                            loc.Clock.SkipTo(q.window.startHour, q.window.startMinute);
                            loc.Quests.Tick(0.1f);
                            if (loc.activeQuest.current != q) continue;
                        }
                        else if (beforeMin < expectStart)
                        {
                            // Auto-advance bridged a gap — that's the success path.
                            gapChainOk++;
                        }

                        bool success = (day % 7) != 0;
                        if (success && q is QuizQuestSO qzq && qzq.quizSet != null && qzq.quizSet.subject != null)
                        {
                            loc.Score.OnQuizResult(8, 10, qzq.quizSet.subject);
                            quizRewards++;
                        }

                        loc.Quests.Complete(q, success);
                        if (success) completed++; else missed++;
                        sb.AppendLine($"  [clock={loc.clock.hour:00}:{loc.clock.minute:00}] {q.title} -> {(success ? "OK" : "MISS")}");
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
            sb.AppendLine("## Summary");
            sb.AppendLine($"- Quests completed:    {completed}");
            sb.AppendLine($"- Quests missed:       {missed}");
            sb.AppendLine($"- Auto-chain gaps OK:  {gapChainOk}");
            sb.AppendLine($"- Quests skipped:      {skipped}");
            sb.AppendLine($"- Quiz rewards:        {quizRewards}");
            sb.AppendLine($"- Expel events:        {expelCount}");
            sb.AppendLine($"- Errors:              {totalErrors}");
            sb.AppendLine($"- Final hocTap:        {loc.playerState.hocTap}");
            sb.AppendLine($"- Final renLuyen:      {loc.playerState.renLuyen}");
            bool pass = totalErrors == 0 && skipped == 0;
            sb.AppendLine($"## RESULT: {(pass ? "PASS — quest chain auto-advances correctly" : "FAIL — auto-advance broken or errors thrown")}");
            Finish(sb, totalErrors);
        }

        static void Finish(StringBuilder sb, int errors)
        {
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[PlaytestAutoChainSim] done. errors={errors}. Report -> {ReportPath}");
        }
    }
}
