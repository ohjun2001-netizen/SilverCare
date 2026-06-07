// Assets/Scripts/Golf/GolfObstacleMotion.cs
using UnityEngine;

namespace SilverCare.Golf
{
    /// <summary>
    /// 움직이는 골프 장애물 — 회전(풍차) 또는 좌우 왕복(슬라이딩 벽).
    /// 콜라이더가 공을 실제로 밀어내도록 Kinematic Rigidbody로 이동시킨다.
    /// 부모(코스)가 PositionCourseNearPlayer로 이동·회전돼 있어도 로컬 변환을 쓰므로 함께 따라간다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GolfObstacleMotion : MonoBehaviour
    {
        public enum Kind { SpinY, SlideX }

        [SerializeField] Kind kind = Kind.SpinY;
        [SerializeField] float speed = 60f;     // SpinY: 초당 회전각(deg) / SlideX: 진동 각속도
        [SerializeField] float amplitude = 1.2f; // SlideX: 왕복 거리(로컬 X, ±)

        Rigidbody _rb;
        Vector3 _baseLocalPos;
        float _phase;

        public void Configure(Kind k, float spd, float amp)
        {
            kind = k;
            speed = spd;
            amplitude = amp;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            // 빠르게 움직여도 공을 관통하지 않게(연속 충돌).
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _baseLocalPos = transform.localPosition;
            _phase = Random.value * Mathf.PI * 2f;
        }

        // 트리거(벽 경로) 안에 멈춘 공이 있으면 깨워서 벽이 밀 수 있게 한다.
        void OnTriggerStay(Collider other)
        {
            if (kind != Kind.SlideX)
                return;
            var ball = other.GetComponent<BallController>();
            if (ball != null)
                ball.WakeFromRest();
        }

        void FixedUpdate()
        {
            if (kind == Kind.SpinY)
            {
                transform.Rotate(0f, speed * Time.fixedDeltaTime, 0f, Space.Self);
            }
            else // SlideX
            {
                float off = Mathf.Sin(Time.time * speed + _phase) * amplitude;
                Vector3 axis = transform.localRotation * Vector3.right; // 코스 진행 방향 수직(로컬 우측)
                Vector3 targetLocal = _baseLocalPos + axis * off;
                // 부모 기준 로컬 → 월드로 변환 후 MovePosition으로 이동해야
                // 닿은 공에 벽의 이동 속도가 제대로 전달돼 공이 함께 밀려난다.
                Vector3 targetWorld = transform.parent != null
                    ? transform.parent.TransformPoint(targetLocal)
                    : targetLocal;
                _rb.MovePosition(targetWorld);
            }
        }
    }
}
