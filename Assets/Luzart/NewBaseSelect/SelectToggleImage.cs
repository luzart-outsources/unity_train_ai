using UnityEngine;
using UnityEngine.UI;

namespace Luzart.NewBase
{
    public class SelectToggleImage : SelectToggle
    {
        public Image[] imSelect;
        public Sprite[] spSelect;
        public Sprite[] spUnSelect;

        public override void Select(bool isSelect)
        {
            if (imSelect != null)
            {
                int length = imSelect.Length;
                for (int i = 0; i < length; i++)
                {
                    if (isSelect)
                    {
                        imSelect[i].sprite = spSelect[i];
                    }
                    else
                    {
                        imSelect[i].sprite = spUnSelect[i];
                    }

                }
            }
        }
    }
}
