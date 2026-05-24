// Assets/Scripts/Baduk/BadukRoomEnvironment.cs
using UnityEngine;

namespace Baduk
{
    public static class BadukRoomEnvironment
    {
        const string RootName = "BadukRoom";
        const string InteriorResourceRoot = "BadukInterior";
        const string ParkResourceRoot = "BadukPark";
        const string NpcResourceRoot = "BadukNPC";

        public static void Spawn(Vector3 boardCenter, float halfW, float halfD,
            float tableY, Quaternion roomRot, bool adjustCamera = true)
        {
            Cleanup();

            RenderSettings.ambientLight = new Color(0.70f, 0.66f, 0.58f);

            var root = new GameObject(RootName);
            Vector3 right = roomRot * Vector3.right;
            Vector3 forward = roomRot * Vector3.forward;
            float boardRadius = Mathf.Max(halfW, halfD);

            ConfigureOutdoorMood();
            SpawnParkBackground(root, boardCenter, halfW, halfD, tableY, roomRot, right, forward);
            SpawnDarkTable(root, boardCenter, halfW, halfD, tableY, roomRot);
            SpawnStoneBowl(root, boardCenter + right * (boardRadius + 0.24f) + forward * 0.02f, tableY, true);
            SpawnStoneBowl(root, boardCenter - right * (boardRadius + 0.24f) + forward * 0.02f, tableY, false);

            if (adjustCamera)
                AdjustCamera(boardCenter, halfD, tableY, forward);
        }

        public static void SpawnPracticeBackdrop(Vector3 boardCenter, Quaternion roomRot)
        {
            Cleanup();
        }

        public static void Cleanup()
        {
            var room = GameObject.Find(RootName);
            if (room != null) Object.Destroy(room);

            var table = GameObject.Find("BadukTable");
            if (table != null) Object.Destroy(table);

            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);
            RenderSettings.fog = false;
        }

