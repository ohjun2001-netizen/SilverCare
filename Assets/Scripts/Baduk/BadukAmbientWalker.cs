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

        public void Initialize(Vector3 origin, Vector3 interestPoint, float wanderRadius, float moveSpeed, float groundY,
            float idleMin = 0.8f, float idleMax = 2.4f, float modelYawOffset = 0f)
        {
            _animator = GetComponentInChildren<Animator>();
            _origin = origin;
            _interestPoint = interestPoint;
            _wanderRadius = wanderRadius;
            _moveSpeed = moveSpeed;
            _groundY = groundY;
            _idleMin = Mathf.Max(3.5f, idleMin);
            _idleMax = Mathf.Max(7.5f, idleMax);
            _turnSpeed = Random.Range(0.65f, 1.15f);
            _lookWeight = Random.Range(0.85f, 1.15f);
            _personalScale = Random.Range(0.985f, 1.03f);
            _modelYawOffset = modelYawOffset;
            _motionPhase = Random.Range(0f, Mathf.PI * 2f);
            _breathPhase = Random.Range(0f, Mathf.PI * 2f);
            _headLookTimer = Random.Range(1.2f, 3.8f);
            _headLookOffset = Random.Range(-5f, 5f);
            _pauseLean = Random.Range(-1.5f, 1.5f);
            _baseScale = transform.localScale * _personalScale;
            transform.localScale = _baseScale;
            transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
            ApplyIdleAnimation();
            BeginIdle();
        }

        void Update()
        {
            if (_isIdling)
            {
                _idleTimer -= Time.deltaTime;
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
            _stepAmount = Mathf.Lerp(_stepAmount, 1f, Time.deltaTime * 2.8f);
            transform.position = flatCurrent + dir * (_currentSpeed * Time.deltaTime);
            UpdateWalkMicroMotion();
            ApplyWalkAnimation();

            Vector3 boardDir = Vector3.ProjectOnPlane(_interestPoint - transform.position, Vector3.up).normalized;
            Vector3 blendedDir = dir;
            if (boardDir.sqrMagnitude > 0.001f)
                blendedDir = Vector3.Slerp(dir, boardDir, 0.68f).normalized;

            if (blendedDir.sqrMagnitude > 0.001f)
            {
                Quaternion look = MakeFacingRotation(blendedDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * _turnSpeed);
            }
        }

        void PickNextTarget()
        {
            _isIdling = false;
            Vector2 circle = Random.insideUnitCircle * _wanderRadius;
            if (circle.magnitude < _wanderRadius * 0.35f)
                circle = circle.normalized * (_wanderRadius * Random.Range(0.35f, 0.8f));
            _target = _origin + new Vector3(circle.x, 0f, circle.y);
        }

        void BeginIdle()
        {
            _isIdling = true;
            _idleTimer = Random.Range(_idleMin, _idleMax);
            ApplyIdleAnimation();
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

            Vector3 toBoard = Vector3.ProjectOnPlane(_interestPoint - transform.position, Vector3.up);
            if (toBoard.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = MakeFacingRotation(toBoard.normalized) * Quaternion.Euler(0f, _headLookOffset, _pauseLean);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (_turnSpeed * 0.55f * _lookWeight));
        }

        void UpdateIdleMicroMotion()
        {
            _breathPhase += Time.deltaTime * 1.35f;
            float breath = Mathf.Sin(_breathPhase) * 0.004f;
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * (1f + breath), _baseScale.z);
        }

        void UpdateWalkMicroMotion()
        {
            _motionPhase += Time.deltaTime * Mathf.Lerp(1.8f, 3.4f, Mathf.Clamp01(_stepAmount));
            float bob = Mathf.Abs(Mathf.Sin(_motionPhase)) * 0.008f * _stepAmount;
            Vector3 pos = transform.position;
            pos.y = _groundY + bob;
            transform.position = pos;

            float sway = Mathf.Sin(_motionPhase * 0.5f) * 1.4f * _stepAmount;
            transform.rotation *= Quaternion.Euler(0f, 0f, sway * Time.deltaTime);
        }

        Quaternion MakeFacingRotation(Vector3 flatDirection)
        {
            Quaternion baseRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            return baseRotation * Quaternion.Euler(0f, _modelYawOffset, 0f);
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
            _animator.SetInteger("legs", 1);
            _animator.SetInteger("arms", 1);
        }
    }
}
