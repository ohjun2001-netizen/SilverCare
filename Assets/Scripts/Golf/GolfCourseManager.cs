// Assets/Scripts/Golf/GolfCourseManager.cs
// 담당: 양석원
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfCourseManager : MonoBehaviour
    {
        private GameObject _current;

        // 코스 로드 (이전 코스 파괴 후 신규 생성)
        public void LoadCourse(int index)
        {
            if (_current != null) Destroy(_current);
            if (index < 0) return; // -1 = 파괴만

            _current = index switch
            {
                0 => BuildCourse("Course1", 10f, 2f,  9f,  null),
                1 => BuildCourse("Course2", 15f, 2.5f, 13.5f, new[] {
                        new Obs(new Vector3(-0.3f, 0.5f, 7f),  new Vector3(1.2f, 1f, 0.2f)) }),
                2 => BuildCourse("Course3", 20f, 3f,  18.5f, new[] {
                        new Obs(new Vector3(-0.5f, 0.5f, 8f),  new Vector3(1.4f, 1f, 0.2f)),
                        new Obs(new Vector3( 0.5f, 0.5f, 14f), new Vector3(1.4f, 1f, 0.2f)) }),
                _ => null
            };

            Debug.Log($"[Golf] 코스 {index + 1} 생성");
        }

        // 공 초기 위치 (티 위, 약간 들어올림)
        public Vector3 GetTeePosition(int courseIndex) =>
            transform.TransformPoint(new Vector3(0f, 0.08f, 0.5f));

        // ── 코스 빌더 ────────────────────────────────────────────────
        GameObject BuildCourse(string name, float len, float wid, float holeZ, Obs[] obs)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);

            // 바닥 (잔디)
            var ground = Prim(PrimitiveType.Plane, root.transform, "Ground",
                new Vector3(0f, 0f, len * 0.5f),
                new Vector3(wid / 10f, 1f, len / 10f));
            SetColor(ground, new Color(0.2f, 0.55f, 0.15f));

            var grassPM = new PhysicMaterial("GrassMat");
            grassPM.dynamicFriction = 0.4f;
            grassPM.staticFriction  = 0.45f;
            grassPM.bounciness      = 0f;
            grassPM.frictionCombine = PhysicMaterialCombine.Average;
            grassPM.bounceCombine   = PhysicMaterialCombine.Minimum;
            ground.GetComponent<Collider>().sharedMaterial = grassPM;

            // 경계벽 4면
            Wall(root, "WallL",    new Vector3(-wid * 0.5f - 0.1f, 0.3f, len * 0.5f), new Vector3(0.2f, 0.6f, len));
            Wall(root, "WallR",    new Vector3( wid * 0.5f + 0.1f, 0.3f, len * 0.5f), new Vector3(0.2f, 0.6f, len));
            Wall(root, "WallBack", new Vector3(0f, 0.3f, len + 0.1f),                 new Vector3(wid + 0.4f, 0.6f, 0.2f));

            // 티 마커 (노랑)
            var tee = Prim(PrimitiveType.Cylinder, root.transform, "Tee",
                new Vector3(0f, 0.005f, 0.5f), new Vector3(0.5f, 0.005f, 0.5f));
            SetColor(tee, new Color(1f, 0.9f, 0.1f));
            Destroy(tee.GetComponent<Collider>());

            // 홀 + 깃발
            BuildHole(root.transform, new Vector3(0f, 0f, holeZ));

            // 장애물
            if (obs != null)
                foreach (var o in obs) Obstacle(root, o.Pos, o.Size);

            return root;
        }

        void BuildHole(Transform parent, Vector3 localPos)
        {
            // 검정 원 (시각)
            var disc = Prim(PrimitiveType.Cylinder, parent, "HoleDisc",
                localPos + Vector3.up * 0.002f, new Vector3(0.3f, 0.002f, 0.3f));
            SetColor(disc, Color.black);
            Destroy(disc.GetComponent<Collider>());

            // 트리거 (볼 감지)
            var trigGO = new GameObject("HoleTrigger");
            trigGO.transform.SetParent(parent, false);
            trigGO.transform.localPosition = localPos + Vector3.up * 0.12f;
            trigGO.AddComponent<GolfHoleTrigger>();
            var col = trigGO.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.16f;
            col.height = 0.3f;

            // 깃대 + 깃발
            var flagRoot = new GameObject("Flag");
            flagRoot.transform.SetParent(parent, false);
            flagRoot.transform.localPosition = localPos;

            var pole = Prim(PrimitiveType.Cylinder, flagRoot.transform, "Pole",
                new Vector3(0f, 0.7f, 0f), new Vector3(0.03f, 0.7f, 0.03f));
            SetColor(pole, Color.white);
            Destroy(pole.GetComponent<Collider>());

            var flag = Prim(PrimitiveType.Quad, flagRoot.transform, "Flag",
                new Vector3(0.22f, 1.28f, 0f), new Vector3(0.4f, 0.22f, 1f));
            SetColor(flag, Color.red);
            Destroy(flag.GetComponent<Collider>());
        }

        void Wall(GameObject parent, string n, Vector3 pos, Vector3 size)
        {
            var w = Prim(PrimitiveType.Cube, parent.transform, n, pos, size);
            SetColor(w, new Color(0.75f, 0.55f, 0.25f));
        }

        void Obstacle(GameObject parent, Vector3 pos, Vector3 size)
        {
            var o = Prim(PrimitiveType.Cube, parent.transform, "Obstacle", pos, size);
            SetColor(o, new Color(0.7f, 0.2f, 0.2f));
        }

        // ── 유틸 ────────────────────────────────────────────────────
        static GameObject Prim(PrimitiveType type, Transform parent, string name,
            Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            return go;
        }

        static void SetColor(GameObject go, Color c)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = c;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        readonly struct Obs
        {
            public readonly Vector3 Pos;
            public readonly Vector3 Size;
            public Obs(Vector3 p, Vector3 s) { Pos = p; Size = s; }
        }
    }
}
