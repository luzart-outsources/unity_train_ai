using System;
using Cysharp.Threading.Tasks;
using TMPro;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIDialogueController : UIScreenBase
    {
        [SerializeField] TMP_Text npcName;
        [SerializeField] TMP_Text history;
        [SerializeField] TMP_InputField input;
        [SerializeField] Button sendButton;
        [SerializeField] Button closeButton;

        Func<string, UniTask<string>> _replyFn;
        NPCSO _npc;
        UniTaskCompletionSource _tcs;

        protected override void Awake()
        {
            base.Awake();
            if (sendButton != null) sendButton.onClick.AddListener(OnSend);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        public UniTask ShowAsync(NPCSO npc, Func<string, UniTask<string>> replyFn)
        {
            _npc = npc;
            _replyFn = replyFn;
            _tcs = new UniTaskCompletionSource();
            if (npcName != null) npcName.text = npc != null ? npc.displayName : "";
            if (history != null) history.text = npc != null ? $"[{npc.displayName}] Chao dong chi, co chuyen gi vay?" : "";
            if (input != null) input.text = "";
            Show();
            return _tcs.Task;
        }

        async void OnSend()
        {
            if (_replyFn == null || input == null) return;
            string text = input.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (history != null) history.text += $"\n[Ban] {text}";
            input.text = "";
            string reply = await _replyFn(text);
            if (history != null) history.text += $"\n[{(_npc != null ? _npc.displayName : "NPC")}] {reply}";
        }

        void OnClose() { _tcs?.TrySetResult(); Hide(); }
    }
}
