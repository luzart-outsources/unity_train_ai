using TrainAI.Configs;

namespace TrainAI.Systems.Quiz
{
    // Pure C# - testable. UI co the wrap qua state machine sub.
    public class QuizRuntime
    {
        private readonly QuizSetSO _set;
        private int _currentIdx;
        private float _remainingSeconds;

        public int CorrectCount { get; private set; }
        public int TotalAnswered { get; private set; }
        public bool IsFinished => _set == null || _currentIdx >= _set.Count;
        public int CurrentIndex => _currentIdx;
        public float RemainingSeconds => _remainingSeconds;

        public QuestionSO Current =>
            _set != null && !IsFinished ? _set.questions[_currentIdx] : null;

        public QuizRuntime(QuizSetSO set)
        {
            _set = set;
            ResetTimer();
        }

        private void ResetTimer()
        {
            _remainingSeconds = _set != null ? _set.secondsPerQuestion : 15f;
        }

        // Goi tu Update voi Time.deltaTime.
        public bool Tick(float dt)
        {
            if (IsFinished) return false;
            _remainingSeconds -= dt;
            if (_remainingSeconds <= 0f)
            {
                Answer(-1); // timeout = sai
                return true; // event "timeout"
            }
            return false;
        }

        // Return: true if correct, false if wrong.
        public bool Answer(int chosenIdx)
        {
            if (IsFinished) return false;
            var q = Current;
            bool correct = q != null && chosenIdx == q.correctIndex;
            if (correct) CorrectCount++;
            TotalAnswered++;
            _currentIdx++;
            ResetTimer();
            return correct;
        }
    }
}
