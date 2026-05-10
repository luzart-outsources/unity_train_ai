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
            FetchReplyAsync(text).Forget();
        }

        private void OnClose()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            UIManager.Instance.HideAsync(this.Id).Forget();
        }

        private async UniTaskVoid FetchReplyAsync(string playerText)
        {
            if (Data == null || Data.Npc == null) return;
            var ct = _cts != null ? _cts.Token : default;
            try
            {
                var dialogue = GameServices.Dialogue;
                if (dialogue == null) return;
                string reply = await dialogue.GetReplyAsync(playerText, Data.Npc, ct);
                AddBubble(reply, isPlayer: false);
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e)
            {
                Debug.LogError($"[DialogueScreen] reply error: {e}");
            }
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
                // Fallback: just create a TMP text child.
                go = new GameObject("Bubble");
                go.transform.SetParent(bubbleContainer, false);
                var t = go.AddComponent<TextMeshProUGUI>();
                t.text = text;
                t.fontSize = 18;
                t.color = isPlayer ? new Color(0.2f, 0.6f, 1f) : Color.white;
            }
            // If prefab has TMP child, set text.
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
