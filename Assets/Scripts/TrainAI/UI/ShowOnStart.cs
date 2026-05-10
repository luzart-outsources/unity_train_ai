using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;

namespace TrainAI.UI
{
    // Helper component dat trong scene Title - tu show 1 UIId khi Start.
    public class ShowOnStart : MonoBehaviour
    {
        [SerializeField] private int uiIdInt;       // serialize int de Editor wire don gian.

        public int UIIdInt { get => uiIdInt; set => uiIdInt = value; }

        private async void Start()
        {
            // Cho UIManager init xong (1 frame).
            await UniTask.NextFrame();
            if (UIManager.Instance == null) return;
            try
            {
                await UIManager.Instance.ShowAsync((UIId)uiIdInt);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShowOnStart] Failed to show UIId={uiIdInt}: {e.Message}");
            }
        }
    }
}
