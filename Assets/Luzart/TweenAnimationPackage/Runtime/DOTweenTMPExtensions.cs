using TMPro;

namespace DG.Tweening
{
    /// <summary>
    /// DOTween extensions cho TextMeshPro. Thay thế DOTweenModuleTMP của DOTween Pro
    /// bằng cách tween <see cref="TMP_Text.maxVisibleCharacters"/> — reveal chữ theo
    /// kiểu typewriter mà vẫn giữ rich-text tags và tránh allocate string mỗi frame.
    /// </summary>
    public static class DOTweenTMPExtensions
    {
        /// <summary>
        /// Set full <paramref name="endValue"/> ngay để TMP layout (bao gồm rich-text tags),
        /// rồi tween <c>maxVisibleCharacters</c> từ 0 đến tổng số ký tự hiển thị trong
        /// khoảng <paramref name="duration"/> giây.
        /// </summary>
        public static Tweener DOText(this TMP_Text target, string endValue, float duration)
        {
            if (target == null) return null;
            if (endValue == null) endValue = string.Empty;

            target.text = endValue;
            target.ForceMeshUpdate();
            int total = target.textInfo.characterCount;
            target.maxVisibleCharacters = 0;

            var tween = DOTween.To(
                () => target.maxVisibleCharacters,
                x => target.maxVisibleCharacters = x,
                total,
                duration);
            tween.SetTarget(target);
            return tween;
        }
    }
}
