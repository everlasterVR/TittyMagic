using UnityEngine;

namespace TittyMagic.Components
{
    public class RectTransformChange
    {
        private readonly RectTransform _rectTransform;
        private readonly Vector2 _originalPosition;
        private readonly Vector2 _newPosition;

        public RectTransformChange(RectTransform rectTransform, Vector2 offset)
        {
            _rectTransform = rectTransform;
            var position = rectTransform.anchoredPosition;
            _originalPosition = new Vector2(position.x, position.y);
            _newPosition = new Vector2(position.x + offset.x, position.y + offset.y);
            rectTransform.anchoredPosition = _newPosition;
        }

        public void Apply()
        {
            _rectTransform.anchoredPosition = _newPosition;
        }

        public void RestoreOriginal()
        {
            _rectTransform.anchoredPosition = _originalPosition;
        }
    }
}
