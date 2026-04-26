// Assets/Scripts/Baduk/Replay/NpcAvatarSpawner.cs
// 바둑판 주변에 placeholder 아바타 배치 (대국자 2 + 훈수꾼 1)
// 진짜 3D 모델이 있으면 Inspector에서 prefab 할당 → placeholder 대신 사용
using UnityEngine;

namespace Baduk.Replay
{
    public class NpcAvatarSpawner : MonoBehaviour
    {
        [Header("아바타 프리팹 (없으면 placeholder 캡슐 사용)")]
        [SerializeField] GameObject blackPlayerPrefab;
        [SerializeField] GameObject whitePlayerPrefab;
        [SerializeField] GameObject commentatorPrefab;

        [Header("배치 위치 (보드 로컬 좌표 기준)")]
        [Tooltip("흑 대국자 위치 (보드 north 쪽)")]
        [SerializeField] Vector3 blackSeat = new Vector3(4f, 0f, 2.5f);
        [Tooltip("백 대국자 위치 (보드 south 쪽)")]
        [SerializeField] Vector3 whiteSeat = new Vector3(4f, 0f, -10.5f);
        [Tooltip("훈수꾼 위치 (보드 옆)")]
        [SerializeField] Vector3 commentatorSeat = new Vector3(-3f, 0f, -4f);

        [Header("Placeholder 색상")]
        [SerializeField] Color blackColor       = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] Color whiteColor       = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] Color commentatorColor = new Color(0.8f, 0.55f, 0.25f);

        Transform _boardTransform;
        GameObject _black, _white, _commentator;

        public void Spawn(Transform boardTransform)
        {
            _boardTransform = boardTransform;
            ClearExisting();

            _black       = SpawnOne("BlackPlayer",  blackPlayerPrefab,  blackSeat,       blackColor);
            _white       = SpawnOne("WhitePlayer",  whitePlayerPrefab,  whiteSeat,       whiteColor);
            _commentator = SpawnOne("Commentator",  commentatorPrefab,  commentatorSeat, commentatorColor);

            // 대국자들끼리 마주보게, 훈수꾼은 보드 쪽으로
            FaceTowardLocal(_black,       new Vector3(4f, 0f, -4f));
            FaceTowardLocal(_white,       new Vector3(4f, 0f, -4f));
            FaceTowardLocal(_commentator, new Vector3(4f, 0f, -4f));
        }

        public void Despawn() => ClearExisting();

        GameObject SpawnOne(string name, GameObject prefab, Vector3 localSeat, Color color)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, _boardTransform);
                go.name = name;
            }
            else
            {
                go = MakePlaceholder(name, color);
                go.transform.SetParent(_boardTransform, false);
            }
            go.transform.localPosition = localSeat;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        static GameObject MakePlaceholder(string name, Color color)
        {
            // 몸통 (캡슐) + 머리 (구) — 노인 비주얼은 아니지만 위치/존재감만 표현
            var root = new GameObject(name);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale    = new Vector3(0.7f, 0.9f, 0.7f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyColor(body, color);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            head.transform.localScale    = Vector3.one * 0.55f;
            Object.Destroy(head.GetComponent<Collider>());
            ApplyColor(head, new Color(0.95f, 0.85f, 0.75f));

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

        void FaceTowardLocal(GameObject go, Vector3 localTarget)
        {
            if (go == null) return;
            Vector3 dir = localTarget - go.transform.localPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            go.transform.localRotation = Quaternion.LookRotation(dir);
        }

        void ClearExisting()
        {
            if (_black) Destroy(_black);
            if (_white) Destroy(_white);
            if (_commentator) Destroy(_commentator);
        }
    }
}
