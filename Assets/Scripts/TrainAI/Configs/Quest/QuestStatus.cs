namespace TrainAI.Configs
{
    // Trang thai runtime cua 1 quest trong ngay hien tai.
    public enum QuestStatus
    {
        NotStarted = 0,
        Active = 1,
        Completed = 2,
        Late = 3,        // Da bat dau muon hon lateAfterMinutes -> bi tru diem
        Missed = 4,      // Da qua deadline ma chua start -> bi tru diem
    }
}
