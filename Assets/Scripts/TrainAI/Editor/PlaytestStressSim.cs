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
    // Stress sim — exercises edge cases the other two sims do not:
    //
    //   * Late completion: finish a quest at endTime - 1 minute. Tests that
    //     the deadline-miss path doesn't fire when completion races the
    //     deadline.
    //
    //   * Sleep miss: skip the Sleep interaction; let the clock roll past
    //     midnight via natural Tick. Verifies GameClockService auto-rollover
    //     fires DayStartedMsg correctly without InteractionRouter.AdvanceDayAndReset.
    //
    //   * Double Complete: call Complete twice on the same quest. Verifies the
    //     idempotency guard at QuestRouter.Complete line 122 still holds.
    //
    //   * Complete with no active quest: invoke Complete(null) or with the
    //     wrong quest reference. Should no-op, not throw.
    //
    //   * Skip-past-window: SkipTo a time AFTER the next quest's endTime.
    //     Auto-chain should not skip backward; the quest should be auto-
    //     missed on the next Tick when QuestRouter notices now >= deadline.
    //
    // Designed to catch the kind of crashes the user reports as "siêu nhiều"
    // before they hit a real playthrough.
    public static class PlaytestStressSim
    {
        const string ReportPath = "Library/playtest_stress_sim.md";
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";

        [MenuItem("Tools/Build Game/Playtest - Stress Sim (edge cases)", false, 253)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TrainAI stress simulation");
            sb.AppendLine($"_run: {DateTime.Now:O}_");
            int errors = 0, passed = 0;

            var loc = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
            if (loc == null) { sb.AppendLine("FATAL: ServiceLocator not found"); Finish(sb, ++errors); return; }

            void Reset()
            {
                loc.playerState?.Reset();
                loc.clock?.Reset();
                loc.activeQuest?.Reset();
                loc.dayProgress?.Reset();
                if (loc.IsBootstrapped) loc.Shutdown();
                loc.Bootstrap();
            }

            // ---- Test 1: double-Complete is idempotent (no double-credit) ----
            sb.AppendLine("\n## Test 1: double Complete is idempotent");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var q = loc.activeQuest.current;
                if (q == null) { errors++; sb.AppendLine("  FAIL: no first quest activated"); }
                else
                {
                    loc.Quests.Complete(q, true);
                    int completedAfterFirst = loc.dayProgress.completedToday.Count;
                    loc.Quests.Complete(q, true);
                    int completedAfterSecond = loc.dayProgress.completedToday.Count;
                    if (completedAfterFirst == completedAfterSecond) { passed++; sb.AppendLine($"  PASS: completedToday stayed at {completedAfterFirst} after double-Complete"); }
                    else { errors++; sb.AppendLine($"  FAIL: double-Complete inflated count {completedAfterFirst} -> {completedAfterSecond}"); }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 2: Complete(null) no-ops ----
            sb.AppendLine("\n## Test 2: Complete(null) is safe");
            try
            {
                Reset();
                loc.Quests.Complete(null, true);
                loc.Quests.Complete(null, false);
                passed++;
                sb.AppendLine("  PASS: no throw on Complete(null)");
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 3: Complete with mismatched quest reference no-ops ----
            sb.AppendLine("\n## Test 3: Complete(wrongQuest) is safe");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var active = loc.activeQuest.current;
                var fakeQ = ScriptableObject.CreateInstance<FreeRoamQuestSO>();
                fakeQ.id = "FAKE";
                loc.Quests.Complete(fakeQ, true);
                if (loc.activeQuest.current == active) { passed++; sb.AppendLine("  PASS: active quest unchanged after Complete(fakeQ)"); }
                else { errors++; sb.AppendLine("  FAIL: Complete(fakeQ) cleared real active quest"); }
                ScriptableObject.DestroyImmediate(fakeQ);
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 4: late completion (at endTime-1) doesn't double-fire miss ----
            sb.AppendLine("\n## Test 4: late completion right before deadline");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var q = loc.activeQuest.current;
                if (q == null) { errors++; sb.AppendLine("  FAIL: no quest activated"); }
                else
                {
                    int endMin = q.window.endHour * 60 + q.window.endMinute - 1;
                    int eh = endMin / 60, em = endMin % 60;
                    loc.Clock.SkipTo(eh, em);
                    // Tick first so the deadline-check sees the time
                    loc.Quests.Tick(0.1f);
                    // Complete BEFORE the next tick: should land in completedToday, not missed.
                    loc.Quests.Complete(q, true);
                    bool inCompleted = loc.dayProgress.completedToday.Contains(q.id);
                    bool inMissed    = loc.dayProgress.missedToday.Contains(q.id);
                    if (inCompleted && !inMissed) { passed++; sb.AppendLine($"  PASS: '{q.title}' booked completed at {eh:00}:{em:00}"); }
                    else { errors++; sb.AppendLine($"  FAIL: completed={inCompleted} missed={inMissed}"); }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 5: Sleep miss → clock rolls past midnight → DayStartedMsg fires ----
            sb.AppendLine("\n## Test 5: clock auto-rollover when Sleep quest is missed");
            try
            {
                Reset();
                bool sawDayStart = false;
                int observedDay = -1;
                Action<DayStartedMsg> handler = m => { sawDayStart = true; observedDay = m.day; };
                BroadcastService.Subscribe(handler);

                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Clock.SkipTo(23, 59);
                // Hammer Tick with enough real-seconds that ANY reasonable
                // gameHourPerRealMinute setting (0.1 .. 60) pushes the clock
                // past midnight. 600 real-sec = 10 real-min, which at 0.1
                // game-hour/real-min still yields 60 game-min → 1 hr forward.
                loc.Clock.Tick(600f);

                BroadcastService.Unsubscribe(handler);

                if (sawDayStart && observedDay == 2) { passed++; sb.AppendLine($"  PASS: DayStartedMsg fired with day={observedDay} via natural rollover"); }
                else { errors++; sb.AppendLine($"  FAIL: sawDayStart={sawDayStart} observedDay={observedDay} (clock={loc.clock.hour:00}:{loc.clock.minute:00} day={loc.clock.day})"); }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 6: SkipTo past deadline auto-misses on next Tick (not crash) ----
            sb.AppendLine("\n## Test 6: SkipTo past deadline auto-misses");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var q = loc.activeQuest.current;
                if (q == null) { errors++; sb.AppendLine("  FAIL: no quest activated"); }
                else
                {
                    string id = q.id;
                    loc.Clock.SkipTo(q.window.endHour + 1, 0);
                    loc.Quests.Tick(0.1f);
                    bool inMissed = loc.dayProgress.missedToday.Contains(id);
                    bool stillActive = loc.activeQuest.current == q;
                    if (inMissed && !stillActive) { passed++; sb.AppendLine($"  PASS: '{q.title}' auto-missed and cleared"); }
                    else { errors++; sb.AppendLine($"  FAIL: missed={inMissed} stillActive={stillActive}"); }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 7: chain still continuous after auto-miss ----
            sb.AppendLine("\n## Test 7: chain continues after auto-miss");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var first = loc.activeQuest.current;
                if (first == null) { errors++; sb.AppendLine("  FAIL: no quest activated"); }
                else
                {
                    // Walk past the first quest's deadline to force miss
                    loc.Clock.SkipTo(first.window.endHour + 1, 0);
                    loc.Quests.Tick(0.1f);
                    // After miss + AutoChain, a different pending quest should now be active
                    // OR the clock auto-advanced to the next quest's start time.
                    var second = loc.activeQuest.current;
                    if (second != null && second != first) { passed++; sb.AppendLine($"  PASS: chain advanced from '{first.title}' to '{second.title}'"); }
                    else
                    {
                        // The second-quest's startTime may still be in the future
                        // relative to first.endHour+1; AutoChain should have
                        // skipped clock forward. Tick again to confirm.
                        loc.Quests.Tick(0.1f);
                        second = loc.activeQuest.current;
                        if (second != null && second != first) { passed++; sb.AppendLine($"  PASS (2nd tick): chain advanced to '{second.title}'"); }
                        else { errors++; sb.AppendLine($"  FAIL: no chain after miss; current={second?.title ?? "null"}"); }
                    }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 8: rapid back-to-back complete chain (10 quests in a row) ----
            sb.AppendLine("\n## Test 8: rapid back-to-back complete chain");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                int activated = 0, completed = 0;
                for (int i = 0; i < 20; i++)
                {
                    loc.Quests.Tick(0.1f);
                    var q = loc.activeQuest.current;
                    if (q == null) break;
                    activated++;
                    loc.Quests.Complete(q, true);
                    completed++;
                }
                if (activated >= 8 && completed == activated) { passed++; sb.AppendLine($"  PASS: chained {completed} quests in tight loop, all completed"); }
                else { errors++; sb.AppendLine($"  FAIL: activated={activated} completed={completed}"); }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 9: Shutdown/Bootstrap cycle doesn't leak subscribers ----
            sb.AppendLine("\n## Test 9: re-bootstrap doesn't double-fire DayStarted");
            try
            {
                Reset();
                // 5 cycles BEFORE we subscribe — each Shutdown calls
                // BroadcastService.Clear() which would otherwise wipe our
                // probe handler. Subscribe last so we observe ONLY the
                // post-cycle state.
                for (int i = 0; i < 5; i++)
                {
                    loc.Shutdown();
                    loc.Bootstrap();
                }
                int dayStartCount = 0;
                Action<DayStartedMsg> handler = _ => dayStartCount++;
                BroadcastService.Subscribe(handler);

                loc.Clock.AdvanceDay();
                BroadcastService.Unsubscribe(handler);

                if (dayStartCount == 1) { passed++; sb.AppendLine("  PASS: exactly 1 DayStartedMsg after 5 bootstrap cycles"); }
                else { errors++; sb.AppendLine($"  FAIL: got {dayStartCount} DayStartedMsg (expected 1) — subscriber leak"); }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 10: FreeRoam quest at deadline is graceful success ----
            // Regression for the "11:30-14:00 lunch break" UX: when Nghi_trua
            // ended at 14:00, QuestRouter.MissCurrent fired, polluting
            // missedToday and broadcasting QuestMissedMsg (which hid the arrow
            // and flashed "Hết giờ"). Free-roam slots aren't task failures —
            // they're rest windows — so the deadline should complete them.
            sb.AppendLine("\n## Test 10: FreeRoam quest at deadline completes (not misses)");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(11, 30); // start of Nghi_trua window
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                // Walk through the day until Nghi_trua is current.
                int safety = 20;
                while (safety-- > 0 && !(loc.activeQuest.current is FreeRoamQuestSO))
                {
                    var q = loc.activeQuest.current;
                    if (q == null) { loc.Quests.Tick(0.1f); continue; }
                    if (q is FreeRoamQuestSO) break;
                    loc.Quests.Complete(q, true);
                    loc.Quests.Tick(0.1f);
                }
                var freeRoam = loc.activeQuest.current as FreeRoamQuestSO;
                if (freeRoam == null) { errors++; sb.AppendLine($"  FAIL: no FreeRoam quest activated (current={loc.activeQuest.current?.title ?? "null"})"); }
                else
                {
                    string id = freeRoam.id;
                    int renBefore = loc.playerState.renLuyen;
                    int missCountBefore = loc.dayProgress.missedToday.Count;
                    int completedBefore = loc.dayProgress.completedToday.Count;
                    // Roll past Nghi_trua's deadline so Tick triggers the
                    // graceful-end path.
                    loc.Clock.SkipTo(freeRoam.window.endHour, freeRoam.window.endMinute);
                    loc.Quests.Tick(0.1f);
                    bool wasMissed = loc.dayProgress.missedToday.Contains(id);
                    bool wasCompleted = loc.dayProgress.completedToday.Contains(id);
                    int renAfter = loc.playerState.renLuyen;
                    if (!wasMissed && wasCompleted && renAfter == renBefore)
                    { passed++; sb.AppendLine($"  PASS: '{freeRoam.title}' booked completed (not missed), renLuyen stayed at {renAfter}"); }
                    else
                    { errors++; sb.AppendLine($"  FAIL: missed={wasMissed} completed={wasCompleted} renLuyen {renBefore}->{renAfter} (missedToday={loc.dayProgress.missedToday.Count - missCountBefore})"); }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 11: non-FreeRoam quest miss applies latePenalty (-5 renLuyen) ----
            // GDD: "Khi mà đi trễ/ không hoàn thành 1 nhiệm vụ, hệ thống trừ 5 điểm".
            // The penalty path runs through the quest runtime's OnComplete(false)
            // callback — verify it actually fires when the clock walks past
            // deadline without the player completing.
            sb.AppendLine("\n## Test 11: late penalty deducts renLuyen on real-quest miss");
            try
            {
                Reset();
                loc.clock.day = 1;
                loc.Clock.SkipTo(4, 30);
                loc.Quests.StartDay(1);
                loc.Quests.Tick(0.1f);
                var q = loc.activeQuest.current;
                if (q == null || q is FreeRoamQuestSO) { errors++; sb.AppendLine($"  FAIL: first quest is null or free-roam (current={q?.title ?? "null"})"); }
                else
                {
                    int renBefore = loc.playerState.renLuyen;
                    int expectedPenalty = q.latePenalty;
                    loc.Clock.SkipTo(q.window.endHour, q.window.endMinute);
                    loc.Quests.Tick(0.1f);
                    int renAfter = loc.playerState.renLuyen;
                    int delta = renBefore - renAfter;
                    if (delta == expectedPenalty)
                    { passed++; sb.AppendLine($"  PASS: '{q.title}' miss deducted {delta} renLuyen (latePenalty={expectedPenalty})"); }
                    else
                    { errors++; sb.AppendLine($"  FAIL: '{q.title}' renLuyen {renBefore}->{renAfter} (delta={delta}, expected {expectedPenalty})"); }
                }
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            // ---- Test 12: SimpleWalkMovement walks toward target then arrives ----
            // GDD section 89: "NPC di chuyển tự do (Học sinh) — di chuyển theo
            // thời gian biểu". The scheduler pushes targets into MovementService
            // via SetTarget, and the agent's Tick must close the distance over
            // a few intervals then stop within arriveRadius. Headless verifies
            // the math without needing play-mode (which crashed under 7 NPCs).
            sb.AppendLine("\n## Test 12: SimpleWalkAgent walks toward target and arrives");
            try
            {
                var dummyGo = new GameObject("SimpleWalkTestSubject");
                dummyGo.transform.position = new Vector3(0, 0, 0);
                var so = ScriptableObject.CreateInstance<FreeRoamQuestSO>(); // unused but proves SO API still works
                ScriptableObject.DestroyImmediate(so);

                var walkSO = ScriptableObject.CreateInstance<TrainAI.SO.Concrete.SimpleWalkMovementSO>();
                walkSO.speed = 4f;
                walkSO.arriveRadius = 0.5f;
                walkSO.turnSpeed = 720f;
                var agent = walkSO.Bind(dummyGo.transform);
                agent.Target = new Vector3(10, 0, 0);

                int iterations = 0;
                Vector3 lastPos = dummyGo.transform.position;
                bool madeProgress = false;
                while (iterations < 100 && Vector3.Distance(dummyGo.transform.position, agent.Target) > 0.6f)
                {
                    agent.Tick(null, 0.2f);
                    if (Vector3.Distance(dummyGo.transform.position, lastPos) > 0.01f) madeProgress = true;
                    lastPos = dummyGo.transform.position;
                    iterations++;
                }
                float finalDist = Vector3.Distance(dummyGo.transform.position, agent.Target);
                bool arrived = finalDist < 0.6f;
                if (madeProgress && arrived)
                { passed++; sb.AppendLine($"  PASS: agent walked {dummyGo.transform.position.x:F1}m in {iterations} ticks, dist to target={finalDist:F2}"); }
                else
                { errors++; sb.AppendLine($"  FAIL: madeProgress={madeProgress} arrived={arrived} finalDist={finalDist:F2} iters={iterations} pos={dummyGo.transform.position}"); }

                ScriptableObject.DestroyImmediate(walkSO);
                UnityEngine.Object.DestroyImmediate(dummyGo);
            }
            catch (Exception e) { errors++; sb.AppendLine("  ERR: " + e.Message); }

            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine($"- Tests passed: {passed}");
            sb.AppendLine($"- Errors / Fails: {errors}");
            sb.AppendLine($"## RESULT: {(errors == 0 ? "PASS" : "FAIL")}");
            Finish(sb, errors);
        }

        static void Finish(StringBuilder sb, int errors)
        {
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[PlaytestStressSim] done. errors={errors}. Report -> {ReportPath}");
        }
    }
}
