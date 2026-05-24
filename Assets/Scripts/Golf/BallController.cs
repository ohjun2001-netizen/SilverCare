// Assets/Scripts/Golf/BallController.cs
// 담당: 양석원
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Golf
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float maxSwingForce  = 15f;
        [SerializeField] private float swingThreshold = 2f;

        [Header("Aim")]
        [SerializeField] private float aimRotateSpeed  = 80f;
        [SerializeField] private float maxChargeTime   = 1.5f;
        [SerializeField] private float oobY            = -1f;   // 이 y 아래면 OOB

        private Rigidbody       _rb;
        private GolfGameManager _manager;
        private GolfUIManager   _ui;
        private Transform       _rightController;
        private Vector3         _prevCtrlPos;
        private bool            _swingArmed;
        private bool            _isMoving;
        private LineRenderer    _aimLine;
        private float           _aimAngle;
        private Vector3         _lastTeePos;

        // PC 차지
        private bool  _isCharging;
        private float _chargeTime;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            BuildAimLine();
        }

        void Start()
        {
            _manager = FindObjectOfType<GolfGameManager>();
            _ui      = FindObjectOfType<GolfUIManager>();

            var ctrlGO = GameObject.Find("RightHand Controller");
            if (ctrlGO != null)
            {
                _rightController = ctrlGO.transform;
                _prevCtrlPos     = _rightController.position;
            }
        }

        void Update()
        {
            // ── 공 정지 감지 ──────────────────────────────────────
            if (_isMoving && _rb.velocity.magnitude < 0.06f)
            {
                _isMoving = false;
                _ui?.SetSwingUIActive(true);
            }

            // ── 아웃오브바운즈 → 벌타 + 티 리셋 ────────────────────
            if (transform.position.y < oobY)
            {
                _manager?.OnBallOutOfBounds();
                _ui?.SetSwingUIActive(true);
                return;
            }

            // ── 조준선 (정지 중만) ────────────────────────────────
            _aimLine.enabled = !_isMoving;
            if (!_isMoving)
            {
                if (Input.GetKey(KeyCode.LeftArrow))  _aimAngle -= aimRotateSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.RightArrow)) _aimAngle += aimRotateSpeed * Time.deltaTime;

                Vector3 aimDir = GetAimDirection();

                // 차지량에 따라 조준선 길이·색 변화
                float chargePct = _isCharging ? Mathf.Clamp01(_chargeTime / maxChargeTime) : 0f;
                _aimLine.startColor = Color.Lerp(Color.yellow, Color.red, chargePct);
                _aimLine.endColor   = new Color(1f, 1f - chargePct, 0f, 0f);
                float lineLen = Mathf.Lerp(1.5f, 5f, chargePct);
                _aimLine.SetPosition(0, transform.position);
                _aimLine.SetPosition(1, transform.position + aimDir * lineLen);
            }

            if (_isMoving) return;

            if (XRPointerInput.TryGetSelectionHit(80f, out RaycastHit laserHit))
            {
                ApplyLaserSwing(laserHit);
                return;
            }

            // ── PC: Space 차지 스윙 ───────────────────────────────
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isCharging = true;
                _chargeTime = 0f;
            }
            if (_isCharging && Input.GetKey(KeyCode.Space))
            {
                _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);
            }
            if (_isCharging && Input.GetKeyUp(KeyCode.Space))
            {
                float t     = Mathf.Clamp01(_chargeTime / maxChargeTime);
                float force = Mathf.Lerp(maxSwingForce * 0.15f, maxSwingForce, t);
                ApplySwing(GetAimDirection(), force);
                _isCharging = false;
                _chargeTime = 0f;
                return;
            }

            // ── VR: 퍼팅 감지 — 실제 XR 기기 연결 시에만 동작 ──
            if (_rightController != null && UnityEngine.XR.XRSettings.isDeviceActive)
            {
                Vector3 ctrlPos  = _rightController.position;
                Vector3 frameVel = (ctrlPos - _prevCtrlPos) / Mathf.Max(Time.deltaTime, 0.001f);
                _prevCtrlPos = ctrlPos;

                Vector3 horizVel   = new Vector3(frameVel.x, 0f, frameVel.z);
                float   horizSpeed = horizVel.magnitude;

                if (!_swingArmed && horizSpeed > 0.3f) _swingArmed = true;

                if (_swingArmed && horizSpeed > swingThreshold)
                {
                    float force = Mathf.Clamp(horizSpeed * 1.5f, 1f, maxSwingForce);
                    ApplySwing(horizVel.normalized, force);
                    _swingArmed = false;
                }
            }
        }

        void ApplyLaserSwing(RaycastHit hit)
        {
            Vector3 flatTarget = hit.point;
            flatTarget.y = transform.position.y;

            Vector3 direction = flatTarget - transform.position;
            if (direction.sqrMagnitude < 0.05f)
                direction = GetAimDirection();

            float distance = Mathf.Max(0.5f, direction.magnitude);
            float force = Mathf.Clamp(distance * 2.2f, maxSwingForce * 0.18f, maxSwingForce * 0.85f);
            ApplySwing(direction.normalized, force);
        }

        Vector3 GetAimDirection()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            return Quaternion.Euler(0f, _aimAngle, 0f) * fwd.normalized;
        }

        public void ApplySwing(Vector3 direction, float force)
        {
            if (_isMoving) return;
            _rb.velocity = Vector3.zero;
            _rb.AddForce(direction.normalized * force, ForceMode.VelocityChange);
            _isMoving    = true;
            _swingArmed  = false;
            _isCharging  = false;
            _aimLine.enabled = false;
            _manager?.OnSwingCompleted();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<GolfHoleTrigger>() != null)
            {
                _rb.velocity        = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _isMoving = false;
                _manager?.OnHoleIn();
            }
        }

        public void ResetBall(Vector3 position)
        {
            _lastTeePos         = position;
            transform.position  = position;
            _rb.velocity        = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _isMoving   = false;
            _swingArmed = false;
            _isCharging = false;
            _chargeTime = 0f;
        }

        void BuildAimLine()
        {
            _aimLine = gameObject.AddComponent<LineRenderer>();
            _aimLine.positionCount = 2;
            _aimLine.startWidth    = 0.04f;
            _aimLine.endWidth      = 0.01f;
            _aimLine.material      = new Material(Shader.Find("Sprites/Default"));
            _aimLine.startColor    = Color.yellow;
            _aimLine.endColor      = new Color(1f, 1f, 0f, 0f);
            _aimLine.useWorldSpace = true;
        }
    }
}
