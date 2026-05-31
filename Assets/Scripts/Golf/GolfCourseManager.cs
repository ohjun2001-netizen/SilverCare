// Assets/Scripts/Golf/GolfCourseManager.cs
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfCourseManager : MonoBehaviour
    {
        [Header("Imported Assets (auto-loaded)")]
        [SerializeField] GameObject flagPrefab;
        [SerializeField] GameObject windmillPrefab;
        [SerializeField] Material fairwayMaterial; // FreeSportsKIt Standard 셰이더 — 사용 가능

        // 런타임 로드 배열
        GameObject[] _treePrefabs;
        GameObject[] _bushPrefabs;
        GameObject[] _flowerPrefabs;
        GameObject   _rockPrefab;
        GameObject   _mountainPrefab;

        GameObject _current;
        CourseSpec _activeSpec;
        bool _hasActiveSpec;
        PhysicMaterial _grassPhysics;

        void Awake()
        {
            AutoLoadAssets();
        }

        void AutoLoadAssets()
        {
            // BadukRoomEnvironment 동일 방식: Resources 폴더 에셋은 Built-in 셰이더로 검증됨
            // URP 에셋(PP_, Yughues)은 AssetDatabase에서 로드해도 마젠타 → Resources 전용으로 대체
            _treePrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("BadukPark/OakTree"),
                Resources.Load<GameObject>("BadukPark/ParkTree"),
                Resources.Load<GameObject>("BadukPark/PoplarTree"),
                Resources.Load<GameObject>("StylizedNature/tree_a"),
                Resources.Load<GameObject>("StylizedNature/tree_b"),
                Resources.Load<GameObject>("StylizedNature/tree_c"),
                Resources.Load<GameObject>("StylizedNature/tree_d"),
            };
            _bushPrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("BadukPark/BushA"),
                Resources.Load<GameObject>("BadukPark/BushB"),
            };

#if UNITY_EDITOR
            // SkyBoxGradient.mat 커스텀 셰이더 → 마젠타 원인 → 미사용
            // GolfAssets CloudSpawner/Sides: URP 재질 위험 → 미사용
            if (flagPrefab == null)
                flagPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GolfAssets/Prefabs/Flag.prefab");
            if (windmillPrefab == null)
                windmillPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GolfAssets/Prefabs/InteractProps/WindMillFullColorFull.prefab");
            // Bumpers.mat / Grass.mat: URP 셰이더 → 마젠타 → 미사용
            if (fairwayMaterial == null)
                fairwayMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/FreeSportsKIt_SA/Materials/fairwaysGOlfBat.mat");
