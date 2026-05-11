using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Systems.Audio;
using TrainAI.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    // GDD: "UIDialogue: 1 InputField + button Send + ben tren la hoi thoai".
    // Chat AI duoc GAN THANG vao screen nay (user yeu cau).
    // Hien tai dung fallback responses tu NPCProfileSO. Sau co the swap qua Sentis ONNX
    // bang cach goi GameServices.Dialogue.GetReply() override.
    public class DialogueScreen : UIBase<DialogueData>
    {
        [Header("Refs")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button btnSend;
        [SerializeField] private Button btnClose;
        [SerializeField] private TextMeshProUGUI txtNpcName;
        [SerializeField] private Image imgPortrait;
        [SerializeField] private Transform bubbleContainer;
        [SerializeField] private GameObject bubblePrefabPlayer;
        [SerializeField] private GameObject bubblePrefabNpc;

        [Header("Chat behavior (GDD-defined)")]
        [Tooltip("Delay gia lap NPC dang nghi (ms).")]
        [SerializeField] private int npcThinkDelayMs = 400;

        private CancellationTokenSource _cts;
        private readonly List<GameObject> _bubbles = new List<GameObject>();

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnSend != null) btnSend.onClick.AddListener(OnSend);
            if (btnClose != null) btnClose.onClick.AddListener(OnClose);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(DialogueData data, CancellationToken ct)
        {
            ClearBubbles();
            if (data?.Npc != null)
            {
                if (txtNpcName != null) txtNpcName.text = data.Npc.displayName;
                if (imgPortrait != null && data.Npc.portrait != null) imgPortrait.sprite = data.Npc.portrait;
                string greet = data.Npc.greetingTemplate;
                if (GameServices.Player != null && !string.IsNullOrEmpty(greet))
                    greet = greet.Replace("{playerName}", GameServices.Player.Name);
                AddBubble(greet, isPlayer: false);
            }
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShownAsync(DialogueData data, CancellationToken ct)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnHiddenAsync(DialogueData data, UIHideReason reason, CancellationToken ct)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            data?.ResultTcs.TrySetResult(true);
            return UniTask.CompletedTask;
        }

        private void OnSend()
        {
            if (inputField == null) return;
            string text = inputField.text;
            inputField.text = "";
            if (string.IsNullOrWhiteSpace(text)) return;
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            AddBubble(text, isPlayer: true);
            HandleChatAsync(text).Forget();
        }

        private void OnClose()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            UIManager.Instance.HideAsync(this.Id).Forget();
        }

        // Chat AI inline o day. Don gian: delay + pick fallback response.
        // Wire Sentis ONNX intent classifier here later neu muon.
        private async UniTaskVoid HandleChatAsync(string playerText)
        {
            if (Data == null || Data.Npc == null) return;
            var ct = _cts != null ? _cts.Token : default;
            try
            {
                if (npcThinkDelayMs > 0)
                    await UniTask.Delay(npcThinkDelayMs, cancellationToken: ct);

                string reply = ResolveReply(playerText, Data.Npc);
                AddBubble(reply, isPlayer: false);
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e)
            {
                Debug.LogError($"[DialogueScreen] chat error: {e}");
            }
        }

        private string ResolveReply(string playerText, NPCProfileSO npc)
        {
            // Uu tien dung DialogueManager neu Quyen wire AI ngoai.
            var dm = GameServices.Dialogue;
            if (dm != null) return dm.GetReply(playerText, npc);

            // Fallback inline.
            if (npc.fallbackResponses == null || npc.fallbackResponses.Count == 0)
                return "...";
            int idx = Random.Range(0, npc.fallbackResponses.Count);
            return npc.fallbackResponses[idx];
        }

        private void AddBubble(string text, bool isPlayer)
        {
            if (bubbleContainer == null) return;
            GameObject prefab = isPlayer ? bubblePrefabPlayer : bubblePrefabNpc;
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, bubbleContainer);
            }
            else
            {
                // Fallback layout: TMP text child + background.
                go = new GameObject(isPlayer ? "Bubble_Player" : "Bubble_NPC");
                go.transform.SetParent(bubbleContainer, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(600, 60);
                var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
                le.minHeight = 60;
                le.preferredHeight = 60;
                var t = go.AddComponent<TextMeshProUGUI>();
                t.text = text;
                t.fontSize = 22;
                t.alignment = isPlayer ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                t.color = isPlayer ? new Color(0.2f, 0.6f, 1f) : Color.black;
            }
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
            _bubbles.Add(go);
        }

        private void ClearBubbles()
        {
            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null) Destroy(_bubbles[i]);
            }
            _bubbles.Clear();
        }
    }
}
