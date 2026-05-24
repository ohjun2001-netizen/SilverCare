using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace SilverCare.Common
{
    public class DesktopVRLookSimulator : MonoBehaviour
    {
        [SerializeField] float sensitivity = 0.18f;
        [SerializeField] float minPitch = -65f;
        [SerializeField] float maxPitch = 65f;

        float _yaw;
        float _pitch;
        bool _initialized;
        Transform _rightController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallOnLoad()
        {
            Install();
            SceneManager.sceneLoaded += (_, _) => Install();
        }

        static void Install()
        {
            if (XRSettings.isDeviceActive) return;

            var cam = Camera.main;
            if (cam == null) return;
            if (cam.GetComponent<DesktopVRLookSimulator>() != null) return;

            cam.gameObject.AddComponent<DesktopVRLookSimulator>();
        }

        void LateUpdate()
        {
            if (XRSettings.isDeviceActive || Mouse.current == null) return;

            if (!_initialized)
            {
                Vector3 euler = transform.rotation.eulerAngles;
                _yaw = euler.y;
                _pitch = NormalizeAngle(euler.x);
                _initialized = true;
            }

            if (!Mouse.current.rightButton.isPressed) return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            AlignPointerToView();
        }

        void AlignPointerToView()
        {
            if (_rightController == null)
            {
                var right = GameObject.Find("RightHand Controller");
                if (right != null) _rightController = right.transform;
            }

            if (_rightController == null) return;

            _rightController.position =
                transform.position +
                transform.right * 0.22f -
                transform.up * 0.18f +
                transform.forward * 0.20f;
            _rightController.rotation = transform.rotation;
        }

        static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
