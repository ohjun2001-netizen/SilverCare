using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.Common
{
    public class XRWorldCanvasClickFallback : MonoBehaviour
    {
        const float MaxDistance = 12f;
        Button _hoveredButton;

        void Update()
        {
            Button bestButton = FindHoveredButton();
            UpdateHoverVisual(bestButton);

            if (!XRPointerInput.WasSelectionPressedThisFrame())
                return;

            if (bestButton != null)
                bestButton.onClick.Invoke();
        }

        Button FindHoveredButton()
        {
            if (!XRPointerInput.TryGetPointerRay(out var ray))
                return null;

            Button bestButton = null;
            float bestDistance = float.MaxValue;

            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                var canvasRt = canvas.GetComponent<RectTransform>();
                var plane = new Plane(canvas.transform.forward, canvas.transform.position);
                if (!plane.Raycast(ray, out float distance))
                    continue;

                if (distance < 0f || distance > MaxDistance || distance >= bestDistance)
                    continue;

                Vector3 worldPoint = ray.GetPoint(distance);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPoint);
                if (!RectTransformUtility.RectangleContainsScreenPoint(canvasRt, screenPoint, canvas.worldCamera))
                    continue;

                var buttons = canvas.GetComponentsInChildren<Button>(false);
                foreach (var button in buttons)
                {
                    if (!button.IsActive() || !button.IsInteractable())
                        continue;

                    var buttonRt = button.GetComponent<RectTransform>();
                    if (!RectTransformUtility.RectangleContainsScreenPoint(buttonRt, screenPoint, canvas.worldCamera))
                        continue;

                    bestButton = button;
                    bestDistance = distance;
                    break;
                }
            }

            return bestButton;
        }

        void UpdateHoverVisual(Button bestButton)
        {
            if (_hoveredButton == bestButton)
                return;

            ClearHoverVisual();

            if (bestButton == null)
                return;

            _hoveredButton = bestButton;
            var feedback = bestButton.GetComponent<XRButtonHoverFeedback>();
            if (feedback == null)
                feedback = bestButton.gameObject.AddComponent<XRButtonHoverFeedback>();

            feedback.SetHovered(true);
        }

        void ClearHoverVisual()
        {
            if (_hoveredButton == null)
                return;

            if (_hoveredButton)
            {
                var feedback = _hoveredButton.GetComponent<XRButtonHoverFeedback>();
                if (feedback != null)
                {
                    feedback.SetHovered(false);
                    _hoveredButton = null;
                }
            }

            _hoveredButton = null;
        }

        void OnDisable()
        {
            ClearHoverVisual();
        }

        void OnDestroy()
        {
            ClearHoverVisual();
        }
    }
}
