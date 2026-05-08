// Assets/Scripts/Baduk/BadukRoomEnvironment.cs
using UnityEngine;

namespace Baduk
{
    public static class BadukRoomEnvironment
    {
        const string ROOT = "BadukRoom";

        public static void Spawn(Vector3 boardCenter, float halfW, float halfD,
                                 float tableY, Quaternion roomRot, bool adjustCamera = true)
        {
            var old = GameObject.Find(ROOT);
            if (old != null) Object.Destroy(old);
            var oldT = GameObject.Find("BadukTable");
            if (oldT != null) Object.Destroy(oldT);

            RenderSettings.ambientLight = new Color(0.45f, 0.40f, 0.35f);

            var root  = new GameObject(ROOT);
            Vector3 right = roomRot * Vector3.right;
            Vector3 fwd   = roomRot * Vector3.forward;

            SpawnFloor(root, boardCenter);
            SpawnDarkTable(root, boardCenter, halfW, halfD, tableY);

            // 기통: 보드 좌우 테이블 위
            SpawnStoneBowl(root, boardCenter + right  * (halfW + 0.18f) + fwd * 0.05f, tableY, true);
            SpawnStoneBowl(root, boardCenter - right  * (halfW + 0.18f) + fwd * 0.05f, tableY, false);

            // 장지 화등 — 테이블 좌우
            SpawnLantern(root, boardCenter + right * (halfW + 0.58f) + fwd * 0.05f, tableY);
            SpawnLantern(root, boardCenter - right * (halfW + 0.58f) + fwd * 0.05f, tableY);

            // 다다미 (안쪽 끝)
            SpawnTatamiArea(root, boardCenter + fwd * (halfD + 0.55f), roomRot);

            // 뒷벽·옆벽
            SpawnWalls(root, boardCenter, roomRot);

            if (adjustCamera)
                AdjustCamera(boardCenter, halfD, tableY, fwd);
        }

        public static void Cleanup()
        {
            var room = GameObject.Find(ROOT);
            if (room != null) Object.Destroy(room);
            var table = GameObject.Find("BadukTable");
            if (table != null) Object.Destroy(table);
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);
        }

