// Assets/Scripts/Baduk/Betting/BettingSpectatorSpawner.cs
// 베팅 모드 전용 - 보드 옆에 구경꾼 N명 배치 (떠들썩한 분위기)
using System.Collections.Generic;
using UnityEngine;

namespace Baduk.Betting
{
    public class BettingSpectatorSpawner : MonoBehaviour
    {
        [Header("프리팹 (없으면 placeholder)")]
        [SerializeField] GameObject spectatorPrefab;

        [Header("스폰 설정")]
        [SerializeField, Range(1, 6)] int spectatorCount = 3;
        [Tooltip("보드 중심 기준 좌석 위치 (로컬 좌표)")]
        [SerializeField] Vector3[] seatPositions = new Vector3[]
        {
            new Vector3(-3.5f, 0f, -1.5f),
            new Vector3(-3.5f, 0f, -6f),
            new Vector3( 11f,  0f, -1.5f),
            new Vector3( 11f,  0f, -6f),
            new Vector3(-3.5f, 0f, -8f),
            new Vector3( 11f,  0f, -8f),
        };

        [SerializeField] Color[] spectatorColors = new Color[]
        {
            new Color(0.7f, 0.4f, 0.3f),
            new Color(0.3f, 0.5f, 0.7f),
            new Color(0.5f, 0.5f, 0.4f),
            new Color(0.6f, 0.3f, 0.5f),
        };

        Transform _boardTransform;
        readonly List<GameObject> _spawned = new();

        public void Spawn(Transform boardTransform)
        {
            _boardTransform = boardTransform;
            Despawn();

            int count = Mathf.Min(spectatorCount, seatPositions.Length);
            for (int i = 0; i < count; i++)
            {
                var go = SpawnOne($"Spectator_{i}", seatPositions[i],
                    spectatorColors[i % spectatorColors.Length]);
                _spawned.Add(go);
            }
        }

        public void Despawn()
        {
            foreach (var go in _spawned) if (go) Destroy(go);
            _spawned.Clear();
        }

        GameObject SpawnOne(string name, Vector3 localSeat, Color color)
        {
            GameObject go;
            if (spectatorPrefab != null)
            {
                go = Instantiate(spectatorPrefab, _boardTransform);
                go.name = name;
            }
            else
            {
                go = MakePlaceholder(name, color);
                go.transform.SetParent(_boardTransform, false);
            }
            go.transform.localPosition = localSeat;

            // 보드 중앙(대국자 사이) 바라보게
            Vector3 lookTarget = new Vector3(4f, 0f, -4f);
            Vector3 dir = lookTarget - localSeat;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                go.transform.localRotation = Quaternion.LookRotation(dir);
            return go;
        }

        static GameObject MakePlaceholder(string name, Color color)
        {
            var root = new GameObject(name);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale    = new Vector3(0.65f, 0.85f, 0.65f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyColor(body, color);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            head.transform.localScale    = Vector3.one * 0.5f;
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
    }
}
