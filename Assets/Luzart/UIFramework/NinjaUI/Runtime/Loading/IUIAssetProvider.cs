using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Abstraction cho asset loader. UIManager khong biet provider la direct prefab,
    /// AssetBundle hay CDN. Cho phep:
    ///   - Test: mock provider khong can asset that.
    ///   - Thay the: sau nay chuyen sang AssetBundle / Addressable / CDN custom ma khong dung UIManager.
    /// Default impl: DirectPrefabUIAssetProvider (prefab drag-drop trong UIConfig).
    /// </summary>
    public interface IUIAssetProvider
    {
        /// <summary>
        /// Load prefab cho 1 UIConfig. Nhieu lan goi cung config -> coalesce + ref count.
        /// Return GameObject prefab (chua instantiate).
        /// </summary>
        UniTask<GameObject> LoadAsync(UIConfig config, CancellationToken ct);

        /// <summary>
        /// Giam ref count. Khi ve 0 + policy cho phep release, asset duoc giai phong
        /// (chi co tac dung voi provider co stream/CDN; DirectPrefab chi clear cache entry).
        /// </summary>
        void Release(UIConfig config);

        /// <summary>
        /// Download size (bytes) cua dependency, de hien progress bar preload.
        /// DirectPrefab tra 0 (khong co gi de download).
        /// </summary>
        UniTask<long> GetDownloadSizeAsync(UIConfig config, CancellationToken ct);

        /// <summary>
        /// Download truoc cac asset can thiet (khong giu instance).
        /// DirectPrefab no-op.
        /// </summary>
        UniTask PreloadAsync(UIConfig config, System.IProgress<float> progress, CancellationToken ct);

        /// <summary>
        /// Preload nhom asset theo label/group key. Dung cho boot group khi co stream provider.
        /// DirectPrefab no-op.
        /// </summary>
        UniTask PreloadByLabelAsync(string label, System.IProgress<float> progress, CancellationToken ct);
    }
}
