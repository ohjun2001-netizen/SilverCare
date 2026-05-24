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
            transform.localScale *= _personalScale;
            transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
            ApplyIdleAnimation();
            BeginIdle();
        }

        void Update()
        {
            if (_isIdling)
            {
                _idleTimer -= Time.deltaTime;
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
            transform.position = flatCurrent + dir * (_moveSpeed * Time.deltaTime);
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
            Vector3 toBoard = Vector3.ProjectOnPlane(_interestPoint - transform.position, Vector3.up);
            if (toBoard.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = MakeFacingRotation(toBoard.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (_turnSpeed * 0.95f * _lookWeight));
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
