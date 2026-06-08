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
        [SerializeField] float minHitSpeed = 0.2f;
        [SerializeField] float maxHitSpeed = 10f;
        [SerializeField] float forceMultiplier = 2.0f;
        [SerializeField] float hitCooldown = 0.4f;
        [Tooltip("Vertical launch angle in degrees")]
        [SerializeField] float loftDegrees = 20f;
        [Tooltip("How much the club face direction overrides swing direction (0=swing only, 1=face only)")]
        [SerializeField] [Range(0f, 1f)] float faceVsSwingBlend = 0.1f;

        [Header("Club Model")]
        [Tooltip("임포트된 골프채 모델의 스케일 (기본값으로 실제 퍼터 크기에 맞춤)")]
        [SerializeField] float clubModelScale = 0.045f;
        [Tooltip("모델 회전 오프셋 (Y축 — 페이스 방향 미세 조정)")]
        [SerializeField] float clubModelYaw = 90f;

        [Header("Feedback")]
        [SerializeField] float hapticAmp = 0.9f;
        [SerializeField] float hapticDur = 0.18f;

        // 기존 프리미티브 fallback 치수
        const float GripLen = 0.13f;
        const float ShaftLength = 1.2f;

        // 임포트 모델 기준 치수 (Golf Club Model.fbx, UnitScaleFactor=1.0)
        // CapsuleCollider: direction=Z, center=(0.95,0.2,0.85), height=20.83 → grip end at Z≈11.27
        // BoxCollider head: center=(−0.15,−0.03,−10.09)
        const float ModelGripZ = 11.27f;
        const float ModelHeadZ = -10.09f;

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

            float scale = clubModelScale;
            // 모델 Z축이 샤프트 방향. Euler(-90,0,0)로 Z→Y 변환(헤드가 -Y 방향으로).
            // 그립 끝이 루트 원점(손 위치)에 오도록 Y축으로 내림.
            float gripHeight = ModelGripZ * scale;           // ≈ 0.507m
            float headY = ModelHeadZ * scale - gripHeight;   // ≈ -0.961m (손에서 헤드까지)

            var clubPrefab = Resources.Load<GameObject>("GolfPark/GolfClub");
            if (clubPrefab != null)
            {
                var visual = Instantiate(clubPrefab, _root.transform);
                visual.name = "ClubVisual";
                visual.transform.localScale = Vector3.one * scale;
                // Euler(-90,0,0): 모델 +Z→ root +Y(그립), 모델 -Z→ root -Y(헤드)
                visual.transform.localRotation = Quaternion.Euler(-90f, clubModelYaw, 0f);
                // 그립 끝을 손 위치(root 원점)에 맞춤
                visual.transform.localPosition = new Vector3(0f, -gripHeight, 0f);
                // 기존 콜라이더는 크기가 잘못되어 있으므로 비활성화
                foreach (var col in visual.GetComponentsInChildren<Collider>(true))
                    col.enabled = false;
            }
            else
            {
                // 폴백: 기존 프리미티브 시각
                BuildPrimitiveVisual();
                headY = -(GripLen + 0.02f + ShaftLength);
            }

            // 헤드 히트 트리거 (공과의 물리 감지)
            var headRoot = new GameObject("PutterHead");
            headRoot.transform.SetParent(_root.transform, false);
            headRoot.transform.localPosition = new Vector3(0f, headY, 0f);
            _head = headRoot.transform;

            // 콜라이더를 넓게 설정해 공이 더 잘 맞도록
            var trigger = headRoot.AddComponent<BoxCollider>();
            trigger.size = new Vector3(0.26f, 0.12f, 0.22f);
            trigger.isTrigger = true;

            var rb = headRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var relay = headRoot.AddComponent<PutterHeadCollider>();
            relay.putter = this;
        }

        void BuildPrimitiveVisual()
        {
            float gripCenterY = -GripLen * 0.5f - 0.02f;
            CreatePart("Grip", PrimitiveType.Cylinder,
                new Vector3(0f, gripCenterY, 0f),
                Quaternion.identity,
                new Vector3(0.045f, GripLen * 0.5f, 0.045f),
                new Color(0.08f, 0.08f, 0.09f), 0.18f, _root.transform);

            float shaftCenterY = -(GripLen + 0.02f + ShaftLength * 0.5f);
            CreatePart("Shaft", PrimitiveType.Cylinder,
                new Vector3(0f, shaftCenterY, 0f),
                Quaternion.identity,
                new Vector3(0.022f, ShaftLength * 0.5f, 0.022f),
                new Color(0.82f, 0.84f, 0.88f), 0.55f, _root.transform);

            var headRoot = new GameObject("PutterHeadVisual");
            headRoot.transform.SetParent(_root.transform, false);
            headRoot.transform.localPosition = new Vector3(0f, -(GripLen + 0.02f + ShaftLength), 0f);

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
            if (Time.time - _enabledAtTime < 0.3f)
                return false;
            if (Time.time - _lastHitTime < hitCooldown)
                return false;
            if (velocity.magnitude < minHitSpeed)
                return false;
            if (!IsTriggerPressed(0.75f))
                return false;

            _lastHitTime = Time.time;
            SendHaptic();
            return true;
        }

        bool IsTriggerPressed(float threshold = 0.75f)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            if (devices.Count > 0 && devices[0].TryGetFeatureValue(CommonUsages.trigger, out float val))
                return val > threshold;
            return false;
        }

        void SendHaptic()
        {
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

            // 퍼터 페이스가 가리키는 방향 (수평 투영)
            Vector3 faceHoriz = transform.forward;
            faceHoriz.y = 0f;
            if (faceHoriz.sqrMagnitude < 0.0001f)
                faceHoriz = Vector3.forward;
            faceHoriz.Normalize();

            // 스윙 속도 방향
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
