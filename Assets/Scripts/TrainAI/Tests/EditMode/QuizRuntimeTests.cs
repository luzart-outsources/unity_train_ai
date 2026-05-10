using NUnit.Framework;
using TrainAI.Configs;
using TrainAI.Systems.Quiz;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class QuizRuntimeTests
    {
        private QuizSetSO _set;

        [SetUp]
        public void Setup()
        {
            _set = ScriptableObject.CreateInstance<QuizSetSO>();
            _set.secondsPerQuestion = 15f;
            _set.questions = new System.Collections.Generic.List<QuestionSO>();
            for (int i = 0; i < 3; i++)
            {
                var q = ScriptableObject.CreateInstance<QuestionSO>();
                q.stem = $"Cau {i + 1}";
                q.options = new[] { "A", "B", "C", "D" };
                q.correctIndex = i % 4;
                _set.questions.Add(q);
            }
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < _set.questions.Count; i++)
                ScriptableObject.DestroyImmediate(_set.questions[i]);
            ScriptableObject.DestroyImmediate(_set);
        }

        [Test]
        public void Answer_Correct_IncrementsCount()
        {
            var rt = new QuizRuntime(_set);
            bool ok = rt.Answer(0); // cau 1 correct = 0
            Assert.That(ok, Is.True);
            Assert.That(rt.CorrectCount, Is.EqualTo(1));
            Assert.That(rt.TotalAnswered, Is.EqualTo(1));
        }

        [Test]
        public void Answer_Wrong_DoesNotIncrementCorrect()
        {
            var rt = new QuizRuntime(_set);
            bool ok = rt.Answer(3); // wrong
            Assert.That(ok, Is.False);
            Assert.That(rt.CorrectCount, Is.EqualTo(0));
            Assert.That(rt.TotalAnswered, Is.EqualTo(1));
        }

        [Test]
        public void IsFinished_True_AfterAllQuestions()
        {
            var rt = new QuizRuntime(_set);
            for (int i = 0; i < 3; i++) rt.Answer(0);
            Assert.That(rt.IsFinished, Is.True);
        }

        [Test]
        public void Tick_Timeout_AdvancesAsWrong()
        {
            var rt = new QuizRuntime(_set);
            rt.Tick(20f); // timeout > 15s
            Assert.That(rt.CurrentIndex, Is.EqualTo(1));
            Assert.That(rt.CorrectCount, Is.EqualTo(0));
        }

        [Test]
        public void Answer_AfterFinished_DoesNothing()
        {
            var rt = new QuizRuntime(_set);
            for (int i = 0; i < 3; i++) rt.Answer(0);
            rt.Answer(0);
            Assert.That(rt.TotalAnswered, Is.EqualTo(3)); // khong tang nua
        }

        [Test]
        public void RemainingSeconds_Decreases()
        {
            var rt = new QuizRuntime(_set);
            float initial = rt.RemainingSeconds;
            rt.Tick(2f);
            Assert.That(rt.RemainingSeconds, Is.EqualTo(initial - 2f).Within(0.01f));
        }
    }
}
