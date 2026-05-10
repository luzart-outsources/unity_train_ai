namespace TrainAI.Configs
{
    // AI mode cua NPC - quyet dinh Phase A (chat) hoac Phase B (movement) co active.
    public enum NPCAIMode
    {
        None = 0,            // Static, khong AI
        SentisChat = 1,      // Phase A - chi chat
        Movement = 2,        // Phase B - chi tu di
        ChatAndMovement = 3, // Ca hai
    }
}
