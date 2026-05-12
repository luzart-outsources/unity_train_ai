namespace TrainAI.Core.Messages
{
    public readonly struct DayStartedMsg
    {
        public readonly int day; public readonly Weekday weekday;
        public DayStartedMsg(int d, Weekday w) { day = d; weekday = w; }
    }

    public readonly struct DayEndedMsg
    {
        public readonly int day; public DayEndedMsg(int d) { day = d; }
    }

    public readonly struct TimeTickMsg
    {
        public readonly int hour, minute;
        public TimeTickMsg(int h, int m) { hour = h; minute = m; }
    }

    public readonly struct QuestActivatedMsg
    {
        public readonly UnityEngine.Object quest;
        public QuestActivatedMsg(UnityEngine.Object q) { quest = q; }
    }

    public readonly struct QuestCompletedMsg
    {
        public readonly UnityEngine.Object quest;
        public readonly bool success;
        public readonly int scoreDelta;
        public QuestCompletedMsg(UnityEngine.Object q, bool s, int sd) { quest = q; success = s; scoreDelta = sd; }
    }

    public readonly struct QuestMissedMsg
    {
        public readonly UnityEngine.Object quest;
        public QuestMissedMsg(UnityEngine.Object q) { quest = q; }
    }

    public readonly struct ScoreChangedMsg
    {
        public readonly int hocTap, renLuyen;
        public ScoreChangedMsg(int h, int r) { hocTap = h; renLuyen = r; }
    }

    public readonly struct ExpelTriggeredMsg { }

    public readonly struct InteractZoneEnteredMsg
    {
        public readonly UnityEngine.Object target;
        public InteractZoneEnteredMsg(UnityEngine.Object t) { target = t; }
    }

    public readonly struct InteractZoneExitedMsg
    {
        public readonly UnityEngine.Object target;
        public InteractZoneExitedMsg(UnityEngine.Object t) { target = t; }
    }

    public readonly struct InteractPressedMsg
    {
        public readonly UnityEngine.Object target;
        public InteractPressedMsg(UnityEngine.Object t) { target = t; }
    }

    public readonly struct SceneTransitionRequestedMsg
    {
        public readonly UnityEngine.Object scene;
        public SceneTransitionRequestedMsg(UnityEngine.Object s) { scene = s; }
    }

    public readonly struct DialogueRequestMsg
    {
        public readonly UnityEngine.Object npc;
        public readonly string input;
        public DialogueRequestMsg(UnityEngine.Object n, string i) { npc = n; input = i; }
    }

    public readonly struct DialogueRepliedMsg
    {
        public readonly UnityEngine.Object npc;
        public readonly string text;
        public DialogueRepliedMsg(UnityEngine.Object n, string t) { npc = n; text = t; }
    }

    public readonly struct QuizStartedMsg
    {
        public readonly UnityEngine.Object set;
        public QuizStartedMsg(UnityEngine.Object s) { set = s; }
    }

    public readonly struct QuizEndedMsg
    {
        public readonly int correctCount, totalCount;
        public QuizEndedMsg(int c, int t) { correctCount = c; totalCount = t; }
    }

    public readonly struct GameEndedMsg
    {
        public readonly int gradeIndex;
        public GameEndedMsg(int g) { gradeIndex = g; }
    }
}
