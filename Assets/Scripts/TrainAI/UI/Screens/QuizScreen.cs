using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using Luzart.NewBase;
using TMPro;
using TrainAI.Configs;
using TrainAI.Systems.Audio;
using TrainAI.Systems.Quiz;
using TrainAI.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    public class QuizScreen : UIBase<QuizData>
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI txtStem;
        [SerializeField] private TextMeshProUGUI txtCounter;
        [SerializeField] private Button[] btnAnswers = new Button[4];
        [SerializeField] private TextMeshProUGUI[] txtAnswers = new TextMeshProUGUI[4];
        [SerializeField] private Image imgCountdown;
        [SerializeField] private TextMeshProUGUI txtCountdown;
        [SerializeField] private SelectSwitchImage[] answerFeedback = new SelectSwitchImage[4];
        [SerializeField] private Button btnContinue;

        private QuizRuntime _runtime;
        private CancellationTokenSource _cts;
        private UniTaskCompletionSource<int> _answerTcs;
        private bool _showingFeedback;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            for (int i = 0; i < btnAnswers.Length; i++)
            {
                int idx = i;
                if (btnAnswers[i] != null)
                    btnAnswers[i].onClick.AddListener(() => OnAnswer(idx));
            }
            if (btnContinue != null) btnContinue.onClick.AddListener(OnContinue);
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnShownAsync(QuizData data, CancellationToken ct)
        {
            if (data == null || data.Set == null || data.Set.Count == 0)
            {
                data?.ResultTcs.TrySetResult(0);
                return;
            }
            _runtime = new QuizRuntime(data.Set);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                while (!_runtime.IsFinished)
                {
                    BindCurrent();
                    SetFeedbackAll(0);
                    if (btnContinue != null) btnContinue.gameObject.SetActive(false);
                    SetButtonsInteractable(true);

                    int chosen = await WaitAnswerAsync(_runtime.Current.correctIndex, _cts.Token);
                    PlayFeedback(chosen, _runtime.Current.correctIndex);
                    _runtime.Answer(chosen);

                    SetButtonsInteractable(false);
                    if (btnContinue != null) btnContinue.gameObject.SetActive(true);
                    await WaitContinueAsync(_cts.Token);
                }
            }
            catch (System.OperationCanceledException) { }

            int correct = _runtime != null ? _runtime.CorrectCount : 0;
            data.ResultTcs.TrySetResult(correct);
            await UIManager.Instance.HideAsync(this.Id);
        }

        protected override UniTask OnHiddenAsync(QuizData data, UIHideReason reason, CancellationToken ct)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            return UniTask.CompletedTask;
        }

        private async UniTask<int> WaitAnswerAsync(int correctIndex, CancellationToken ct)
        {
            _answerTcs = new UniTaskCompletionSource<int>();
            float warningPlayed = 0f;
            while (!_runtime.IsFinished && _answerTcs.Task.Status == UniTaskStatus.Pending)
            {
                if (ct.IsCancellationRequested) return -1;
                float dt = UnityEngine.Time.unscaledDeltaTime;
                if (_runtime.Tick(dt))
                {
                    // timeout - return -1 as wrong, but Answer da chay roi.
                    // Roll back: Tick goi Answer noi bo, nen advance.
                    return -1;
                }
                UpdateCountdown();
                if (_runtime.RemainingSeconds <= 5f && warningPlayed == 0f)
                {
                    AudioManager.Instance?.Play(AudioCueId.Quiz_TimeoutWarning);
                    warningPlayed = 1f;
                }
                await UniTask.Yield(ct);
            }
            return _answerTcs.Task.Status == UniTaskStatus.Succeeded
                ? _answerTcs.Task.GetAwaiter().GetResult()
                : -1;
        }

        private async UniTask WaitContinueAsync(CancellationToken ct)
        {
            _showingFeedback = true;
            // Wait for btnContinue click - simplified using polling SetActive=false signal.
            while (_showingFeedback)
            {
                if (ct.IsCancellationRequested) return;
                await UniTask.Yield(ct);
            }
        }

        private void OnAnswer(int idx)
        {
            if (_answerTcs == null || _answerTcs.Task.Status != UniTaskStatus.Pending) return;
            _answerTcs.TrySetResult(idx);
        }

        private void OnContinue()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            _showingFeedback = false;
        }

        private void BindCurrent()
        {
            var q = _runtime.Current;
            if (q == null) return;
            if (txtStem != null) txtStem.text = q.stem;
            if (txtCounter != null) txtCounter.text = $"{_runtime.CurrentIndex + 1}/{Data.Set.Count}";
            for (int i = 0; i < 4; i++)
            {
                if (txtAnswers[i] != null && q.options != null && i < q.options.Length)
                    txtAnswers[i].text = q.options[i];
            }
        }

        private void UpdateCountdown()
        {
            if (Data == null || Data.Set == null) return;
            float r = _runtime.RemainingSeconds;
            float total = Data.Set.secondsPerQuestion;
            if (imgCountdown != null) imgCountdown.fillAmount = total > 0 ? r / total : 0;
            if (txtCountdown != null) txtCountdown.text = Mathf.CeilToInt(r).ToString();
        }

        private void SetFeedbackAll(int idx)
        {
            for (int i = 0; i < answerFeedback.Length; i++)
                if (answerFeedback[i] != null) answerFeedback[i].Select(idx);
        }

        private void PlayFeedback(int chosen, int correct)
        {
            // 0 = neutral, 1 = green (correct), 2 = red (wrong)
            for (int i = 0; i < answerFeedback.Length; i++)
            {
                if (answerFeedback[i] == null) continue;
                if (i == correct) answerFeedback[i].Select(1);
                else if (i == chosen) answerFeedback[i].Select(2);
                else answerFeedback[i].Select(0);
            }
            if (chosen == correct)
                AudioManager.Instance?.Play(AudioCueId.Quiz_Correct);
            else
                AudioManager.Instance?.Play(AudioCueId.Quiz_Wrong);
        }

        private void SetButtonsInteractable(bool v)
        {
            for (int i = 0; i < btnAnswers.Length; i++)
                if (btnAnswers[i] != null) btnAnswers[i].interactable = v;
        }
    }
}