#endif
        }

        public void ShowPreviewEnvironment()
        {
            ClearCurrent();
            _current = BuildEnvironmentRoot("GolfPreview");
            BuildDrivingRange(_current.transform, GetCourseSpec(0), true);

            // 바둑 배경과 동일: skybox 미사용, SolidColor + 안개
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.84f, 0.94f);
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 28f;
            RenderSettings.ambientLight = new Color(0.80f, 0.88f, 0.82f);

            Camera cam = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.52f, 0.76f, 0.92f);
            }
        }

        public void LoadCourse(int index)
        {
            ClearCurrent();
            _hasActiveSpec = false;

            if (index < 0)
                return;

            CourseSpec spec = GetCourseSpec(index);
            _activeSpec = spec;
            _hasActiveSpec = true;

            _current = BuildEnvironmentRoot(spec.Name);
            BuildDrivingRange(_current.transform, spec, false);
            ApplyGolfMood();
        }

        public Vector3 GetTeePosition(int courseIndex)
        {
            if (_hasActiveSpec)
                return transform.TransformPoint(_activeSpec.TeeLocalPosition);

            return transform.TransformPoint(GetCourseSpec(courseIndex).TeeLocalPosition);
        }

        void ClearCurrent()
        {
            if (_current == null)
                return;

            Destroy(_current);
            _current = null;
        }

        GameObject BuildEnvironmentRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            return root;
        }

        CourseSpec GetCourseSpec(int index)
        {
            switch (index)
            {
                case 0:
                    // 쉬움: 직선, 장애물 없음
                    return new CourseSpec("Forest Practice",
                        new Vector3(0f, 0.04f, 1.6f),
                        new Vector3(0f, 0.04f, 20f),
                        0f, 23f, 10f, 0);
                case 1:
                    // 보통: 오른쪽 곡선(bend=6) + 범퍼 슬라럼
                    return new CourseSpec("Bumper Slalom",
                        new Vector3(0f, 0.04f, 1.6f),
                        new Vector3(6f, 0.04f, 25f),
                        6f, 32f, 10f, 1);
                default:
                    // 어려움: 왼쪽 강한 곡선(bend=-8) + 풍차 + 범퍼
                    return new CourseSpec("Obstacle Run",
                        new Vector3(0f, 0.04f, 1.6f),
                        new Vector3(-9f, 0.04f, 34f),
                        -8f, 42f, 10f, 2);
            }
        }

        void BuildDrivingRange(Transform parent, CourseSpec spec, bool preview)
        {
            BuildSkyDecor(parent);
            BuildGround(parent, spec);
            BuildFairway(parent, spec);
            BuildSideRails(parent, spec);
            BuildTee(parent, spec.TeeLocalPosition);
            BuildHole(parent, spec.HoleLocalPosition, spec);
            BuildTrees(parent, spec);
            BuildDecoration(parent, spec);
            BuildCourseSign(parent, spec);
            BuildImportedProps(parent, spec, preview);

            if (!preview)
                BuildYardMarkers(parent, spec);
        }

        void BuildGround(Transform parent, CourseSpec spec)
        {
            // 코스별 러프 색상: 0=밝은봄, 1=여름황록, 2=깊은숲
            Color roughColor = spec.Index == 0 ? new Color(0.44f, 0.84f, 0.32f)
                             : spec.Index == 1 ? new Color(0.50f, 0.80f, 0.28f)
                                               : new Color(0.32f, 0.64f, 0.22f);

            Vector3 groundMid = GetCurvePoint(spec, 0.5f);
            float w = spec.Width * 2.4f + Mathf.Abs(spec.CenterOffsetX) * 1.8f;
            float l = spec.Length + 18f;

            // 시각 지면 제거 — 물리 전용 지면(PhysicsGround)만 유지

            // 물리 전용 지면 — 코스 원점 기준 넓게 펼침
            // groundMid 오프셋 없이 원점(0,0) 기준 → 곡선 코스에서도 티 위치 확실히 커버
            var physGo = new GameObject("PhysicsGround");
            physGo.transform.SetParent(parent, false);
            var bc = physGo.AddComponent<BoxCollider>();
            bc.center        = new Vector3(0f, -0.05f, spec.Length * 0.5f);
            bc.size          = new Vector3(100f, 0.10f, spec.Length + 60f);
            bc.sharedMaterial = GetGrassPhysics();

        }

        void BuildFairway(Transform parent, CourseSpec spec)
        {
            Color fairwayColor = spec.Index == 0 ? new Color(0.54f, 0.94f, 0.40f)
                               : spec.Index == 1 ? new Color(0.60f, 0.90f, 0.34f)
                                                 : new Color(0.40f, 0.78f, 0.28f);
            Color stripeA = new Color(
                Mathf.Min(1f, fairwayColor.r * 1.12f),
                Mathf.Min(1f, fairwayColor.g * 1.10f),
                Mathf.Min(1f, fairwayColor.b * 1.12f));
            Color stripeB = new Color(
                fairwayColor.r * 0.86f,
                fairwayColor.g * 0.86f,
                fairwayColor.b * 0.86f);

            // 베지어 곡선으로 페어웨이 세그먼트 생성 (유일한 물리 표면)
            // 세그먼트 수 20개, 두께 0.35m, 오버랩 0.4m → 갭 없음
            const int segments = 20;
            Vector3[] pts = SampleCurve(spec, segments);

            for (int i = 0; i < segments; i++)
            {
                Vector3 a = new Vector3(pts[i].x, 0f, pts[i].z);
                Vector3 b = new Vector3(pts[i + 1].x, 0f, pts[i + 1].z);
                Vector3 segCenter = (a + b) * 0.5f + Vector3.up * 0.003f; // RoughField 위 3mm (Z-fighting 방지)
                Vector3 segDir = (b - a).normalized;
                float segLen = Vector3.Distance(a, b) + 0.12f;
                Quaternion segRot = Quaternion.LookRotation(segDir, Vector3.up);

                float segLenOverlap = segLen + 0.15f;
                // 페어웨이: 시각 전용 (keepCollider=false) — 물리는 RoughField 단독 처리
                CreatePrimitive(parent, "Fairway", PrimitiveType.Cube,
                    segCenter, segRot, new Vector3(4.8f, 0.005f, segLenOverlap), fairwayColor, false,
                    fairwayMaterial);

                // 상단 줄무늬 (시각적)
                CreatePrimitive(parent, "FairwayStripe", PrimitiveType.Cube,
                    segCenter + segRot * new Vector3(-1.2f, 0.005f, 0f), segRot,
                    new Vector3(1.0f, 0.003f, segLenOverlap), i % 2 == 0 ? stripeA : stripeB, false);
                CreatePrimitive(parent, "FairwayStripe", PrimitiveType.Cube,
                    segCenter + segRot * new Vector3(1.2f, 0.005f, 0f), segRot,
                    new Vector3(1.0f, 0.003f, segLenOverlap), i % 2 == 0 ? stripeB : stripeA, false);
            }

            // Green: 홀 위에 GreenHighlight가 덮이지 않도록 작게
            CreatePrimitive(parent, "Green", PrimitiveType.Cube,
                spec.HoleLocalPosition + Vector3.down * 0.016f, Quaternion.identity,
                new Vector3(5.0f, 0.032f, 5.0f), new Color(0.42f, 0.88f, 0.34f), true);
            CreatePrimitive(parent, "GreenHighlight", PrimitiveType.Cube,
                spec.HoleLocalPosition + Vector3.up * 0.012f, Quaternion.identity,
                new Vector3(3.5f, 0.006f, 3.5f), new Color(0.58f, 0.96f, 0.42f), false);
        }

        // 베지어 곡선 상의 t(0~1) 위치 반환
        Vector3 GetCurvePoint(CourseSpec spec, float t)
        {
            Vector3 teeXZ = new Vector3(spec.TeeLocalPosition.x, 0f, spec.TeeLocalPosition.z);
            Vector3 holeXZ = new Vector3(spec.HoleLocalPosition.x, 0f, spec.HoleLocalPosition.z);
            Vector3 ctrl = ComputeCtrl(teeXZ, holeXZ, spec.CenterOffsetX);
            Vector3 pt = QuadBezier(teeXZ, ctrl, holeXZ, t);
            float y = Mathf.Lerp(spec.TeeLocalPosition.y, spec.HoleLocalPosition.y, t);
            return new Vector3(pt.x, y, pt.z);
        }

        // t 위치에서의 진행 방향
        Vector3 GetCurveTangent(CourseSpec spec, float t)
        {
            const float dt = 0.05f;
            Vector3 a = GetCurvePoint(spec, Mathf.Max(0f, t - dt));
            Vector3 b = GetCurvePoint(spec, Mathf.Min(1f, t + dt));
            return (b - a).normalized;
        }

        Vector3[] SampleCurve(CourseSpec spec, int segments)
        {
            Vector3 teeXZ = new Vector3(spec.TeeLocalPosition.x, 0f, spec.TeeLocalPosition.z);
            Vector3 holeXZ = new Vector3(spec.HoleLocalPosition.x, 0f, spec.HoleLocalPosition.z);
            Vector3 ctrl = ComputeCtrl(teeXZ, holeXZ, spec.CenterOffsetX);
            var pts = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
                pts[i] = QuadBezier(teeXZ, ctrl, holeXZ, i / (float)segments);
            return pts;
        }

        static Vector3 QuadBezier(Vector3 p0, Vector3 ctrl, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * ctrl + t * t * p2;
        }

        static Vector3 ComputeCtrl(Vector3 from, Vector3 to, float bendAmount)
        {
            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            if (Mathf.Abs(bendAmount) < 0.001f)
                return mid;
            Vector3 dir = (to - from).normalized;
            // dir=(dx,0,dz) → 오른쪽 수직 = (dz,0,-dx)
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
            return mid + perp * bendAmount;
        }

        void BuildTee(Transform parent, Vector3 position)
        {
            // 시각 전용 (물리는 RoughField 담당)
            CreatePrimitive(parent, "TeeBox", PrimitiveType.Cube,
                new Vector3(position.x, 0.003f, position.z), Quaternion.identity,
                new Vector3(2.2f, 0.005f, 1.15f), new Color(0.66f, 0.96f, 0.46f), false);
            CreatePrimitive(parent, "TeeMat", PrimitiveType.Cube,
                new Vector3(position.x, 0.005f, position.z - 0.18f), Quaternion.identity,
                new Vector3(1.0f, 0.004f, 0.46f), new Color(0.22f, 0.68f, 0.28f), false);
        }

        void BuildHole(Transform parent, Vector3 position, CourseSpec spec)
        {
            // 공 지름 9cm × 2 = 홀 지름 18cm (반지름 0.09m)
            const float holeR = 0.09f;
            float holeY = position.y + 0.025f; // 그린 표면 위

            // ── 사실적 홀: 레이어드 실린더 ──────────────────────────
            // Layer 1: 흰 테두리 링 (가장 넓고 높음 — 컵 가장자리)
            CreatePrimitive(parent, "HoleRimOuter", PrimitiveType.Cylinder,
                new Vector3(position.x, holeY + 0.010f, position.z), Quaternion.identity,
                new Vector3(holeR * 2f + 0.08f, 0.006f, holeR * 2f + 0.08f),
                new Color(0.93f, 0.93f, 0.88f), false);

            // Layer 2: 어두운 경사 링 (흰 테두리 안쪽 — 깊이감)
            CreatePrimitive(parent, "HoleSlope", PrimitiveType.Cylinder,
                new Vector3(position.x, holeY + 0.004f, position.z), Quaternion.identity,
                new Vector3(holeR * 2f + 0.02f, 0.008f, holeR * 2f + 0.02f),
                new Color(0.08f, 0.12f, 0.06f), false);

            // Layer 3: 짙은 어둠 (컵 내벽)
            CreatePrimitive(parent, "HoleInner", PrimitiveType.Cylinder,
                new Vector3(position.x, holeY - 0.004f, position.z), Quaternion.identity,
                new Vector3(holeR * 2f, 0.012f, holeR * 2f),
                new Color(0.03f, 0.04f, 0.03f), false);

            // Layer 4: 완전 검정 중심 (컵 바닥)
            CreatePrimitive(parent, "HoleBottom", PrimitiveType.Cylinder,
                new Vector3(position.x, holeY - 0.012f, position.z), Quaternion.identity,
                new Vector3(holeR * 1.6f, 0.014f, holeR * 1.6f),
                Color.black, false);

            // ── 트리거 — 홀 반경과 동일 크기 ─────────────────────────
            var trigger = new GameObject("HoleTrigger");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = new Vector3(position.x, holeY, position.z);
            trigger.AddComponent<GolfHoleTrigger>();
            var col = trigger.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = holeR + 0.02f; // 11cm 반경 — 공이 홀 위에 오면 감지

            BuildFlagArea(parent, position);

            // ── 홀 뒤 백스톱 벽 (홀 통과 방지) ──────────────────────
            Vector3 holeDir = GetCurveTangent(spec, 0.99f).normalized;
            var backstop = new GameObject("HoleBackstop");
            backstop.transform.SetParent(parent, false);
            backstop.transform.localPosition =
                new Vector3(position.x, 0.6f, position.z) + holeDir * 2.0f;
            backstop.transform.localRotation = Quaternion.LookRotation(holeDir, Vector3.up);
            var bbc = backstop.AddComponent<BoxCollider>();
            bbc.center = Vector3.zero;
            bbc.size   = new Vector3(8f, 1.2f, 0.3f);
        }

        void BuildFlagArea(Transform parent, Vector3 position)
        {
            if (flagPrefab != null)
            {
                var flag = Instantiate(flagPrefab, parent);
                flag.name = "Flag";
                flag.transform.localPosition = position;
                flag.transform.localRotation = Quaternion.identity;
                flag.transform.localScale = Vector3.one * 0.8f;
                ApplySafeMaterial(flag, new Color(1f, 0.30f, 0.18f));
                return;
            }

            CreatePrimitive(parent, "FlagPole", PrimitiveType.Cylinder,
                position + new Vector3(0f, 1.1f, 0f), Quaternion.identity,
                new Vector3(0.035f, 1.1f, 0.035f), Color.white, false);
            CreatePrimitive(parent, "FlagBody", PrimitiveType.Quad,
                position + new Vector3(0.36f, 1.85f, 0f), Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.70f, 0.38f, 1f), new Color(1f, 0.36f, 0.20f), false);
        }

        void BuildTrees(Transform parent, CourseSpec spec)
        {
            bool hasRealTrees = _treePrefabs != null && _treePrefabs.Length > 0;
            int count = 16;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                Vector3 pt  = GetCurvePoint(spec, t);
                Vector3 tan = GetCurveTangent(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
                float side = (i % 2 == 0) ? -1f : 1f;
                float dist = 4.2f + (i % 4) * 1.1f;
                Vector3 pos = new Vector3(pt.x, 0f, pt.z) + right * (side * dist);

                if (hasRealTrees)
                {
                    var prefab = _treePrefabs[i % _treePrefabs.Length];
                    if (prefab != null)
                    {
                        var tree = Instantiate(prefab, parent);
                        tree.transform.localPosition = pos;
                        tree.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
                        tree.transform.localScale    = Vector3.one * (1.1f + (i % 3) * 0.3f);
                        // ApplySafeMaterial: URP 셰이더 포함 모든 재질을 Standard로 강제 교체
                        ApplySafeMaterial(tree, i % 2 == 0 ? new Color(0.28f, 0.52f, 0.22f) : new Color(0.35f, 0.60f, 0.25f));
                        RemoveColliders(tree);
                        continue;
                    }
                }
                BuildTree(parent, pos, 0.8f + (i % 4) * 0.12f);
            }
        }

        void BuildTree(Transform parent, Vector3 root, float scale)
        {
            CreatePrimitive(parent, "TreeTrunk", PrimitiveType.Cylinder,
                root + Vector3.up * (0.55f * scale), Quaternion.identity,
                new Vector3(0.14f * scale, 0.55f * scale, 0.14f * scale), new Color(0.50f, 0.30f, 0.13f), false);
            CreatePrimitive(parent, "TreeCrown", PrimitiveType.Sphere,
                root + Vector3.up * (1.28f * scale), Quaternion.identity,
                new Vector3(0.75f * scale, 0.58f * scale, 0.75f * scale), new Color(0.16f, 0.58f, 0.22f), false);
            CreatePrimitive(parent, "TreeCrownTop", PrimitiveType.Sphere,
                root + new Vector3(0.12f * scale, 1.62f * scale, 0.05f * scale), Quaternion.identity,
                new Vector3(0.55f * scale, 0.42f * scale, 0.55f * scale), new Color(0.24f, 0.70f, 0.28f), false);
        }

        void BuildBlossomTree(Transform parent, Vector3 root, float scale)
        {
            CreatePrimitive(parent, "BlossomTrunk", PrimitiveType.Cylinder,
                root + Vector3.up * (0.55f * scale), Quaternion.identity,
                new Vector3(0.13f * scale, 0.55f * scale, 0.13f * scale), new Color(0.48f, 0.28f, 0.14f), false);
            CreatePrimitive(parent, "BlossomCrown", PrimitiveType.Sphere,
                root + Vector3.up * (1.30f * scale), Quaternion.identity,
                new Vector3(0.86f * scale, 0.50f * scale, 0.86f * scale), new Color(0.95f, 0.58f, 0.78f), false);
        }

        void BuildDecoration(Transform parent, CourseSpec spec)
        {
            // 덤불 — Yughues Free Bushes
            bool hasBushes = _bushPrefabs != null && _bushPrefabs.Length > 0;
            for (int i = 0; i < 10; i++)
            {
                float t = (i + 0.5f) / 10f;
                Vector3 pt  = GetCurvePoint(spec, t);
                Vector3 tan = GetCurveTangent(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
                float side = (i % 2 == 0) ? -1f : 1f;
                Vector3 pos = new Vector3(pt.x, 0f, pt.z) + right * (side * 3.2f);

                if (hasBushes)
                {
                    var bush = _bushPrefabs[i % _bushPrefabs.Length];
                    if (bush != null)
                    {
                        var b = Instantiate(bush, parent);
                        b.transform.localPosition = pos;
                        b.transform.localRotation = Quaternion.Euler(0f, i * 51f, 0f);
                        b.transform.localScale    = Vector3.one * 0.9f;
                        ApplySafeMaterial(b, new Color(0.30f, 0.52f, 0.22f));
                        RemoveColliders(b);
                        continue;
                    }
                }
                // Fallback 꽃
                Color fc = i % 2 == 0 ? new Color(1f, 0.24f, 0.54f) : new Color(1f, 0.92f, 0.30f);
                CreatePrimitive(parent, "Flower", PrimitiveType.Sphere,
                    pos + Vector3.up * 0.1f, Quaternion.identity,
                    new Vector3(0.22f, 0.10f, 0.22f), fc, false);
            }

            // 바위 — Pure Poly
            if (_rockPrefab != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = (i + 1f) / 7f;
                    Vector3 pt  = GetCurvePoint(spec, t);
                    Vector3 tan = GetCurveTangent(spec, t);
                    Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
                    Vector3 pos = new Vector3(pt.x, 0f, pt.z) + right * ((i % 2 == 0 ? -1f : 1f) * 5f);
                    var rock = Instantiate(_rockPrefab, parent);
                    rock.transform.localPosition = pos;
                    rock.transform.localRotation = Quaternion.Euler(0f, i * 67f, 0f);
                    rock.transform.localScale    = Vector3.one * (0.7f + (i % 3) * 0.2f);
                    RemoveColliders(rock);
                }
            }

            // 꽃 — Pure Poly
            bool hasFlowers = _flowerPrefabs != null && _flowerPrefabs.Length > 0;
            if (hasFlowers)
            {
                for (int i = 0; i < 8; i++)
                {
                    float t = (i + 0.5f) / 8f;
                    Vector3 pt  = GetCurvePoint(spec, t);
                    Vector3 tan = GetCurveTangent(spec, t);
                    Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
                    Vector3 pos = new Vector3(pt.x, 0f, pt.z) + right * ((i % 2 == 0 ? -1f : 1f) * 2.6f);
                    var flower = _flowerPrefabs[i % _flowerPrefabs.Length];
                    if (flower != null)
                    {
                        var f = Instantiate(flower, parent);
                        f.transform.localPosition = pos;
                        f.transform.localRotation = Quaternion.Euler(0f, i * 55f, 0f);
                        f.transform.localScale    = Vector3.one * 0.65f;
                        RemoveColliders(f);
                    }
                }
            }
        }

        void BuildCourseSign(Transform parent, CourseSpec spec)
        {
            Vector3 pos = new Vector3(-spec.Width * 0.45f, 0.45f, spec.TeeLocalPosition.z + 2.5f);
            CreatePrimitive(parent, "CourseSignBoard", PrimitiveType.Cube,
                pos, Quaternion.Euler(0f, 10f, 0f),
                new Vector3(1.6f, 0.42f, 0.06f), new Color(0.08f, 0.23f, 0.12f), false);
            CreatePrimitive(parent, "CourseSignPostL", PrimitiveType.Cylinder,
                pos + new Vector3(-0.55f, -0.42f, 0f), Quaternion.identity,
                new Vector3(0.035f, 0.45f, 0.035f), new Color(0.22f, 0.18f, 0.12f), false);
            CreatePrimitive(parent, "CourseSignPostR", PrimitiveType.Cylinder,
                pos + new Vector3(0.55f, -0.42f, 0f), Quaternion.identity,
                new Vector3(0.035f, 0.45f, 0.035f), new Color(0.22f, 0.18f, 0.12f), false);
        }

        void BuildSideRails(Transform parent, CourseSpec spec)
        {
            // 코스 양옆 측벽: 임포트 Sides.prefab 또는 프리미티브
            const int railSegs = 14;
            // 필드 가장 바깥 — 페어웨이(±2.4m)보다 훨씬 바깥에 배치
            float railHalfWidth = spec.Width * 0.55f; // Width=10 → 5.5m
            Color railColor = new Color(0.78f, 0.54f, 0.28f);

            for (int i = 0; i < railSegs; i++)
            {
                float t0 = i / (float)railSegs;
                float t1 = (i + 1f) / railSegs;
                Vector3 a = GetCurvePoint(spec, t0);
                Vector3 b = GetCurvePoint(spec, t1);
                Vector3 segDir = (new Vector3(b.x, 0f, b.z) - new Vector3(a.x, 0f, a.z)).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, segDir).normalized;
                Vector3 mid = (a + b) * 0.5f;
                float segLen = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z)) + 0.15f;
                Quaternion rot = Quaternion.LookRotation(segDir, Vector3.up);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 railBase = new Vector3(mid.x, 0f, mid.z) + right * (side * railHalfWidth);

                    // 시각적 울타리: 1.2m 높이
                    CreatePrimitive(parent, "SideRailVisual", PrimitiveType.Cube,
                        railBase + Vector3.up * 0.6f, rot,
                        new Vector3(0.18f, 1.2f, segLen), railColor, false);

                    // 물리 벽: 5m — 공이 절대 넘지 못함 (wallGo를 railBase에 배치 후 center=(0,2.5,0))
                    var wallGo = new GameObject("SideRailPhysics");
                    wallGo.transform.SetParent(parent, false);
                    wallGo.transform.localPosition = railBase;
                    wallGo.transform.localRotation = rot;
                    var wallBc = wallGo.AddComponent<BoxCollider>();
                    wallBc.center = new Vector3(0f, 2.5f, 0f);
                    wallBc.size   = new Vector3(0.25f, 5.0f, segLen);
                }
            }
        }

        void BuildYardMarkers(Transform parent, CourseSpec spec)
        {
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                Vector3 pos = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                CreatePrimitive(parent, "YardMarker", PrimitiveType.Cylinder,
                    new Vector3(pos.x, 0.08f, pos.z) + right * 1.8f, Quaternion.identity,
                    new Vector3(0.16f, 0.08f, 0.16f), new Color(0.94f, 0.92f, 0.72f), false);
            }
        }

        void BuildSkyDecor(Transform parent)
        {
            // GolfAssets의 모든 재질이 URP → cloudSpawner 포함 모두 미사용
            // 배경은 카메라 SolidColor + 안개로만 처리
        }

        void ApplyGolfMood()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.70f, 0.84f, 0.96f);
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 140f;
            RenderSettings.ambientLight = new Color(0.82f, 0.90f, 0.85f);
            RenderSettings.ambientIntensity = 1.1f;

            Camera cam = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.52f, 0.76f, 0.92f);
            }
        }

        void BuildImportedProps(Transform parent, CourseSpec spec, bool preview)
        {
            if (preview)
                return;

            switch (spec.Index)
            {
                case 0:
                    // 쉬움: 장애물 없음
                    break;

                case 1:
                    // 보통: 벽 3개 좌우 교대 슬라럼
                    // 음수 = 왼쪽 벽(오른쪽 통로), 양수 = 오른쪽 벽(왼쪽 통로)
                    SpawnBumper(parent, spec, 0.28f, -1f, new Color(0.85f, 0.18f, 0.14f));
                    SpawnBumper(parent, spec, 0.53f, +1f, new Color(0.85f, 0.18f, 0.14f));
                    SpawnBumper(parent, spec, 0.77f, -1f, new Color(0.85f, 0.18f, 0.14f));
                    break;

                case 2:
                    // 어려움: 풍차 1개 + 범퍼 5개 (전반·후반 분리)
                    if (windmillPrefab != null)
                    {
                        Vector3 windPos = GetCurvePoint(spec, 0.50f);
                        Vector3 windTangent = GetCurveTangent(spec, 0.50f);
                        var windmill = Instantiate(windmillPrefab, parent);
                        windmill.name = "ImportedWindmill";
                        windmill.transform.localPosition = new Vector3(windPos.x, 0.04f, windPos.z);
                        windmill.transform.localRotation = Quaternion.LookRotation(windTangent, Vector3.up);
                        windmill.transform.localScale = Vector3.one * 0.85f;
                        ApplySafeMaterial(windmill, new Color(0.72f, 0.52f, 0.28f));
                        RemoveColliders(windmill);
                    }

                    // 전반: 풍차 전 2개
                    SpawnBumper(parent, spec, 0.20f, -1f, new Color(0.14f, 0.38f, 0.88f));
                    SpawnBumper(parent, spec, 0.36f, +1f, new Color(0.14f, 0.38f, 0.88f));
                    // 풍차 직전: 한쪽만 열어둠
                    SpawnBumper(parent, spec, 0.44f, +1f, new Color(0.88f, 0.48f, 0.06f));
                    // 후반: 풍차 후 2개
                    SpawnBumper(parent, spec, 0.65f, -1f, new Color(0.14f, 0.38f, 0.88f));
                    SpawnBumper(parent, spec, 0.82f, +1f, new Color(0.14f, 0.38f, 0.88f));
                    break;
            }
        }

        void SpawnBumper(Transform parent, CourseSpec spec, float ratio, float sideOffset, Color color)
        {
            Vector3 curvePos = GetCurvePoint(spec, ratio);
            Vector3 tangent  = GetCurveTangent(spec, ratio);
            Vector3 right    = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 basePos  = new Vector3(curvePos.x, 0f, curvePos.z);
            Quaternion wallRot = Quaternion.LookRotation(tangent, Vector3.up);

            // sideOffset 부호로 방향 결정
            // + → 오른쪽 절반 막음 (왼쪽 통로)
            // - → 왼쪽 절반 막음 (오른쪽 통로)
            float wallDir = sideOffset >= 0f ? 1f : -1f;

            // 벽: 페어웨이 절반(3m) 차지, 높이 2.2m — 명확히 시야를 막는 크기
            Vector3 wallCenter = basePos + right * (wallDir * 1.5f) + Vector3.up * 1.1f;
            var wall = CreatePrimitive(parent, "BumperWall", PrimitiveType.Cube,
                wallCenter, wallRot, new Vector3(3.0f, 2.2f, 0.55f), color, true);
                // bumpersMaterial은 URP → 마젠타, Standard 색상 직접 사용

            var col = wall.GetComponent<Collider>();
            if (col != null)
            {
                var bmat = new PhysicMaterial("BumperPhys");
                bmat.bounciness        = 0.65f;
                bmat.dynamicFriction   = 0.08f;
                bmat.bounceCombine     = PhysicMaterialCombine.Maximum;
                bmat.frictionCombine   = PhysicMaterialCombine.Minimum;
                col.sharedMaterial = bmat;
            }

            // 상단 흰 줄무늬
            CreatePrimitive(parent, "BumperWallTop", PrimitiveType.Cube,
                wallCenter + Vector3.up * 1.2f, wallRot,
                new Vector3(3.1f, 0.28f, 0.62f), Color.white * 0.92f, false);

            // 수직 줄무늬 — 장애물 존재감 강조
            for (int s = -1; s <= 1; s += 2)
            {
                CreatePrimitive(parent, "BumperStripe", PrimitiveType.Cube,
                    wallCenter + wallRot * new Vector3(s * 0.9f, 0f, 0f), wallRot,
                    new Vector3(0.22f, 2.25f, 0.60f), Color.white * 0.85f, false);
            }

            // 통로 방향 노란 바닥 화살표
            float gapDir = -wallDir;
            CreatePrimitive(parent, "BumperArrow", PrimitiveType.Cube,
                basePos + right * (gapDir * 0.9f) + Vector3.up * 0.014f, wallRot,
                new Vector3(1.0f, 0.01f, 0.9f), new Color(1f, 0.88f, 0.1f), false);
        }

        static void ApplySafeMaterial(GameObject root, Color fallbackColor)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.bounds.size.magnitude > 18f)
                {
                    renderer.enabled = false;
                    continue;
                }

                var mat = new Material(Shader.Find("Standard"));
                mat.color = fallbackColor;
                mat.SetFloat("_Glossiness", 0.08f);
                renderer.sharedMaterial = mat;
            }
        }

        static void RemoveColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(collider);
        }

        GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPos,
            Quaternion localRot, Vector3 localScale, Color color, bool keepCollider, Material overrideMaterial = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (overrideMaterial != null)
                {
                    renderer.sharedMaterial = overrideMaterial;
                }
                else
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = color;
                    mat.SetFloat("_Glossiness", 0.05f);
                    renderer.sharedMaterial = mat;
                }
            }

            var defaultCollider = go.GetComponent<Collider>();
            if (defaultCollider != null && !keepCollider)
                DestroyImmediate(defaultCollider);
            else if (defaultCollider != null && keepCollider)
                defaultCollider.sharedMaterial = GetGrassPhysics();

            return go;
        }

        PhysicMaterial GetGrassPhysics()
        {
            if (_grassPhysics != null)
                return _grassPhysics;

            _grassPhysics = new PhysicMaterial("GolfGrassPhysics");
            _grassPhysics.dynamicFriction = 0.82f;
            _grassPhysics.staticFriction = 0.90f;
            _grassPhysics.bounciness = 0.04f;
            _grassPhysics.frictionCombine = PhysicMaterialCombine.Maximum;
            _grassPhysics.bounceCombine = PhysicMaterialCombine.Minimum;
            return _grassPhysics;
        }

        readonly struct CourseSpec
        {
            public readonly string Name;
            public readonly Vector3 TeeLocalPosition;
            public readonly Vector3 HoleLocalPosition;
            public readonly float CenterOffsetX;
            public readonly float Length;
            public readonly float Width;
            public readonly int Index;

            public CourseSpec(string name, Vector3 teeLocalPosition, Vector3 holeLocalPosition,
                float centerOffsetX, float length, float width, int index)
            {
                Name = name;
                TeeLocalPosition = teeLocalPosition;
                HoleLocalPosition = holeLocalPosition;
                CenterOffsetX = centerOffsetX;
                Length = length;
                Width = width;
                Index = index;
            }
        }
    }
}
