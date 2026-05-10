namespace TrainAI.Configs
{
    // Loai quest theo GDD InGame. Moi loai map toi 1 IQuestRunner trong Systems/Quest.
    public enum QuestType
    {
        Exercise = 0,        // San van dong - UIConfirm + skip thoi gian
        Cleaning = 1,        // Khu ve sinh - UIConfirm + skip thoi gian
        Eat = 2,             // Phong an - vao Cafeteria + tuong tac ban
        StudyMorning = 3,    // Lop hoc - vao Classroom + UIQuiz
        StudyAfternoon = 4,  // Lop hoc buoi chieu, khac bo de
        Sleep = 5,           // Ky tuc xa - UIConfirm + next day
        FreeRoam = 6,        // Khong phai nhiem vu - cua so thoi gian ranh
        Cutscene = 7,        // Chay cutscene
        Custom = 8,          // Hook code rieng (advanced minigame)
    }
}
