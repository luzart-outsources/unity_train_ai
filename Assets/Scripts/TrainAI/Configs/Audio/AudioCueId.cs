namespace TrainAI.Configs
{
    // ID cho moi audio cue - dung trong code: AudioManager.Play(AudioCueId.UI_Click).
    // Them entry moi: them o day + them AudioCueSO trong AudioBank asset.
    public enum AudioCueId
    {
        None = 0,

        // UI (1xxx)
        UI_Click = 1000,
        UI_OpenPopup = 1001,
        UI_ClosePopup = 1002,
        UI_ToastInfo = 1003,
        UI_ToastWarning = 1004,
        UI_ToastError = 1005,
        UI_ToastSuccess = 1006,

        // Score (2xxx)
        Score_AcademicGain = 2000,
        Score_DisciplinePenalty = 2001,
        Score_KickedOut = 2002,

        // Quest (3xxx)
        Quest_Started = 3000,
        Quest_Completed = 3001,
        Quest_Late = 3002,
        Quest_Missed = 3003,

        // Day cycle (4xxx)
        Day_Start = 4000,
        Day_End = 4001,
        Sleep = 4002,

        // Quiz (5xxx)
        Quiz_Correct = 5000,
        Quiz_Wrong = 5001,
        Quiz_TimeoutWarning = 5002, // 5s cuoi cua cau hoi

        // Music (6xxx)
        Music_MainMenu = 6000,
        Music_World = 6001,
        Music_Classroom = 6002,
        Music_Cutscene = 6003,
        Music_Ending = 6004,

        // Ambient (7xxx)
        Ambient_World = 7000,
        Ambient_Dormitory = 7001,
        Ambient_Classroom = 7002,

        // Player (8xxx)
        Player_Footstep = 8000,
        Player_Interact = 8001,
    }

    // Channel mixing.
    public enum AudioChannel
    {
        Music = 0,
        SFX = 1,
        UI = 2,
        Ambient = 3,
    }
}
