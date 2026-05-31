using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilverCare.Common
{
    [RequireComponent(typeof(Button))]
    public class XRButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        const float HoverScale = 1.16f;

        Button _button;
        Vector3 _baseScale;
        Color _baseColor;
        bool _hasBaseColor;
        bool _hovered;

        void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
            if (_button != null && _button.targetGraphic != null)
            {
                _baseColor = _button.targetGraphic.color;
                _hasBaseColor = true;
            }
        }

        public void SetHovered(bool hovered)
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_hovered == hovered)
                return;

            _hovered = hovered;
            ApplyVisual();
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);

        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        public void OnSelect(BaseEventData eventData) => SetHovered(true);

        public void OnDeselect(BaseEventData eventData) => SetHovered(false);

        void ApplyVisual()
        {
            if (_button == null)
                return;

            transform.localScale = _hovered ? _baseScale * HoverScale : _baseScale;

            if (_button.targetGraphic == null || !_hasBaseColor)
                return;

            if (_hovered)
            {
                Color hoverColor = _button.colors.highlightedColor;
                if (hoverColor.a <= 0.001f)
                    hoverColor = Color.Lerp(_baseColor, Color.white, 0.2f);

                hoverColor.a = Mathf.Max(hoverColor.a, _baseColor.a);
                _button.targetGraphic.color = hoverColor;
            }
            else
            {
                _button.targetGraphic.color = _baseColor;
            }
        }

        void OnDisable()
        {
            _hovered = false;
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
            {
                transform.localScale = _baseScale;
                if (_button.targetGraphic != null && _hasBaseColor)
                    _button.targetGraphic.color = _baseColor;
            }
        }
    }
}
