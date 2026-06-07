// Assets/Scripts/Golf/BallController.cs
using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.XR;

namespace SilverCare.Golf
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        const float BallRadius = 0.03185f;
        const float VisualScaleBoost = 1.25f;

        [Header("Ball State")]
        [SerializeField] float stopSpeed = 0.06f;
        [SerializeField] float oobY = -1f;
        [SerializeField] float minKeyboardShotSpeed = 1.0f;
        [SerializeField] float maxKeyboardShotSpeed = 13f;
        [SerializeField] float maxChargeTime = 1.6f;
        [SerializeField] float defaultLaunchDegrees = 23f;
        [Tooltip("이 속도 이하로 약하게 치면 발사각 0(굴림) — 그린 주변 짧은 퍼팅용")]
        [SerializeField] float puttSpeedThreshold = 1.6f;
        [Tooltip("이 속도 이상이면 발사각 최대(띄움)")]
        [SerializeField] float fullLoftSpeed = 4.5f;
        [SerializeField] float rollingFriction = 1.4f;

        Rigidbody _rb;
        GolfGameManager _manager;
        GolfUIManager _ui;
        ParticleSystem _hitFx;
        Transform _visibleCore;

        bool _isMoving;
        bool _trackingShot;
        bool _isCharging;
        bool _pendingOOB;
        float _chargeTime;
        float _flightTime;
        float _wakeGrace; // 움직이는 벽에 깨워진 직후 즉시 재잠금되지 않도록 하는 유예 시간
        Vector3 _shotStartPosition;
        Vector3 _lastSafePosition;

        public Vector3 LastSafePosition => _lastSafePosition;

        // 실제 충돌 반지름(시각 공과 동일) — 공 시작 높이 계산용.
        public float Radius => BallRadius * VisualScaleBoost;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            EnsureVisibleCore();
            EnsureTrail();
            EnsureHitEffect();
            LockBall();
        }

        void OnEnable()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();
            EnsureVisibleCore();
        }

        void Start()
        {
            _manager = FindObjectOfType<GolfGameManager>();
            _ui = FindObjectOfType<GolfUIManager>();
        }

        void Update()
        {
            HandleKeyboardCharge();

            if (_isMoving)
            {
                _flightTime += Time.deltaTime;
                if (_wakeGrace > 0f)
                    _wakeGrace -= Time.deltaTime;

                bool fellOff = transform.position.y < oobY;
                bool timedOut = _flightTime > 9f;
                bool tooFar = Vector3.Distance(
                    new Vector3(transform.position.x, 0f, transform.position.z),
                    new Vector3(_shotStartPosition.x, 0f, _shotStartPosition.z)) > 65f;

                if ((fellOff || timedOut || tooFar) && !_pendingOOB)
                {
                    TriggerOOB();
                    return;
                }

                // 깨워진 직후 유예 시간 동안은 벽이 밀 시간을 줘야 하므로 재잠금하지 않는다.
                if (_wakeGrace <= 0f && IsGrounded() && HorizontalVelocity(_rb.velocity).magnitude < stopSpeed)
                {
                    LockBall();
                    SnapToGround(); // 굴곡/경사 지형에 공이 파묻히지 않게 지면 표면 위로 올림
                    _isMoving = false;
                    _ui?.SetSwingUIActive(true);

                    if (_trackingShot)
                    {
                        float distance = Vector3.Distance(
                            new Vector3(_shotStartPosition.x, 0f, _shotStartPosition.z),
                            new Vector3(transform.position.x, 0f, transform.position.z));
                        _ui?.ShowShotDistance(distance);
                        _trackingShot = false;
                        _manager?.OnBallStopped(transform.position, _shotStartPosition);
                    }
                }
            }
            else if (!_pendingOOB && transform.position.y < oobY)
            {
                // 정지 상태에서 낙하한 경우 (예: 발판이 사라진 경우)
                TriggerOOB();
            }
        }

        void TriggerOOB()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            LockBall();
            _isMoving = false;
            _trackingShot = false;
            _pendingOOB = true;
            _manager?.OnBallOutOfBounds();
            _ui?.SetSwingUIActive(true);
        }

        // 멈춰서 잠긴 공을 다시 물리 상태로 깨운다 — 움직이는 벽이 닿으면 밀리도록.
        public void WakeFromRest()
        {
            if (_pendingOOB || _rb == null)
                return;
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _isMoving = true;
            _flightTime = 0f; // 벽에 닿는 동안 타임아웃 OB 방지
            _wakeGrace = 0.5f; // 즉시 재잠금 방지 — 벽이 공을 밀 시간 확보 (트리거 안에선 매 프레임 갱신)
        }

        void FixedUpdate()
        {
            if (!_isMoving || !IsGrounded())
                return;

            // 공이 지면에 닿아 있는 동안 마지막 안전 위치 갱신
            _lastSafePosition = transform.position;

            Vector3 horizontal = HorizontalVelocity(_rb.velocity);
            Vector3 slowed = Vector3.MoveTowards(horizontal, Vector3.zero, rollingFriction * Time.fixedDeltaTime);
            _rb.velocity = new Vector3(slowed.x, _rb.velocity.y, slowed.z);
            _rb.angularVelocity = Vector3.MoveTowards(_rb.angularVelocity, Vector3.zero, rollingFriction * 0.7f * Time.fixedDeltaTime);
        }

        public void HitFromPutter(Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f || speed <= 0f)
                return;

            Vector3 launchDirection = BuildLaunchDirection(direction, speed);

            _shotStartPosition = transform.position;
            _trackingShot = true;
            _flightTime = 0f;

            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.velocity = launchDirection * speed;
            _rb.angularVelocity = Vector3.Cross(Vector3.up, HorizontalVelocity(launchDirection).normalized) * (speed * 0.65f);

            _isMoving = true;

            if (_hitFx != null)
            {
                _hitFx.transform.position = transform.position;
                _hitFx.Play();
            }

            _ui?.ShowShotFeedback(Mathf.Clamp01(speed / 6f));
            AudioManager.Instance?.PlayButtonClick();
            _manager?.OnSwingCompleted();
        }

        void HandleKeyboardCharge()
        {
            if (_isMoving)
                return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isCharging = true;
                _chargeTime = 0f;
                _ui?.ShowPower(0f);
                _ui?.SetSwingUIActive(true);
            }

            if (_isCharging && Input.GetKey(KeyCode.Space))
            {
                _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);
                _ui?.ShowPower(_chargeTime / maxChargeTime);
            }

            if (_isCharging && Input.GetKeyUp(KeyCode.Space))
            {
                float power = Mathf.Clamp01(_chargeTime / maxChargeTime);
                float speed = Mathf.Lerp(minKeyboardShotSpeed, maxKeyboardShotSpeed, power);
                _isCharging = false;
                _chargeTime = 0f;
                _ui?.ShowPower(0f);
                HitFromPutter(GetCameraForward(), speed);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<GolfHoleTrigger>() == null)
                return;
            if (_pendingOOB)
                return;
            if (_manager != null && !_manager.CanHoleIn)
                return; // 시작/재시작 직후(스윙 전) 비정상 홀 진입 무시

            // 공을 홀 안으로 빨아들이는 연출 후 OnHoleIn 호출
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.useGravity = false;
            _rb.isKinematic = false;
            _isMoving = false;
            _trackingShot = false;
            StartCoroutine(SinkIntoHole(other.transform.position));
        }

        IEnumerator SinkIntoHole(Vector3 holeCenter)
        {
            // 즉각 햅틱 — 양손 진동
            TriggerHoleHaptic();

            // 별표 이펙트 스폰
            SpawnHoleStarEffect(holeCenter + Vector3.up * 0.15f);

            // 공이 홀 안으로 빨려드는 애니메이션
            Vector3 startPos = transform.position;
            Vector3 targetPos = holeCenter - Vector3.up * 0.22f;
            float elapsed = 0f;

            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                // 서서히 작아지는 느낌
                float scale = Mathf.Lerp(1f, 0.3f, t);
                if (_visibleCore != null)
                    _visibleCore.localScale = Vector3.one * (BallRadius * 2f * scale);
                yield return null;
            }

            LockBall();
            if (_visibleCore != null)
                _visibleCore.localScale = Vector3.one * (BallRadius * 2f * VisualScaleBoost);
            _ui?.ShowShotFeedback(1.2f);
            _manager?.OnHoleIn();
        }

        void TriggerHoleHaptic()
        {
            foreach (XRNode node in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                var devices = new List<InputDevice>();
                InputDevices.GetDevicesAtXRNode(node, devices);
                if (devices.Count > 0
                    && devices[0].TryGetHapticCapabilities(out HapticCapabilities caps)
                    && caps.supportsImpulse)
                {
                    devices[0].SendHapticImpulse(0, 1.0f, 0.1f);
                }
            }
        }

        void SpawnHoleStarEffect(Vector3 pos)
        {
            var fx = new GameObject("HoleStarFX");
            fx.transform.position = pos;
            var ps = fx.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration         = 0.1f;
            main.loop             = false;
            main.playOnAwake      = false;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.1f), new Color(1f, 1f, 1f));
            main.gravityModifier  = 0.4f;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.maxParticles     = 50;

            var emit = ps.emission;
            emit.rateOverTime = 0f;
            emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 45f;
            shape.radius    = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0f),
                         new GradientColorKey(Color.white,  0.4f),
                         new GradientColorKey(Color.yellow, 1f) },
                new[] { new GradientAlphaKey(1f, 0f),
                         new GradientAlphaKey(1f, 0.5f),
                         new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            // 두 번째 파티클 시스템 — 낮게 퍼지는 불꽃
            var fx2 = new GameObject("HoleStarFX2");
            fx2.transform.SetParent(fx.transform, false);
            var ps2 = fx2.AddComponent<ParticleSystem>();
            var m2 = ps2.main;
            m2.duration        = 0.1f;
            m2.loop            = false;
            m2.playOnAwake     = false;
            m2.startLifetime   = 0.6f;
            m2.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
            m2.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            m2.startColor      = new Color(1f, 0.6f, 0f);
            m2.gravityModifier = -0.1f;
            m2.simulationSpace = ParticleSystemSimulationSpace.World;
            var e2 = ps2.emission;
            e2.rateOverTime = 0f;
            e2.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
            var sh2 = ps2.shape;
            sh2.shapeType = ParticleSystemShapeType.Circle;
            sh2.radius    = 0.08f;
            // ps.Play()가 자식(ps2)까지 재귀 실행하므로 ps2.Play()는 생략한다.
            // 중복 Play → "Setting duration while playing" 경고 방지.
            ps.Play();
            Destroy(fx, 2.5f);
        }

        public void ResetBall(Vector3 position)
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();
            EnsureVisibleCore();
            // position.y 우선 사용, 바닥 아래로 내려가지 않도록 최소값 보장
            transform.position = new Vector3(position.x, Mathf.Max(position.y, BallRadius + 0.01f), position.z);
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            LockBall();
            _isMoving = false;
            _trackingShot = false;
            _isCharging = false;
            _pendingOOB = false;
            _chargeTime = 0f;
            _flightTime = 0f;
            _lastSafePosition = transform.position;
            _ui?.ShowPower(0f);
            _ui?.ShowShotDistance(0f);
        }

        // 멈춘 공을 그 자리(xz)의 지면 표면 위로 끌어올린다 — 굴곡/경사/그린 단상에서 파묻힘 방지.
        // 위로 올리기만 하고(끌어올림) 내리지는 않아 공중 부유는 만들지 않는다.
        void SnapToGround()
        {
            float r = BallRadius * VisualScaleBoost;
            Vector3 origin = transform.position + Vector3.up * 1.5f;

            var ownColliders = GetComponentsInChildren<Collider>();
            foreach (var c in ownColliders)
                c.enabled = false;

            bool found = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore);

            foreach (var c in ownColliders)
                c.enabled = true;

            // 공 바로 아래(현 위치 근방)의 지면만 채택 — 위쪽 벽 상단 등은 제외.
            if (found && hit.point.y <= transform.position.y + r + 0.05f)
            {
                float targetY = hit.point.y + r + 0.005f;
                if (transform.position.y < targetY)
                    transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }

        Vector3 ProjectToGround(Vector3 position)
        {
            const float maxSnapDown = 8f;  // 공중 OOB 후에도 반드시 지면에 스냅
            Vector3 rayStart = position + Vector3.up * 5f;

            var ownColliders = GetComponentsInChildren<Collider>();
            foreach (var ownCollider in ownColliders)
                ownCollider.enabled = false;

            bool foundGround = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore);

            foreach (var ownCollider in ownColliders)
                ownCollider.enabled = true;

            // hit.point가 예상 위치보다 1m 이상 높으면 벽/장애물 상단 → 무시
            if (foundGround && hit.point.y >= position.y - maxSnapDown && hit.point.y <= position.y + 1.0f)
            {
                return hit.point + Vector3.up * (BallRadius + 0.01f); // 지면 위 1cm 여유
            }

            return position;
        }

        void EnsureVisibleCore()
        {
            var sphere = GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = gameObject.AddComponent<SphereCollider>();
            // 콜라이더를 시각 공과 동일 크기로 맞춰 공이 지면에 파묻혀 보이지 않게 한다.
            sphere.radius = BallRadius * VisualScaleBoost;

            if (_visibleCore == null)
            {
                var existing = transform.Find("GolfBallVisibilityCore");
                _visibleCore = existing != null ? existing : null;
            }

            if (_visibleCore == null)
            {
                var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.name = "GolfBallVisibilityCore";
                core.transform.SetParent(transform, false);
                core.transform.localPosition = Vector3.zero;
                core.transform.localScale = Vector3.one;

                var coreCollider = core.GetComponent<Collider>();
                if (coreCollider != null)
                    Destroy(coreCollider);

                var renderer = core.GetComponent<Renderer>();
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                mat.color = new Color(0.02f, 0.018f, 0.015f);  // 초록 잔디 위에서 확실히 보이는 검은색 공
                if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", 0.75f);
                renderer.sharedMaterial = mat;

                _visibleCore = core.transform;
            }

            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = true;
            }

            _visibleCore.localPosition = Vector3.zero;
            _visibleCore.localScale = Vector3.one * (BallRadius * 2f * VisualScaleBoost);
            _visibleCore.gameObject.SetActive(true);

            var coreRenderer = _visibleCore.GetComponent<Renderer>();
            if (coreRenderer != null)
            {
                coreRenderer.enabled = true;
                EnsureBallMaterial(coreRenderer);
            }
        }

        static void EnsureBallMaterial(Renderer renderer)
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var mat = renderer.sharedMaterial;
            if (mat == null || mat.shader != shader)
            {
                mat = new Material(shader);
                renderer.sharedMaterial = mat;
            }

            mat.color = new Color(0.02f, 0.018f, 0.015f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.75f);
        }

        void EnsureTrail()
        {
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
                Destroy(trail);
        }

        void LockBall()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.useGravity = false;
            _rb.isKinematic = true;

            // 잠금 시 지면 아래 있으면 위로 올림 (BallRadius + 1cm 여유)
            Vector3 pos = transform.position;
            if (pos.y < BallRadius + 0.01f)
                transform.position = new Vector3(pos.x, BallRadius + 0.01f, pos.z);
        }

        Vector3 GetCameraForward()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            return forward.normalized;
        }

        Vector3 BuildLaunchDirection(Vector3 direction, float speed)
        {
            Vector3 horizontal = HorizontalVelocity(direction);
            if (horizontal.sqrMagnitude < 0.0001f)
                horizontal = Vector3.forward;
            horizontal.Normalize();

            // 입력에 담긴 발사각(VR 퍼터의 loft, 또는 키보드 기본값)
            float vy = direction.normalized.y;
            float inputLoft = vy > 0.05f
                ? Mathf.Asin(Mathf.Clamp01(vy))
                : defaultLaunchDegrees * Mathf.Deg2Rad;

            // 약하게 칠수록 발사각을 낮춰 공이 굴러가게 한다(그린 주변 짧은 퍼팅 미세 조정).
            float loftScale = Mathf.Clamp01(Mathf.InverseLerp(puttSpeedThreshold, fullLoftSpeed, speed));
            float loft = inputLoft * loftScale;

            return (horizontal * Mathf.Cos(loft) + Vector3.up * Mathf.Sin(loft)).normalized;
        }

        bool IsGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * 0.03f;
            return Physics.Raycast(origin, Vector3.down, BallRadius + 0.08f, ~0, QueryTriggerInteraction.Ignore);
        }

        static Vector3 HorizontalVelocity(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        void EnsureHitEffect()
        {
            var fxRoot = new GameObject("GolfHitFX");
            fxRoot.transform.SetParent(transform, false);
            fxRoot.transform.localPosition = Vector3.zero;
            _hitFx = fxRoot.AddComponent<ParticleSystem>();

            var main = _hitFx.main;
            main.duration = 0.35f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.22f;
            main.startSpeed = 1.4f;
            main.startSize = 0.08f;
            main.startColor = new Color(1f, 0.93f, 0.55f, 0.9f);
            main.gravityModifier = 0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _hitFx.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = _hitFx.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.04f;

            var colorOverLifetime = _hitFx.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.65f), 0f),
                    new GradientColorKey(new Color(1f, 0.70f, 0.28f), 0.6f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;
        }
    }
}
