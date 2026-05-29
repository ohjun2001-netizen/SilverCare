// Assets/Scripts/Golf/VRPutter.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace SilverCare.Golf
{
    public class VRPutter : MonoBehaviour
    {
        [Header("Hit")]
        [SerializeField] float minHitSpeed = 0.4f;
        [SerializeField] float maxHitSpeed = 10f;
        [SerializeField] float forceMultiplier = 2.0f;
        [SerializeField] float hitCooldown = 0.4f;
        [Tooltip("Vertical launch angle in degrees")]
        [SerializeField] float loftDegrees = 20f;
        [Tooltip("How much the club face direction overrides swing direction (0=swing only, 1=face only)")]
        [SerializeField] [Range(0f, 1f)] float faceVsSwingBlend = 0.1f; // face 10%, swing velocity 80%

        [Header("Shape")]
        [SerializeField] float shaftLength = 1.2f;
        [SerializeField] float headForwardOffset = 0f;

        [Header("Feedback")]
        [SerializeField] float hapticAmp = 0.9f;
        [SerializeField] float hapticDur = 0.18f;

        Transform _rightController;
        ActionBasedController _rightAction;
        GameObject _root;
        Transform _head;
        Vector3 _prevHeadWorldPos;
        Vector3 _velocity;
        bool _hasPrev;
        float _lastHitTime = -999f;
        float _enabledAtTime;

        public Vector3 HeadVelocity => _velocity;
        public float MinHitSpeed => minHitSpeed;
        public float MaxHitSpeed => maxHitSpeed;
        public float ForceMultiplier => forceMultiplier;
        public float LoftRadians => loftDegrees * Mathf.Deg2Rad;
        public float FaceVsSwingBlend => faceVsSwingBlend;

        void Start()
        {
            FindController();
        }

        void OnDisable()
        {
            _hasPrev = false;
            if (_root != null) _root.SetActive(false);
        }

        void OnEnable()
        {
            _enabledAtTime = Time.time;
            _hasPrev = false;
            if (_root != null) _root.SetActive(true);
        }

        void Update()
        {
            if (_rightController == null)
            {
                FindController();
                if (_rightController == null)
                {
                    if (_root != null) _root.SetActive(false);
                    return;
                }
            }

            if (_root == null)
                BuildVisual();

            if (_root != null && !_root.activeSelf)
                _root.SetActive(true);

            PoseRoot();
            UpdateVelocity();
        }

        void PoseRoot()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f)
                fwd = Vector3.forward;
            fwd.Normalize();

            bool xrActive = XRSettings.isDeviceActive;
            if (!xrActive && cam != null)
            {
                Vector3 right = cam.transform.right;
                right.y = 0f;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;
                right.Normalize();
                _root.transform.position = cam.transform.position + right * 0.30f + fwd * 0.45f + Vector3.down * 0.40f;
                _root.transform.rotation = Quaternion.LookRotation(Vector3.up, -fwd);
            }
            else
            {
                _root.transform.position = _rightController.position;
                Vector3 ctrlFwd = _rightController.forward;
                Vector3 ctrlUp = _rightController.up;
                if (ctrlFwd.sqrMagnitude < 0.001f) ctrlFwd = Vector3.forward;
                if (ctrlUp.sqrMagnitude < 0.001f) ctrlUp = Vector3.up;
                _root.transform.rotation = Quaternion.LookRotation(ctrlUp, -ctrlFwd);
            }
        }

        void UpdateVelocity()
        {
            if (_head == null)
                return;

            if (!_hasPrev)
            {
                _prevHeadWorldPos = _head.position;
                _hasPrev = true;
                return;
            }

            Vector3 current = _head.position;
            _velocity = (current - _prevHeadWorldPos) / Mathf.Max(Time.deltaTime, 0.001f);
            _prevHeadWorldPos = current;
        }

        void FindController()
        {
            if (_rightController != null)
                return;

            var go = GameObject.Find("RightHand Controller");
            if (go != null)
            {
                _rightController = go.transform;
                _rightAction = go.GetComponent<ActionBasedController>();
                return;
            }

            foreach (var t in FindObjectsOfType<Transform>(true))
            {
                if (t.name == "RightHand Controller")
                {
                    _rightController = t;
                    _rightAction = t.GetComponent<ActionBasedController>();
                    return;
                }
            }
        }

        void BuildVisual()
        {
            _root = new GameObject("VRPutterRoot");
            _root.transform.SetParent(transform, false);

            float gripLen = 0.13f;
            float gripCenterY = -gripLen * 0.5f - 0.02f;
            CreatePart("Grip", PrimitiveType.Cylinder,
                new Vector3(0f, gripCenterY, 0f),
                Quaternion.identity,
                new Vector3(0.045f, gripLen * 0.5f, 0.045f),
                new Color(0.08f, 0.08f, 0.09f), 0.18f, _root.transform);

            float shaftCenterY = -(gripLen + 0.02f + shaftLength * 0.5f);
            CreatePart("Shaft", PrimitiveType.Cylinder,
                new Vector3(0f, shaftCenterY, 0f),
                Quaternion.identity,
                new Vector3(0.022f, shaftLength * 0.5f, 0.022f),
                new Color(0.82f, 0.84f, 0.88f), 0.55f, _root.transform);

            var headRoot = new GameObject("PutterHead");
            headRoot.transform.SetParent(_root.transform, false);
            headRoot.transform.localPosition = new Vector3(0f, -(gripLen + 0.02f + shaftLength), headForwardOffset);
            headRoot.transform.localRotation = Quaternion.identity;
            _head = headRoot.transform;

            CreatePart("HeadBody", PrimitiveType.Cube,
                Vector3.zero, Quaternion.identity,
                new Vector3(0.13f, 0.06f, 0.10f),
                new Color(0.16f, 0.18f, 0.22f), 0.55f, headRoot.transform);

            CreatePart("HeadCrown", PrimitiveType.Sphere,
                new Vector3(0f, 0.018f, -0.005f), Quaternion.identity,
                new Vector3(0.13f, 0.045f, 0.11f),
                new Color(0.10f, 0.11f, 0.14f), 0.7f, headRoot.transform);

            CreatePart("FaceMarker", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.052f),
                Quaternion.Euler(loftDegrees, 0f, 0f),
                new Vector3(0.12f, 0.052f, 0.004f),
                new Color(0.95f, 0.78f, 0.30f), 0.6f, headRoot.transform);

            var trigger = headRoot.AddComponent<BoxCollider>();
            trigger.size = new Vector3(0.18f, 0.09f, 0.14f);
            trigger.isTrigger = true;

            var rb = headRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var relay = headRoot.AddComponent<PutterHeadCollider>();
            relay.putter = this;
        }

        void CreatePart(string name, PrimitiveType type, Vector3 localPos, Quaternion localRot,
            Vector3 localScale, Color color, float gloss, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Glossiness", gloss);
                renderer.sharedMaterial = mat;
            }
        }

        public bool TryConsumeHit(out Vector3 velocity)
        {
            velocity = _velocity;
            if (Time.time - _enabledAtTime < 0.5f)
                return false;
            if (Time.time - _lastHitTime < hitCooldown)
                return false;
            if (velocity.magnitude < minHitSpeed)
                return false;
            // 트리거 버튼을 누르고 있어야만 타구 가능
            if (!IsTriggerHeld())
                return false;

            _lastHitTime = Time.time;
            SendHaptic();
            return true;
        }

        bool IsTriggerHeld()
        {
            // XR 디바이스에서 직접 트리거 값 읽기 (ActionBinding 불필요)
            var rightDevice = GetRightHandDevice();
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float val))
                    return val > 0.3f;
            }

            // 에디터/폴백: 항상 허용
            return true;
        }

        void SendHaptic()
        {
            // XR 디바이스 직접 햅틱만 사용 — ActionBasedController와 이중 호출 시 무한진동 발생
            var rightDevice = GetRightHandDevice();
            if (rightDevice.isValid
                && rightDevice.TryGetHapticCapabilities(out HapticCapabilities caps)
                && caps.supportsImpulse)
            {
                rightDevice.SendHapticImpulse(0, hapticAmp, hapticDur);
            }
        }

        static InputDevice GetRightHandDevice()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            return devices.Count > 0 ? devices[0] : default;
        }
    }

    public class PutterHeadCollider : MonoBehaviour
    {
        public VRPutter putter;

        void OnTriggerStay(Collider other)
        {
            if (putter == null)
                return;

            var ball = other.GetComponentInParent<BallController>();
            if (ball == null)
                return;

            if (!putter.TryConsumeHit(out Vector3 headVelocity))
                return;

            // 퍼터 페이스가 가리키는 방향 (85% 비중) — VR에서 의도한 방향과 일치
            Vector3 faceHoriz = transform.forward;
            faceHoriz.y = 0f;
            if (faceHoriz.sqrMagnitude < 0.0001f)
                faceHoriz = Vector3.forward;
            faceHoriz.Normalize();

            // 스윙 속도 방향 (15% 비중) — 보조 방향 보정
            Vector3 swingHoriz = new Vector3(headVelocity.x, 0f, headVelocity.z);
            if (swingHoriz.sqrMagnitude < 0.0001f)
                swingHoriz = faceHoriz;
            swingHoriz.Normalize();

            Vector3 direction = Vector3.Slerp(swingHoriz, faceHoriz, putter.FaceVsSwingBlend).normalized;

            float loft = putter.LoftRadians;
            Vector3 hitDir = direction * Mathf.Cos(loft) + Vector3.up * Mathf.Sin(loft);

            float hitSpeed = Mathf.Min(headVelocity.magnitude * putter.ForceMultiplier, putter.MaxHitSpeed);
            ball.HitFromPutter(hitDir, hitSpeed);
        }
    }
}
