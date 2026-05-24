using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Collections.Generic;

namespace SilverCare.Common
{
    public static class XRUIUtility
    {
        static bool _sceneHookRegistered;
        static readonly Dictionary<string, (Vector3 position, Vector3 forward)> SceneViewAnchors = new();

        public static void ConfigureWorldCanvas(GameObject canvasObject, Canvas canvas)
        {
            if (canvasObject == null || canvas == null) return;

            EnsureEventSystem();
            ConfigureWorldCanvasComponents(canvasObject, canvas);
            EnsureControllerUIInput();
            EnsureRuntimeHelpers();
        }

        public static void PlaceCanvasInFront(Canvas canvas, float distance = 2.1f, float height = 1.5f)
        {
            if (canvas == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            canvas.worldCamera = cam;
            GetSceneViewAnchor(cam, out Vector3 anchorPosition, out Vector3 forward);

            var rt = canvas.GetComponent<RectTransform>();
            Vector3 pos = anchorPosition + forward * distance;
            pos.y = height;
            rt.position = pos;
            rt.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public static void PlaceCanvasFacingCamera(Canvas canvas, float distance = 2.1f, float verticalOffset = 0.08f)
        {
            if (canvas == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            canvas.worldCamera = cam;
            GetSceneViewAnchor(cam, out Vector3 anchorPosition, out Vector3 forward);

            var rt = canvas.GetComponent<RectTransform>();
            Vector3 pos = anchorPosition + forward * distance + Vector3.up * verticalOffset;
            rt.position = pos;

            Vector3 flatForward = Vector3.ProjectOnPlane(pos - anchorPosition, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f) flatForward = forward;
            rt.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        public static void GetSceneViewAnchor(Camera cam, out Vector3 position, out Vector3 forward)
        {
            if (cam == null)
            {
                position = new Vector3(0f, 1.6f, 0f);
                forward = Vector3.forward;
                return;
            }

            EnsureSceneHook();

            string scenePath = SceneManager.GetActiveScene().path;
            if (!SceneViewAnchors.TryGetValue(scenePath, out var anchor))
            {
                Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (flatForward.sqrMagnitude < 0.001f)
                    flatForward = Vector3.forward;

                anchor = (cam.transform.position, flatForward);
                SceneViewAnchors[scenePath] = anchor;
            }

            position = anchor.position;
            forward = anchor.forward;
        }

        public static void EnsureEventSystem()
        {
            EnsureSceneHook();
            EnsureRuntimeHelpers();

            var eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<XRUIInputModule>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            if (eventSystem.GetComponent<XRWorldCanvasClickFallback>() == null)
                eventSystem.gameObject.AddComponent<XRWorldCanvasClickFallback>();

            ConfigureSceneWorldCanvases();
        }

        static void EnsureControllerUIInput()
        {
            foreach (var ray in Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                ray.enableUIInteraction = true;
            }

            foreach (var controller in Object.FindObjectsOfType<ActionBasedController>(true))
            {
                if (controller.selectAction.action != null && controller.selectAction.action.bindings.Count > 0 &&
                    controller.uiPressAction.action != null && controller.uiPressAction.action.bindings.Count > 0)
                {
                    continue;
                }

                bool rightHand = controller.name.ToLowerInvariant().Contains("right");
                string hand = rightHand ? "RightHand" : "LeftHand";

                var select = new InputAction($"{hand} Select", InputActionType.Button);
                select.AddBinding($"<XRController>{{{hand}}}/triggerPressed");
                select.AddBinding($"<XRController>{{{hand}}}/gripPressed");
                select.Enable();

                var selectValue = new InputAction($"{hand} Select Value", InputActionType.Value, expectedControlType: "Axis");
                selectValue.AddBinding($"<XRController>{{{hand}}}/trigger");
                selectValue.Enable();

                controller.selectAction = new InputActionProperty(select);
                controller.selectActionValue = new InputActionProperty(selectValue);
                controller.uiPressAction = new InputActionProperty(select);
                controller.uiPressActionValue = new InputActionProperty(selectValue);
            }
        }

        static void EnsureSceneHook()
        {
            if (_sceneHookRegistered) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneHookRegistered = true;
        }

        static void EnsureRuntimeHelpers()
        {
            if (Object.FindObjectOfType<XRLineVisualFixer>(true) != null)
                return;

            var go = new GameObject("XR Runtime Helpers");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<XRLineVisualFixer>();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneViewAnchors.Remove(scene.path);
            ConfigureSceneWorldCanvases();
            EnsureControllerUIInput();
        }

        static void ConfigureSceneWorldCanvases()
        {
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) continue;
                ConfigureWorldCanvasComponents(canvas.gameObject, canvas);
            }
        }

        static void ConfigureWorldCanvasComponents(GameObject canvasObject, Canvas canvas)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
                canvasObject.AddComponent<GraphicRaycaster>();

            if (canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }
}
