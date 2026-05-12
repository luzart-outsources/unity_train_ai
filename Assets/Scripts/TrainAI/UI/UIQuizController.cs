using Cysharp.Threading.Tasks;
using TMPro;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIQuizController : UIScreenBase
    {
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text counterText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Button[] answerButtons = new Button[4];
        [SerializeField] TMP_Text[] answerLabels = new TMP_Text[4];
        [SerializeField] Button continueButton;

        readonly Color _idleColor = Color.white;
        readonly Color _correctColor = new(0.4f, 0.85f, 0.4f);
        readonly Color _wrongColor = new(0.9f, 0.4f, 0.4f);

        QuizSetSO _set;
        int _index;
        int _correctCount;
        bool _answered;
        float _remaining;
        UniTaskCompletionSource<QuizResult> _tcs;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int idx = i;
                if (answerButtons[i] != null)
                    answerButtons[i].onClick.AddListener(() => OnAnswer(idx));
            }
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        public UniTask<QuizResult> ShowAsync(QuizSetSO set)
        {
            _set = set;
            _index = 0;
            _correctCount = 0;
            _tcs = new UniTaskCompletionSource<QuizResult>();
            Show();
            ShowQuestion();
            return _tcs.Task;
        }

        void Update()
        {
            if (_answered || _set == null || _index >= _set.questions.Count) return;
            _remaining -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _remaining)).ToString();
            if (_remaining <= 0f) Skip();
        }

        void ShowQuestion()
        {
            if (_set == null || _index >= _set.questions.Count) { Finish(); return; }
            var q = _set.questions[_index];
            _answered = false;
            _remaining = _set.perQuestionSec;

            if (counterText != null) counterText.text = $"{_index + 1}/{_set.questions.Count}";
            if (questionText != null) questionText.text = q != null ? q.question : "";
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                bool has = q != null && q.answers != null && i < q.answers.Length;
                answerButtons[i].gameObject.SetActive(has);
                if (has)
                {
                    if (answerLabels[i] != null) answerLabels[i].text = q.answers[i];
                    var img = answerButtons[i].GetComponent<Image>();
                    if (img != null) img.color = _idleColor;
                    answerButtons[i].interactable = true;
                }
            }
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }

        void OnAnswer(int idx)
        {
            if (_answered || _set == null) return;
            _answered = true;
            var q = _set.questions[_index];
            bool correct = q != null && idx == q.correctIndex;
            if (correct) _correctCount++;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                answerButtons[i].interactable = false;
                var img = answerButtons[i].GetComponent<Image>();
                if (img == null) continue;
                if (i == q.correctIndex) img.color = _correctColor;
                else if (i == idx) img.color = _wrongColor;
            }
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        void Skip()
        {
            _answered = true;
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        void OnContinue()
        {
            _index++;
            if (_set == null || _index >= _set.questions.Count) Finish();
            else ShowQuestion();
        }

        void Finish()
        {
            int total = _set != null ? _set.questions.Count : 0;
            var result = new QuizResult { correctCount = _correctCount, totalCount = total };
            Hide();
            _tcs?.TrySetResult(result);
        }
    }
}
