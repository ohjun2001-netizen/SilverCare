// Assets/Scripts/Baduk/Replay/NpcAvatarSpawner.cs
// 바둑판 좌/우에 흑·백 아바타 2인 배치.
// Mixamo FBX 프리팹 없으면 프리미티브 placeholder 자동 생성.
using System.Collections;
using UnityEngine;

namespace Baduk.Replay
{
    public class NpcAvatarSpawner : MonoBehaviour
    {
        [Header("아바타 프리팹 (없으면 placeholder)")]
        [SerializeField] GameObject blackNpcPrefab;
        [SerializeField] GameObject whiteNpcPrefab;

        [Header("크기 / 위치")]
        [SerializeField] float avatarScale         = 0.72f;
        [SerializeField] float sideOffsetFromBoard = 0.60f;
        [SerializeField] float feetYOffset         = -0.95f; // 보드 평면 기준 발 Y

        [Header("Placeholder 색상 (FBX 없을 때)")]
        [SerializeField] Color blackRobeColor = new Color(0.11f, 0.11f, 0.14f);
        [SerializeField] Color whiteRobeColor = new Color(0.87f, 0.85f, 0.80f);

        static readonly Color SkinColor = new Color(0.93f, 0.80f, 0.68f);
        static readonly Color HairColor = new Color(0.13f, 0.09f, 0.07f);
        static readonly Color EyeColor  = new Color(0.08f, 0.06f, 0.06f);

        struct AvatarInfo
        {
            public GameObject Root;
            public Transform  HeadGroup;  // 고개 끄덕임 타겟
            public Transform  ActiveArm;  // 보드 쪽 팔 (흑: ArmR, 백: ArmL)
            public bool       ArmIsLeft;  // true = ArmL (Z 부호 반전)
        }

        AvatarInfo _black;
        AvatarInfo _white;
        readonly System.Collections.Generic.List<GameObject> _spectators = new();

        // ── 공개 API ───────────────────────────────────────────────

        public void Spawn(Transform boardTransform)
        {
            Despawn();
            if (boardTransform == null)
            {
                Debug.LogWarning("[NpcAvatarSpawner] boardTransform 없음 — 스킵");
                return;
            }

            Vector3 boardCenter = boardTransform.position;
            float   boardHalfW  = 0.40f;
            var bb = boardTransform.GetComponent<BadukBoard>();
            if (bb != null)
            {
                float cx = (bb.C1 - bb.C0) * BadukBoard.CELL / 2f;
                float cy = (bb.R1 - bb.R0) * BadukBoard.CELL / 2f;
                boardCenter = boardTransform.TransformPoint(new Vector3(cx, 0f, -cy));
                boardHalfW  = cx * boardTransform.lossyScale.x;
            }

            Camera  cam  = Camera.main;
            Vector3 away = Vector3.forward;
            if (cam != null)
            {
                away   = boardCenter - cam.transform.position;
                away.y = 0f;
            }
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            away.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, away).normalized;
            float   dist  = boardHalfW + sideOffsetFromBoard;

            // 흑: 보드 왼쪽 → 오른팔(ArmR)이 보드 쪽
            _black = SpawnAvatar(blackNpcPrefab, "BlackAvatar",
                                 boardCenter - right * dist + Vector3.up * feetYOffset,
                                 boardCenter, blackRobeColor, armIsLeft: false);
            // 백: 보드 오른쪽 → 왼팔(ArmL)이 보드 쪽
            _white = SpawnAvatar(whiteNpcPrefab, "WhiteAvatar",
                                 boardCenter + right * dist + Vector3.up * feetYOffset,
                                 boardCenter, whiteRobeColor, armIsLeft: true);

            SpawnSpectators(boardCenter, right, away);
        }

        // 돌 놓는 모션 (팔 뻗기 + 고개 끄덕임)
        public void TriggerPlaceStone(string color)
        {
            AvatarInfo info = color == "black" ? _black : _white;
            if (info.Root == null) return;

            var anim = info.Root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("PlaceStone");
                return;
            }

