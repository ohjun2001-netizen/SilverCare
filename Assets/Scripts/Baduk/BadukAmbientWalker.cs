using UnityEngine;

namespace Baduk
{
    public class BadukAmbientWalker : MonoBehaviour
    {
        Animator _animator;
        Vector3 _origin;
        Vector3 _interestPoint;
        float _wanderRadius;
        float _moveSpeed;
        float _idleMin;
        float _idleMax;
        float _groundY;
        Vector3 _target;
        float _idleTimer;
        float _turnSpeed;
        float _lookWeight;
        float _personalScale;
        float _modelYawOffset;
        float _motionPhase;
        float _breathPhase;
        float _headLookTimer;
        float _headLookOffset;
        float _pauseLean;
        float _currentSpeed;
        float _stepAmount;
        Vector3 _baseScale;
        bool _isIdling = true;
        float _boardKeepout;
        Vector3[] _avoidPoints;
        float[] _avoidRadii;
        float _initialBoardLookTimer;
        BadukBoard _liveBoard;

        public void Initialize(Vector3 origin, Vector3 interestPoint, float wanderRadius, float moveSpeed, float groundY,
            float idleMin = 0.8f, float idleMax = 2.4f, float modelYawOffset = 0f, float boardKeepout = 0f,
            Vector3[] avoidPoints = null, float[] avoidRadii = null)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
                _animator.applyRootMotion = false;
            _origin = origin;
            _interestPoint = interestPoint;
            _wanderRadius = wanderRadius;
            _moveSpeed = moveSpeed;
            _groundY = groundY;
            _boardKeepout = boardKeepout;
            _avoidPoints = avoidPoints;
            _avoidRadii = avoidRadii;
            _liveBoard = Object.FindObjectOfType<BadukBoard>();
            _idleMin = Mathf.Max(15f, idleMin);
            _idleMax = Mathf.Max(30f, idleMax);
            _turnSpeed = Random.Range(0.65f, 1.15f);
            _lookWeight = Random.Range(0.85f, 1.15f);
            _personalScale = Random.Range(0.985f, 1.03f);
            _modelYawOffset = modelYawOffset;
            _motionPhase = Random.Range(0f, Mathf.PI * 2f);
            _breathPhase = Random.Range(0f, Mathf.PI * 2f);
            _headLookTimer = Random.Range(1.2f, 3.8f);
            _headLookOffset = 0f;
            _pauseLean = Random.Range(-1.5f, 1.5f);
            _baseScale = transform.localScale * _personalScale;
            transform.localScale = _baseScale;
            transform.position = ClampOutsideBoard(new Vector3(transform.position.x, _groundY, transform.position.z));
            _isIdling = true;
            FaceBoardInstantly();
            ApplyIdleAnimation();
            _initialBoardLookTimer = 5.0f;
        }

        void Update()
        {
            if (_initialBoardLookTimer > 0f)
            {
                _initialBoardLookTimer -= Time.deltaTime;
                if (_initialBoardLookTimer <= 0f)
                    PickNextTarget();
                ApplyIdleAnimation();
                if (_animator != null)
                    _animator.speed = 1f;
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, Time.deltaTime * 4.5f);
                _stepAmount = Mathf.Lerp(_stepAmount, 0f, Time.deltaTime * 4.5f);
                UpdateIdleMicroMotion();
                FaceBoardInstantly();
                return;
            }

