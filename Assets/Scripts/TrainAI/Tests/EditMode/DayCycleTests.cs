using NUnit.Framework;
using TrainAI.Configs;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class DayCycleTests
    {
        [Test]
        public void GetPlanForDay_FindsByDayNumber()
        {
            var cycle = ScriptableObject.CreateInstance<DayCycleConfigSO>();
            var d1 = ScriptableObject.CreateInstance<DayPlanSO>(); d1.dayNumber = 1;
            var d2 = ScriptableObject.CreateInstance<DayPlanSO>(); d2.dayNumber = 2;
            cycle.dayPlans = new System.Collections.Generic.List<DayPlanSO> { d1, d2 };

            Assert.That(cycle.GetPlanForDay(1), Is.SameAs(d1));
            Assert.That(cycle.GetPlanForDay(2), Is.SameAs(d2));
            Assert.That(cycle.GetPlanForDay(99), Is.Null);

            ScriptableObject.DestroyImmediate(d1);
            ScriptableObject.DestroyImmediate(d2);
            ScriptableObject.DestroyImmediate(cycle);
        }

        [Test]
        public void DayPlan_FirstQuest_ReturnsFirstOrNull()
        {
            var plan = ScriptableObject.CreateInstance<DayPlanSO>();
            Assert.That(plan.FirstQuest, Is.Null);

            var q1 = ScriptableObject.CreateInstance<QuestDefSO>();
            plan.quests = new System.Collections.Generic.List<QuestDefSO> { q1 };
            Assert.That(plan.FirstQuest, Is.SameAs(q1));

            ScriptableObject.DestroyImmediate(q1);
            ScriptableObject.DestroyImmediate(plan);
        }

        [Test]
        public void NPCSchedule_GetLocationAtHour_ReturnsLatestEntry()
        {
            var sched = ScriptableObject.CreateInstance<NPCScheduleSO>();
            sched.entries = new System.Collections.Generic.List<NPCScheduleSO.ScheduleEntry>
            {
                new NPCScheduleSO.ScheduleEntry { hour = 6, locationKey = "SanVanDong" },
                new NPCScheduleSO.ScheduleEntry { hour = 7, locationKey = "NhaAn" },
                new NPCScheduleSO.ScheduleEntry { hour = 14, locationKey = "LopHoc" },
            };
            Assert.That(sched.GetLocationAtHour(5), Is.EqualTo(""));
            Assert.That(sched.GetLocationAtHour(6), Is.EqualTo("SanVanDong"));
            Assert.That(sched.GetLocationAtHour(6), Is.EqualTo("SanVanDong"));
            Assert.That(sched.GetLocationAtHour(8), Is.EqualTo("NhaAn"));
            Assert.That(sched.GetLocationAtHour(15), Is.EqualTo("LopHoc"));

            ScriptableObject.DestroyImmediate(sched);
        }
    }
}
