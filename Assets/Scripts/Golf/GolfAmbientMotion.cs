// Assets/Scripts/Golf/GolfAmbientMotion.cs
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.Golf
{
    /// <summary>
    /// 한국식 파크 골프 코스의 가벼운 분위기 연출을 한 컴포넌트에서 처리한다.
    /// - 새: 하늘을 원형으로 천천히 돌며 상하로 부드럽게 떠다닌다.
    /// - 나무: 크라운을 산들바람처럼 미세하게 흔든다.
    /// 인스턴스마다 스크립트를 붙이지 않고, 환경 루트의 단일 Update 에서 모두 갱신해 Quest 2 부하를 최소화한다.
    /// </summary>
    public class GolfAmbientMotion : MonoBehaviour
    {
        struct Bird
        {
            public Transform t;
            public Vector3 center;
            public float radius;
            public float angle;     // 현재 각도(rad)
            public float angSpeed;  // 각속도(rad/s)
            public float bobAmp;
            public float bobSpeed;
            public float bobPhase;
        }

        struct Sway
        {
            public Transform t;
            public Quaternion baseRot;
            public float amp;       // 도(degree)
            public float speed;
            public float phase;
        }

        readonly List<Bird> _birds = new List<Bird>();
        readonly List<Sway> _sways = new List<Sway>();

        public void AddBird(Transform t, Vector3 center, float radius, float angSpeed,
            float startAngle, float bobAmp, float bobSpeed)
        {
            if (t == null) return;
            _birds.Add(new Bird
            {
                t = t,
                center = center,
                radius = radius,
                angle = startAngle,
                angSpeed = angSpeed,
                bobAmp = bobAmp,
                bobSpeed = bobSpeed,
                bobPhase = Random.value * Mathf.PI * 2f,
            });
        }

        public void AddSway(Transform t, float amplitudeDeg, float speed)
        {
            if (t == null) return;
            _sways.Add(new Sway
            {
                t = t,
                baseRot = t.localRotation,
                amp = amplitudeDeg,
                speed = speed,
                phase = Random.value * Mathf.PI * 2f,
            });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            for (int i = 0; i < _birds.Count; i++)
            {
                Bird b = _birds[i];
                if (b.t == null) continue;

                b.angle += b.angSpeed * dt;
                float x = b.center.x + Mathf.Cos(b.angle) * b.radius;
                float z = b.center.z + Mathf.Sin(b.angle) * b.radius;
                float y = b.center.y + Mathf.Sin(time * b.bobSpeed + b.bobPhase) * b.bobAmp;
                b.t.localPosition = new Vector3(x, y, z);

                // 진행 방향을 바라보게(접선 방향)
                Vector3 tangent = new Vector3(-Mathf.Sin(b.angle), 0f, Mathf.Cos(b.angle)) * Mathf.Sign(b.angSpeed);
                if (tangent.sqrMagnitude > 0.0001f)
                    b.t.localRotation = Quaternion.LookRotation(tangent, Vector3.up);

                _birds[i] = b;
            }

            for (int i = 0; i < _sways.Count; i++)
            {
                Sway s = _sways[i];
                if (s.t == null) continue;

                float angle = Mathf.Sin(time * s.speed + s.phase) * s.amp;
                s.t.localRotation = s.baseRot * Quaternion.Euler(angle, 0f, angle * 0.5f);
            }
        }
    }
}