        // ── 카메라 ───────────────────────────────────────────
        static void AdjustCamera(Vector3 boardCenter, float halfD, float tableY, Vector3 fwd)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // 보드 근처 뒤쪽, 테이블 위 0.65m
            Vector3 pos = boardCenter - fwd * (halfD + 0.22f);
            pos.y = tableY + 0.65f;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(
                (boardCenter - pos).normalized, Vector3.up);
        }

        // ── 어두운 나무 테이블 ───────────────────────────────
        static void SpawnDarkTable(GameObject root, Vector3 center,
                                   float halfW, float halfD, float tableY)
        {
            float pad   = 0.16f;
            float thick = 0.05f;
            Prim(root, "TableTop", PrimitiveType.Cube,
                 new Vector3(center.x, tableY - thick * 0.5f, center.z),
                 new Vector3((halfW + pad) * 2f, thick, (halfD + pad) * 2f),
                 new Color(0.80f, 0.62f, 0.32f));

            float legH = tableY - 0.01f;
            float lx = halfW + pad * 0.5f, lz = halfD + pad * 0.5f;
            foreach (var o in new[] {
                new Vector3( lx, 0,  lz), new Vector3(-lx, 0,  lz),
                new Vector3( lx, 0, -lz), new Vector3(-lx, 0, -lz) })
            {
                Prim(root, "Leg", PrimitiveType.Cube,
                     new Vector3(center.x + o.x, legH * 0.5f, center.z + o.z),
                     new Vector3(0.045f, legH, 0.045f),
                     new Color(0.13f, 0.07f, 0.02f));
            }
        }

        // ── 바닥 ─────────────────────────────────────────────
        static void SpawnFloor(GameObject root, Vector3 center)
        {
            Prim(root, "Floor", PrimitiveType.Cube,
                 new Vector3(center.x, -0.01f, center.z),
                 new Vector3(7f, 0.02f, 7f),
                 new Color(0.09f, 0.05f, 0.02f));
        }

        // ── 벽 ───────────────────────────────────────────────
        static void SpawnWalls(GameObject root, Vector3 center, Quaternion rot)
        {
            Vector3 r = rot * Vector3.right;
            Vector3 f = rot * Vector3.forward;
            float h = 2.4f, y = h * 0.5f;
            var c = new Color(0.11f, 0.06f, 0.02f);

            var wl = Prim(root, "WallL", PrimitiveType.Cube,
                center - r * 3f + Vector3.up * y, new Vector3(0.07f, h, 7f), c);
            wl.transform.rotation = rot;

            var wr = Prim(root, "WallR", PrimitiveType.Cube,
                center + r * 3f + Vector3.up * y, new Vector3(0.07f, h, 7f), c);
            wr.transform.rotation = rot;

            var wb = Prim(root, "WallBack", PrimitiveType.Cube,
                center + f * 3.2f + Vector3.up * y, new Vector3(7f, h, 0.07f), c);
            wb.transform.rotation = rot;
        }

        // ── 기통 ─────────────────────────────────────────────
        static void SpawnStoneBowl(GameObject root, Vector3 pos, float tableY, bool black)
        {
            float bowlY = tableY + 0.03f;
            Prim(root, black ? "BlackBowl" : "WhiteBowl", PrimitiveType.Cylinder,
                 new Vector3(pos.x, bowlY, pos.z),
                 new Vector3(0.15f, 0.03f, 0.15f),
                 new Color(0.50f, 0.28f, 0.09f));

            Color sc = black ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.93f, 0.90f, 0.85f);
            int n = 18;
            for (int i = 0; i < n; i++)
            {
                float a = i * (360f / n) + Random.Range(-6f, 6f);
                float r = Random.Range(0.005f, 0.052f);
                Prim(root, "S", PrimitiveType.Sphere,
                     new Vector3(pos.x + Mathf.Sin(a * Mathf.Deg2Rad) * r,
                                 bowlY + 0.024f + Random.Range(0f, 0.008f),
                                 pos.z + Mathf.Cos(a * Mathf.Deg2Rad) * r),
                     Vector3.one * 0.016f, sc);
            }
        }

        // ── 장지 화등 ─────────────────────────────────────────
        static void SpawnLantern(GameObject root, Vector3 pos, float tableY)
        {
            float standH = Mathf.Max(0.25f, tableY - 0.18f);

            // 받침대
            Prim(root, "Stand", PrimitiveType.Cube,
                 new Vector3(pos.x, standH * 0.5f + 0.01f, pos.z),
                 new Vector3(0.04f, standH, 0.04f),
                 new Color(0.11f, 0.06f, 0.02f));

            // 등갓 몸체 (반투명 느낌의 따뜻한 색)
            float ly = standH + 0.19f;
            Prim(root, "LanternBody", PrimitiveType.Cube,
                 new Vector3(pos.x, ly, pos.z),
                 new Vector3(0.28f, 0.36f, 0.28f),
                 new Color(0.90f, 0.84f, 0.62f));

            // 포인트 라이트
            var lg = new GameObject("Light");
            lg.transform.SetParent(root.transform);
            lg.transform.position = new Vector3(pos.x, ly, pos.z);
            var light = lg.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1f, 0.88f, 0.54f);
            light.intensity = 2.0f;
            light.range     = 3.5f;
        }

        // ── 다다미 ────────────────────────────────────────────
        static void SpawnTatamiArea(GameObject root, Vector3 center, Quaternion rot)
        {
            Vector3 right = rot * Vector3.right;
            for (int i = -2; i <= 2; i++)
            {
                Color c = (i % 2 == 0)
                    ? new Color(0.63f, 0.73f, 0.38f)
                    : new Color(0.56f, 0.66f, 0.33f);
                var p   = new Vector3(center.x, 0.004f, center.z) + right * (i * 0.44f);
                var obj = Prim(root, "Tatami", PrimitiveType.Cube,
                               p, new Vector3(0.42f, 0.008f, 0.94f), c);
                obj.transform.rotation = rot;
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────
        static GameObject Prim(GameObject root, string name, PrimitiveType type,
                                Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(root.transform);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position   = pos;
            go.transform.localScale = scale;
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }
    }
}
