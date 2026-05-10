using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Default provider khong dung Addressables - prefab duoc reference truc tiep
    /// trong UIConfig.AssetRef (drag-drop). Phu hop project khong can streaming asset
    /// hoac CDN, vi du game offline / demo / DATN.
    ///
    /// Ho tro:
    ///   - Reference counting (de match interface, debug leak).
    ///   - Request coalescing (nhieu ShowAsync cung UIId -> 1 task).
    ///
    /// Dac diem khac voi Addressable provider:
    ///   - Khong co async network load -> tra ve ngay neu prefab da binded.
    ///   - PreloadAsync / GetDownloadSize / PreloadByLabel la no-op.
    ///   - Release khong giai phong RAM (prefab giu boi UIRegistrySO),
    ///     chi remove khoi cache de tracking ref count consistent.
    /// </summary>
    public class DirectPrefabUIAssetProvider : IUIAssetProvider
    {
        private class CacheEntry
        {
            public GameObject Prefab;
            public int RefCount;
        }

        private readonly Dictionary<UIId, CacheEntry> cache = new Dictionary<UIId, CacheEntry>();

        public UniTask<GameObject> LoadAsync(UIConfig config, CancellationToken ct)
        {
            if (!config.IsValid)
                throw new System.InvalidOperationException($"[NinjaUI] UIConfig {config?.Id} is invalid.");

            if (config.AssetRef == null)
                throw new System.InvalidOperationException(
                    $"[NinjaUI] UIConfig.AssetRef cho {config.Id} chua duoc gan prefab. " +
                    "Mo UIRegistrySO va drag-drop prefab vao field AssetRef.");

            ct.ThrowIfCancellationRequested();

            if (!cache.TryGetValue(config.Id, out var entry))
            {
                entry = new CacheEntry { Prefab = config.AssetRef };
                cache[config.Id] = entry;
            }
            entry.RefCount++;

            return UniTask.FromResult(entry.Prefab);
        }

        public void Release(UIConfig config)
        {
            if (!cache.TryGetValue(config.Id, out var entry)) return;

            entry.RefCount--;
            if (entry.RefCount > 0) return;

            // Khong co Addressables.Release de goi - prefab giu boi UIRegistrySO,
            // chi cleanup cache entry de tracking consistent.
            if (config.CachePolicy == UICachePolicy.ReleaseOnClose ||
                config.CachePolicy == UICachePolicy.PoolOnClose)
            {
                cache.Remove(config.Id);
            }
        }

        public UniTask<long> GetDownloadSizeAsync(UIConfig config, CancellationToken ct)
        {
            // Direct prefab - khong co gi de download.
            return UniTask.FromResult(0L);
        }

        public UniTask PreloadAsync(UIConfig config, System.IProgress<float> progress, CancellationToken ct)
        {
            // Prefab da load san trong UIRegistrySO khi scene load.
            progress?.Report(1f);
            return UniTask.CompletedTask;
        }

        public UniTask PreloadByLabelAsync(string label, System.IProgress<float> progress, CancellationToken ct)
        {
            // No-op: khong co label-based grouping khi khong dung Addressables.
            // Neu sau nay can boot-time preload theo label, them field vao UIConfig
            // (vd string[] BootLabels) va loop registry tu thay Addressables.
            progress?.Report(1f);
            return UniTask.CompletedTask;
        }

        /// <summary>Force clear cache, dung khi unload scene / game reset.</summary>
        public void ReleaseAll()
        {
            cache.Clear();
        }
    }
}
