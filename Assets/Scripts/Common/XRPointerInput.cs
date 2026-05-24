using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

namespace SilverCare.Common
{
    public static class XRPointerInput
    {
        static bool _wasTriggerPressed;
        static Transform _rightHand;
        static XRRayInteractor _rightHandRayInteractor;
        static string _pointerScenePath;
        static int _selectionFrame = -1;
        static bool _selectionPressedThisFrame;

        public static bool TryGetSelectionHit(float maxDistance, out RaycastHit hit)
        {
            hit = default;
            if (!WasSelectionPressedThisFrame()) return false;

            // 1) VR 레이저(오른손 Ray Interactor): 레이저가 가리키는 지점을 최우선으로 사용
            if (TryGetInteractorHit(out hit)) return true;

            // 2) Ray Interactor 히트가 없으면 오른손 컨트롤러 포인터에서 직접 레이캐스트
            if (TryGetControllerPointerHit(maxDistance, out hit)) return true;

            // 3) 데스크톱 폴백: 레이저/컨트롤러가 없을 때만 마우스 위치 사용
            if (TryGetMouseHit(maxDistance, out hit)) return true;

            return false;
        }

        public static bool WasSelectionPressedThisFrame()
        {
            if (_selectionFrame == Time.frameCount) return _selectionPressedThisFrame;

            _selectionFrame = Time.frameCount;
            _selectionPressedThisFrame =
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                WasRightTriggerPressedThisFrame();
            return _selectionPressedThisFrame;
        }

        public static bool TryGetPointerRay(out Ray ray)
        {
            var pointer = GetRightHandPointer();
            if (pointer != null)
            {
                ray = new Ray(pointer.position, pointer.forward);
                return true;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                ray = new Ray(cam.transform.position, cam.transform.forward);
                return true;
            }

            ray = default;
            return false;
        }

        static bool TryGetMouseHit(float maxDistance, out RaycastHit hit)
        {
            hit = default;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;

            var cam = Camera.main;
            if (cam == null) return false;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            return Physics.Raycast(ray, out hit, maxDistance);
        }

        // 오른손 컨트롤러 포인터(레이저 기준)에서 직접 레이캐스트. 카메라 폴백은 쓰지 않는다.
        static bool TryGetControllerPointerHit(float maxDistance, out RaycastHit hit)
        {
            hit = default;
            var pointer = GetRightHandPointer();
            if (pointer == null) return false;

            var ray = new Ray(pointer.position, pointer.forward);
            return Physics.Raycast(ray, out hit, maxDistance);
        }

        static bool WasRightTriggerPressedThisFrame()
        {
            bool pressed = IsRightTriggerPressed();
            bool justPressed = pressed && !_wasTriggerPressed;
            _wasTriggerPressed = pressed;
            return justPressed;
        }

        static bool IsRightTriggerPressed()
        {
            foreach (var device in InputSystem.devices)
            {
                if (!device.usages.Contains(CommonUsages.RightHand)) continue;

                var triggerButton = device.TryGetChildControl<ButtonControl>("triggerPressed");
                if (triggerButton != null && triggerButton.isPressed) return true;

                var trigger = device.TryGetChildControl<AxisControl>("trigger");
                if (trigger != null && trigger.ReadValue() > 0.55f) return true;
            }

            return false;
        }

        static Transform GetRightHandPointer()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            if (_pointerScenePath != scenePath)
            {
                _pointerScenePath = scenePath;
                _rightHand = null;
                _rightHandRayInteractor = null;
            }

            if (_rightHand == null || !_rightHand)
                _rightHand = FindRightHandPointer();

            if (_rightHand != null) return _rightHand;
            return null;
        }

        static bool TryGetInteractorHit(out RaycastHit hit)
        {
            hit = default;
            var interactor = GetRightHandRayInteractor();
            if (interactor == null) return false;
            return interactor.TryGetCurrent3DRaycastHit(out hit);
        }

        static XRRayInteractor GetRightHandRayInteractor()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            if (_pointerScenePath != scenePath)
            {
                _pointerScenePath = scenePath;
                _rightHand = null;
                _rightHandRayInteractor = null;
            }

            if (_rightHandRayInteractor == null || !_rightHandRayInteractor)
                _rightHandRayInteractor = FindRightHandRayInteractor();

            return _rightHandRayInteractor;
        }

        static XRRayInteractor FindRightHandRayInteractor()
        {
            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null) continue;
                string name = ray.name.ToLowerInvariant();
                if (name.Contains("right")) return ray;
            }

            return null;
        }

        static Transform FindRightHandPointer()
        {
            string[] preferredNames =
            {
                "RightHand Controller",
                "Right Hand Controller",
                "Right Controller",
                "RightHand",
                "Right Hand"
            };

            foreach (string name in preferredNames)
            {
                var go = GameObject.Find(name);
                if (go != null) return go.transform;
            }

            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null) continue;
                string name = ray.name.ToLowerInvariant();
                if (name.Contains("right")) return ray.transform;
            }

            foreach (var controller in Object.FindObjectsOfType<ActionBasedController>(true))
            {
                if (controller == null) continue;
                string name = controller.name.ToLowerInvariant();
                if (name.Contains("right")) return controller.transform;
            }

            return null;
        }
    }
}
