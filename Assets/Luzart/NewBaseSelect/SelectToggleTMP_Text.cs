namespace Luzart.NewBase
{
    public class SelectToggleTMP_Text : SelectToggle
    {
        public TMPro.TMP_Text tmpText;
        public string textSelect;
        public string textUnSelect;
        public override void Select(bool value)
        {
            base.Select(value);
            if (tmpText != null)
            {
                tmpText.text = value ? textSelect : textUnSelect;
            }
        }
    }
}
