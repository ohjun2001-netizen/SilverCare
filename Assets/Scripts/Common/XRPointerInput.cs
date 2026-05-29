using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace SilverCare.Common
{
    public static class XRPointerInput
    {
        struct TimedHit
        {
            public RaycastHit hit;
            public float time;
        }

        static bool _wasTriggerPressed;
        static Transform _rightHand;
        static XRRayInteractor _rightHandRayInteractor;
        static ActionBasedController _rightHandController;
        static string _pointerScenePath;
        static int _selectionFrame = -1;
        static bool _selectionPressedThisFrame;
        static readonly Dictionary<int, TimedHit> RecentInteractorHits = new();

        public static bool TryGetSelectionHit(float maxDistance, out RaycastHit hit)
        {
            hit = default;
            UpdateInteractorHitCache();

            if (!WasSelectionPressedThisFrame())
                return false;

            if (TryGetInteractorHit(out hit))
                return true;

            if (TryGetControllerPointerHit(maxDistance, out hit))
                return true;

            if (TryGetMouseHit(maxDistance, out hit))
                return true;

            return false;
        }

        public static bool WasSelectionPressedThisFrame()
        {
            if (_selectionFrame == Time.frameCount)
                return _selectionPressedThisFrame;

            _selectionFrame = Time.frameCount;
            _selectionPressedThisFrame =
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                WasControllerActionPressedThisFrame() ||
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
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return false;

            var cam = Camera.main;
            if (cam == null)
                return false;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            return Physics.Raycast(ray, out hit, maxDistance);
        }

        static bool TryGetControllerPointerHit(float maxDistance, out RaycastHit hit)
        {
            hit = default;
            var pointer = GetRightHandPointer();
            if (pointer == null)
                return false;

            var ray = new Ray(pointer.position, pointer.forward);
            return Physics.Raycast(ray, out hit, maxDistance);
        }

        static bool WasControllerActionPressedThisFrame()
        {
            var controller = GetRightHandController();
            if (controller == null)
                return false;

            var selectAction = controller.selectAction.action;
            if (selectAction != null && selectAction.WasPressedThisFrame())
                return true;

            var uiPressAction = controller.uiPressAction.action;
            if (uiPressAction != null && uiPressAction.WasPressedThisFrame())
                return true;

            return false;
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
            var controller = GetRightHandController();
            if (controller != null)
            {
                var selectValue = controller.selectActionValue.action;
                if (selectValue != null && selectValue.ReadValue<float>() > 0.1f)
                    return true;

                var uiPressValue = controller.uiPressActionValue.action;
                if (uiPressValue != null && uiPressValue.ReadValue<float>() > 0.1f)
                    return true;
            }

            foreach (var device in InputSystem.devices)
            {
                if (!device.usages.Contains(CommonUsages.RightHand))
                    continue;

                var triggerButton = device.TryGetChildControl<ButtonControl>("triggerPressed");
                if (triggerButton != null && triggerButton.isPressed)
                    return true;

                var primaryButton = device.TryGetChildControl<ButtonControl>("primaryButton");
                if (primaryButton != null && primaryButton.isPressed)
                    return true;

                var trigger = device.TryGetChildControl<AxisControl>("trigger");
                if (trigger != null && trigger.ReadValue() > 0.1f)
                    return true;
            }

            return false;
        }

        static Transform GetRightHandPointer()
        {
            ResetSceneCacheIfNeeded();

            if (_rightHand == null || !_rightHand)
                _rightHand = FindRightHandPointer();

            return _rightHand;
        }

        static bool TryGetInteractorHit(out RaycastHit hit)
        {
            hit = default;
            var interactor = GetRightHandRayInteractor();
            if (interactor == null)
                return false;

            if (interactor.TryGetCurrent3DRaycastHit(out hit))
                return true;

            int id = interactor.GetInstanceID();
            if (RecentInteractorHits.TryGetValue(id, out TimedHit timedHit) &&
                Time.unscaledTime - timedHit.time <= 0.2f)
            {
                hit = timedHit.hit;
                return true;
            }

            return false;
        }

        static XRRayInteractor GetRightHandRayInteractor()
        {
            ResetSceneCacheIfNeeded();

            if (_rightHandRayInteractor == null || !_rightHandRayInteractor)
                _rightHandRayInteractor = FindRightHandRayInteractor();

            return _rightHandRayInteractor;
        }

        static ActionBasedController GetRightHandController()
        {
            ResetSceneCacheIfNeeded();

            if (_rightHandController == null || !_rightHandController)
                _rightHandController = FindRightHandController();

            return _rightHandController;
        }

        static void UpdateInteractorHitCache()
        {
            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null)
                    continue;

                if (!ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                    continue;

                RecentInteractorHits[ray.GetInstanceID()] = new TimedHit
                {
                    hit = hit,
                    time = Time.unscaledTime
                };
            }
        }

        static void ResetSceneCacheIfNeeded()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            if (_pointerScenePath == scenePath)
                return;

            _pointerScenePath = scenePath;
            _rightHand = null;
            _rightHandRayInteractor = null;
            _rightHandController = null;
            RecentInteractorHits.Clear();
        }

        static XRRayInteractor FindRightHandRayInteractor()
        {
            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null)
                    continue;

                string name = ray.name.ToLowerInvariant();
                if (name.Contains("right"))
                    return ray;
            }

            return null;
        }

        static ActionBasedController FindRightHandController()
        {
            foreach (var controller in Object.FindObjectsOfType<ActionBasedController>(true))
            {
                if (controller == null)
                    continue;

                string name = controller.name.ToLowerInvariant();
                if (name.Contains("right"))
                    return controller;
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
                if (go != null)
                    return go.transform;
            }

            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null)
                    continue;

                string name = ray.name.ToLowerInvariant();
                if (name.Contains("right"))
                    return ray.transform;
            }

            foreach (var controller in Object.FindObjectsOfType<ActionBasedController>(true))
            {
                if (controller == null)
                    continue;

                string name = controller.name.ToLowerInvariant();
                if (name.Contains("right"))
                    return controller.transform;
            }

            return null;
        }
    }
}