        static void AdjustCamera(Vector3 boardCenter, float halfD, float tableY, Vector3 forward)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 pos = boardCenter - forward * (halfD + 0.08f);
            pos.y = tableY + 0.36f;
            cam.transform.position = pos;
            Vector3 lookTarget = boardCenter + forward * 0.03f - Vector3.up * 0.02f;
            cam.transform.rotation = Quaternion.LookRotation((lookTarget - pos).normalized, Vector3.up);
            cam.fieldOfView = 58f;
        }

        static void ConfigureOutdoorMood()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.79f, 0.84f, 0.77f);
            RenderSettings.fogStartDistance = 4f;
            RenderSettings.fogEndDistance = 18f;
        }

        static void SpawnParkBackground(GameObject root, Vector3 center, float halfW, float halfD, float tableY,
            Quaternion rot, Vector3 right, Vector3 forward)
        {
            SpawnGround(root, center);
            SpawnBenches(root, center, right, forward, tableY);
            SpawnSpectators(root, center, right, forward);
            SpawnTreeLine(root, center, right, forward);
            SpawnBushLine(root, center, right, forward);
        }

        static void SpawnDarkTable(GameObject root, Vector3 center, float halfW, float halfD, float tableY, Quaternion roomRot)
        {
            float tableHalf = Mathf.Max(halfW, halfD) + 0.36f;
            float thick = 0.06f;

            var top = Prim(root, "TableTop", PrimitiveType.Cube,
                new Vector3(center.x, tableY - thick * 0.5f, center.z),
                new Vector3(tableHalf * 2f, thick, tableHalf * 2f),
                new Color(0.78f, 0.56f, 0.30f));
            ApplyInteriorMaterial(top, "Table");
            top.transform.rotation = roomRot;

            float legH = Mathf.Max(0.20f, tableY - 0.01f);
            float lx = tableHalf - 0.16f;
            float lz = tableHalf - 0.16f;
            foreach (var offset in new[]
            {
                new Vector3( lx, 0f,  lz),
                new Vector3(-lx, 0f,  lz),
                new Vector3( lx, 0f, -lz),
                new Vector3(-lx, 0f, -lz)
            })
            {
                Vector3 rotatedOffset = roomRot * offset;
                var leg = Prim(root, "Leg", PrimitiveType.Cube,
                    new Vector3(center.x + rotatedOffset.x, legH * 0.5f, center.z + rotatedOffset.z),
                    new Vector3(0.045f, legH, 0.045f),
                    new Color(0.34f, 0.20f, 0.10f));
                ApplyInteriorMaterial(leg, "Table");
                leg.transform.rotation = roomRot;
            }
        }

        static void SpawnGround(GameObject root, Vector3 center)
        {
            var floor = Prim(root, "Floor", PrimitiveType.Cube,
                new Vector3(center.x, -0.03f, center.z + 2.0f),
                new Vector3(18f, 0.06f, 18f),
                new Color(0.53f, 0.61f, 0.43f));

            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.50f, 0.59f, 0.42f);
                material.SetFloat("_Glossiness", 0.05f);
                renderer.material = material;
            }
        }

        static void SpawnStoneBowl(GameObject root, Vector3 pos, float tableY, bool black)
        {
            float bowlY = tableY + 0.028f;
            var bowl = Prim(root, black ? "BlackBowl" : "WhiteBowl", PrimitiveType.Cylinder,
                new Vector3(pos.x, bowlY, pos.z),
                new Vector3(0.17f, 0.035f, 0.17f),
                new Color(0.46f, 0.28f, 0.13f));
            ApplyInteriorMaterial(bowl, "Table");

            Color stoneColor = black ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.94f, 0.92f, 0.86f);
            const int count = 18;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) + Random.Range(-6f, 6f);
                float radius = Random.Range(0.005f, 0.052f);
                Prim(root, "BowlStone", PrimitiveType.Sphere,
                    new Vector3(pos.x + Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                        bowlY + 0.024f + Random.Range(0f, 0.008f),
                        pos.z + Mathf.Cos(angle * Mathf.Deg2Rad) * radius),
                    Vector3.one * 0.016f, stoneColor);
            }
        }

        static void SpawnBenches(GameObject root, Vector3 center, Vector3 right, Vector3 forward, float tableY)
        {
            Vector3 benchPosition = center + forward * 3.15f;
            Quaternion benchRotation = Quaternion.LookRotation((center + forward * 0.45f - benchPosition).normalized, Vector3.up);

            if (!SpawnParkPrefab(root, "ParkChair", benchPosition, benchRotation, new Vector3(1.55f, 1.55f, 1.55f)))
                SpawnBenchFallback(root, benchPosition, benchRotation);
        }

        static void SpawnSpectators(GameObject root, Vector3 center, Vector3 right, Vector3 forward)
        {
            Vector3 boardInterest = center + forward * 0.10f;
            Vector3[] positions =
            {
                center - right * 1.65f + forward * 1.75f,
                center + right * 1.72f + forward * 1.82f,
                center - right * 2.55f + forward * 2.55f,
                center + right * 2.40f + forward * 2.62f,
                center - right * 0.75f + forward * 2.95f
            };

            float[] heights = { 0.98f, 1.02f, 0.95f, 1.04f, 0.97f };
            Color[] clothes =
            {
                new Color(0.24f, 0.31f, 0.42f),
                new Color(0.38f, 0.28f, 0.22f),
                new Color(0.29f, 0.33f, 0.27f),
                new Color(0.42f, 0.35f, 0.28f),
                new Color(0.22f, 0.24f, 0.29f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 lookTarget = boardInterest + ((i % 2 == 0) ? right * 0.08f : -right * 0.08f);
                Quaternion rot = Quaternion.LookRotation((lookTarget - positions[i]).normalized, Vector3.up);
                float wanderRadius = (i < 2) ? 0.14f : 0.18f;
                float moveSpeed = 0.010f + (i * 0.0010f);
                if (!SpawnSpectatorNpc(root, positions[i], rot, boardInterest, wanderRadius, moveSpeed, heights[i]))
                    SpawnSpectatorFigure(root, positions[i], rot, heights[i], clothes[i], wanderRadius, moveSpeed);
            }
        }

        static void SpawnTreeLine(GameObject root, Vector3 center, Vector3 right, Vector3 forward)
        {
            Vector3 backBand = center + forward * 4.8f;
            Vector3[] positions =
            {
                backBand - right * 5.4f,
                backBand - right * 3.2f + forward * 0.4f,
                backBand - right * 1.3f - forward * 0.3f,
                backBand + right * 1.7f,
                backBand + right * 4.6f - forward * 0.5f
            };

            string[] names = { "OakTree", "ParkTree", "PoplarTree", "OakTree", "PoplarTree" };
            Vector3[] scales =
            {
                new Vector3(1.55f, 1.55f, 1.55f),
                new Vector3(1.65f, 1.65f, 1.65f),
                new Vector3(1.65f, 1.65f, 1.65f),
                new Vector3(1.45f, 1.45f, 1.45f),
                new Vector3(1.75f, 1.75f, 1.75f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Quaternion rot = Quaternion.Euler(0f, 40f + (i * 25f), 0f);
                if (!SpawnParkPrefab(root, names[i], positions[i], rot, scales[i]))
                    SpawnTreeFallback(root, positions[i], scales[i].y);
            }

            Vector3 farBand = center + forward * 7.2f;
            Vector3[] farPositions =
            {
                farBand - right * 7.2f,
                farBand - right * 4.8f + forward * 0.4f,
                farBand - right * 2.1f - forward * 0.2f,
                farBand + right * 1.2f + forward * 0.5f,
                farBand + right * 3.9f - forward * 0.3f,
                farBand + right * 6.6f
            };

            for (int i = 0; i < farPositions.Length; i++)
            {
                string prefabName = (i % 2 == 0) ? "PoplarTree" : "OakTree";
                Quaternion rot = Quaternion.Euler(0f, 18f + (i * 31f), 0f);
                Vector3 scale = (i % 2 == 0)
                    ? new Vector3(1.95f, 1.95f, 1.95f)
                    : new Vector3(1.65f, 1.65f, 1.65f);
                if (!SpawnParkPrefab(root, prefabName, farPositions[i], rot, scale))
                    SpawnTreeFallback(root, farPositions[i], scale.y);
            }
        }

        static void SpawnBushLine(GameObject root, Vector3 center, Vector3 right, Vector3 forward)
        {
            Vector3 bushBand = center + forward * 2.4f;
            Vector3[] positions =
            {
                bushBand - right * 2.2f,
                bushBand - right * 1.0f + forward * 0.2f,
                bushBand + right * 0.9f + forward * 0.15f,
                bushBand + right * 2.4f - forward * 0.1f
            };

            for (int i = 0; i < positions.Length; i++)
            {
                string prefabName = (i % 2 == 0) ? "BushA" : "BushB";
                Quaternion rot = Quaternion.Euler(0f, 60f * i, 0f);
                if (!SpawnParkPrefab(root, prefabName, positions[i], rot, new Vector3(0.95f, 0.95f, 0.95f)))
                    SpawnBushFallback(root, positions[i]);
            }

            Vector3 sideBand = center + forward * 3.5f;
            Vector3[] extraPositions =
            {
                sideBand - right * 4.4f,
                sideBand - right * 3.1f + forward * 0.15f,
                sideBand + right * 3.0f + forward * 0.20f,
                sideBand + right * 4.5f - forward * 0.12f
            };

            for (int i = 0; i < extraPositions.Length; i++)
            {
                string prefabName = (i % 2 == 0) ? "BushB" : "BushA";
                Quaternion rot = Quaternion.Euler(0f, 25f + (i * 70f), 0f);
                Vector3 scale = new Vector3(1.18f, 1.18f, 1.18f);
                if (!SpawnParkPrefab(root, prefabName, extraPositions[i], rot, scale))
                    SpawnBushFallback(root, extraPositions[i]);
            }
        }

        static bool SpawnParkPrefab(GameObject root, string prefabName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var prefab = Resources.Load<GameObject>($"{ParkResourceRoot}/{prefabName}");
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab, position, rotation, root.transform);
            instance.name = prefabName;
            instance.transform.localScale = scale;
            SnapPrefabToGround(instance);
            RemoveColliders(instance);
            return true;
        }

        static bool SpawnSpectatorNpc(GameObject root, Vector3 position, Quaternion rotation, Vector3 interestPoint,
            float wanderRadius, float moveSpeed, float heightScale)
        {
            var prefab = Resources.Load<GameObject>($"{NpcResourceRoot}/SimpleHuman");
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab, position, rotation, root.transform);
            instance.name = "SpectatorNPC";
            float baseScale = 0.92f;
            float scaleWeight = 0.25f;
            instance.transform.localScale = Vector3.one * (baseScale + ((heightScale - 1f) * scaleWeight));
            SnapPrefabToGround(instance);
            RemoveColliders(instance);

            var styler = instance.AddComponent<BadukElderNpcStyler>();
            styler.ApplyStyle(Random.Range(1, 100000));

            var walker = instance.AddComponent<BadukAmbientWalker>();
            walker.Initialize(instance.transform.position, interestPoint, wanderRadius, moveSpeed, 0f, 4.0f, 8.5f, 180f);
            return true;
        }

        static void SnapPrefabToGround(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float offset = bounds.min.y;
            instance.transform.position -= new Vector3(0f, offset, 0f);
        }

        static void RemoveColliders(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);
        }

        static void SpawnBenchFallback(GameObject root, Vector3 center, Quaternion rot)
        {
            var seat = Prim(root, "BenchSeat", PrimitiveType.Cube,
                center + Vector3.up * 0.23f, new Vector3(0.72f, 0.06f, 0.22f), new Color(0.45f, 0.27f, 0.12f));
            seat.transform.rotation = rot;

            var back = Prim(root, "BenchBack", PrimitiveType.Cube,
                center + rot * new Vector3(0f, 0.44f, -0.10f),
                new Vector3(0.72f, 0.22f, 0.05f), new Color(0.40f, 0.24f, 0.10f));
            back.transform.rotation = rot;
        }

        static void SpawnSpectatorFigure(GameObject root, Vector3 center, Quaternion rot, float height, Color clothColor,
            float wanderRadius, float moveSpeed)
        {
            var actor = new GameObject("Spectator");
            actor.transform.SetParent(root.transform);
            actor.transform.position = center;
            actor.transform.rotation = rot;

            float bodyHeight = height * 0.52f;
            float legHeight = height * 0.40f;
            float headRadius = height * 0.11f;

            var body = Prim(actor, "SpectatorBody", PrimitiveType.Capsule,
                center + Vector3.up * (legHeight + bodyHeight * 0.5f),
                new Vector3(0.28f, bodyHeight * 0.5f, 0.24f),
                clothColor);
            body.transform.rotation = rot;

            var head = Prim(actor, "SpectatorHead", PrimitiveType.Sphere,
                center + Vector3.up * (legHeight + bodyHeight + headRadius * 0.9f),
                Vector3.one * (headRadius * 2f),
                new Color(0.80f, 0.67f, 0.56f));
            head.transform.rotation = rot;

            Vector3 right = rot * Vector3.right;
            Vector3 forward = rot * Vector3.forward;
            Color trouserColor = Color.Lerp(clothColor, Color.black, 0.35f);

            var legL = Prim(actor, "SpectatorLegL", PrimitiveType.Cube,
                center + Vector3.up * (legHeight * 0.5f) - right * 0.055f,
                new Vector3(0.07f, legHeight, 0.07f),
                trouserColor);
            legL.transform.rotation = rot;

            var legR = Prim(actor, "SpectatorLegR", PrimitiveType.Cube,
                center + Vector3.up * (legHeight * 0.5f) + right * 0.055f,
                new Vector3(0.07f, legHeight, 0.07f),
                trouserColor);
            legR.transform.rotation = rot;

            var armL = Prim(actor, "SpectatorArmL", PrimitiveType.Cube,
                center + Vector3.up * (legHeight + bodyHeight * 0.60f) - right * 0.19f + forward * 0.02f,
                new Vector3(0.06f, bodyHeight * 0.58f, 0.06f),
                clothColor);
            armL.transform.rotation = rot * Quaternion.Euler(0f, 0f, 12f);

            var armR = Prim(actor, "SpectatorArmR", PrimitiveType.Cube,
                center + Vector3.up * (legHeight + bodyHeight * 0.60f) + right * 0.19f + forward * 0.02f,
                new Vector3(0.06f, bodyHeight * 0.58f, 0.06f),
                clothColor);
            armR.transform.rotation = rot * Quaternion.Euler(0f, 0f, -12f);

            actor.transform.position = center;
            actor.transform.rotation = rot;

            var walker = actor.AddComponent<BadukAmbientWalker>();
            walker.Initialize(center, center + rot * Vector3.forward * 1.2f, wanderRadius, moveSpeed, 0f, 4.0f, 8.5f);
        }

        static void SpawnTreeFallback(GameObject root, Vector3 center, float scale)
        {
            var trunk = Prim(root, "TreeTrunk", PrimitiveType.Cylinder,
                center + Vector3.up * (1.05f * scale),
                new Vector3(0.22f * scale, 1.05f * scale, 0.22f * scale),
                new Color(0.39f, 0.28f, 0.16f));
            var canopy = Prim(root, "TreeCanopy", PrimitiveType.Sphere,
                center + Vector3.up * (2.45f * scale),
                new Vector3(1.45f * scale, 1.35f * scale, 1.45f * scale),
                new Color(0.37f, 0.57f, 0.28f));
            var canopy2 = Prim(root, "TreeCanopy2", PrimitiveType.Sphere,
                center + new Vector3(0.46f * scale, 2.18f * scale, -0.12f * scale),
                new Vector3(1.08f * scale, 1.00f * scale, 1.08f * scale),
                new Color(0.42f, 0.62f, 0.31f));
            Object.Destroy(trunk.GetComponent<Collider>());
            Object.Destroy(canopy.GetComponent<Collider>());
            Object.Destroy(canopy2.GetComponent<Collider>());
        }

        static void SpawnBushFallback(GameObject root, Vector3 center)
        {
            Prim(root, "Bush", PrimitiveType.Sphere,
                center + Vector3.up * 0.28f,
                new Vector3(0.72f, 0.55f, 0.72f),
                new Color(0.35f, 0.51f, 0.25f));
        }


        static void ApplyInteriorMaterial(GameObject target, string materialName)
        {
            if (target == null) return;

            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            var material = Resources.Load<Material>($"{InteriorResourceRoot}/{materialName}");
            if (material == null) return;

            renderer.material = material;
        }

        static GameObject Prim(GameObject root, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(root.transform);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = scale;

            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }
    }
}
