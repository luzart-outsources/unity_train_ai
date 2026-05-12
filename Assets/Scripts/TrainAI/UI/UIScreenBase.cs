using UnityEngine;

namespace TrainAI.UI
{
    public abstract class UIScreenBase : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            Hide();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        public virtual void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }
    }
}
