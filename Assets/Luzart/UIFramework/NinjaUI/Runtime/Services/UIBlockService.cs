using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Service block input toàn cục với reference counting + lý do (debug).
    /// Học từ Lumber IUIService.PushBlock(reason) → IDisposable pattern.
    ///
    /// Usage:
    /// <code>
    /// using (blockService.PushBlock("loading_character"))
    /// {
    ///     await LoadCharacterAsync();
    /// } // tự động release khi exit scope
    /// </code>
    ///
    /// Dùng cho:
    ///   - Chờ server response, không cho user click loạn.
    ///   - Loading scene/asset quan trọng.
    ///   - Animation cinematic không nên bị spam.
    /// </summary>
    public class UIBlockService : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("GraphicRaycaster hoặc RectTransform full-screen sẽ được SetActive khi có block.")]
        [SerializeField] private GameObject blockerOverlay;

        [Tooltip("Log verbose mỗi lần push/release.")]
        [SerializeField] private bool logVerbose = false;

        private readonly Dictionary<BlockToken, string> activeBlocks = new Dictionary<BlockToken, string>();

        public int ActiveBlockCount => activeBlocks.Count;
        public bool IsBlocked => activeBlocks.Count > 0;

        public event Action<bool> OnBlockStateChanged;

        /// <summary>Push 1 block. Trả IDisposable, dispose để release.</summary>
        public IDisposable PushBlock(string reason = null)
        {
            var token = new BlockToken(this);
            activeBlocks[token] = reason ?? "unknown";
            UpdateOverlay();

            if (logVerbose)
                Debug.Log($"[NinjaUI][Block] PUSH '{reason}' → count={activeBlocks.Count}");

            return token;
        }

        private void Release(BlockToken token)
        {
            if (!activeBlocks.TryGetValue(token, out var reason)) return;
            activeBlocks.Remove(token);
            UpdateOverlay();

            if (logVerbose)
                Debug.Log($"[NinjaUI][Block] RELEASE '{reason}' → count={activeBlocks.Count}");
        }

        /// <summary>Force clear tất cả block (dùng khi scene reset, error recovery).</summary>
        public void ForceClearAll()
        {
            if (activeBlocks.Count == 0) return;
            var count = activeBlocks.Count;
            activeBlocks.Clear();
            UpdateOverlay();
            Debug.LogWarning($"[NinjaUI][Block] Force cleared {count} blocks.");
        }

        /// <summary>List reasons đang block, dùng cho debug panel.</summary>
        public IEnumerable<string> GetActiveReasons()
        {
            foreach (var kv in activeBlocks) yield return kv.Value;
        }

        private void UpdateOverlay()
        {
            bool wasBlocked = blockerOverlay != null && blockerOverlay.activeSelf;
            bool shouldBlock = activeBlocks.Count > 0;

            if (blockerOverlay != null)
                blockerOverlay.SetActive(shouldBlock);

            if (wasBlocked != shouldBlock)
                OnBlockStateChanged?.Invoke(shouldBlock);
        }

        private class BlockToken : IDisposable
        {
            private UIBlockService owner;
            public BlockToken(UIBlockService owner) { this.owner = owner; }

            public void Dispose()
            {
                if (owner == null) return;
                owner.Release(this);
                owner = null;
            }
        }
    }
}
