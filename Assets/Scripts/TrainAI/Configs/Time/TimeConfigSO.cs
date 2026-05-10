using System;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // Config dong ho in-game theo GDD: 1h game = 3p thuc, 30 ngay, skip cuoi tuan.
    [CreateAssetMenu(menuName = "TrainAI/Time/Time Config", fileName = "TimeConfig")]
    public class TimeConfigSO : ScriptableObject
    {
        [Header("Ty le thoi gian")]
        [InfoBox("GDD: 1h game = 3p thuc = 180s. Test nhanh: 60s, cham: 300s.")]
        [DropdownNamed("60|1h = 1p (test nhanh)", "180|1h = 3p (GDD chuan)", "300|1h = 5p (cham)")]
        public float secondsPerGameHour = 180f;

        [Tooltip("Tick rate: phat OnTick moi N phut game.")]
        [Slider(1, 15)]
        public int tickEveryGameMinutes = 1;

        [Header("Vong lap ngay")]
        [Slider(1, 30)] public int firstDay = 1;
        [Slider(7, 60)] public int totalDays = 30;

        [InfoBox("GDD: skip thu 7 + CN. Sau ngay 6 (T7) -> ngay 8 (T2 tuan sau).")]
        public bool skipWeekend = true;

        [Tooltip("Ngay 1 = thu may. GDD khong noi ro -> default Mon (T2).")]
        public DayOfWeek weekStart = DayOfWeek.Monday;

        [Header("Quy tac reset ngay")]
        [InfoBox("Sang ngay moi: thoi diem = quest dau tien - X phut. GDD = 30p.")]
        [Slider(0, 120)] public int dayStartOffsetMinutesBeforeFirstQuest = 30;

        [Header("Late penalty")]
        [InfoBox("Qua X phut game khong toi quest hien tai -> Late + tru diem.", InfoBoxType.Warning)]
        [Slider(0, 60)] public int lateAfterMinutes = 15;

        [Header("Default day start (fallback neu khong co quest)")]
        [Slider(0, 23)] public int defaultDayStartHour = 5;
        [Slider(0, 59)] public int defaultDayStartMinute = 0;
    }
}