            if (_isIdling)
            {
                _idleTimer -= Time.deltaTime;
                if (_animator != null)
                    _animator.speed = 1f;
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, Time.deltaTime * 4.5f);
                _stepAmount = Mathf.Lerp(_stepAmount, 0f, Time.deltaTime * 4.5f);
                UpdateIdleMicroMotion();
                LookTowardBoard();
                if (_idleTimer <= 0f)
                    PickNextTarget();
                return;
            }

            Vector3 flatTarget = new Vector3(_target.x, _groundY, _target.z);
            Vector3 flatCurrent = new Vector3(transform.position.x, _groundY, transform.position.z);
            Vector3 delta = flatTarget - flatCurrent;
            float distance = delta.magnitude;

            if (distance < 0.04f)
            {
                ApplyIdleAnimation();
                BeginIdle();
                return;
            }

            Vector3 dir = delta / Mathf.Max(distance, 0.0001f);
            _currentSpeed = Mathf.Lerp(_currentSpeed, _moveSpeed, Time.deltaTime * 2.2f);
            float walkIntensity = Mathf.InverseLerp(0f, _moveSpeed, _currentSpeed);
            _stepAmount = Mathf.Lerp(_stepAmount, walkIntensity, Time.deltaTime * 5.0f);
            transform.position = ClampOutsideBoard(flatCurrent + dir * (_currentSpeed * Time.deltaTime));
            UpdateWalkMicroMotion();
            ApplyWalkAnimation();

            Vector3 boardLookPoint = GetBoardLookPoint();
            Vector3 boardDir = Vector3.ProjectOnPlane(boardLookPoint - transform.position, Vector3.up).normalized;
            Vector3 blendedDir = dir;
            if (boardDir.sqrMagnitude > 0.001f)
            {
                Vector3 blend = Vector3.Lerp(dir, boardDir, 0.04f);
                if (blend.sqrMagnitude > 0.001f)
                    blendedDir = blend.normalized;
            }

            if (blendedDir.sqrMagnitude > 0.001f)
            {
                RotateYawToward(blendedDir, Time.deltaTime * 360f * _turnSpeed);
            }
        }

        void PickNextTarget()
        {
            _isIdling = false;
            Vector2 circle = Random.insideUnitCircle * _wanderRadius;
            if (circle.magnitude < _wanderRadius * 0.35f)
                circle = circle.normalized * (_wanderRadius * Random.Range(0.35f, 0.8f));
            _target = ClampOutsideBoard(_origin + new Vector3(circle.x, 0f, circle.y));
        }

        Vector3 ClampOutsideBoard(Vector3 candidate)
        {
            if (_boardKeepout > 0f)
            {
                Vector3 boardLookPoint = GetBoardLookPoint();
                Vector3 fromBoard = candidate - boardLookPoint;
                fromBoard.y = 0f;
                if (fromBoard.sqrMagnitude < _boardKeepout * _boardKeepout)
                {
                    Vector3 push = fromBoard.sqrMagnitude > 0.0001f
                        ? fromBoard.normalized
                        : Vector3.ProjectOnPlane(_origin - _interestPoint, Vector3.up).normalized;
                    if (push.sqrMagnitude < 0.0001f)
                        push = Vector3.forward;
                    candidate = boardLookPoint + push * _boardKeepout;
                }
            }

            if (_avoidPoints != null && _avoidRadii != null)
            {
                int count = Mathf.Min(_avoidPoints.Length, _avoidRadii.Length);
                for (int i = 0; i < count; i++)
                {
                    float radius = Mathf.Max(0f, _avoidRadii[i]);
                    if (radius <= 0f)
                        continue;

                    Vector3 fromObstacle = candidate - _avoidPoints[i];
                    fromObstacle.y = 0f;
                    if (fromObstacle.sqrMagnitude >= radius * radius)
                        continue;

                    Vector3 push = fromObstacle.sqrMagnitude > 0.0001f
                        ? fromObstacle.normalized
                        : Vector3.ProjectOnPlane(_origin - _avoidPoints[i], Vector3.up).normalized;
                    if (push.sqrMagnitude < 0.0001f)
                        push = Vector3.forward;
                    candidate = _avoidPoints[i] + push * radius;
                }
            }

            candidate.y = _groundY;
            return candidate;
        }

        void BeginIdle()
        {
            _isIdling = true;
            _idleTimer = Random.Range(_idleMin, _idleMax);
            ApplyIdleAnimation();
            FaceBoardInstantly();
        }

        void LookTowardBoard()
        {
            _headLookTimer -= Time.deltaTime;
            if (_headLookTimer <= 0f)
            {
                _headLookTimer = Random.Range(1.8f, 4.6f);
                _headLookOffset = Random.Range(-9f, 9f);
                _pauseLean = Random.Range(-2.0f, 2.0f);
            }

            Vector3 toBoard = Vector3.ProjectOnPlane(GetBoardLookPoint() - transform.position, Vector3.up);
            if (toBoard.sqrMagnitude < 0.001f)
                return;

            Vector3 lookDirection = Quaternion.Euler(0f, _headLookOffset, 0f) * toBoard.normalized;
            RotateYawToward(lookDirection, Time.deltaTime * 55f * _turnSpeed * _lookWeight);
        }

        void LateUpdate()
        {
            if (_initialBoardLookTimer > 0f)
                FaceBoardInstantly();
        }

        void UpdateIdleMicroMotion()
        {
            _breathPhase += Time.deltaTime * 1.35f;
            float breath = Mathf.Sin(_breathPhase) * 0.004f;
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * (1f + breath), _baseScale.z);
        }

        void UpdateWalkMicroMotion()
        {
            _motionPhase += Time.deltaTime * Mathf.Lerp(0.55f, 1.05f, Mathf.Clamp01(_stepAmount));
            float bob = Mathf.Abs(Mathf.Sin(_motionPhase)) * 0.0035f * _stepAmount;
            Vector3 pos = transform.position;
            pos.y = _groundY + bob;
            transform.position = pos;

            // Keep walking upright; rotating the whole body on Z made NPCs appear to spin in VR.
        }

        Quaternion MakeFacingRotation(Vector3 flatDirection)
        {
            Quaternion baseRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            return baseRotation * Quaternion.Euler(0f, _modelYawOffset, 0f);
        }

        void FaceBoardInstantly()
        {
            Vector3 toBoard = Vector3.ProjectOnPlane(GetBoardLookPoint() - transform.position, Vector3.up);
            if (toBoard.sqrMagnitude < 0.001f)
                return;

            transform.rotation = MakeFacingRotation(toBoard.normalized);
        }

        Vector3 GetBoardLookPoint()
        {
            if (TryGetLiveBoardCenter(out Vector3 center))
                return center;

            return _interestPoint;
        }

        bool TryGetLiveBoardCenter(out Vector3 center)
        {
            center = default;
            if (_liveBoard == null)
                _liveBoard = Object.FindObjectOfType<BadukBoard>();
            if (_liveBoard == null)
                return false;

            float w = (_liveBoard.C1 - _liveBoard.C0) * BadukBoard.CELL;
            float h = (_liveBoard.R1 - _liveBoard.R0) * BadukBoard.CELL;
            if (w <= 0f && h <= 0f)
                return false;

            center = _liveBoard.transform.TransformPoint(new Vector3(w * 0.5f, 0f, -h * 0.5f));
            return true;
        }

        void RotateYawToward(Vector3 flatDirection, float maxDegreesDelta)
        {
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.001f)
                return;

            float currentYaw = transform.eulerAngles.y;
            float targetYaw = MakeFacingRotation(flatDirection.normalized).eulerAngles.y;
            float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, maxDegreesDelta);
            transform.rotation = Quaternion.Euler(0f, nextYaw, 0f);
        }

        void ApplyIdleAnimation()
        {
            if (_animator == null) return;
            _animator.SetInteger("legs", 5);
            _animator.SetInteger("arms", 5);
        }

        void ApplyWalkAnimation()
        {
            if (_animator == null) return;
            if (_stepAmount < 0.4f)
            {
                ApplyIdleAnimation();
                return;
            }

            _animator.speed = Mathf.Lerp(0.05f, 0.20f, Mathf.Clamp01(_currentSpeed / _moveSpeed));
            _animator.SetInteger("legs", 1);
            _animator.SetInteger("arms", 1);
        }
    }
}
