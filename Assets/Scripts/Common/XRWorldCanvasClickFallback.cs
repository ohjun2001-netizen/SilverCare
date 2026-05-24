using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.Common
{
    public class XRWorldCanvasClickFallback : MonoBehaviour
    {
        const float MaxDistance = 12f;

        void Update()
        {
            if (!XRPointerInput.WasSelectionPressedThisFrame()) return;
            if (!XRPointerInput.TryGetPointerRay(out var ray)) return;

            Button bestButton = null;
            float bestDistance = float.MaxValue;

            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace) continue;

                var canvasRt = canvas.GetComponent<RectTransform>();
                var plane = new Plane(canvas.transform.forward, canvas.transform.position);
                if (!plane.Raycast(ray, out float distance)) continue;
                if (distance < 0f || distance > MaxDistance || distance >= bestDistance) continue;

                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 screenPoint = Camera.main != null
                    ? Camera.main.WorldToScreenPoint(worldPoint)
                    : worldPoint;

                if (!RectTransformUtility.RectangleContainsScreenPoint(canvasRt, screenPoint, canvas.worldCamera))
                    continue;

                var buttons = canvas.GetComponentsInChildren<Button>(false);
                foreach (var button in buttons)
                {
                    if (!button.IsActive() || !button.IsInteractable()) continue;

                    var buttonRt = button.GetComponent<RectTransform>();
                    if (!RectTransformUtility.RectangleContainsScreenPoint(buttonRt, screenPoint, canvas.worldCamera))
                        continue;

                    bestButton = button;
                    bestDistance = distance;
                    break;
                }
            }

            if (bestButton != null)
                bestButton.onClick.Invoke();
        }
    }
}
