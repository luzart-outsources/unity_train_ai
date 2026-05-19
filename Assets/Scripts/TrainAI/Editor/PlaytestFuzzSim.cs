using System;
using System.Collections.Generic;
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
    // Deterministic fuzz simulation.
    //
    // Throws every imaginable garbage operation at QuestRouter / GameClockService
    // (random Complete(null), wrong-quest Complete, repeated Complete, junk
    // SkipTo) interleaved with legitimate quest progression, and verifies the
    // services NEVER throw an unhandled exception across 30 days.
    //
    // Fixed seed (42) so a flake here means a real regression, not a heisenbug.
    // The other 3 sims test the happy path + named edge cases; this one is the
    // safety net for the long tail of "what if a buggy interaction or save-file
    // corrupts state midway through the day."
    public static class PlaytestFuzzSim
    {
        const string ReportPath = "Library/playtest_fuzz_sim.md";
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";
        const int Seed = 42;
        const int Days = 30;
        const int JunkOpsPerQuest = 4; // each quest interleaves with random garbage

        [MenuItem("Tools/Build Game/Playtest - Fuzz Sim (random garbage ops)", false, 254)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TrainAI fuzz simulation");
            sb.AppendLine($"_run: {DateTime.Now:O}  seed={Seed}  days={Days}_");

            var loc = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
            if (loc == null) { sb.AppendLine("FATAL: ServiceLocator not found"); Finish(sb, 1); return; }

            // Reset + bootstrap
            loc.playerState?.Reset();
            loc.clock?.Reset();
            loc.activeQuest?.Reset();
            loc.dayProgress?.Reset();
            if (loc.IsBootstrapped) loc.Shutdown();
            loc.Bootstrap();

            var rng = new System.Random(Seed);

            // A throwaway "fake quest" used as junk Complete target. Created once.
            var fakeQuest = ScriptableObject.CreateInstance<FreeRoamQuestSO>();
            fakeQuest.id = "FUZZ_FAKE_QUEST";

            int unhandledThrows = 0;
            int junkOpsExecuted = 0;
            int realCompletes = 0;
            int realMisses = 0;
            int autoMissesObserved = 0;
            int daysCompleted = 0;

            Action<QuestMissedMsg> onMiss = _ => autoMissesObserved++;
            BroadcastService.Subscribe(onMiss);

            try
            {
                for (int day = 1; day <= Days; day++)
                {
                    // Junk between days: random SkipTo, random Complete(null), etc.
                    for (int j = 0; j < 3; j++) ExecuteJunk(loc, rng, fakeQuest, ref junkOpsExecuted, ref unhandledThrows);

                    try
                    {
                        loc.clock.day = day;
                        loc.Clock.SkipTo(4, 30);
                        loc.Quests.StartDay(day);
                    }
                    catch (Exception e) { unhandledThrows++; sb.AppendLine($"  D{day} StartDay THREW: {e.GetType().Name}: {e.Message}"); continue; }

                    var daySO = loc.dayDB != null ? loc.dayDB.ByIndex(day) : null;
                    int questCount = daySO?.quests?.Count ?? 0;
                    if (questCount == 0) continue;

                    // Mid-day junk + 70% real complete probability per quest
                    for (int qi = 0; qi < questCount; qi++)
                    {
                        // Interleave junk
                        for (int j = 0; j < JunkOpsPerQuest; j++) ExecuteJunk(loc, rng, fakeQuest, ref junkOpsExecuted, ref unhandledThrows);

                        try { loc.Quests.Tick(0.1f); }
                        catch (Exception e) { unhandledThrows++; sb.AppendLine($"  D{day}/Q{qi} Tick THREW: {e.Message}"); continue; }

                        var active = loc.activeQuest.current;
                        if (active == null) continue;

                        try
                        {
                            // 70% complete-successfully, 20% miss-by-completing-false, 10% leave for auto-miss
                            double roll = rng.NextDouble();
                            if (roll < 0.70)
                            {
                                // Quiz quests need score-system pump to behave like real interaction layer
                                if (active is QuizQuestSO qz && qz.quizSet != null && qz.quizSet.subject != null)
                                    loc.Score.OnQuizResult(rng.Next(5, 11), 10, qz.quizSet.subject);

                                loc.Quests.Complete(active, true);
                                realCompletes++;
                            }
                            else if (roll < 0.90)
                            {
                                loc.Quests.Complete(active, false);
                                realMisses++;
                            }
                            else
                            {
                                // Walk clock past the quest's deadline so MissCurrent fires from Tick.
                                loc.Clock.SkipTo(active.window.endHour + 1, 0);
                                loc.Quests.Tick(0.1f);
                            }
                        }
                        catch (Exception e) { unhandledThrows++; sb.AppendLine($"  D{day}/Q{qi} Complete THREW: {e.Message}"); }
                    }

                    try { loc.Clock.AdvanceDay(); daysCompleted++; }
                    catch (Exception e) { unhandledThrows++; sb.AppendLine($"  D{day} AdvanceDay THREW: {e.Message}"); }
                }
            }
            finally
            {
                BroadcastService.Unsubscribe(onMiss);
                ScriptableObject.DestroyImmediate(fakeQuest);
            }

            // Final state sanity
            int finalDay = loc.clock.day;
            int finalHocTap = loc.playerState.hocTap;
            int finalRenLuyen = loc.playerState.renLuyen;

            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine($"- Unhandled throws:    {unhandledThrows}");
            sb.AppendLine($"- Days completed:      {daysCompleted}");
            sb.AppendLine($"- Junk ops executed:   {junkOpsExecuted}");
            sb.AppendLine($"- Real completes:      {realCompletes}");
            sb.AppendLine($"- Real misses:         {realMisses}");
            sb.AppendLine($"- Auto-misses observed:{autoMissesObserved}");
            sb.AppendLine($"- Final clock.day:     {finalDay}");
            sb.AppendLine($"- Final hocTap:        {finalHocTap}");
            sb.AppendLine($"- Final renLuyen:      {finalRenLuyen}");
            bool pass = unhandledThrows == 0 && daysCompleted == Days;
            sb.AppendLine($"## RESULT: {(pass ? "PASS — services survived fuzz with 0 throws" : "FAIL — see throws above")}");
            Finish(sb, unhandledThrows);
        }

        // Randomly pick a junk op. Each must catch its own exception so the
        // fuzz doesn't abort on the first throw — we want to MEASURE throws,
        // not stop at them.
        static void ExecuteJunk(ServiceLocatorSO loc, System.Random rng, QuestSO fake,
                                ref int junkOpsExecuted, ref int unhandledThrows)
        {
            junkOpsExecuted++;
            int op = rng.Next(0, 8);
            try
            {
                switch (op)
                {
                    case 0: loc.Quests.Complete(null, true); break;
                    case 1: loc.Quests.Complete(null, false); break;
                    case 2: loc.Quests.Complete(fake, true); break;
                    case 3: loc.Quests.Complete(fake, rng.Next(2) == 0); break;
                    case 4: loc.Clock.SkipTo(rng.Next(0, 24), rng.Next(0, 60)); break;
                    case 5: loc.Quests.Tick(0.001f); break;
                    case 6:
                        // Repeated Complete on whatever is currently active.
                        var cur = loc.activeQuest.current;
                        if (cur != null) loc.Quests.Complete(cur, true);
                        break;
                    case 7: loc.Quests.IsInteractableAllowed(null); break;
                }
            }
            catch (Exception e)
            {
                unhandledThrows++;
                Debug.LogError($"[FuzzSim] junk op {op} threw: {e.Message}");
            }
        }

        static void Finish(StringBuilder sb, int errors)
        {
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[PlaytestFuzzSim] done. unhandled-throws={errors}. Report -> {ReportPath}");
        }
    }
}