            if (info.ActiveArm != null) StartCoroutine(ArmPlaceRoutine(info.ActiveArm, info.ArmIsLeft));
            if (info.HeadGroup != null) StartCoroutine(HeadNodDelayed(info.HeadGroup, 0.45f));
        }

        // 하위 호환
        public void TriggerHeadNod(string color) => TriggerPlaceStone(color);

        public void Despawn()
        {
            if (_black.Root != null) { Destroy(_black.Root); _black = default; }
            if (_white.Root != null) { Destroy(_white.Root); _white = default; }
            foreach (var s in _spectators) if (s != null) Destroy(s);
            _spectators.Clear();
        }

        void OnDestroy() => Despawn();

        // ── 관전자 스폰 ────────────────────────────────────────────

        void SpawnSpectators(Vector3 boardCenter, Vector3 right, Vector3 away)
        {
            // 바닥 Y: 보드보다 약 1.3m 아래 (tableY 기준)
            float floorY = boardCenter.y - 1.3f;

            // (위치 오프셋, 의상색) — 보드 반대편 + 양옆에 배치
            var configs = new (Vector3 offset, Color robe)[]
            {
                ( away * 1.2f - right * 0.6f,  new Color(0.25f, 0.18f, 0.42f)), // 뒤 왼쪽
                ( away * 1.2f + right * 0.6f,  new Color(0.42f, 0.25f, 0.18f)), // 뒤 오른쪽
                ( away * 1.5f,                 new Color(0.18f, 0.38f, 0.28f)), // 뒤 중앙
                (-right * 1.7f + away * 0.4f,  new Color(0.35f, 0.20f, 0.20f)), // 좌측
                ( right * 1.7f + away * 0.4f,  new Color(0.20f, 0.28f, 0.42f)), // 우측
            };

            for (int i = 0; i < configs.Length; i++)
            {
                // 약간의 랜덤 분산
                float rx = (i * 0.37f % 0.3f) - 0.15f;
                float rz = (i * 0.53f % 0.3f) - 0.15f;
                Vector3 pos = boardCenter + configs[i].offset
                            + right * rx + away * rz
                            + Vector3.up * floorY;

                var go = BuildStandingSpectator($"Spectator{i}", configs[i].robe);
                go.transform.position   = pos;
                go.transform.localScale = Vector3.one * avatarScale;

                Vector3 dir = boardCenter - pos; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

                _spectators.Add(go);
            }
        }

        // 서 있는 관전자 플레이스홀더 (높이 ~1.85 로컬 유닛)
        static GameObject BuildStandingSpectator(string name, Color robeColor)
        {
            var root = new GameObject(name);

            // 다리
            P(root.transform, "LegL", PrimitiveType.Cylinder,
              new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.13f, 0.45f, 0.13f), robeColor);
            P(root.transform, "LegR", PrimitiveType.Cylinder,
              new Vector3( 0.12f, 0.45f, 0f), new Vector3(0.13f, 0.45f, 0.13f), robeColor);

            // 발
            P(root.transform, "FootL", PrimitiveType.Cube,
              new Vector3(-0.12f, 0.05f, 0.06f), new Vector3(0.12f, 0.06f, 0.20f), robeColor);
            P(root.transform, "FootR", PrimitiveType.Cube,
              new Vector3( 0.12f, 0.05f, 0.06f), new Vector3(0.12f, 0.06f, 0.20f), robeColor);

            // 몸통
            P(root.transform, "Torso", PrimitiveType.Cube,
              new Vector3(0f, 1.10f, 0f), new Vector3(0.48f, 0.55f, 0.27f), robeColor);

            // 어깨
            P(root.transform, "Shoulders", PrimitiveType.Cube,
              new Vector3(0f, 1.40f, 0f), new Vector3(0.55f, 0.11f, 0.24f), robeColor);

            // 팔 (양쪽, 정적)
            P(root.transform, "ArmL", PrimitiveType.Cylinder,
              new Vector3(-0.32f, 1.10f, 0f), new Vector3(0.10f, 0.28f, 0.10f), robeColor);
            P(root.transform, "ArmR", PrimitiveType.Cylinder,
              new Vector3( 0.32f, 1.10f, 0f), new Vector3(0.10f, 0.28f, 0.10f), robeColor);

            // 목
            P(root.transform, "Neck", PrimitiveType.Cylinder,
              new Vector3(0f, 1.52f, 0f), new Vector3(0.12f, 0.08f, 0.12f), SkinColor);

            // 머리 그룹
            var headGrp = new GameObject("HeadGroup");
            headGrp.transform.SetParent(root.transform, false);
            headGrp.transform.localPosition = new Vector3(0f, 1.60f, 0f);

            P(headGrp.transform, "Head", PrimitiveType.Sphere,
              new Vector3(0f, 0.12f, 0f), new Vector3(0.40f, 0.43f, 0.40f), SkinColor);
            P(headGrp.transform, "Hair", PrimitiveType.Sphere,
              new Vector3(0f, 0.20f, -0.02f), new Vector3(0.43f, 0.28f, 0.43f), HairColor);
            P(headGrp.transform, "EyeL", PrimitiveType.Sphere,
              new Vector3(-0.09f, 0.13f, 0.18f), new Vector3(0.065f, 0.055f, 0.035f), EyeColor);
            P(headGrp.transform, "EyeR", PrimitiveType.Sphere,
              new Vector3( 0.09f, 0.13f, 0.18f), new Vector3(0.065f, 0.055f, 0.035f), EyeColor);

            return root;
        }

        // ── 스폰 ───────────────────────────────────────────────────

        AvatarInfo SpawnAvatar(GameObject prefab, string name,
                               Vector3 pos, Vector3 lookAt, Color robeColor, bool armIsLeft)
        {
            AvatarInfo info;
            info.ArmIsLeft = armIsLeft;
            if (prefab != null)
            {
                info.Root      = Instantiate(prefab);
                info.HeadGroup = info.Root.transform.Find("HeadGroup")
                              ?? info.Root.transform.Find("Head")
                              ?? info.Root.transform;
                string armName = armIsLeft ? "ArmL" : "ArmR";
                info.ActiveArm = info.Root.transform.Find(armName) ?? info.Root.transform;
            }
            else
            {
                info = BuildPlaceholder(name, robeColor);
                info.ArmIsLeft = armIsLeft;
                string armName = armIsLeft ? "ArmL" : "ArmR";
                info.ActiveArm = info.Root.transform.Find(armName);
            }

            info.Root.transform.position   = pos;
            info.Root.transform.localScale = Vector3.one * avatarScale;

            Vector3 dir = lookAt - pos; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                info.Root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            return info;
        }

        // ── 모션 코루틴 ────────────────────────────────────────────

        IEnumerator ArmPlaceRoutine(Transform arm, bool isLeft)
        {
            // Z 부호: 오른팔=-14, 왼팔=+14 (거울 대칭)
            float z = isLeft ? 14f : -14f;
            Quaternion rest  = Quaternion.Euler(  0f, 0f, z);
            Quaternion reach = Quaternion.Euler(-30f, 0f, z * 0.86f);

            yield return LerpRot(arm, rest,  reach, 0.32f);
            yield return new WaitForSeconds(0.14f);
            yield return LerpRot(arm, reach, rest,  0.36f);
        }

        IEnumerator HeadNodDelayed(Transform headGroup, float delay)
        {
            yield return new WaitForSeconds(delay);
            Quaternion original = headGroup.localRotation;
            Quaternion nodded   = original * Quaternion.Euler(26f, 0f, 0f);
            // 주기 5배: 0.14s → 0.70s
            yield return LerpRot(headGroup, original, nodded, 0.70f);
            yield return LerpRot(headGroup, nodded, original, 0.70f);
            headGroup.localRotation = original;
        }

        IEnumerator LerpRot(Transform t, Quaternion from, Quaternion to, float dur)
        {
            for (float e = 0f; e < dur; e += Time.deltaTime)
            {
                if (t == null) yield break;
                t.localRotation = Quaternion.Slerp(from, to, e / dur);
                yield return null;
            }
            if (t != null) t.localRotation = to;
        }

        // ── Placeholder 생성 ───────────────────────────────────────

        static AvatarInfo BuildPlaceholder(string name, Color robeColor)
        {
            var root = new GameObject(name);

            // ── 하체 (앉은 자세) ───────────────────────────
            BuildLegs(root.transform, robeColor);

            // 엉덩이
            P(root.transform, "Hip", PrimitiveType.Cube,
              new Vector3(0f, 0.80f, 0.08f), new Vector3(0.46f, 0.16f, 0.32f), robeColor);

            // ── 상체 ──────────────────────────────────────
            P(root.transform, "Torso", PrimitiveType.Cube,
              new Vector3(0f, 1.10f, 0f), new Vector3(0.52f, 0.58f, 0.28f), robeColor);

            // 어깨 라인 (조금 넓게)
            P(root.transform, "Shoulders", PrimitiveType.Cube,
              new Vector3(0f, 1.41f, -0.01f), new Vector3(0.60f, 0.13f, 0.25f), robeColor);

            // 가슴 라인 (약간 밝게 — 옷 주름 느낌)
            P(root.transform, "ChestLine", PrimitiveType.Cube,
              new Vector3(0f, 1.10f, 0.15f), new Vector3(0.08f, 0.40f, 0.01f),
              robeColor * 1.25f);

            // ── 목 ────────────────────────────────────────
            P(root.transform, "Neck", PrimitiveType.Cylinder,
              new Vector3(0f, 1.52f, 0.01f), new Vector3(0.13f, 0.09f, 0.13f), SkinColor);

            // ── 머리 그룹 (HeadNod 타겟) ──────────────────
            var headGroup = new GameObject("HeadGroup");
            headGroup.transform.SetParent(root.transform, false);
            headGroup.transform.localPosition = new Vector3(0f, 1.62f, 0.01f);

            // 두상
            P(headGroup.transform, "Head", PrimitiveType.Sphere,
              new Vector3(0f, 0.12f, 0f), new Vector3(0.43f, 0.46f, 0.43f), SkinColor);

            // 머리카락 (위 절반 덮음)
            P(headGroup.transform, "Hair", PrimitiveType.Sphere,
              new Vector3(0f, 0.21f, -0.02f), new Vector3(0.46f, 0.30f, 0.46f), HairColor);

            // 눈 (앞쪽에 돌출)
            P(headGroup.transform, "EyeL", PrimitiveType.Sphere,
              new Vector3(-0.10f, 0.14f, 0.19f), new Vector3(0.075f, 0.065f, 0.04f), EyeColor);
            P(headGroup.transform, "EyeR", PrimitiveType.Sphere,
              new Vector3( 0.10f, 0.14f, 0.19f), new Vector3(0.075f, 0.065f, 0.04f), EyeColor);

            // 코
            P(headGroup.transform, "Nose", PrimitiveType.Sphere,
              new Vector3(0f, 0.07f, 0.22f), new Vector3(0.05f, 0.04f, 0.05f),
              SkinColor * 0.88f);

            // 귀
            P(headGroup.transform, "EarL", PrimitiveType.Sphere,
              new Vector3(-0.22f, 0.12f, 0.02f), new Vector3(0.06f, 0.09f, 0.04f), SkinColor);
            P(headGroup.transform, "EarR", PrimitiveType.Sphere,
              new Vector3( 0.22f, 0.12f, 0.02f), new Vector3(0.06f, 0.09f, 0.04f), SkinColor);

            // ── 오른팔 (PlaceStone 타겟 — 어깨 피벗) ─────
            var armR = new GameObject("ArmR");
            armR.transform.SetParent(root.transform, false);
            armR.transform.localPosition = new Vector3(0.31f, 1.40f, 0f);
            armR.transform.localRotation = Quaternion.Euler(20f, 0f, -14f); // 자연스러운 휴식 각도

            P(armR.transform, "UpperArmR", PrimitiveType.Cylinder,
              new Vector3(0f, -0.19f, 0f), new Vector3(0.11f, 0.21f, 0.11f), robeColor);
            P(armR.transform, "ForearmR", PrimitiveType.Cylinder,
              new Vector3(0.02f, -0.50f, 0.03f), new Vector3(0.10f, 0.18f, 0.10f), SkinColor);
            P(armR.transform, "HandR", PrimitiveType.Cube,
              new Vector3(0.03f, -0.72f, 0.06f), new Vector3(0.10f, 0.08f, 0.09f), SkinColor);
            // 손가락 (3개 간략화)
            for (int i = -1; i <= 1; i++)
            {
                P(armR.transform, $"FingerR{i+2}", PrimitiveType.Cylinder,
                  new Vector3(0.03f + i * 0.025f, -0.84f, 0.07f),
                  new Vector3(0.025f, 0.055f, 0.025f), SkinColor);
            }

            // ── 왼팔 (정적) ───────────────────────────────
            var armL = new GameObject("ArmL");
            armL.transform.SetParent(root.transform, false);
            armL.transform.localPosition = new Vector3(-0.31f, 1.40f, 0f);
            armL.transform.localRotation = Quaternion.Euler(20f, 0f, 14f);

            P(armL.transform, "UpperArmL", PrimitiveType.Cylinder,
              new Vector3(0f, -0.19f, 0f), new Vector3(0.11f, 0.21f, 0.11f), robeColor);
            P(armL.transform, "ForearmL", PrimitiveType.Cylinder,
              new Vector3(-0.02f, -0.50f, 0.03f), new Vector3(0.10f, 0.18f, 0.10f), SkinColor);
            P(armL.transform, "HandL", PrimitiveType.Cube,
              new Vector3(-0.03f, -0.72f, 0.06f), new Vector3(0.10f, 0.08f, 0.09f), SkinColor);

            return new AvatarInfo
            {
                Root       = root,
                HeadGroup  = headGroup.transform,
                ActiveArm  = armR.transform, // SpawnAvatar에서 armIsLeft에 따라 재설정됨
                ArmIsLeft  = false,
            };
        }

        static void BuildLegs(Transform parent, Color robeColor)
        {
            BuildOneLeg(parent, "R",  0.17f, robeColor);
            BuildOneLeg(parent, "L", -0.17f, robeColor);
        }

        static void BuildOneLeg(Transform parent, string side, float xOff, Color robeColor)
        {
            // 허벅지 (앞으로 뻗은 자세 — 90° 회전)
            var thigh = P(parent, $"Thigh{side}", PrimitiveType.Cylinder,
                new Vector3(xOff, 0.74f, 0.16f),
                new Vector3(0.14f, 0.21f, 0.14f), robeColor);
            thigh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 정강이 (아래로)
            P(parent, $"Shin{side}", PrimitiveType.Cylinder,
              new Vector3(xOff, 0.42f, 0.30f),
              new Vector3(0.12f, 0.18f, 0.12f), robeColor);

            // 발
            P(parent, $"Foot{side}", PrimitiveType.Cube,
              new Vector3(xOff, 0.22f, 0.32f),
              new Vector3(0.13f, 0.07f, 0.22f), robeColor);
        }

        static GameObject P(Transform parent, string name, PrimitiveType type,
                             Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            Object.Destroy(go.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            go.GetComponent<Renderer>().material = mat;
            return go;
        }
    }
}
