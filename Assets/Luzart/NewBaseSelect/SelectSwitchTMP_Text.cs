namespace Luzart.NewBase
{
    public class SelectSwitchTMP_Text : SelectSwitch
    {
        public TMPro.TMP_Text tmpText;
        public string[] options;
        public override void Select(int value)
        {
            if (tmpText != null && options != null && value >= 0 && value < options.Length)
            {
                tmpText.text = options[value];
            }
        }
    }
}
