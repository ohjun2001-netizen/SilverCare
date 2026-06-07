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
        GameObject[] _wideSceneryPrefabs;
        GameObject   _rockPrefab;
        GameObject   _mountainPrefab;

        // 한국식 파크 추가 에셋 (Resources/GolfPark/ 우선, 에디터 폴백)
        GameObject[] _pathPrefabs;
        GameObject[] _bridgePrefabs;
        GameObject   _pondPrefab;
        GameObject   _benchPrefab;
        GameObject   _firTreePrefab;
        GameObject   _fencePrefab;

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
                Resources.Load<GameObject>("StylizedNature/tree_e"),
                Resources.Load<GameObject>("StylizedNature/tree_f"),
                Resources.Load<GameObject>("StylizedNature/tree_g"),
                Resources.Load<GameObject>("StylizedNature/tree_h"),
                Resources.Load<GameObject>("StylizedNature/tree_i"),
                Resources.Load<GameObject>("StylizedNature/tree_j"),
                Resources.Load<GameObject>("StylizedNature/tree_k"),
            };
            _bushPrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("BadukPark/BushA"),
                Resources.Load<GameObject>("BadukPark/BushB"),
            };
            _wideSceneryPrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("SelectionBackdrop/PP_Birch_Tree_05"),
                Resources.Load<GameObject>("SelectionBackdrop/PP_Tree_10"),
                Resources.Load<GameObject>("SelectionBackdrop/PP_Rock_Moss_Grown_11"),
                Resources.Load<GameObject>("SelectionBackdrop/PP_Grass_15"),
                Resources.Load<GameObject>("SelectionBackdrop/PP_Meadow_07"),
            };

            const string PP = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/";
            _flowerPrefabs = new GameObject[]
            {
                LoadProp("GolfPark/PP_Daffodil_03",  PP + "PP_Daffodil_03.prefab"),
                LoadProp("GolfPark/PP_Sunflower_04", PP + "PP_Sunflower_04.prefab"),
                LoadProp("GolfPark/PP_Hyacinth_04",  PP + "PP_Hyacinth_04.prefab"),
            };
            _pathPrefabs = new GameObject[]
            {
                LoadProp("GolfPark/PP_Meadow_Path_05", PP + "PP_Meadow_Path_05.prefab"),
                LoadProp("GolfPark/PP_Floor_Tile_05",  PP + "PP_Floor_Tile_05.prefab"),
                LoadProp("GolfPark/PP_Floor_Tile_15",  PP + "PP_Floor_Tile_15.prefab"),
            };
            _bridgePrefabs = new GameObject[]
            {
                LoadProp("GolfPark/PP_Bridge_15_Left",   PP + "PP_Bridge_15_Left.prefab"),
                LoadProp("GolfPark/PP_Bridge_15_Middle", PP + "PP_Bridge_15_Middle.prefab"),
                LoadProp("GolfPark/PP_Bridge_15_Right",  PP + "PP_Bridge_15_Right.prefab"),
            };
            _pondPrefab     = LoadProp("GolfPark/PP_Lake_Ground_04", PP + "PP_Lake_Ground_04.prefab");
            _fencePrefab    = LoadProp("GolfPark/PP_Small_Fence_01", PP + "PP_Small_Fence_01.prefab");
            _firTreePrefab  = LoadProp("GolfPark/Fir_Tree", "Assets/Darth_Artisan/Free_Trees/Prefabs/Fir_Tree.prefab");
            _benchPrefab    = LoadProp("GolfPark/ChairA", "Assets/Parks And Nature Pack/Prefab/ChairA.prefab")
                              ?? Resources.Load<GameObject>("BadukPark/ParkChair");
            _rockPrefab     = LoadProp("GolfPark/PP_Rock_Pile_05", PP + "PP_Rock_Pile_Forest_Moss_05.prefab")
                              ?? Resources.Load<GameObject>("SelectionBackdrop/PP_Rock_Moss_Grown_11");
            _mountainPrefab = Resources.Load<GameObject>("StylizedNature/mountain");

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

        // Resources(빌드+에디터) 우선, 없으면 에디터 전용 AssetDatabase 폴백.
        // 신규 프리팹은 GolfParkResourceSetup 가 Resources/GolfPark/ 로 복사하므로 빌드에서도 로드된다.
        GameObject LoadProp(string resourcesPath, string editorPath = null)
        {
            var go = Resources.Load<GameObject>(resourcesPath);
#if UNITY_EDITOR
            if (go == null && !string.IsNullOrEmpty(editorPath))
                go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
#endif
            return go;
        }

        public void ShowPreviewEnvironment()
        {
            ClearCurrent();
            _current = BuildEnvironmentRoot("GolfPreview");

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

        void BuildPreviewLandscape(Transform parent)
        {
            Color grass = new Color(0.42f, 0.78f, 0.30f);
            Color fair = new Color(0.55f, 0.90f, 0.36f);

            var ground = CreatePrimitive(parent, "GolfPreviewGround", PrimitiveType.Cube,
                new Vector3(0f, -0.020f, 14f), Quaternion.identity,
                new Vector3(78f, 0.026f, 56f), grass, false);
            ApplyGeneratedTexture(ground, CreateGrassTexture(grass, new Color(0.31f, 0.66f, 0.22f)));

            CreatePrimitive(parent, "GolfPreviewFairway", PrimitiveType.Cube,
                new Vector3(0f, 0.002f, 15f), Quaternion.identity,
                new Vector3(8.5f, 0.010f, 32f), fair, false, fairwayMaterial);
            CreatePrimitive(parent, "GolfPreviewGreen", PrimitiveType.Cylinder,
                new Vector3(0f, 0.012f, 28f), Quaternion.identity,
                new Vector3(6.5f, 0.012f, 6.5f), new Color(0.46f, 0.88f, 0.34f), false);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 7; i++)
                {
                    float x = side * (13f + (i % 3) * 3.2f);
                    float z = 0f + i * 5.0f;
                    SpawnWideSceneryPrefab(parent, PickSceneryPrefab(0, i), new Vector3(x, 0f, z),
                        1.05f + (i % 3) * 0.15f, new Color(0.24f, 0.56f, 0.22f),
                        Quaternion.Euler(0f, i * 41f, 0f));
                }
            }

            for (int i = -4; i <= 4; i++)
            {
                float x = i * 5.8f;
                SpawnWideSceneryPrefab(parent, PickSceneryPrefab(2, Mathf.Abs(i)), new Vector3(x, 0f, 37f),
                    1.05f, new Color(0.38f, 0.56f, 0.30f), Quaternion.Euler(0f, i * 30f, 0f));
            }

            BuildCloud(parent, new Vector3(-6f, 7.5f, 18f), 1.2f);
            BuildCloud(parent, new Vector3(5f, 8.2f, 30f), 1.0f);
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

        public Vector3 GetHoleWorldPosition(int courseIndex)
        {
            Vector3 holeLocal = _hasActiveSpec
                ? _activeSpec.HoleLocalPosition
                : GetCourseSpec(courseIndex).HoleLocalPosition;
            return transform.TransformPoint(holeLocal);
        }

        public Vector3 GetTeeMatCenterPosition(int courseIndex)
        {
            if (_current != null)
            {
                var teeMat = _current.transform.Find("TeeMat");
                var renderer = teeMat != null ? teeMat.GetComponent<Renderer>() : null;
                if (renderer != null)
                {
                    Bounds bounds = renderer.bounds;
                    return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                }
            }

            Vector3 teePosition = _hasActiveSpec
                ? _activeSpec.TeeLocalPosition
                : GetCourseSpec(courseIndex).TeeLocalPosition;

            return transform.TransformPoint(GetTeeMatLocalPosition(teePosition));
        }

        void ClearCurrent()
        {
            if (_current == null)
                return;

            // 즉시 제거 — 다시 하기/재배치 시 이전 코스가 한 프레임 겹쳐 보이는 잔상 방지.
            DestroyImmediate(_current);
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
                    // 보통: 완만한 경사가 있는 S자 통나무 미니골프 코스
                    return new CourseSpec("Park Mini Golf",
                        new Vector3(0f, 0.04f, 1.6f),
                        new Vector3(-1.8f, 0.12f, 24f),
                        4.8f, 29f, 10f, 1);
                default:
                    // 어려움: 길고 S자로 굽이치는 코스 + 큰 언덕/내리막 + 장애물 다수
                    return new CourseSpec("Hill Golf",
                        new Vector3(0f, 0.04f, 1.6f),
                        new Vector3(0f, 0.22f, 38f),
                        -2.5f, 44f, 10f, 2);
            }
        }

        void BuildDrivingRange(Transform parent, CourseSpec spec, bool preview)
        {
            BuildSkyDecor(parent);
            BuildTerrainMesh(parent, spec);
            BuildParkGroundVisual(parent, spec);
            BuildKoreanPark(parent, spec);
            BuildSideRails(parent, spec);
            BuildTee(parent, spec.TeeLocalPosition);
            BuildHole(parent, spec.HoleLocalPosition, spec);
            BuildCourseObstacles(parent, spec);
            VisualizeBlockers(parent);
        }

        [Header("Debug")]
        [Tooltip("켜면 공을 막는 모든 콜라이더를 반투명 빨강으로 표시 — 보이지 않는 벽 위치 확인용")]
        [SerializeField] bool showBlockerDebug = false;

        // 공을 막는 모든 콜라이더(트리거·지면 제외)를 반투명 빨강 박스로 덮어 위치를 보이게 한다.
        // 시각 메시 없이 빨강만 떠 있는 곳 = 보이지 않는 벽. 사용자가 지목하면 해당 콜라이더를 정리한다.
        void VisualizeBlockers(Transform parent)
        {
            if (!showBlockerDebug)
                return;

            var mat = MakeTranslucent(new Color(1f, 0.12f, 0.12f, 0.38f));
            var colliders = parent.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    continue;
                string n = col.gameObject.name;
                if (n.Contains("PhysicsGround") || n.Contains("Terrain") || n.Contains("SafetyNet") || n.Contains("BlockerDebug"))
                    continue;
                if (col is MeshCollider) // 지형/메시 콜라이더 제외
                    continue;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "BlockerDebug";
                var mc = marker.GetComponent<Collider>();
                if (mc != null)
                    DestroyImmediate(mc);
                marker.GetComponent<Renderer>().sharedMaterial = mat;
                marker.transform.SetParent(col.transform, false);

                if (col is BoxCollider box)
                {
                    marker.transform.localPosition = box.center;
                    marker.transform.localRotation = Quaternion.identity;
                    marker.transform.localScale = box.size * 1.03f;
                }
                else if (col is SphereCollider sph)
                {
                    marker.transform.localPosition = sph.center;
                    marker.transform.localScale = Vector3.one * (sph.radius * 2f * 1.03f);
                }
                else if (col is CapsuleCollider cap)
                {
                    marker.transform.localPosition = cap.center;
                    float d = cap.radius * 2f;
                    marker.transform.localScale = new Vector3(d, cap.height, d) * 1.03f;
                }
                else
                {
                    DestroyImmediate(marker);
                }
            }
        }

        // 골프장 디테일(시각). 잔디 지면 본체는 BuildTerrainMesh(굴곡 메시)가 담당하므로 여기선 평평 큐브 없음.
        void BuildParkGroundVisual(Transform parent, CourseSpec spec)
        {
            // 코스별 러프 톤
            Color roughColor = spec.Index == 0 ? new Color(0.40f, 0.78f, 0.30f)
                             : spec.Index == 1 ? new Color(0.46f, 0.75f, 0.27f)
                                               : new Color(0.36f, 0.70f, 0.28f);

            // (러프 색 얼룩 RoughPatch 제거 — 바닥에 원형 윤곽처럼 보여 거슬림)

            // 3) 페어웨이(곡선) — 가장자리 + 밝고/어두운 교차 mow 스트라이프
            Color fairBright = spec.Index == 0 ? new Color(0.60f, 0.92f, 0.42f)
                             : spec.Index == 1 ? new Color(0.62f, 0.88f, 0.38f)
                                               : new Color(0.54f, 0.84f, 0.36f);
            Color fairDark = new Color(fairBright.r * 0.86f, fairBright.g * 0.86f, fairBright.b * 0.86f);
            Color fairEdge = new Color(fairBright.r * 0.82f, fairBright.g * 0.84f, fairBright.b * 0.74f);

            // 페어웨이를 곡선 높이(GetCurvePoint)에 깔아 경사 지면 위에 정합시킨다.
            // 단, 그린(원형) 영역에는 깔지 않아 페어웨이 띠가 원형 윤곽을 가리지 않게 한다.
            const int fseg = 24;
            const float greenClear = 4.0f; // 그린 에이프런 반경(3.5) 바깥 — 페어웨이가 그린을 침범하지 않게
            for (int i = 0; i < fseg; i++)
            {
                Vector3 a = GetCurvePoint(spec, i / (float)fseg);
                Vector3 b = GetCurvePoint(spec, (i + 1) / (float)fseg);
                Vector3 c = (a + b) * 0.5f;

                float dxh = c.x - spec.HoleLocalPosition.x;
                float dzh = c.z - spec.HoleLocalPosition.z;
                if (dxh * dxh + dzh * dzh < greenClear * greenClear)
                    continue; // 그린 원형 보존

                c.y = TerrainHeight(spec, c.x, c.z); // 실제 지형 높이를 따라 떠 보이지 않게

                Vector3 dir = b - a;
                if (dir.sqrMagnitude < 1e-5f) dir = Vector3.forward;
                dir.Normalize();
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                float len = Vector3.Distance(a, b) + 0.25f;

                // 물리 지면 윗면(표면)에 바짝 붙여 공이 잔디 위에 자연스럽게 놓이게 한다.
                CreatePrimitive(parent, "FairwayEdge", PrimitiveType.Cube,
                    c + Vector3.up * 0.002f, rot, new Vector3(5.4f, 0.005f, len), fairEdge, false);
                CreatePrimitive(parent, "FairwayMow", PrimitiveType.Cube,
                    c + Vector3.up * 0.005f, rot, new Vector3(4.6f, 0.006f, len),
                    i % 2 == 0 ? fairBright : fairDark, false);
            }

            // 4) 그린 — 별도의 매끄러운 원형 메시 디스크(에이프런 + 퍼팅그린). 홀 레이어보다 아래.
            float greenY = spec.HoleLocalPosition.y;
            Vector3 greenCenter = new Vector3(spec.HoleLocalPosition.x, greenY, spec.HoleLocalPosition.z);
            BuildGreenDisc(parent, "GreenApron", greenCenter + Vector3.up * 0.008f, 3.5f,
                new Color(0.48f, 0.84f, 0.38f), physics: true);
            BuildGreenDisc(parent, "PuttingGreen", greenCenter + Vector3.up * 0.012f, 2.2f,
                new Color(0.60f, 0.93f, 0.48f));

            // (모래 벙커 제거 — 사각 콜라이더가 가장자리에서 공을 막고 시각도 거슬려 삭제)

            // 6) 클로버·들꽃 점 — 러프에 산포(페어웨이는 피함, 지면 높이 정합)
            for (int i = 0; i < 26; i++)
            {
                float t = (i + 0.5f) / 26f;
                Vector3 pt = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                float side = (i % 2 == 0) ? -1f : 1f;
                float dist = 4.2f + (i % 5) * 1.3f; // 페어웨이(±2.3m) 바깥
                Vector3 pos = RoughPoint(spec, pt, right, side * dist) + Vector3.up * 0.04f;

                float fgdx = pos.x - spec.HoleLocalPosition.x;
                float fgdz = pos.z - spec.HoleLocalPosition.z;
                if (fgdx * fgdx + fgdz * fgdz < 4.0f * 4.0f)
                    continue; // 그린 영역엔 들꽃 미배치

                Color dot = (i % 4 == 0) ? new Color(1f, 0.95f, 0.55f)
                          : (i % 4 == 1) ? new Color(0.96f, 0.96f, 0.98f)
                                         : new Color(roughColor.r * 1.15f, roughColor.g * 1.10f, roughColor.b * 0.90f);
                CreatePrimitive(parent, "GroundFlowerDot", PrimitiveType.Sphere,
                    pos, Quaternion.identity, new Vector3(0.16f, 0.06f, 0.16f), dot, false);
            }
        }

        // 매끄러운 원형 그린 디스크(팬 메시). physics=true면 높은 마찰 콜라이더로 그린에서 공이 덜 굴러간다.
        void BuildGreenDisc(Transform parent, string name, Vector3 center, float radius, Color color, bool physics = false)
        {
            const int segments = 48;
            var verts = new Vector3[segments + 1];
            verts[0] = Vector3.zero; // 중심(로컬)
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }

            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris[i * 3] = 0;
                tris[i * 3 + 1] = next + 1; // 위(+Y)를 향하는 면이 되도록 와인딩
                tris[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.05f);
            mr.sharedMaterial = mat;

            if (physics)
            {
                // 그린 위에서 공이 덜 굴러가도록 높은 마찰의 얇은 콜라이더(원 근사 박스).
                var bc = go.AddComponent<BoxCollider>();
                bc.center = Vector3.zero;
                bc.size = new Vector3(radius * 2f, 0.006f, radius * 2f);
                var pm = new PhysicMaterial("GreenPhys");
                pm.dynamicFriction = 0.92f;
                pm.staticFriction = 0.98f;
                pm.bounciness = 0.0f;
                pm.frictionCombine = PhysicMaterialCombine.Maximum;
                pm.bounceCombine = PhysicMaterialCombine.Minimum;
                bc.sharedMaterial = pm;
            }
        }

        // ── 단계별 경사·장애물 (평평 물리 바닥 y≈0 기준) ──────────────────
        //  쉬움=없음 / 보통=완만한 둔덕+범퍼+게이트 / 어려움=둔덕2+범퍼3+게이트2+바위
        void BuildCourseObstacles(Transform parent, CourseSpec spec)
        {
            if (spec.Index == 0)
                return;

            if (spec.Index == 1)
            {
                SpawnCourseMound(parent, spec, 0.50f, 1.0f);
                SpawnObstacleBumper(parent, spec, 0.34f, -1.2f);
                SpawnObstacleBumper(parent, spec, 0.64f, 1.2f);
                SpawnObstacleGate(parent, spec, 0.80f, 1.05f);
            }
            else
            {
                // 어려움: 둔덕3 + 바위2 + 모든 벽을 좌우 왕복 미닫이 벽 5개로(범퍼·게이트 대체)
                SpawnCourseMound(parent, spec, 0.30f, 1.15f);
                SpawnCourseMound(parent, spec, 0.55f, 1.10f);
                SpawnCourseMound(parent, spec, 0.78f, 1.30f);

                SpawnSlidingWall(parent, spec, 0.22f);
                SpawnSlidingWall(parent, spec, 0.40f);
                SpawnSlidingWall(parent, spec, 0.58f);
                SpawnSlidingWall(parent, spec, 0.72f);
                SpawnSlidingWall(parent, spec, 0.88f);

                SpawnRockObstacle(parent, spec, 0.34f, 3.0f);
                SpawnRockObstacle(parent, spec, 0.66f, -3.2f);
            }
        }

        // 경사 지면 표면 위의 곡선 점(높이 포함) — 장애물을 경사 페어웨이에 앉힌다.
        Vector3 GroundCurvePoint(CourseSpec spec, float t)
        {
            return GetCurvePoint(spec, t);
        }

        // 완만한 둔덕 — 절반 묻은 누운 캡슐(속이 꽉 찬 솔리드). 공이 매끈하게 타고 넘고,
        // 텐트형과 달리 밑에 빈 공간이 없어 공이 끼지 않는다.
        void SpawnCourseMound(Transform parent, CourseSpec spec, float t, float scale)
        {
            Vector3 c = GroundCurvePoint(spec, t);
            Vector3 tan = GetCurveTangent(spec, t);
            Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);
            Color col = new Color(0.48f, 0.84f, 0.38f);

            float radius = 0.30f * scale;
            // 캡슐을 옆(페어웨이 가로=right)으로 눕히고 절반쯤 지면에 묻어 둔덕(반원)만 노출.
            var go = CreatePrimitive(parent, "CourseMound", PrimitiveType.Capsule,
                c + Vector3.down * (radius * 0.50f),
                rot * Quaternion.Euler(0f, 0f, 90f),
                new Vector3(radius * 2f, 2.4f, radius * 2f), col, true);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                // 마찰을 낮춰 공이 둔덕에서 멈추지 않고 자연스럽게 굴러내리게 한다.
                var m = new PhysicMaterial("MoundPhys");
                m.dynamicFriction = 0.30f;
                m.staticFriction = 0.26f;
                m.bounciness = 0.0f;
                m.frictionCombine = PhysicMaterialCombine.Multiply;
                m.bounceCombine = PhysicMaterialCombine.Minimum;
                collider.sharedMaterial = m;
            }
        }

        // 통나무 범퍼 — 페어웨이 일부를 막고 공을 튕긴다(통로 확보)
        void SpawnObstacleBumper(Transform parent, CourseSpec spec, float t, float sideOffset)
        {
            Vector3 c = GroundCurvePoint(spec, t);
            Vector3 tan = GetCurveTangent(spec, t);
            Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
            Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);
            Vector3 pos = c + right * sideOffset + Vector3.up * 0.18f;
            Color wood = new Color(0.58f, 0.34f, 0.16f);

            var go = CreatePrimitive(parent, "ObstacleBumper", PrimitiveType.Cube,
                pos, rot, new Vector3(1.5f, 0.36f, 0.34f), wood, true);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                var m = new PhysicMaterial("ObstacleBumperPhys");
                m.bounciness = 0.55f;
                m.dynamicFriction = 0.10f;
                m.bounceCombine = PhysicMaterialCombine.Maximum;
                m.frictionCombine = PhysicMaterialCombine.Minimum;
                col.sharedMaterial = m;
            }
            CreatePrimitive(parent, "ObstacleBumperTop", PrimitiveType.Cube,
                pos + Vector3.up * 0.20f, rot, new Vector3(1.55f, 0.06f, 0.40f), Color.white * 0.9f, false);
        }

        // 좁은 게이트 — 양옆 기둥 사이로 통과(통로 = 2*halfGap)
        void SpawnObstacleGate(Transform parent, CourseSpec spec, float t, float halfGap)
        {
            Vector3 c = GroundCurvePoint(spec, t);
            Vector3 tan = GetCurveTangent(spec, t);
            Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;
            Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);
            Color wood = new Color(0.46f, 0.27f, 0.13f);

            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pos = c + right * (s * (halfGap + 0.25f)) + Vector3.up * 0.22f;
                CreatePrimitive(parent, "ObstacleGatePost", PrimitiveType.Cube,
                    pos, rot, new Vector3(0.5f, 0.44f, 0.40f), wood, true);
                CreatePrimitive(parent, "ObstacleGateCap", PrimitiveType.Sphere,
                    pos + Vector3.up * 0.26f, Quaternion.identity, new Vector3(0.28f, 0.28f, 0.28f),
                    new Color(0.85f, 0.78f, 0.40f), false);
            }
        }

        // 바위 장애물 — 페어웨이 가장자리 해저드(어려움)
        void SpawnRockObstacle(Transform parent, CourseSpec spec, float t, float sideOffset)
        {
            Vector3 c = GroundCurvePoint(spec, t);
            Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
            Vector3 pos = c + right * sideOffset + Vector3.up * 0.20f;
            CreatePrimitive(parent, "ObstacleRock", PrimitiveType.Sphere,
                pos, Quaternion.Euler(0f, t * 360f, 0f), new Vector3(0.55f, 0.42f, 0.55f),
                new Color(0.50f, 0.50f, 0.46f), true);
        }

        // 좌우로 왕복하는 미닫이 벽 — 페어웨이의 빈 쪽으로 타이밍 맞춰 통과.
        void SpawnSlidingWall(Transform parent, CourseSpec spec, float t)
        {
            Vector3 c = GroundCurvePoint(spec, t);
            Vector3 tan = GetCurveTangent(spec, t);
            Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);

            var slide = new GameObject("SlidingWall");
            slide.transform.SetParent(parent, false);
            slide.transform.localPosition = c + Vector3.up * 0.275f; // 벽(높이 0.55) 밑면이 지면에 닿게
            slide.transform.localRotation = rot;
            slide.AddComponent<Rigidbody>();
            slide.AddComponent<GolfObstacleMotion>().Configure(GolfObstacleMotion.Kind.SlideX, 1.5f, 1.5f);

            // 감지 트리거(벽보다 약간 큼) — 멈춘 공을 깨워서 벽이 밀 수 있게 한다.
            var trig = slide.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = Vector3.zero;
            trig.size = new Vector3(2.0f, 0.7f, 0.62f);

            // 벽 본체: 진행방향(forward=tan)으로 얇고, 좌우(right=perp)로 절반 폭을 막는다.
            CreatePrimitive(slide.transform, "SlidingWallBody", PrimitiveType.Cube,
                Vector3.zero, Quaternion.identity,
                new Vector3(1.6f, 0.55f, 0.34f), new Color(0.22f, 0.42f, 0.72f), true);
            CreatePrimitive(slide.transform, "SlidingWallTop", PrimitiveType.Cube,
                Vector3.up * 0.30f, Quaternion.identity,
                new Vector3(1.7f, 0.08f, 0.40f), Color.white * 0.9f, false);
        }


        // 2D 굴곡 메시 지형 — 종단 경사(GetCourseHeight) + 러프 Perlin 굴곡(페어웨이/그린은 감쇠).
        void BuildTerrainMesh(Transform parent, CourseSpec spec)
        {
            Color roughColor = spec.Index == 0 ? new Color(0.40f, 0.78f, 0.30f)
                             : spec.Index == 1 ? new Color(0.46f, 0.75f, 0.27f)
                                               : new Color(0.36f, 0.70f, 0.28f);
            Texture2D grassTex = CreateGrassTexture(roughColor, new Color(0.34f, 0.70f, 0.24f));

            const float halfWidth = 24f;
            const float step = 1.2f;
            float zStart = -14f;                 // 티 뒤 패딩
            float zEnd = spec.Length + 20f;      // 홀 너머 패딩
            int nx = Mathf.CeilToInt((halfWidth * 2f) / step) + 1;
            int nz = Mathf.CeilToInt((zEnd - zStart) / step) + 1;

            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            {
                float z = zStart + iz * step;
                for (int ix = 0; ix < nx; ix++)
                {
                    float x = -halfWidth + ix * step;
                    int idx = iz * nx + ix;
                    verts[idx] = new Vector3(x, TerrainHeight(spec, x, z), z);
                    uvs[idx] = new Vector2(x * 0.25f, z * 0.25f);
                }
            }

            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int iz = 0; iz < nz - 1; iz++)
            {
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int v0 = iz * nx + ix;
                    int v2 = v0 + nx;
                    tris[t++] = v0; tris[t++] = v2; tris[t++] = v0 + 1;
                    tris[t++] = v0 + 1; tris[t++] = v2; tris[t++] = v2 + 1;
                }
            }

            var mesh = new Mesh { name = "GolfTerrain" };
            if (verts.Length > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("TerrainMesh");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = grassTex;
            mat.color = Color.white;
            mat.SetFloat("_Glossiness", 0.02f);
            mr.sharedMaterial = mat;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.sharedMaterial = GetGrassPhysics();

            // 안전망: 표면 한참 아래(>OB) 평평 콜라이더 — 메시 갭으로 빠져도 받친다.
            var safety = new GameObject("GroundSafetyNet");
            safety.transform.SetParent(parent, false);
            var bc = safety.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, -0.7f, spec.Length * 0.5f);
            bc.size = new Vector3(96f, 0.2f, spec.Length + 80f);
            bc.sharedMaterial = GetGrassPhysics();
        }

        // (x,z)에서 곡선 최근접점까지의 거리와 그 점의 종단 높이.
        void NearestCurve(CourseSpec spec, float x, float z, out float baseY, out float dist)
        {
            const int N = 40;
            float best = float.MaxValue;
            baseY = 0f;
            for (int i = 0; i <= N; i++)
            {
                Vector3 p = GetCurvePoint(spec, i / (float)N);
                float dx = p.x - x;
                float dz = p.z - z;
                float d = dx * dx + dz * dz;
                if (d < best) { best = d; baseY = p.y; }
            }
            dist = Mathf.Sqrt(best);
        }

        // 2D 지형 높이: 종단 경사 + 러프 Perlin 굴곡(페어웨이 중심선·그린 근처는 감쇠해 매끈).
        float TerrainHeight(CourseSpec spec, float x, float z)
        {
            NearestCurve(spec, x, z, out float baseY, out float dist);

            float seed = spec.Index * 7.3f;
            float n = Mathf.PerlinNoise((x + seed) * 0.08f, (z + seed) * 0.08f) - 0.5f;
            n += (Mathf.PerlinNoise((x - seed) * 0.20f, (z + seed) * 0.20f) - 0.5f) * 0.4f;
            float undulation = n * 0.45f; // 진폭 ±~0.31m

            // 페어웨이(중심선 근처) 감쇠: dist 3m 이내=평탄, 6.5m 이상=풀 굴곡
            float damp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.0f, 6.5f, dist));

            // 그린 = 평평한 플랫폼: 홀 주변은 종단 경사·굴곡을 모두 빼고 홀 높이로 평탄화.
            // (그린보다 높은 주변 지형이 원형 그린 가장자리를 덮지 않게 — 실제 골프장 그린처럼)
            float hdx = x - spec.HoleLocalPosition.x;
            float hdz = z - spec.HoleLocalPosition.z;
            float holeDist = Mathf.Sqrt(hdx * hdx + hdz * hdz);
            float greenFlat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.0f, 8.0f, holeDist)); // 0=그린(평평) → 1=일반
            damp *= greenFlat;

            float surfaceY = Mathf.Lerp(spec.HoleLocalPosition.y, baseY, greenFlat);
            return surfaceY + undulation * damp;
        }

        // 곡선 밖 임의 지점의 지면 표면 높이(굴곡 포함) — 모든 장식·시각요소 정합의 단일 소스.
        float GroundHeightAtXZ(CourseSpec spec, Vector3 localXZ)
        {
            return TerrainHeight(spec, localXZ.x, localXZ.z);
        }

        // 곡선 밖 장식을 지면 표면 높이에 올린 로컬 위치.
        Vector3 OnGround(CourseSpec spec, float x, float z)
        {
            var p = new Vector3(x, 0f, z);
            p.y = GroundHeightAtXZ(spec, p);
            return p;
        }

        // 곡선점에서 옆으로 offset 떨어진 러프 지점 — 굴곡 지형 높이에 정합.
        Vector3 RoughPoint(CourseSpec spec, Vector3 curvePt, Vector3 right, float offset)
        {
            var p = new Vector3(curvePt.x, 0f, curvePt.z) + right * offset;
            p.y = TerrainHeight(spec, p.x, p.z);
            return p;
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
            bc.size          = new Vector3(180f, 0.10f, spec.Length + 95f);
            bc.sharedMaterial = GetGrassPhysics();

            Vector3 visualCenter = new Vector3(0f, -0.018f, spec.Length * 0.5f);
            var rough = CreatePrimitive(parent, "RoughFieldVisual", PrimitiveType.Cube,
                visualCenter, Quaternion.identity,
                new Vector3(92f, 0.025f, spec.Length + 74f), roughColor, false);
            ApplyGeneratedTexture(rough, CreateGrassTexture(roughColor, new Color(0.30f, 0.67f, 0.20f)));

            for (int i = -8; i <= 8; i++)
            {
                if (i == 0) continue;
                float x = i * 5.2f;
                CreatePrimitive(parent, "RoughMowStripe", PrimitiveType.Cube,
                    new Vector3(x, 0.0005f, spec.Length * 0.5f), Quaternion.identity,
                    new Vector3(2.4f, 0.004f, spec.Length + 66f),
                    i % 2 == 0 ? new Color(0.39f, 0.77f, 0.25f) : new Color(0.33f, 0.68f, 0.22f), false);
            }

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
                Vector3 a = pts[i];
                Vector3 b = pts[i + 1];
                Vector3 segCenter = (a + b) * 0.5f + Vector3.up * 0.006f; // 경사 세그먼트가 평지 바닥보다 위에 오도록 보장
                Vector3 segDir = (b - a).normalized;
                float segLen = Vector3.Distance(a, b) + 0.12f;
                Quaternion segRot = Quaternion.LookRotation(segDir, Vector3.up);

                float segLenOverlap = segLen + 0.15f;
                // 페어웨이: 경사 콜라이더를 켜서 2/3단계에서 실제로 공이 언덕을 타게 한다.
                CreatePrimitive(parent, "Fairway", PrimitiveType.Cube,
                    segCenter, segRot, new Vector3(4.8f, 0.035f, segLenOverlap), fairwayColor, true,
                    fairwayMaterial);

                // 상단 줄무늬 (시각적)
                CreatePrimitive(parent, "FairwayStripe", PrimitiveType.Cube,
                    segCenter + segRot * new Vector3(-1.2f, 0.026f, 0f), segRot,
                    new Vector3(1.0f, 0.003f, segLenOverlap), i % 2 == 0 ? stripeA : stripeB, false);
                CreatePrimitive(parent, "FairwayStripe", PrimitiveType.Cube,
                    segCenter + segRot * new Vector3(1.2f, 0.026f, 0f), segRot,
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

            // 어려움: S자 — 진행 방향 수직으로 좌우로 굽이치게 해 보통(단일 곡선)과 형태를 다르게.
            if (spec.Index == 2)
            {
                Vector3 fwd = holeXZ - teeXZ;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f)
                {
                    fwd.Normalize();
                    Vector3 perp = new Vector3(fwd.z, 0f, -fwd.x);
                    // 시작/끝은 0(티·홀은 중심), 중간에서 좌우로 크게 흔든다.
                    float sCurve = Mathf.Sin(t * Mathf.PI * 2f) * 5.0f;
                    pt += perp * sCurve;
                }
            }

            float y = GetCourseHeight(spec, t);
            return new Vector3(pt.x, y, pt.z);
        }

        float GetCourseHeight(CourseSpec spec, float t)
        {
            float baseY = Mathf.Lerp(spec.TeeLocalPosition.y, spec.HoleLocalPosition.y, t);

            // 티 구역(앞 ~5m)은 평평하게 — 언덕이 티 바로 앞에서 솟지 않도록 살짝 뒤로 미룬다.
            float teeFlat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.22f, t));

            if (spec.Index == 1)
            {
                // 보통: 앞쪽은 완만한 오르막, 후반은 살짝 내려가며 퍼팅감 유지
                float gentleHill = Mathf.Sin(t * Mathf.PI) * 0.18f * teeFlat;
                float smallDip = Mathf.Sin(t * Mathf.PI * 2f) * 0.035f * teeFlat;
                return baseY + gentleHill + smallDip;
            }

            if (spec.Index == 2)
            {
                // 어려움: 더 큰 중간 언덕을 넘어 홀 쪽으로 내려가는 코스
                float mainHill = Mathf.Sin(t * Mathf.PI) * 0.66f * teeFlat;
                float approachPhase = Mathf.Clamp01(Mathf.InverseLerp(0.58f, 1f, t));
                float approachDip = -Mathf.Sin(approachPhase * Mathf.PI) * 0.14f;
                return baseY + mainHill + approachDip;
            }

            return baseY;
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
            // 티 구역은 코스 시작(t=0) 높이(position.y)에 평평하게 — 공이 굴러내려가지 않게.
            CreatePrimitive(parent, "TeeBox", PrimitiveType.Cube,
                new Vector3(position.x, position.y + 0.004f, position.z), Quaternion.identity,
                new Vector3(2.2f, 0.005f, 1.15f), new Color(0.66f, 0.96f, 0.46f), false);
            CreatePrimitive(parent, "TeeMat", PrimitiveType.Cube,
                GetTeeMatLocalPosition(position), Quaternion.identity,
                new Vector3(1.0f, 0.004f, 0.46f), new Color(0.22f, 0.68f, 0.28f), false);
        }

        static Vector3 GetTeeMatLocalPosition(Vector3 teePosition)
        {
            return new Vector3(teePosition.x, teePosition.y + 0.008f, teePosition.z - 0.18f);
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

            // ── 홀 뒤 백스톱 벽 (홀 통과 방지) — 홀에서 충분히 뒤에 둬 여유 공간 확보 ──
            Vector3 holeDir = GetCurveTangent(spec, 0.99f).normalized;
            var backstop = new GameObject("HoleBackstop");
            backstop.transform.SetParent(parent, false);
            backstop.transform.localPosition =
                new Vector3(position.x, 0.6f, position.z) + holeDir * 4.0f;
            backstop.transform.localRotation = Quaternion.LookRotation(holeDir, Vector3.up);
            var bbc = backstop.AddComponent<BoxCollider>();
            bbc.center = Vector3.zero;
            bbc.size   = new Vector3(8f, 1.2f, 0.3f);

            // 보이지 않는 벽 → 측면 경계벽과 동일한 불투명 나무벽으로 보이게 한다.
            Color backstopColor = new Color(0.80f, 0.56f, 0.30f);
            Color backstopTop = new Color(0.60f, 0.40f, 0.20f);
            CreatePrimitive(backstop.transform, "HoleBackstopVisual", PrimitiveType.Cube,
                Vector3.zero, Quaternion.identity,
                new Vector3(8f, 1.2f, 0.28f), backstopColor, false);
            CreatePrimitive(backstop.transform, "HoleBackstopEdge", PrimitiveType.Cube,
                new Vector3(0f, 0.64f, 0f), Quaternion.identity,
                new Vector3(8.1f, 0.10f, 0.36f), backstopTop, false);
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
                float dist = 6.4f + (i % 4) * 1.25f;
                Vector3 pos = RoughPoint(spec, pt, right, side * dist);

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
                new Vector3(0.14f * scale, 0.55f * scale, 0.14f * scale), new Color(0.50f, 0.30f, 0.13f), true);
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
                new Vector3(0.13f * scale, 0.55f * scale, 0.13f * scale), new Color(0.48f, 0.28f, 0.14f), true);
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
                Vector3 pos = RoughPoint(spec, pt, right, side * 5.2f);

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
                    Vector3 pos = RoughPoint(spec, pt, right, (i % 2 == 0 ? -1f : 1f) * 7.1f);
                    var rock = Instantiate(_rockPrefab, parent);
                    rock.transform.localPosition = pos;
                    rock.transform.localRotation = Quaternion.Euler(0f, i * 67f, 0f);
                    rock.transform.localScale    = Vector3.one * (0.7f + (i % 3) * 0.2f);
                    ApplySafeMaterial(rock, new Color(0.45f, 0.46f, 0.42f));
                    RemoveColliders(rock);
                    AddApproxCollider(rock); // 바위는 단단 — 공 통과 방지
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
                    Vector3 pos = RoughPoint(spec, pt, right, (i % 2 == 0 ? -1f : 1f) * 4.8f);
                    var flower = _flowerPrefabs[i % _flowerPrefabs.Length];
                    if (flower != null)
                    {
                        var f = Instantiate(flower, parent);
                        f.transform.localPosition = pos;
                        f.transform.localRotation = Quaternion.Euler(0f, i * 55f, 0f);
                        f.transform.localScale    = Vector3.one * 0.65f;
                        ApplySafeMaterial(f, i % 2 == 0 ? new Color(1f, 0.45f, 0.62f) : new Color(1f, 0.86f, 0.28f));
                        RemoveColliders(f);
                    }
                }
            }
        }

        void BuildDistantScenery(Transform parent, CourseSpec spec)
        {
            float midZ = spec.Length * 0.52f;
            float farZ = spec.Length + 11f;

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (spec.Width * 1.35f + 8.0f);
                CreatePrimitive(parent, "GolfCliffBase", PrimitiveType.Cube,
                    new Vector3(x, 0.55f, midZ), Quaternion.Euler(0f, side * -7f, 0f),
                    new Vector3(5.0f, 1.1f, spec.Length + 18f), new Color(0.45f, 0.39f, 0.29f), false);
                CreatePrimitive(parent, "GolfCliffTopGrass", PrimitiveType.Cube,
                    new Vector3(x, 1.15f, midZ), Quaternion.Euler(0f, side * -7f, 0f),
                    new Vector3(5.2f, 0.16f, spec.Length + 18f), new Color(0.32f, 0.68f, 0.25f), false);

                for (int i = 0; i < 7; i++)
                {
                    float z = 4f + i * (spec.Length / 6f);
                    float offset = (i % 2 == 0) ? 0.5f : -0.4f;
                    BuildTree(parent, new Vector3(x - side * (1.8f + offset), 1.15f, z), 1.05f + (i % 3) * 0.12f);
                }
            }

            for (int i = -5; i <= 5; i++)
            {
                float x = i * 4.9f;
                float scale = 1.1f + Mathf.Abs(i) * 0.06f;
                BuildTree(parent, new Vector3(x, 0f, farZ + Mathf.Sin(i * 1.7f) * 1.1f), scale);
            }

            CreatePrimitive(parent, "DistantHillLeft", PrimitiveType.Sphere,
                new Vector3(-9.5f, 0.20f, farZ + 5.0f), Quaternion.identity,
                new Vector3(8.5f, 1.6f, 4.2f), new Color(0.27f, 0.58f, 0.26f), false);
            CreatePrimitive(parent, "DistantHillRight", PrimitiveType.Sphere,
                new Vector3(9.8f, 0.18f, farZ + 4.4f), Quaternion.identity,
                new Vector3(9.0f, 1.5f, 4.6f), new Color(0.30f, 0.62f, 0.28f), false);

            BuildCloud(parent, new Vector3(-5.8f, 8.2f, spec.Length * 0.55f), 1.25f);
            BuildCloud(parent, new Vector3(4.2f, 8.7f, spec.Length * 0.72f), 1.05f);
            BuildCloud(parent, new Vector3(0.3f, 9.4f, spec.Length + 8f), 1.45f);
        }

        void BuildCloud(Transform parent, Vector3 center, float scale)
        {
            Color cloud = new Color(1f, 1f, 1f, 0.92f);
            CreatePrimitive(parent, "CloudPuff", PrimitiveType.Sphere,
                center, Quaternion.identity, new Vector3(1.45f, 0.34f, 0.55f) * scale, cloud, false);
            CreatePrimitive(parent, "CloudPuff", PrimitiveType.Sphere,
                center + new Vector3(0.72f * scale, 0.05f, 0.08f * scale), Quaternion.identity,
                new Vector3(1.0f, 0.28f, 0.45f) * scale, cloud, false);
            CreatePrimitive(parent, "CloudPuff", PrimitiveType.Sphere,
                center + new Vector3(-0.72f * scale, -0.02f, -0.06f * scale), Quaternion.identity,
                new Vector3(0.95f, 0.24f, 0.40f) * scale, cloud, false);
        }

        void BuildExpandedLandscape(Transform parent, CourseSpec spec)
        {
            float centerZ = spec.Length * 0.48f;
            float farZ = spec.Length + 18f;

            CreatePrimitive(parent, "OuterParkMeadowLeft", PrimitiveType.Cube,
                new Vector3(-34f, -0.006f, centerZ), Quaternion.identity,
                new Vector3(26f, 0.018f, spec.Length + 62f), new Color(0.38f, 0.72f, 0.26f), false);
            CreatePrimitive(parent, "OuterParkMeadowRight", PrimitiveType.Cube,
                new Vector3(34f, -0.006f, centerZ), Quaternion.identity,
                new Vector3(26f, 0.018f, spec.Length + 62f), new Color(0.37f, 0.70f, 0.25f), false);
            CreatePrimitive(parent, "OuterParkBackMeadow", PrimitiveType.Cube,
                new Vector3(0f, -0.007f, farZ + 10f), Quaternion.identity,
                new Vector3(92f, 0.016f, 28f), new Color(0.34f, 0.68f, 0.24f), false);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 10; i++)
                {
                    float z = -4f + i * ((spec.Length + 34f) / 9f);
                    float x = side * (18f + (i % 4) * 3.4f + Mathf.Sin(i * 1.73f) * 1.2f);
                    float scale = 1.15f + (i % 3) * 0.18f;
                    SpawnWideSceneryPrefab(parent, PickSceneryPrefab(0, i), new Vector3(x, 0f, z), scale,
                        new Color(0.24f, 0.56f, 0.22f), Quaternion.Euler(0f, i * 43f, 0f));
                }
            }

            for (int i = -6; i <= 6; i++)
            {
                float x = i * 6.5f + Mathf.Sin(i * 2.1f) * 1.4f;
                float z = farZ + Mathf.Cos(i * 1.37f) * 2.2f;
                SpawnWideSceneryPrefab(parent, PickSceneryPrefab(1, Mathf.Abs(i)), new Vector3(x, 0f, z),
                    1.25f + Mathf.Abs(i) * 0.035f, new Color(0.26f, 0.58f, 0.24f),
                    Quaternion.Euler(0f, i * 31f, 0f));
            }

            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (11.5f + (i % 3) * 2.5f);
                float z = 3.5f + i * (spec.Length / 14f);
                SpawnWideSceneryPrefab(parent, PickSceneryPrefab(2, i), new Vector3(x, 0.02f, z),
                    0.8f + (i % 4) * 0.08f, new Color(0.44f, 0.62f, 0.32f),
                    Quaternion.Euler(0f, i * 37f, 0f));
            }

            for (int i = 0; i < 7; i++)
            {
                float x = -18f + i * 6f;
                SpawnWideSceneryPrefab(parent, PickSceneryPrefab(6, i), new Vector3(x, 0.02f, -6.5f),
                    1.0f, new Color(0.48f, 0.32f, 0.18f), Quaternion.Euler(0f, 90f, 0f));
            }
        }

        GameObject PickSceneryPrefab(int preferredIndex, int salt)
        {
            if (_wideSceneryPrefabs == null || _wideSceneryPrefabs.Length == 0)
                return null;

            for (int i = 0; i < _wideSceneryPrefabs.Length; i++)
            {
                int index = Mathf.Abs(preferredIndex + salt + i) % _wideSceneryPrefabs.Length;
                if (_wideSceneryPrefabs[index] != null)
                    return _wideSceneryPrefabs[index];
            }

            return null;
        }

        void SpawnWideSceneryPrefab(Transform parent, GameObject prefab, Vector3 localPos, float scale,
            Color fallbackColor, Quaternion localRot)
        {
            if (prefab == null)
            {
                BuildTree(parent, localPos, scale);
                return;
            }

            var instance = Instantiate(prefab, parent);
            instance.name = "WideScenery_" + prefab.name;
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one * scale;
            ApplySafeMaterial(instance, fallbackColor);
            RemoveColliders(instance);
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

        // 코스 양옆 경계벽 — 공이 코스 밖으로 못 나가게 가둔다(미니골프식). 곡선·지형 따라 배치.
        void BuildSideRails(Transform parent, CourseSpec spec)
        {
            const int railSegs = 28;
            float railHalfWidth = spec.Width * 0.5f; // Width=10 → 5m
            Color railColor = new Color(0.80f, 0.56f, 0.30f);
            Color railTop = new Color(0.60f, 0.40f, 0.20f);

            for (int i = 0; i < railSegs; i++)
            {
                Vector3 a = GetCurvePoint(spec, i / (float)railSegs);
                Vector3 b = GetCurvePoint(spec, (i + 1) / (float)railSegs);
                Vector3 aFlat = new Vector3(a.x, 0f, a.z);
                Vector3 bFlat = new Vector3(b.x, 0f, b.z);
                Vector3 segDir = (bFlat - aFlat).normalized;
                if (segDir.sqrMagnitude < 1e-5f) segDir = Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, segDir).normalized;
                Vector3 midFlat = (aFlat + bFlat) * 0.5f;
                float segLen = Vector3.Distance(aFlat, bFlat) + 0.3f;
                Quaternion rot = Quaternion.LookRotation(segDir, Vector3.up);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 baseXZ = midFlat + right * (side * railHalfWidth);
                    float gy = TerrainHeight(spec, baseXZ.x, baseXZ.z);
                    Vector3 railBase = new Vector3(baseXZ.x, gy, baseXZ.z);

                    // 불투명 경계벽 — 시각과 물리 높이를 동일하게 맞춰 보이지 않는 벽 제거
                    const float wallH = 1.6f;
                    CreatePrimitive(parent, "SideRailVisual", PrimitiveType.Cube,
                        railBase + Vector3.up * (wallH * 0.5f), rot,
                        new Vector3(0.22f, wallH, segLen), railColor, false);
                    CreatePrimitive(parent, "SideRailTop", PrimitiveType.Cube,
                        railBase + Vector3.up * (wallH + 0.05f), rot,
                        new Vector3(0.30f, 0.10f, segLen), railTop, false);

                    var wallGo = new GameObject("SideRailPhysics");
                    wallGo.transform.SetParent(parent, false);
                    wallGo.transform.localPosition = railBase;
                    wallGo.transform.localRotation = rot;
                    var wallBc = wallGo.AddComponent<BoxCollider>();
                    wallBc.center = new Vector3(0f, wallH * 0.5f, 0f);
                    wallBc.size = new Vector3(0.25f, wallH, segLen);
                    wallBc.sharedMaterial = GetGrassPhysics();
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
            RenderSettings.fogColor = new Color(0.73f, 0.90f, 1.00f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 165f;
            RenderSettings.ambientLight = new Color(0.88f, 0.96f, 0.88f);
            RenderSettings.ambientIntensity = 1.22f;

            Camera cam = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.49f, 0.79f, 1.00f);
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
                    BuildParkMiniGolfProps(parent, spec);
                    break;

                case 2:
                    BuildHillGolfProps(parent, spec);
                    break;
            }
        }

        void BuildParkMiniGolfProps(Transform parent, CourseSpec spec)
        {
            Color wood = new Color(0.58f, 0.34f, 0.16f);

            // 통나무 범퍼를 S자 리듬으로 배치해 단순한 직선 코스처럼 보이지 않게 한다.
            SpawnLowWoodBumper(parent, spec, 0.20f, -1.20f, wood);
            SpawnLowWoodBumper(parent, spec, 0.38f, 1.25f, wood);
            SpawnLowWoodBumper(parent, spec, 0.58f, -1.05f, wood);
            SpawnLowWoodBumper(parent, spec, 0.76f, 1.10f, wood);

            SpawnNarrowGate(parent, spec, 0.48f, 2.05f, new Color(0.46f, 0.27f, 0.13f));
            SpawnNarrowGate(parent, spec, 0.68f, 1.82f, new Color(0.46f, 0.27f, 0.13f));

            for (int i = 0; i < 5; i++)
            {
                float t = 0.15f + i * 0.17f;
                Vector3 pt = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                float side = i % 2 == 0 ? -1f : 1f;
                BuildStoneCluster(parent, new Vector3(pt.x, pt.y, pt.z) + right * (side * 4.1f), 0.7f + i * 0.04f);
            }
        }

        void BuildHillGolfProps(Transform parent, CourseSpec spec)
        {
            Color wood = new Color(0.58f, 0.34f, 0.16f);

            // 언덕 코스: 중간 고지대와 내리막 그린이 한눈에 보이도록 시각용 둔덕을 추가한다.
            BuildHillShoulder(parent, spec, 0.34f, -3.8f, 1.15f);
            BuildHillShoulder(parent, spec, 0.47f, 3.8f, 1.25f);
            BuildHillShoulder(parent, spec, 0.70f, -4.4f, 1.05f);

            SpawnLowWoodBumper(parent, spec, 0.30f, -1.10f, wood);
            SpawnLowWoodBumper(parent, spec, 0.48f, 1.15f, wood);
            SpawnNarrowGate(parent, spec, 0.64f, 1.95f, new Color(0.42f, 0.25f, 0.12f));

            BuildStoneCluster(parent, OffsetFromCurve(spec, 0.42f, -4.5f), 1.05f);
            BuildStoneCluster(parent, OffsetFromCurve(spec, 0.58f, 4.4f), 0.95f);
            BuildStoneCluster(parent, OffsetFromCurve(spec, 0.82f, -4.8f), 0.90f);
        }

        void BuildHillShoulder(Transform parent, CourseSpec spec, float ratio, float sideOffset, float scale)
        {
            Vector3 pos = OffsetFromCurve(spec, ratio, sideOffset);
            CreatePrimitive(parent, "HillShoulder", PrimitiveType.Sphere,
                pos + Vector3.down * 0.10f, Quaternion.identity,
                new Vector3(3.4f, 0.42f, 2.4f) * scale, new Color(0.34f, 0.66f, 0.25f), false);
        }

        Vector3 OffsetFromCurve(CourseSpec spec, float ratio, float sideOffset)
        {
            Vector3 pt = GetCurvePoint(spec, ratio);
            Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, ratio)).normalized;
            return RoughPoint(spec, pt, right, sideOffset);
        }

        void SpawnNarrowGate(Transform parent, CourseSpec spec, float ratio, float halfGap, Color color)
        {
            Vector3 curvePos = GetCurvePoint(spec, ratio);
            Vector3 tangent = GetCurveTangent(spec, ratio);
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 pos = new Vector3(curvePos.x, curvePos.y, curvePos.z) + right * (side * halfGap) + Vector3.up * 0.20f;
                CreatePrimitive(parent, "NarrowGateLog", PrimitiveType.Cube,
                    pos, rot, new Vector3(0.42f, 0.40f, 0.36f), color, true);
            }
        }

        void BuildStoneCluster(Transform parent, Vector3 center, float scale)
        {
            Color rock = new Color(0.40f, 0.42f, 0.36f);
            CreatePrimitive(parent, "GolfStoneCluster", PrimitiveType.Sphere,
                center + new Vector3(0f, 0.10f, 0f), Quaternion.identity,
                new Vector3(0.52f, 0.22f, 0.38f) * scale, rock, true);
            CreatePrimitive(parent, "GolfStoneCluster", PrimitiveType.Sphere,
                center + new Vector3(0.38f * scale, 0.08f, 0.18f * scale), Quaternion.identity,
                new Vector3(0.32f, 0.18f, 0.28f) * scale, rock * 1.08f, false);
            CreatePrimitive(parent, "GolfStoneCluster", PrimitiveType.Sphere,
                center + new Vector3(-0.34f * scale, 0.07f, -0.16f * scale), Quaternion.identity,
                new Vector3(0.28f, 0.15f, 0.24f) * scale, rock * 0.92f, false);
        }

        void SpawnLowWoodBumper(Transform parent, CourseSpec spec, float ratio, float sideOffset, Color color)
        {
            Vector3 curvePos = GetCurvePoint(spec, ratio);
            Vector3 tangent = GetCurveTangent(spec, ratio);
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 pos = new Vector3(curvePos.x, curvePos.y, curvePos.z) + right * sideOffset + Vector3.up * 0.16f;

            CreatePrimitive(parent, "LowWoodBumper", PrimitiveType.Cube,
                pos, Quaternion.LookRotation(tangent, Vector3.up),
                new Vector3(1.35f, 0.30f, 0.22f), color, true);
        }

        void BuildPalmTree(Transform parent, Vector3 root, float scale)
        {
            Color trunk = new Color(0.56f, 0.36f, 0.18f);
            Color leaf = new Color(0.10f, 0.50f, 0.22f);
            CreatePrimitive(parent, "PalmTrunk", PrimitiveType.Cylinder,
                root + Vector3.up * (0.78f * scale), Quaternion.Euler(0f, 0f, 8f),
                new Vector3(0.13f * scale, 0.78f * scale, 0.13f * scale), trunk, false);

            Vector3 crown = root + Vector3.up * (1.55f * scale);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                CreatePrimitive(parent, "PalmLeaf", PrimitiveType.Cube,
                    crown + Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.42f * scale),
                    Quaternion.Euler(18f, angle, 0f),
                    new Vector3(0.18f * scale, 0.035f * scale, 0.95f * scale), leaf, false);
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

        // ────────────────────────────────────────────────────────────────
        //  한국식 시니어 파크 — 정자/벤치/산책로/꽃밭/연못+다리/벚꽃·소나무/새
        //  모든 장식은 플레이 통로(±5.5m) 바깥, 콜라이더 없음(공·플레이어 방해 금지).
        // ────────────────────────────────────────────────────────────────
        void BuildKoreanPark(Transform parent, CourseSpec spec)
        {
            var motion = parent.gameObject.AddComponent<GolfAmbientMotion>();

            // (모래색 산책로 제거 — 맵 곳곳의 베이지 사각형이 거슬려 삭제)
            BuildKoreanPavilion(parent, OnGround(spec, -9.5f, spec.TeeLocalPosition.z + 3.5f));
            BuildParkBenches(parent, spec);
            BuildFlowerBeds(parent, spec);
            BuildCherryBlossomRow(parent, spec, motion);
            BuildPineRow(parent, spec, motion);
            BuildDecoration(parent, spec);          // 덤불/꽃/바위 fill (기존 재사용)
            BuildPondWithBridge(parent, spec);
            BuildParkRocks(parent, spec);
            BuildParkHorizon(parent, spec);         // 원경 나무 라인 + 언덕 + 구름
            BuildBirds(parent, spec, motion);
        }

        // 임포트 프리팹 1개를 안전 머티리얼(Standard 평면색) + 콜라이더 제거로 배치.
        GameObject SpawnProp(Transform parent, GameObject prefab, Vector3 localPos, float scale,
            Quaternion localRot, Color fallbackColor, bool solid = false)
        {
            if (prefab == null)
                return null;

            var inst = Instantiate(prefab, parent);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = localRot;
            inst.transform.localScale = Vector3.one * scale;
            ApplySafeMaterial(inst, fallbackColor);
            RemoveColliders(inst);
            if (solid)
                AddApproxCollider(inst); // 단단한 구조물 — 공이 통과하지 않게 근사 콜라이더
            return inst;
        }

        // 렌더러 bounds로 단순 근사 박스 콜라이더 추가(정밀 메시콜라이더 회피, 공 충돌용).
        void AddApproxCollider(GameObject root)
        {
            Bounds bounds = default;
            bool has = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled)
                    continue;
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            if (!has)
                return;

            var bc = root.AddComponent<BoxCollider>();
            bc.center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 ls = root.transform.lossyScale;
            // 수평 폭을 줄기/기단 크기로 제한 — 나무 수관 등 넓은 AABB가 통로로 삐져나와
            // 보이지 않는 벽이 되는 걸 막는다(장식물은 통로 밖, 측면 경계벽이 공을 가둠).
            const float maxFootprint = 1.0f;
            bc.size = new Vector3(
                Mathf.Min(bounds.size.x / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), maxFootprint),
                bounds.size.y / Mathf.Max(1e-4f, Mathf.Abs(ls.y)),
                Mathf.Min(bounds.size.z / Mathf.Max(1e-4f, Mathf.Abs(ls.z)), maxFootprint));
            bc.sharedMaterial = GetGrassPhysics();
        }

        void BuildParkWalkways(Transform parent, CourseSpec spec)
        {
            Color pave = new Color(0.82f, 0.78f, 0.66f);
            float walkLen = spec.Length + 20f;
            float centerZ = spec.Length * 0.5f;

            // 산책로를 코스 길이를 따라 세그먼트로 깔아 경사 지면에 정합시킨다.
            const int wsegs = 12;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < wsegs; i++)
                {
                    float z = -8f + i * (walkLen / wsegs);
                    Vector3 p = OnGround(spec, side * 7.0f, z);
                    CreatePrimitive(parent, "Walkway", PrimitiveType.Cube,
                        p + Vector3.up * 0.006f, Quaternion.identity,
                        new Vector3(1.6f, 0.02f, walkLen / wsegs + 0.3f), pave, false);
                }
            }

            // 티 뒤 진입로(가로)
            Vector3 ent = OnGround(spec, 0f, spec.TeeLocalPosition.z - 4.0f);
            CreatePrimitive(parent, "WalkwayEntrance", PrimitiveType.Cube,
                ent + Vector3.up * 0.006f, Quaternion.identity,
                new Vector3(15.6f, 0.02f, 1.6f), pave, false);
        }

        // 절차적 한국식 정자(亭子): 석재 기단 + 목재 기둥/난간 + 2단 팔작지붕.
        void BuildKoreanPavilion(Transform parent, Vector3 baseLocal)
        {
            Color stone    = new Color(0.62f, 0.60f, 0.55f);
            Color wood     = new Color(0.50f, 0.32f, 0.16f);
            Color woodDark = new Color(0.38f, 0.24f, 0.12f);
            Color roof     = new Color(0.18f, 0.26f, 0.30f);
            Color roofEdge = new Color(0.10f, 0.16f, 0.20f);

            float w = 3.2f;
            float pillarH = 2.3f;

            var root = new GameObject("KoreanPavilion");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = baseLocal;
            root.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            Transform p = root.transform;

            CreatePrimitive(p, "PavilionBase", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 0f), Quaternion.identity,
                new Vector3(w + 0.8f, 0.24f, w + 0.8f), stone, true);
            CreatePrimitive(p, "PavilionFloor", PrimitiveType.Cube,
                new Vector3(0f, 0.27f, 0f), Quaternion.identity,
                new Vector3(w + 0.2f, 0.08f, w + 0.2f), wood, false);

            float half = w * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),   new Vector3(-half, 0f, half),
            };
            foreach (var c in corners)
            {
                CreatePrimitive(p, "PavilionPillar", PrimitiveType.Cylinder,
                    new Vector3(c.x, 0.30f + pillarH * 0.5f, c.z), Quaternion.identity,
                    new Vector3(0.18f, pillarH * 0.5f, 0.18f), wood, true);
            }

            // 난간 가로대(네 변, 낮게)
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % 4];
                Vector3 mid = (a + b) * 0.5f;
                Vector3 dir = b - a;
                Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                CreatePrimitive(p, "PavilionRailBar", PrimitiveType.Cube,
                    new Vector3(mid.x, 0.55f, mid.z), rot,
                    new Vector3(0.08f, 0.10f, dir.magnitude), woodDark, false);
            }

            CreatePrimitive(p, "PavilionBeam", PrimitiveType.Cube,
                new Vector3(0f, 0.30f + pillarH, 0f), Quaternion.identity,
                new Vector3(w + 0.4f, 0.14f, w + 0.4f), woodDark, false);

            float roofY = 0.30f + pillarH + 0.07f;
            CreatePrimitive(p, "PavilionRoofEave", PrimitiveType.Cube,
                new Vector3(0f, roofY + 0.06f, 0f), Quaternion.identity,
                new Vector3(w + 2.1f, 0.10f, w + 2.1f), roofEdge, false);
            CreatePrimitive(p, "PavilionRoofLower", PrimitiveType.Cube,
                new Vector3(0f, roofY + 0.18f, 0f), Quaternion.identity,
                new Vector3(w + 1.7f, 0.18f, w + 1.7f), roof, false);
            CreatePrimitive(p, "PavilionRoofUpper", PrimitiveType.Cube,
                new Vector3(0f, roofY + 0.42f, 0f), Quaternion.Euler(0f, 45f, 0f),
                new Vector3((w + 0.6f) * 0.78f, 0.30f, (w + 0.6f) * 0.78f), roof, false);
            CreatePrimitive(p, "PavilionFinial", PrimitiveType.Sphere,
                new Vector3(0f, roofY + 0.66f, 0f), Quaternion.identity,
                new Vector3(0.22f, 0.26f, 0.22f), roofEdge, false);
        }

        void BuildParkBenches(Transform parent, CourseSpec spec)
        {
            PlaceBench(parent, OnGround(spec, 6.6f, spec.TeeLocalPosition.z + 1.5f), -90f);
            PlaceBench(parent, OnGround(spec, -6.6f, spec.Length * 0.45f), 90f);
            PlaceBench(parent, OnGround(spec, 6.6f, spec.Length * 0.70f), -90f);
        }

        void PlaceBench(Transform parent, Vector3 pos, float yaw)
        {
            if (_benchPrefab != null)
            {
                var inst = SpawnProp(parent, _benchPrefab, pos, 1.0f,
                    Quaternion.Euler(0f, yaw, 0f), new Color(0.52f, 0.34f, 0.18f), solid: true);
                if (inst != null)
                {
                    inst.name = "ParkBench";
                    return;
                }
            }
            BuildProceduralBench(parent, pos, yaw);
        }

        void BuildProceduralBench(Transform parent, Vector3 pos, float yaw)
        {
            Color wood = new Color(0.52f, 0.34f, 0.18f);
            var b = new GameObject("ParkBench");
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            b.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = b.transform;

            CreatePrimitive(t, "BenchSeat", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f),
                Quaternion.identity, new Vector3(1.4f, 0.08f, 0.42f), wood, true);
            CreatePrimitive(t, "BenchBack", PrimitiveType.Cube, new Vector3(0f, 0.68f, -0.18f),
                Quaternion.identity, new Vector3(1.4f, 0.40f, 0.06f), wood, true);
            for (int s = -1; s <= 1; s += 2)
            {
                CreatePrimitive(t, "BenchLeg", PrimitiveType.Cube, new Vector3(s * 0.6f, 0.21f, 0f),
                    Quaternion.identity, new Vector3(0.08f, 0.42f, 0.40f), Darken(wood), false);
            }
        }

        static Color Darken(Color c) => new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f);

        void BuildFlowerBeds(Transform parent, CourseSpec spec)
        {
            BuildFlowerBed(parent, OnGround(spec, -7.5f, spec.TeeLocalPosition.z + 6f));
            BuildFlowerBed(parent, OnGround(spec, 7.8f, spec.Length * 0.6f));
        }

        void BuildFlowerBed(Transform parent, Vector3 center)
        {
            CreatePrimitive(parent, "FlowerBedSoil", PrimitiveType.Cube,
                center + Vector3.up * 0.03f, Quaternion.Euler(0f, 20f, 0f),
                new Vector3(2.2f, 0.06f, 1.4f), new Color(0.34f, 0.22f, 0.14f), false);

            bool hasFlowers = _flowerPrefabs != null && _flowerPrefabs.Length > 0;
            for (int i = 0; i < 9; i++)
            {
                float fx = ((i % 3) - 1) * 0.6f;
                float fz = ((i / 3) - 1) * 0.42f;
                Vector3 pos = center + new Vector3(fx, 0.06f, fz);

                GameObject prefab = hasFlowers ? _flowerPrefabs[i % _flowerPrefabs.Length] : null;
                if (prefab != null)
                {
                    Color fc = i % 3 == 0 ? new Color(1f, 0.85f, 0.20f)
                             : i % 3 == 1 ? new Color(0.95f, 0.40f, 0.60f)
                                          : new Color(0.70f, 0.50f, 0.95f);
                    var f = SpawnProp(parent, prefab, pos, 0.5f, Quaternion.Euler(0f, i * 40f, 0f), fc);
                    if (f != null) f.name = "BedFlower";
                }
                else
                {
                    Color fc = i % 2 == 0 ? new Color(1f, 0.24f, 0.54f) : new Color(1f, 0.92f, 0.30f);
                    CreatePrimitive(parent, "BedFlower", PrimitiveType.Sphere,
                        pos + Vector3.up * 0.12f, Quaternion.identity,
                        new Vector3(0.20f, 0.24f, 0.20f), fc, false);
                }
            }

            if (_fencePrefab != null)
            {
                for (int side = -1; side <= 1; side += 2)
                    SpawnProp(parent, _fencePrefab, center + new Vector3(side * 1.2f, 0f, 0f),
                        0.8f, Quaternion.Euler(0f, 90f, 0f), new Color(0.60f, 0.45f, 0.28f), solid: true);
            }
        }

        void BuildCherryBlossomRow(Transform parent, CourseSpec spec, GolfAmbientMotion motion)
        {
            const int n = 5;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                Vector3 pt = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                Vector3 pos = RoughPoint(spec, pt, right, -8.5f - (i % 2) * 0.8f);

                var root = new GameObject("CherryBlossom");
                root.transform.SetParent(parent, false);
                root.transform.localPosition = pos;
                BuildBlossomTree(root.transform, Vector3.zero, 1.1f + (i % 3) * 0.15f);
                motion?.AddSway(root.transform, 1.2f, 0.9f + i * 0.05f);
            }
        }

        void BuildPineRow(Transform parent, CourseSpec spec, GolfAmbientMotion motion)
        {
            const int n = 6;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.3f) / n;
                Vector3 pt = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                Vector3 pos = RoughPoint(spec, pt, right, 8.8f + (i % 2) * 1.0f);
                float scale = 1.0f + (i % 3) * 0.2f;

                if (_firTreePrefab != null)
                {
                    var inst = SpawnProp(parent, _firTreePrefab, pos, scale,
                        Quaternion.Euler(0f, i * 47f, 0f), new Color(0.20f, 0.42f, 0.22f), solid: true);
                    if (inst != null)
                    {
                        inst.name = "PineTree";
                        motion?.AddSway(inst.transform, 0.8f, 0.8f + i * 0.04f);
                        continue;
                    }
                }

                var root = new GameObject("PineTree");
                root.transform.SetParent(parent, false);
                root.transform.localPosition = pos;
                BuildTree(root.transform, Vector3.zero, 0.9f + (i % 3) * 0.15f);
                motion?.AddSway(root.transform, 0.8f, 0.8f);
            }
        }

        void BuildPondWithBridge(Transform parent, CourseSpec spec)
        {
            Vector3 pondCenter = OnGround(spec, 12.5f, spec.Length + 5f);

            CreatePrimitive(parent, "PondBed", PrimitiveType.Cylinder,
                pondCenter + Vector3.down * 0.08f, Quaternion.identity,
                new Vector3(5.5f, 0.10f, 4.0f), new Color(0.28f, 0.30f, 0.24f), false);

            var water = CreatePrimitive(parent, "PondWater", PrimitiveType.Cylinder,
                pondCenter + Vector3.up * 0.02f, Quaternion.identity,
                new Vector3(5.2f, 0.04f, 3.7f), new Color(0.30f, 0.55f, 0.72f), false);
            var wr = water.GetComponent<Renderer>();
            if (wr != null && wr.sharedMaterial != null)
                wr.sharedMaterial.SetFloat("_Glossiness", 0.6f);

            if (_pondPrefab != null)
                SpawnProp(parent, _pondPrefab, pondCenter, 1.2f, Quaternion.identity,
                    new Color(0.30f, 0.55f, 0.72f));

            if (_bridgePrefabs != null && _bridgePrefabs.Length > 1 && _bridgePrefabs[1] != null)
            {
                var mid = SpawnProp(parent, _bridgePrefabs[1], pondCenter + Vector3.up * 0.25f,
                    1.0f, Quaternion.Euler(0f, 90f, 0f), new Color(0.52f, 0.34f, 0.18f), solid: true);
                if (mid != null) mid.name = "PondBridge";
            }
            else
            {
                BuildProceduralBridge(parent, pondCenter);
            }
        }

        void BuildProceduralBridge(Transform parent, Vector3 center)
        {
            Color wood = new Color(0.52f, 0.34f, 0.18f);
            var root = new GameObject("PondBridge");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center + Vector3.up * 0.35f;
            root.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Transform t = root.transform;

            const int planks = 9;
            for (int i = 0; i < planks; i++)
            {
                float f = (i / (float)(planks - 1)) - 0.5f;
                float z = f * 5.0f;
                float y = Mathf.Cos(f * Mathf.PI) * 0.35f;
                CreatePrimitive(t, "BridgePlank", PrimitiveType.Cube,
                    new Vector3(0f, y, z), Quaternion.Euler(f * 22f, 0f, 0f),
                    new Vector3(1.6f, 0.06f, 0.55f), i % 2 == 0 ? wood : Darken(wood), true);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < planks; i++)
                {
                    float f = (i / (float)(planks - 1)) - 0.5f;
                    float z = f * 5.0f;
                    float y = Mathf.Cos(f * Mathf.PI) * 0.35f + 0.32f;
                    CreatePrimitive(t, "BridgeRail", PrimitiveType.Cube,
                        new Vector3(side * 0.78f, y, z), Quaternion.identity,
                        new Vector3(0.06f, 0.30f, 0.10f), Darken(wood), false);
                }
            }
        }

        void BuildParkRocks(Transform parent, CourseSpec spec)
        {
            for (int i = 0; i < 6; i++)
            {
                float t = (i + 0.5f) / 6f;
                Vector3 pt = GetCurvePoint(spec, t);
                Vector3 right = Vector3.Cross(Vector3.up, GetCurveTangent(spec, t)).normalized;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 pos = RoughPoint(spec, pt, right, side * (7.5f + (i % 3) * 0.6f));

                if (_rockPrefab != null)
                    SpawnProp(parent, _rockPrefab, pos, 0.7f + (i % 3) * 0.2f,
                        Quaternion.Euler(0f, i * 63f, 0f), new Color(0.45f, 0.46f, 0.42f), solid: true);
                else
                    BuildStoneCluster(parent, pos, 0.8f + (i % 2) * 0.2f);
            }
        }

        void BuildParkHorizon(Transform parent, CourseSpec spec)
        {
            float farZ = spec.Length + 16f;

            for (int i = -6; i <= 6; i++)
            {
                float x = i * 5.2f;
                SpawnWideSceneryPrefab(parent, PickSceneryPrefab(1, Mathf.Abs(i)),
                    new Vector3(x, 0f, farZ + Mathf.Sin(i * 1.3f) * 1.5f),
                    1.3f + Mathf.Abs(i) * 0.04f, new Color(0.26f, 0.52f, 0.26f),
                    Quaternion.Euler(0f, i * 33f, 0f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float z = i * (spec.Length / 5f);
                    SpawnWideSceneryPrefab(parent, PickSceneryPrefab(0, i),
                        OnGround(spec, side * (16f + (i % 3) * 2f), z), 1.2f,
                        new Color(0.24f, 0.50f, 0.24f), Quaternion.Euler(0f, i * 41f, 0f));
                }
            }

            CreatePrimitive(parent, "ParkDistantHillL", PrimitiveType.Sphere,
                new Vector3(-22f, -1.0f, farZ + 8f), Quaternion.identity,
                new Vector3(28f, 6.5f, 16f), new Color(0.30f, 0.50f, 0.34f), false);
            CreatePrimitive(parent, "ParkDistantHillR", PrimitiveType.Sphere,
                new Vector3(24f, -1.2f, farZ + 12f), Quaternion.identity,
                new Vector3(32f, 7.5f, 18f), new Color(0.27f, 0.46f, 0.32f), false);

            BuildCloud(parent, new Vector3(-6f, 9f, spec.Length * 0.5f), 1.3f);
            BuildCloud(parent, new Vector3(5f, 9.6f, spec.Length * 0.75f), 1.1f);
            BuildCloud(parent, new Vector3(0.5f, 10.2f, farZ), 1.5f);
        }

        void BuildBirds(Transform parent, CourseSpec spec, GolfAmbientMotion motion)
        {
            if (motion == null)
                return;

            const int n = 5;
            Color dark = new Color(0.15f, 0.15f, 0.17f);
            for (int i = 0; i < n; i++)
            {
                var bird = new GameObject("Bird");
                bird.transform.SetParent(parent, false);
                CreatePrimitive(bird.transform, "Wing", PrimitiveType.Cube,
                    new Vector3(-0.24f, 0f, 0f), Quaternion.Euler(0f, 0f, 22f),
                    new Vector3(0.50f, 0.04f, 0.14f), dark, false);
                CreatePrimitive(bird.transform, "Wing", PrimitiveType.Cube,
                    new Vector3(0.24f, 0f, 0f), Quaternion.Euler(0f, 0f, -22f),
                    new Vector3(0.50f, 0.04f, 0.14f), dark, false);

                Vector3 center = new Vector3(
                    Random.Range(-6f, 6f),
                    11f + i * 0.6f,
                    spec.Length * 0.5f + Random.Range(-4f, 4f));
                float radius = 6f + i * 1.5f;
                float angSpeed = (i % 2 == 0 ? 1f : -1f) * (0.18f + i * 0.02f);
                motion.AddBird(bird.transform, center, radius, angSpeed,
                    Random.value * Mathf.PI * 2f, 0.5f, 0.8f);
            }
        }

        // Built-in Standard 셰이더 반투명 머티리얼 (알파 블렌드).
        static Material MakeTranslucent(Color color)
        {
            var m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Mode", 3f); // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.color = color;
            m.SetFloat("_Glossiness", 0f);
            return m;
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
                    // Standard 셰이더를 런타임에 생성할 때 Opaque 모드를 명시하지 않으면
                    // Quest 등에서 반투명으로 렌더될 수 있어 강제 설정한다.
                    mat.SetFloat("_Mode", 0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = -1;
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

        void ApplyGeneratedTexture(GameObject target, Texture2D texture)
        {
            var renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer == null || texture == null)
                return;

            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            mat.mainTexture = texture;
            mat.SetFloat("_Glossiness", 0.02f);
            renderer.sharedMaterial = mat;
        }

        Texture2D CreateGrassTexture(Color baseColor, Color bladeColor)
        {
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.055f + 2.1f, y * 0.055f + 4.7f);
                    float blade = Mathf.Abs(Mathf.Sin((x * 0.18f + n * 2.4f) * Mathf.PI));
                    Color color = Color.Lerp(baseColor, bladeColor, Mathf.Clamp01(n * 0.45f + blade * 0.12f));
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return tex;
        }

        PhysicMaterial GetGrassPhysics()
        {
            if (_grassPhysics != null)
                return _grassPhysics;

            _grassPhysics = new PhysicMaterial("GolfGrassPhysics");
            _grassPhysics.dynamicFriction = 0.50f;
            _grassPhysics.staticFriction = 0.60f;
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
