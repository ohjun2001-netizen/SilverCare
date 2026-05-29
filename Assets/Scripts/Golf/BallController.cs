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
        const float BallRadius = 0.045f;

        [Header("Ball State")]
        [SerializeField] float stopSpeed = 0.06f;
        [SerializeField] float oobY = -1f;
        [SerializeField] float minKeyboardShotSpeed = 4.5f;
        [SerializeField] float maxKeyboardShotSpeed = 13f;
        [SerializeField] float maxChargeTime = 1.6f;
        [SerializeField] float defaultLaunchDegrees = 28f;
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
        Vector3 _shotStartPosition;
        Vector3 _lastSafePosition;

        public Vector3 LastSafePosition => _lastSafePosition;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            EnsureVisibleCore();
            EnsureTrail();
            EnsureHitEffect();
            LockBall();
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

                if (IsGrounded() && HorizontalVelocity(_rb.velocity).magnitude < stopSpeed)
                {
                    LockBall();
                    _isMoving = false;
                    _ui?.SetSwingUIActive(true);

                    if (_trackingShot)
                    {
                        float distance = Vector3.Distance(
                            new Vector3(_shotStartPosition.x, 0f, _shotStartPosition.z),
                            new Vector3(transform.position.x, 0f, transform.position.z));
                        _ui?.ShowShotDistance(distance);
                        _trackingShot = false;
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

            Vector3 launchDirection = BuildLaunchDirection(direction);

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
                _visibleCore.localScale = Vector3.one * (BallRadius * 2f);
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
            ps2.Play();

            ps.Play();
            Destroy(fx, 2.5f);
        }

        public void ResetBall(Vector3 position)
        {
            EnsureVisibleCore();
            // ProjectToGround 대신 고정 Y 사용 → 랜덤 위치 방지
            // PhysicsGround 상단 Y=0 기준, 공 반지름 + 1cm 여유
            transform.position = new Vector3(position.x, BallRadius + 0.01f, position.z);
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
            sphere.radius = BallRadius;

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
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.88f, 0.05f);  // 밝은 노란색 — 초록 표면 위에서 확실히 보임
                mat.SetFloat("_Glossiness", 0.75f);
                renderer.sharedMaterial = mat;

                _visibleCore = core.transform;
            }

            bool hasImportedVisual = false;
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform != _visibleCore && renderer.transform.parent != _visibleCore)
                {
                    renderer.enabled = true;
                    hasImportedVisual = true;
                }
            }

            _visibleCore.localPosition = Vector3.zero;
            _visibleCore.localScale = Vector3.one * (BallRadius * 2f);  // 물리 콜라이더와 동일 크기
            _visibleCore.gameObject.SetActive(!hasImportedVisual);
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

        Vector3 BuildLaunchDirection(Vector3 direction)
        {
            Vector3 horizontal = HorizontalVelocity(direction);
            if (horizontal.sqrMagnitude < 0.0001f)
                horizontal = Vector3.forward;
            horizontal.Normalize();

            float y = direction.normalized.y;
            if (y > 0.05f)
                return direction.normalized;

            float radians = defaultLaunchDegrees * Mathf.Deg2Rad;
            return (horizontal * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians)).normalized;
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
