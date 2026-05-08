// Assets/Scripts/Baduk/Replay/NpcAvatarSpawner.cs
// 사용자 정면(보드 너머)에 NPC 한 명을 월드 스페이스에 spawn.
// 보드의 자식이 아니므로 보드 스케일(BadukVRInput에서 0.025배 축소) 영향 없음.
using UnityEngine;

namespace Baduk.Replay
{
    public class NpcAvatarSpawner : MonoBehaviour
    {
        [Header("아바타 프리팹 (없으면 placeholder)")]
        [SerializeField] GameObject npcPrefab;

        [Header("크기 / 위치")]
        [Tooltip("아바타 월드 스케일 (placeholder 기준 ~2.3m 높이의 비율)")]
        [SerializeField] float avatarScale = 0.7f;
        [Tooltip("보드 중심에서 사용자 반대 방향으로 떨어뜨릴 거리(m)")]
        [SerializeField] float distanceFromBoard = 0.6f;
        [Tooltip("보드 평면 기준 발 높이 오프셋 (음수 = 아래). 머리(local y=2.05)가 사용자 얼굴 높이에 오도록 조정.")]
        [SerializeField] float feetYOffsetFromBoard = -1.1f;

        [Header("Placeholder 색상")]
        [SerializeField] Color placeholderBodyColor = new Color(0.35f, 0.30f, 0.40f);
        [SerializeField] Color placeholderHeadColor = new Color(0.95f, 0.85f, 0.75f);

        GameObject _npc;

        public void Spawn(Transform boardTransform)
        {
            Despawn();
            if (boardTransform == null)
            {
                Debug.LogWarning("[NpcAvatarSpawner] boardTransform 없음 — spawn 스킵");
                return;
            }

            // 보드의 실제 월드 중앙 — boardTransform.position은 좌상단 코너이므로 (cx, 0, -cy) 오프셋 적용
            Vector3 boardCenter;
            var bb = boardTransform.GetComponent<BadukBoard>();
            if (bb != null)
            {
                float cx = (bb.C1 - bb.C0) * BadukBoard.CELL / 2f;
                float cy = (bb.R1 - bb.R0) * BadukBoard.CELL / 2f;
                boardCenter = boardTransform.TransformPoint(new Vector3(cx, 0f, -cy));
            }
            else
            {
                boardCenter = boardTransform.position;
            }

            Camera  cam         = Camera.main;

            // 카메라 → 보드 수평 방향. NPC는 보드 너머(이 방향으로 더 멀리)에 위치.
            Vector3 awayFromUser;
            if (cam != null)
            {
                awayFromUser = boardCenter - cam.transform.position;
                awayFromUser.y = 0f;
            }
            else awayFromUser = Vector3.zero;
            if (awayFromUser.sqrMagnitude < 0.0001f) awayFromUser = Vector3.forward;
            awayFromUser.Normalize();

            Vector3 npcPos = boardCenter
                           + awayFromUser * distanceFromBoard
                           + Vector3.up   * feetYOffsetFromBoard;

            _npc = npcPrefab != null
                ? Instantiate(npcPrefab)
                : MakePlaceholder("OpponentNPC", placeholderBodyColor, placeholderHeadColor);

            // 부모 X — 월드 스페이스 그대로 (보드 스케일 영향 받지 않음)
            _npc.transform.position   = npcPos;
            _npc.transform.localScale = Vector3.one * avatarScale;

            // NPC 정면이 사용자 쪽을 보도록
            if (cam != null)
            {
                Vector3 lookTarget = new Vector3(cam.transform.position.x, npcPos.y, cam.transform.position.z);
                Vector3 dir = lookTarget - npcPos;
                if (dir.sqrMagnitude > 0.0001f)
                    _npc.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        public void Despawn()
        {
            if (_npc != null) { Destroy(_npc); _npc = null; }
        }

        void OnDestroy() => Despawn();

        // ── Placeholder (캡슐 몸통 + 구 머리) ─────────────────────
        // 기본 크기: 머리 정수리 ≈ y=2.33. avatarScale=0.7 적용 시 약 1.63m.
        static GameObject MakePlaceholder(string name, Color bodyColor, Color headColor)
        {
            var root = new GameObject(name);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale    = new Vector3(0.7f, 0.9f, 0.7f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyColor(body, bodyColor);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            head.transform.localScale    = Vector3.one * 0.55f;
            Object.Destroy(head.GetComponent<Collider>());
            ApplyColor(head, headColor);

            return root;
        }

        static void ApplyColor(GameObject go, Color c)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = new Material(rend.sharedMaterial);
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            rend.material = mat;
        }
    }
}
