using UnityEngine;

namespace Luzart.NewBase
{
    public class SelectToggleGameObject : SelectToggle
    {
        public GameObject[] obSelect;
        public GameObject[] obUnSelect;

        public override void Select(bool isSelect)
        {
            base.Select(isSelect);
            int lengthSelect = obSelect.Length;
            int lengthUnSelect = obUnSelect.Length;
            int length = Mathf.Max(lengthSelect, lengthUnSelect);
            for (int i = 0; i < length; i++)
            {
                int index = i;
                if (index < lengthSelect)
                {
                    obSelect[index]?.SetActive(isSelect);
                }
                if (index < lengthUnSelect)
                {
                    obUnSelect[index]?.SetActive(!isSelect);
                }
            }
        }
    }
}
