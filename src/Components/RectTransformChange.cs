using UnityEngine;

namespace TittyMagic.Components
{
    public class RectTransformChange
    {
        private readonly RectTransform _rectTransform;
        private readonly Vector2 _originalPosition;
        private readonly Vector2 _newPosition;

        public RectTransformChange(RectTransform rectTransform, Vector2 originalPosition, Vector2 offset)
        {
            _rectTransform = rectTransform;
            _originalPosition = originalPosition;
            _newPosition = new Vector2(_originalPosition.x + offset.x, _originalPosition.y + offset.y);
            rectTransform.anchoredPosition = _newPosition;
        }

        public void Apply() =>
            _rectTransform.anchoredPosition = _newPosition;

        public void RestoreOriginal() =>
            _rectTransform.anchoredPosition = _originalPosition;
    }
}
