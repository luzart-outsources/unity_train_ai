using Luzart;

namespace TrainAI.UI.Components
{
    // Cast helper - UIId la enum int, them entry game-specific qua const.
    // UIRegistrySO entry phai map dung gia tri (vd Quiz = 2010) tu inspector.
    public static class UIIdGame
    {
        public const UIId OpeningCutscene = (UIId)1011;
        public const UIId Ending = (UIId)1012;
        public const UIId KickedOut = (UIId)6;
        public const UIId Quiz = (UIId)2010;
        public const UIId Confirm = (UIId)2011;
        public const UIId Dialogue = (UIId)2012;
    }
}
