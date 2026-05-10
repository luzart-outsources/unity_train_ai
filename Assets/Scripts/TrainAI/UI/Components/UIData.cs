using Cysharp.Threading.Tasks;
using TrainAI.Configs;

namespace TrainAI.UI.Components
{
    // Data POCO cho ConfirmScreen.
    public class ConfirmData
    {
        public string Title;
        public string Message;
        public string OkLabel = "OK";
        public string CancelLabel;        // null = khong hien Cancel
        public UniTaskCompletionSource<bool> ResultTcs = new UniTaskCompletionSource<bool>();
    }

    // Data POCO cho QuizScreen.
    public class QuizData
    {
        public QuizSetSO Set;
        public UniTaskCompletionSource<int> ResultTcs = new UniTaskCompletionSource<int>();
    }

    // Data POCO cho DialogueScreen.
    public class DialogueData
    {
        public NPCProfileSO Npc;
        public UniTaskCompletionSource<bool> ResultTcs = new UniTaskCompletionSource<bool>();
    }

    // Data POCO cho LoadingScreen.
    public class LoadingData
    {
        public string Text;
    }

    // Data POCO cho EndingScreen.
    public class EndingData
    {
        public int Discipline;
        public int Academic;
        public string DisciplineGrade;
        public string AcademicGrade;
    }

    // Data POCO cho CharacterCreate.
    public class CharacterCreateData
    {
        public string DefaultName;
        public UniTaskCompletionSource<string> ResultTcs = new UniTaskCompletionSource<string>();
    }
}
