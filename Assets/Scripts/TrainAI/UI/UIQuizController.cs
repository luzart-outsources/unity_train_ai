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

        // Bold pass/fail feedback that overlays the answer area for ~1.5s
        // after each pick. Color tints are subtle enough to confuse on a
        // glance (per user feedback 2026-05-22) — a giant ✓/✗ with a Vietnamese
        // label makes the result unambiguous.
        [SerializeField] GameObject feedbackOverlay;
        [SerializeField] TMP_Text feedbackIconText;
        [SerializeField] TMP_Text feedbackResultText;
        readonly Color _feedbackPassColor = new(0.16f, 0.78f, 0.20f);
        readonly Color _feedbackFailColor = new(0.85f, 0.20f, 0.20f);
        readonly Color _feedbackSkipColor = new(0.85f, 0.65f, 0.10f);

        readonly Color _idleColor = Color.white;
        // Selected-by-player palette is brighter / more saturated than the
        // "this is the answer key" palette so the player can always tell
        // which answer was theirs even when it happened to be correct.
        readonly Color _selectedCorrectColor = new(0.16f, 0.68f, 0.20f); // deep green: player picked + right
        readonly Color _selectedWrongColor   = new(0.78f, 0.18f, 0.18f); // deep red: player picked + wrong
        readonly Color _revealCorrectColor   = new(0.62f, 0.92f, 0.62f); // pale green: this is the right answer
        readonly Color _dimColor             = new(0.78f, 0.78f, 0.78f); // grey: not selected, not correct

        QuizSetSO _set;
        int _index;
        int _correctCount;
        bool _answered;
        int _selectedIdx = -1;
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
            _selectedIdx = -1;
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
                    // Restore scale in case the prior question's selection
                    // bumped it up — without this, every selected slot stays
                    // visually "pressed" through later questions.
                    answerButtons[i].transform.localScale = Vector3.one;
                    answerButtons[i].interactable = true;
                }
            }
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (feedbackOverlay != null) feedbackOverlay.SetActive(false);
        }

        void OnAnswer(int idx)
        {
            if (_answered || _set == null) return;
            _answered = true;
            _selectedIdx = idx;
            var q = _set.questions[_index];
            bool correct = q != null && idx == q.correctIndex;
            if (correct) _correctCount++;

            PaintFeedback(q, idx, timedOut: false);
            ShowOverlay(correct ? "OK" : "FAIL");
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        // Per GDD section 28: "thời gian 15s/câu. Không chọn thì coi như không
        // biết, bỏ qua". When the timer hits 0 we mark the question skipped,
        // but we still need to TELL the player what the right answer was —
        // otherwise the quiz becomes opaque punishment instead of a learning
        // moment. Reveal the correct answer in pale green, leave the rest
        // grey, and gate Continue behind the timer-runs-out state.
        void Skip()
        {
            _answered = true;
            _selectedIdx = -1;
            var q = _set != null && _index < _set.questions.Count ? _set.questions[_index] : null;
            PaintFeedback(q, idx: -1, timedOut: true);
            ShowOverlay("TIMEOUT");
            if (timerText != null) timerText.text = "0";
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        // Big symbol + label panel that briefly overlays the answer grid. Kept
        // visible through the entire post-answer state (until Continue is
        // clicked or auto-hidden by ShowQuestion). Designer keeps the existing
        // colored buttons as residual feedback after dismissal.
        void ShowOverlay(string kind)
        {
            if (feedbackOverlay == null) return;
            feedbackOverlay.SetActive(true);
            // Use ASCII-only glyphs ("V"/"X"/"!") so TMP doesn't have to extend
            // its dynamic font atlas on first quiz answer. Unicode checkmarks
            // (U+2713 / U+2717) previously triggered a runtime atlas upload mid-
            // frame which raced with the render command buffer → D3D12
            // assertion 'm_CmdState == kActive' and editor crash. Visual is
            // still clear via the color + label combo (green V + "Dung!" = pass).
            switch (kind)
            {
                case "OK":
                    if (feedbackIconText != null) { feedbackIconText.text = "V"; feedbackIconText.color = _feedbackPassColor; }
                    if (feedbackResultText != null) { feedbackResultText.text = "Dung!"; feedbackResultText.color = _feedbackPassColor; }
                    break;
                case "FAIL":
                    if (feedbackIconText != null) { feedbackIconText.text = "X"; feedbackIconText.color = _feedbackFailColor; }
                    if (feedbackResultText != null) { feedbackResultText.text = "Sai!"; feedbackResultText.color = _feedbackFailColor; }
                    break;
                default: // TIMEOUT
                    if (feedbackIconText != null) { feedbackIconText.text = "!"; feedbackIconText.color = _feedbackSkipColor; }
                    if (feedbackResultText != null) { feedbackResultText.text = "Het gio!"; feedbackResultText.color = _feedbackSkipColor; }
                    break;
            }
        }

        // Single paint path covers all three end-states: correct pick, wrong
        // pick, and timeout. Centralising avoids the bug where Skip() reset
        // _answered without touching button colors so the player saw a
        // "Continue" button appear over white buttons with no feedback at all.
        void PaintFeedback(QuizQuestionSO q, int idx, bool timedOut)
        {
            int correctIdx = q != null ? q.correctIndex : -1;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                answerButtons[i].interactable = false;
                var img = answerButtons[i].GetComponent<Image>();
                if (img == null) continue;

                if (timedOut)
                {
                    // No selection — show correct answer as the reveal, dim
                    // the wrong ones so it's unmissable which was right.
                    img.color = (i == correctIdx) ? _revealCorrectColor : _dimColor;
                }
                else if (i == idx)
                {
                    // Player's selected option gets the deep / saturated color
                    // so it's distinguishable from the auto-revealed correct.
                    img.color = (i == correctIdx) ? _selectedCorrectColor : _selectedWrongColor;
                    // Slight scale bump as additional "you picked this" cue
                    // — works even for colorblind users.
                    answerButtons[i].transform.localScale = new Vector3(1.06f, 1.06f, 1f);
                }
                else if (i == correctIdx)
                {
                    // Show what the right answer was, even when the player
                    // got it wrong — turns the moment into a learning beat
                    // instead of an opaque "you missed" prompt.
                    img.color = _revealCorrectColor;
                }
                else
                {
                    img.color = _dimColor;
                }
            }
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
