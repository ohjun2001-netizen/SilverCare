using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SilverCare.Common
{
    public static class SelectionBackdropUtility
    {
        static readonly Dictionary<string, GameObject> ActiveRoots = new();
        static readonly string[] StylizedTreePaths =
        {
            "StylizedNature/tree_a",
            "StylizedNature/tree_b",
            "StylizedNature/tree_c",
            "StylizedNature/tree_d",
            "StylizedNature/tree_e",
            "StylizedNature/tree_f",
            "StylizedNature/tree_g",
            "StylizedNature/tree_h",
            "StylizedNature/tree_i",
            "StylizedNature/tree_j",
            "StylizedNature/tree_k"
        };

        public static void ShowNatureBackdrop(Transform anchor, string key)
        {
            if (anchor == null) return;

            ClearBackdrop(key);
            ClearOrphanBackdrops();
            ApplyLobbySkyMood();

            var root = new GameObject(GetRootName(key));
            ActiveRoots[key] = root;
            Vector3 forward = anchor.forward;
            Vector3 right = anchor.right;
            Vector3 center = anchor.position + forward * 5.6f;
            Vector3 ground = center + Vector3.down * 1.08f - forward * 1.25f;
            float groundY = ResolveBackdropGroundY(anchor.position);
            ground.y = groundY;
            Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);

            SpawnGround(root.transform, ground, facing);
            SpawnSky(root.transform, ground, facing);
            SpawnMountains(root.transform, ground + forward * 5.9f, right, facing);
            SpawnSoftClouds(root.transform, ground + forward * 7.0f, right, facing);
            SpawnSunLight(root.transform, ground + forward * 1.0f - right * 2.0f + Vector3.up * 1.6f, forward, right);
            SpawnStylizedSun(root.transform, ground + forward * 5.4f, right, facing);
            SpawnLobbyStyleParkDetails(root.transform, ground, right, forward, facing);
            SpawnSurroundingParkSpace(root.transform, ground, right, forward);
            SpawnBackParkDepth(root.transform, ground, right, forward);

            SpawnTree(root.transform, ground - right * 4.1f + forward * 1.8f, 0);
            SpawnTree(root.transform, ground + right * 4.1f + forward * 1.8f, 1);
            SpawnTree(root.transform, ground - right * 5.0f - forward * 1.6f, 2);
            SpawnTree(root.transform, ground + right * 5.0f - forward * 1.6f, 3);
            SpawnTree(root.transform, ground - right * 5.7f - forward * 0.1f, 4);
            SpawnTree(root.transform, ground + right * 5.7f - forward * 0.1f, 5);

            SpawnFlowerPatch(root.transform, ground - right * 2.1f - forward * 0.25f, new Color(0.90f, 0.46f, 0.42f));
            SpawnFlowerPatch(root.transform, ground + right * 2.1f - forward * 0.25f, new Color(0.98f, 0.78f, 0.40f));
            SpawnFlowerRibbon(root.transform, ground - right * 1.4f - forward * 0.95f, right, new Color(0.86f, 0.54f, 0.72f));
            SpawnFlowerRibbon(root.transform, ground + right * 1.4f - forward * 0.95f, -right, new Color(0.98f, 0.78f, 0.40f));
            SpawnShrubCluster(root.transform, ground - right * 3.25f - forward * 0.85f, right, forward, 0);
            SpawnShrubCluster(root.transform, ground + right * 3.15f - forward * 0.70f, right, forward, 1);
            SpawnShrubCluster(root.transform, ground - right * 2.85f + forward * 1.15f, right, forward, 2);
            SpawnShrubCluster(root.transform, ground + right * 2.95f + forward * 1.05f, right, forward, 3);
            SpawnStonePath(root.transform, ground - forward * 1.05f, right, forward, facing);
            SpawnParkBench(root.transform, ground + forward * 0.35f - right * 3.0f, facing);
            SpawnParkBench(root.transform, ground + forward * 0.45f + right * 3.0f, facing);
            SpawnWarmGardenLamp(root.transform, ground - right * 4.65f - forward * 0.35f, facing);
            SpawnWarmGardenLamp(root.transform, ground + right * 4.65f - forward * 0.35f, facing);
            SpawnFence(root.transform, ground - forward * 1.8f, right, facing);
        }

        static float ResolveBackdropGroundY(Vector3 anchorPosition)
        {
            var xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
                return xrOrigin.transform.position.y;

            var xrRig = GameObject.Find("XR Origin (XR Rig)");
            if (xrRig != null)
                return xrRig.transform.position.y;

            return Mathf.Min(anchorPosition.y - 1.08f, 0f);
        }

        public static void ClearBackdrop(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (ActiveRoots.TryGetValue(key, out var trackedRoot))
            {
                DestroyRoot(trackedRoot);
                ActiveRoots.Remove(key);
            }

            var rootName = GetRootName(key);
            var sceneRoots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                var go = sceneRoots[i].gameObject;
                if (go.name == rootName)
                    DestroyRoot(go);
            }
        }

        public static void ClearAllBackdrops()
        {
            foreach (var kv in ActiveRoots)
                DestroyRoot(kv.Value);

            ActiveRoots.Clear();
            ClearOrphanBackdrops();
        }

        static string GetRootName(string key) => $"SelectionBackdrop_{key}";

        static void ClearOrphanBackdrops()
        {
            var sceneRoots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                var go = sceneRoots[i].gameObject;
                if (go.name.StartsWith("SelectionBackdrop_") || IsSelectionBadukProp(go.name))
                    DestroyRoot(go.transform.root.gameObject);
            }
        }

        static bool IsSelectionBadukProp(string name)
        {
            return name.StartsWith("SelectionBaduk") ||
                   name.StartsWith("ForegroundBaduk") ||
                   name.StartsWith("SelectionBoard") ||
                   name.StartsWith("SelectionBlackStone") ||
                   name.StartsWith("SelectionWhiteStone") ||
                   name.StartsWith("SelectionTeaCup") ||
                   name.StartsWith("SelectionBench") ||
                   name.StartsWith("SelectionSpectator");
        }

        static void DestroyRoot(GameObject root)
        {
            if (root == null)
                return;

            root.SetActive(false);
            Object.Destroy(root);
        }

        static void SpawnGround(Transform parent, Vector3 position, Quaternion rotation)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "BackdropGround";
            ground.transform.SetParent(parent, false);
            ground.transform.position = position - Vector3.up * 0.0125f;
            ground.transform.rotation = rotation;
            ground.transform.localScale = new Vector3(20.5f, 0.025f, 17.5f);

            var renderer = ground.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.56f, 0.72f, 0.44f);
            material.SetFloat("_Glossiness", 0.08f);
            renderer.material = material;
            Object.Destroy(ground.GetComponent<Collider>());

            SpawnParkGroundSurface(parent, position, rotation);
        }

        static void SpawnParkGroundSurface(Transform parent, Vector3 position, Quaternion rotation)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;

            CreatePrimitive(parent, "BackdropFullParkFloor", PrimitiveType.Cube,
                position + Vector3.up * 0.015f, rotation,
                new Vector3(30.0f, 0.014f, 30.0f), new Color(0.48f, 0.68f, 0.36f));

            CreatePrimitive(parent, "BackdropForegroundGrass", PrimitiveType.Cube,
                position - forward * 5.00f + Vector3.up * 0.019f, rotation,
                new Vector3(22.0f, 0.018f, 11.00f), new Color(0.47f, 0.68f, 0.36f));

            CreatePrimitive(parent, "BackdropRearGrass", PrimitiveType.Cube,
                position + forward * 3.60f + Vector3.up * 0.018f, rotation,
                new Vector3(20.8f, 0.016f, 8.60f), new Color(0.52f, 0.72f, 0.40f));

            CreatePrimitive(parent, "BackdropMainWalkway", PrimitiveType.Cube,
                position - forward * 0.55f + Vector3.up * 0.018f, rotation,
                new Vector3(2.05f, 0.018f, 4.80f), new Color(0.68f, 0.62f, 0.50f));

            CreatePrimitive(parent, "BackdropCrossWalkway", PrimitiveType.Cube,
                position - forward * 1.52f + Vector3.up * 0.021f, rotation,
                new Vector3(9.20f, 0.016f, 0.52f), new Color(0.64f, 0.56f, 0.43f));

            for (int i = -5; i <= 5; i++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "BackdropPathTile";
                tile.transform.SetParent(parent, false);
                tile.transform.position = position - forward * (0.22f + (i + 5) * 0.40f) + Vector3.up * 0.033f;
                tile.transform.rotation = rotation;
                tile.transform.localScale = new Vector3(1.70f, 0.012f, 0.28f);
                ApplyColor(tile, i % 2 == 0 ? new Color(0.74f, 0.69f, 0.58f) : new Color(0.66f, 0.61f, 0.51f));
            }

            for (int i = -3; i <= 3; i++)
            {
                CreatePrimitive(parent, "BackdropGrassPatchLeft", PrimitiveType.Cylinder,
                    position - right * (2.25f + Mathf.Abs(i) * 0.22f) + forward * (i * 0.54f) + Vector3.up * 0.028f,
                    rotation * Quaternion.Euler(90f, i * 21f, 0f), new Vector3(0.70f, 0.012f, 0.38f), new Color(0.45f, 0.68f, 0.36f));
                CreatePrimitive(parent, "BackdropGrassPatchRight", PrimitiveType.Cylinder,
                    position + right * (2.25f + Mathf.Abs(i) * 0.22f) + forward * (i * 0.54f) + Vector3.up * 0.028f,
                    rotation * Quaternion.Euler(90f, -i * 21f, 0f), new Vector3(0.70f, 0.012f, 0.38f), new Color(0.47f, 0.70f, 0.37f));
            }

            for (int i = 0; i < 18; i++)
            {
                float x = -4.8f + (i % 9) * 1.2f;
                float z = -2.2f + (i / 9) * 3.1f;
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "BackdropFallenLeaf";
                leaf.transform.SetParent(parent, false);
                leaf.transform.position = position + right * x + forward * z + Vector3.up * 0.045f;
                leaf.transform.rotation = rotation * Quaternion.Euler(0f, i * 37f, 0f);
                leaf.transform.localScale = new Vector3(0.16f, 0.008f, 0.055f);
                ApplyColor(leaf, i % 3 == 0 ? new Color(0.70f, 0.42f, 0.16f) : new Color(0.58f, 0.52f, 0.18f));
            }
        }

        static void SpawnSky(Transform parent, Vector3 ground, Quaternion rotation)
        {
            var prefab = Resources.Load<GameObject>("StylizedNature/sky");
            if (prefab == null) return;

            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;

            var sky = Object.Instantiate(prefab, parent);
            sky.name = "BackdropSky";
            sky.transform.position = ground + Vector3.up * 2.05f + forward * 8.6f;
            sky.transform.rotation = rotation;
            sky.transform.localScale *= 8.5f;

            for (int i = -1; i <= 1; i++)
            {
                var clone = Object.Instantiate(prefab, parent);
                clone.name = $"BackdropSky_{i + 2}";
                clone.transform.position = ground + Vector3.up * 2.15f + forward * (8.8f + Mathf.Abs(i) * 0.15f) + right * (i * 7.8f);
                clone.transform.rotation = rotation * Quaternion.Euler(0f, i * 2.5f, 0f);
                clone.transform.localScale *= 8.2f;
            }
        }

        static void SpawnMountains(Transform parent, Vector3 center, Vector3 right, Quaternion rotation)
        {
            var prefab = Resources.Load<GameObject>("StylizedNature/mountain");
            if (prefab == null) return;

            Vector3 forward = rotation * Vector3.forward;
            for (int i = -2; i <= 2; i++)
            {
                var mountain = Object.Instantiate(prefab, parent);
                mountain.name = $"BackdropMountain_{i + 2}";
                mountain.transform.position = center + right * (i * 2.9f) + forward * (1.8f + Mathf.Abs(i) * 0.18f) - Vector3.up * 0.04f;
                mountain.transform.rotation = rotation * Quaternion.Euler(0f, i * 11f, 0f);
                mountain.transform.localScale *= 1.55f + Mathf.Abs(i) * 0.15f;
            }
        }

        static void SpawnTree(Transform parent, Vector3 position, int index)
        {
            string path = StylizedTreePaths[Mathf.Abs(index) % StylizedTreePaths.Length];
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return;

            var tree = Object.Instantiate(prefab, parent);
            tree.name = $"BackdropTree_{index}";
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, index * 19f, 0f);
            tree.transform.localScale *= 1.55f + (index % 3) * 0.08f;
            SoftenTreePalette(tree);
        }

        static void SpawnFlowerPatch(Transform parent, Vector3 center, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float offsetX = (i % 3 - 1) * 0.26f;
                float offsetZ = (i / 3) * 0.24f;
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "FlowerPatch";
                flower.transform.SetParent(parent, false);
                flower.transform.position = center + new Vector3(offsetX, 0.08f, offsetZ);
                flower.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                material.SetFloat("_Glossiness", 0.18f);
                flower.GetComponent<Renderer>().material = material;
                Object.Destroy(flower.GetComponent<Collider>());
            }
        }

        static void SpawnFlowerRibbon(Transform parent, Vector3 start, Vector3 dir, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "FlowerRibbon";
                flower.transform.SetParent(parent, false);
                flower.transform.position = start + dir * (i * 0.34f) + Vector3.up * 0.08f;
                flower.transform.localScale = new Vector3(0.10f, 0.07f, 0.10f);
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                material.SetFloat("_Glossiness", 0.18f);
                flower.GetComponent<Renderer>().material = material;
                Object.Destroy(flower.GetComponent<Collider>());
            }
        }

        static void SpawnShrubCluster(Transform parent, Vector3 center, Vector3 right, Vector3 forward, int seed)
        {
            Color[] greens =
            {
                new Color(0.36f, 0.58f, 0.28f),
                new Color(0.44f, 0.66f, 0.34f),
                new Color(0.30f, 0.50f, 0.24f)
            };

            for (int i = 0; i < 7; i++)
            {
                float side = (i % 4 - 1.5f) * 0.20f;
                float depth = (i / 4) * 0.20f;
                var shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shrub.name = "BackdropShrub";
                shrub.transform.SetParent(parent, false);
                shrub.transform.position = center + right * side + forward * depth + Vector3.up * (0.13f + i * 0.006f);
                shrub.transform.localScale = new Vector3(0.34f + (i % 2) * 0.08f, 0.20f, 0.30f);
                ApplyColor(shrub, greens[(seed + i) % greens.Length]);
            }

            SpawnFlowerPatch(parent, center + forward * 0.08f + Vector3.up * 0.01f, new Color(0.95f, 0.70f, 0.42f));
        }

        static void SpawnStonePath(Transform parent, Vector3 start, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            for (int i = 0; i < 8; i++)
            {
                float wobble = Mathf.Sin(i * 1.7f) * 0.16f;
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stone.name = "BackdropSteppingStone";
                stone.transform.SetParent(parent, false);
                stone.transform.position = start + forward * (i * 0.42f) + right * wobble + Vector3.up * 0.018f;
                stone.transform.rotation = rotation * Quaternion.Euler(90f, i * 13f, 0f);
                stone.transform.localScale = new Vector3(0.34f, 0.025f, 0.23f);
                ApplyColor(stone, new Color(0.60f, 0.59f, 0.50f));
            }
        }

        static void SpawnParkBench(Transform parent, Vector3 center, Quaternion rotation)
        {
            Color wood = new Color(0.56f, 0.36f, 0.18f);
            Color leg = new Color(0.30f, 0.22f, 0.16f);
            CreatePrimitive(parent, "BackdropBenchSeat", PrimitiveType.Cube,
                center + Vector3.up * 0.34f, rotation, new Vector3(1.15f, 0.08f, 0.28f), wood);
            CreatePrimitive(parent, "BackdropBenchBack", PrimitiveType.Cube,
                center + Vector3.up * 0.58f + (rotation * Vector3.forward) * 0.12f, rotation, new Vector3(1.15f, 0.26f, 0.06f), wood);
            CreatePrimitive(parent, "BackdropBenchLegL", PrimitiveType.Cube,
                center - (rotation * Vector3.right) * 0.42f + Vector3.up * 0.18f, rotation, new Vector3(0.08f, 0.34f, 0.08f), leg);
            CreatePrimitive(parent, "BackdropBenchLegR", PrimitiveType.Cube,
                center + (rotation * Vector3.right) * 0.42f + Vector3.up * 0.18f, rotation, new Vector3(0.08f, 0.34f, 0.08f), leg);
        }

        static void SpawnWarmGardenLamp(Transform parent, Vector3 center, Quaternion rotation)
        {
            CreatePrimitive(parent, "BackdropGardenLampPost", PrimitiveType.Cube,
                center + Vector3.up * 0.46f, rotation, new Vector3(0.055f, 0.92f, 0.055f), new Color(0.34f, 0.24f, 0.16f));

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "BackdropGardenLampGlow";
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = center + Vector3.up * 0.96f;
            lamp.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            ApplyColor(lamp, new Color(1.0f, 0.82f, 0.42f));
        }

        static void SpawnPond(Transform parent, Vector3 center, Quaternion rotation)
        {
            var water = CreatePrimitive(parent, "BackdropPondWater", PrimitiveType.Cylinder,
                center + Vector3.up * 0.012f, rotation * Quaternion.Euler(90f, 0f, 0f),
                new Vector3(1.35f, 0.035f, 0.82f), new Color(0.38f, 0.66f, 0.74f));
            var renderer = water.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.SetFloat("_Glossiness", 0.45f);

            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.74f, 0.02f, Mathf.Sin(angle) * 0.46f);
                var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = "BackdropPondStone";
                stone.transform.SetParent(parent, false);
                stone.transform.position = center + offset;
                stone.transform.rotation = rotation;
                stone.transform.localScale = new Vector3(0.18f, 0.08f, 0.13f);
                ApplyColor(stone, new Color(0.50f, 0.49f, 0.43f));
            }
        }

        static void SpawnGardenBed(Transform parent, Vector3 center, Quaternion rotation)
        {
            CreatePrimitive(parent, "BackdropGardenBed", PrimitiveType.Cube,
                center, rotation, new Vector3(1.55f, 0.05f, 2.10f), new Color(0.34f, 0.45f, 0.25f));

            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            for (int i = 0; i < 8; i++)
            {
                float x = (i % 4 - 1.5f) * 0.28f;
                float z = (i / 4 - 0.5f) * 0.48f;
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "BackdropGardenStem";
                stem.transform.SetParent(parent, false);
                stem.transform.position = center + right * x + forward * z + Vector3.up * 0.13f;
                stem.transform.rotation = rotation;
                stem.transform.localScale = new Vector3(0.025f, 0.12f, 0.025f);
                ApplyColor(stem, new Color(0.24f, 0.45f, 0.18f));

                var bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bloom.name = "BackdropGardenBloom";
                bloom.transform.SetParent(parent, false);
                bloom.transform.position = center + right * x + forward * z + Vector3.up * 0.28f;
                bloom.transform.localScale = new Vector3(0.12f, 0.10f, 0.12f);
                ApplyColor(bloom, i % 2 == 0 ? new Color(0.94f, 0.58f, 0.38f) : new Color(0.96f, 0.76f, 0.34f));
            }
        }

        static void SpawnRestBench(Transform parent, Vector3 center, Quaternion rotation)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Color wood = new Color(0.55f, 0.35f, 0.18f);
            Color dark = new Color(0.24f, 0.19f, 0.15f);

            CreatePrimitive(parent, "BackdropRestBenchSeat", PrimitiveType.Cube,
                center + Vector3.up * 0.24f, rotation, new Vector3(1.25f, 0.09f, 0.32f), wood);
            CreatePrimitive(parent, "BackdropRestBenchBack", PrimitiveType.Cube,
                center + Vector3.up * 0.52f + forward * 0.17f, rotation, new Vector3(1.25f, 0.28f, 0.07f), wood);
            CreatePrimitive(parent, "BackdropRestBenchLegL", PrimitiveType.Cube,
                center - right * 0.46f + Vector3.up * 0.13f, rotation, new Vector3(0.08f, 0.26f, 0.08f), dark);
            CreatePrimitive(parent, "BackdropRestBenchLegR", PrimitiveType.Cube,
                center + right * 0.46f + Vector3.up * 0.13f, rotation, new Vector3(0.08f, 0.26f, 0.08f), dark);
        }

        static void SpawnPebbleGroup(Transform parent, Vector3 center, Vector3 right, Vector3 forward)
        {
            for (int i = 0; i < 7; i++)
            {
                float x = (i % 3 - 1) * 0.18f;
                float z = (i / 3) * 0.16f;
                var pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pebble.name = "BackdropPebble";
                pebble.transform.SetParent(parent, false);
                pebble.transform.position = center + right * x + forward * z;
                pebble.transform.localScale = new Vector3(0.13f + i * 0.006f, 0.055f, 0.10f);
                ApplyColor(pebble, new Color(0.58f, 0.56f, 0.48f));
            }
        }

        static void SpawnGrassTufts(Transform parent, Vector3 ground, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            for (int i = 0; i < 22; i++)
            {
                float x = -4.7f + (i % 11) * 0.92f;
                float z = -1.35f + (i / 11) * 2.65f;
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tuft.name = "BackdropGrassBlade";
                tuft.transform.SetParent(parent, false);
                tuft.transform.position = ground + right * x + forward * z + Vector3.up * 0.10f;
                tuft.transform.rotation = rotation * Quaternion.Euler(0f, i * 31f, 12f);
                tuft.transform.localScale = new Vector3(0.035f, 0.20f + (i % 3) * 0.03f, 0.035f);
                ApplyColor(tuft, new Color(0.30f, 0.52f, 0.22f));
            }
        }

        static void SpawnSoftClouds(Transform parent, Vector3 basePos, Vector3 right, Quaternion rotation)
        {
            for (int i = -2; i <= 2; i++)
            {
                var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloud.name = "BackdropSoftCloud";
                cloud.transform.SetParent(parent, false);
                cloud.transform.position = basePos + right * (i * 1.55f) + Vector3.up * (3.1f + Mathf.Abs(i) * 0.08f);
                cloud.transform.rotation = rotation;
                cloud.transform.localScale = new Vector3(0.95f, 0.22f, 0.16f);
                ApplyColor(cloud, new Color(0.94f, 0.97f, 0.96f));
            }
        }

        static void SpawnLobbyStylePavilion(Transform parent, Vector3 basePos, Quaternion rotation)
        {
            const float scale = 1.15f;
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;

            CreatePrimitive(parent, "BackdropPavilionDeck", PrimitiveType.Cube,
                basePos + Vector3.up * 0.05f, rotation, new Vector3(4.8f * scale, 0.09f, 2.55f * scale), new Color(0.58f, 0.38f, 0.20f));
            CreatePrimitive(parent, "BackdropPavilionRoof", PrimitiveType.Cube,
                basePos + Vector3.up * (1.55f * scale), rotation, new Vector3(5.25f * scale, 0.18f, 2.95f * scale), new Color(0.34f, 0.18f, 0.11f));
            CreatePrimitive(parent, "BackdropPavilionRoofCap", PrimitiveType.Cube,
                basePos + Vector3.up * (1.72f * scale), rotation, new Vector3(4.45f * scale, 0.13f, 2.22f * scale), new Color(0.42f, 0.23f, 0.14f));

            foreach (var offset in new[]
            {
                right * (2.05f * scale) + forward * (1.05f * scale),
                -right * (2.05f * scale) + forward * (1.05f * scale),
                right * (2.05f * scale) - forward * (1.05f * scale),
                -right * (2.05f * scale) - forward * (1.05f * scale)
            })
            {
                CreatePrimitive(parent, "BackdropPavilionPost", PrimitiveType.Cube,
                    basePos + offset + Vector3.up * (0.80f * scale), rotation,
                    new Vector3(0.13f, 1.48f * scale, 0.13f), new Color(0.43f, 0.25f, 0.12f));
            }
        }

        static void SpawnLobbyStyleParkDetails(Transform parent, Vector3 ground, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            SpawnPond(parent, ground + right * 3.15f + forward * 1.1f + Vector3.up * 0.02f, rotation);
            SpawnGardenBed(parent, ground - right * 3.05f + forward * 0.45f + Vector3.up * 0.03f, rotation);
            SpawnGardenBed(parent, ground + right * 3.00f + forward * 0.35f + Vector3.up * 0.03f, rotation);
            SpawnRestBench(parent, ground - right * 3.5f + forward * 1.35f + Vector3.up * 0.05f, rotation);
            SpawnRestBench(parent, ground + right * 3.5f + forward * 1.25f + Vector3.up * 0.05f, rotation);
            SpawnImportedParkAssets(parent, ground, right, forward, rotation);
            SpawnPebbleGroup(parent, ground - right * 1.9f + forward * 0.85f + Vector3.up * 0.03f, right, forward);
            SpawnPebbleGroup(parent, ground + right * 1.9f + forward * 0.75f + Vector3.up * 0.03f, right, forward);
            SpawnGrassTufts(parent, ground, right, forward, rotation);
        }

        static void SpawnImportedParkAssets(Transform parent, Vector3 ground, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            SpawnSafeParkAsset(parent, "BadukPark/ParkTree", "ImportedParkTreeLeft",
                ground - right * 4.55f + forward * 0.75f, rotation * Quaternion.Euler(0f, 18f, 0f), 1.35f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/ParkTree", "ImportedParkTreeRight",
                ground + right * 4.55f + forward * 0.70f, rotation * Quaternion.Euler(0f, -22f, 0f), 1.35f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/OakTree", "ImportedOakBackLeft",
                ground - right * 3.65f + forward * 2.45f, rotation * Quaternion.Euler(0f, 32f, 0f), 1.45f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/PoplarTree", "ImportedPoplarBackRight",
                ground + right * 3.85f + forward * 2.35f, rotation * Quaternion.Euler(0f, -28f, 0f), 1.42f, ImportedAssetTone.Tree);

            SpawnSafeParkAsset(parent, "BadukPark/BushA", "ImportedBushLeft",
                ground - right * 2.55f - forward * 0.25f + Vector3.up * 0.02f, rotation, 0.95f, ImportedAssetTone.Leaf);
            SpawnSafeParkAsset(parent, "BadukPark/BushB", "ImportedBushRight",
                ground + right * 2.55f - forward * 0.20f + Vector3.up * 0.02f, rotation, 0.95f, ImportedAssetTone.Leaf);
            SpawnSafeParkAsset(parent, "BadukPark/BushA", "ImportedBushBackLeft",
                ground - right * 3.55f + forward * 1.55f + Vector3.up * 0.02f, rotation * Quaternion.Euler(0f, 35f, 0f), 0.85f, ImportedAssetTone.Leaf);
            SpawnSafeParkAsset(parent, "BadukPark/BushB", "ImportedBushBackRight",
                ground + right * 3.55f + forward * 1.45f + Vector3.up * 0.02f, rotation * Quaternion.Euler(0f, -35f, 0f), 0.85f, ImportedAssetTone.Leaf);

            SpawnSafeParkAsset(parent, "BadukPark/ParkChair", "ImportedParkChairLeft",
                ground - right * 3.15f + forward * 0.78f + Vector3.up * 0.04f, rotation * Quaternion.Euler(0f, 9f, 0f), 0.62f, ImportedAssetTone.Wood);
            SpawnSafeParkAsset(parent, "BadukPark/ParkChair", "ImportedParkChairRight",
                ground + right * 3.15f + forward * 0.78f + Vector3.up * 0.04f, rotation * Quaternion.Euler(0f, -9f, 0f), 0.62f, ImportedAssetTone.Wood);
        }

        static void SpawnSurroundingParkSpace(Transform parent, Vector3 ground, Vector3 right, Vector3 forward)
        {
            Vector3[] ringDirs =
            {
                forward,
                (forward + right).normalized,
                right,
                (-forward + right).normalized,
                -forward,
                (-forward - right).normalized,
                -right,
                (forward - right).normalized
            };

            for (int i = 0; i < ringDirs.Length; i++)
            {
                Vector3 dir = ringDirs[i];
                Vector3 tangent = Vector3.Cross(Vector3.up, dir).normalized;
                Quaternion faceCenter = Quaternion.LookRotation(-dir, Vector3.up);

                float farRadius = i % 2 == 0 ? 7.4f : 8.6f;
                float nearRadius = i % 2 == 0 ? 5.2f : 5.9f;

                if (Vector3.Dot(dir, -forward) < 0.88f)
                {
                    string treePath = i % 3 == 0 ? "BadukPark/OakTree" : i % 3 == 1 ? "BadukPark/ParkTree" : "BadukPark/PoplarTree";
                    SpawnSafeParkAsset(parent, treePath, $"RingTree_{i}",
                        ground + dir * farRadius + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, i * 17f, 0f), 1.22f, ImportedAssetTone.Tree);
                }

                string bushPath = i % 2 == 0 ? "BadukPark/BushA" : "BadukPark/BushB";
                SpawnSafeParkAsset(parent, bushPath, $"RingBush_{i}_A",
                    ground + dir * nearRadius + tangent * 0.8f + Vector3.up * 0.02f, faceCenter, 0.82f, ImportedAssetTone.Leaf);
                SpawnSafeParkAsset(parent, bushPath, $"RingBush_{i}_B",
                    ground + dir * (nearRadius + 0.55f) - tangent * 0.7f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, 25f, 0f), 0.72f, ImportedAssetTone.Leaf);

                if (i % 2 == 0)
                {
                    SpawnSafeParkAsset(parent, "BadukPark/ParkChair", $"RingChair_{i}",
                        ground + dir * 4.6f - tangent * 1.25f + Vector3.up * 0.04f, faceCenter, 0.58f, ImportedAssetTone.Wood);
                }

                SpawnGroundFlowerDots(parent, ground + dir * 4.2f + tangent * 0.35f + Vector3.up * 0.04f, tangent, dir, i);
            }

            SpawnCircularWalkway(parent, ground, right, forward);
        }

        static void SpawnBackParkDepth(Transform parent, Vector3 ground, Vector3 right, Vector3 forward)
        {
            Vector3 back = -forward;
            Quaternion faceCenter = Quaternion.LookRotation(forward, Vector3.up);

            SpawnSafeParkAsset(parent, "BadukPark/OakTree", "BackDepthOakLeft",
                ground + back * 8.8f - right * 3.8f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, -18f, 0f), 1.36f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/PoplarTree", "BackDepthPoplarRight",
                ground + back * 8.9f + right * 3.9f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, 22f, 0f), 1.34f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/ParkTree", "BackDepthParkTreeLeftFar",
                ground + back * 10.8f - right * 6.0f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, 28f, 0f), 1.18f, ImportedAssetTone.Tree);
            SpawnSafeParkAsset(parent, "BadukPark/ParkTree", "BackDepthParkTreeRightFar",
                ground + back * 10.7f + right * 6.1f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, -24f, 0f), 1.18f, ImportedAssetTone.Tree);

            SpawnRestBench(parent, ground + back * 5.9f - right * 2.8f + Vector3.up * 0.04f, faceCenter * Quaternion.Euler(0f, -14f, 0f));
            SpawnRestBench(parent, ground + back * 5.9f + right * 2.8f + Vector3.up * 0.04f, faceCenter * Quaternion.Euler(0f, 14f, 0f));

            SpawnGardenBed(parent, ground + back * 5.2f - right * 4.4f + Vector3.up * 0.03f, faceCenter);
            SpawnGardenBed(parent, ground + back * 5.2f + right * 4.4f + Vector3.up * 0.03f, faceCenter);
            SpawnShrubCluster(parent, ground + back * 4.6f - right * 1.8f, right, forward, 6);
            SpawnShrubCluster(parent, ground + back * 4.6f + right * 1.8f, right, forward, 7);
            SpawnWarmGardenLamp(parent, ground + back * 4.4f - right * 3.25f, faceCenter);
            SpawnWarmGardenLamp(parent, ground + back * 4.4f + right * 3.25f, faceCenter);
            SpawnSafeParkAsset(parent, "BadukPark/BushA", "BackDepthLowBushLeft",
                ground + back * 6.6f - right * 1.15f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, 18f, 0f), 0.78f, ImportedAssetTone.Leaf);
            SpawnSafeParkAsset(parent, "BadukPark/BushB", "BackDepthLowBushRight",
                ground + back * 6.6f + right * 1.15f + Vector3.up * 0.02f, faceCenter * Quaternion.Euler(0f, -18f, 0f), 0.78f, ImportedAssetTone.Leaf);
            SpawnBackFlowerRibbon(parent, ground + back * 5.35f - right * 0.95f + Vector3.up * 0.04f, right, 8);
            SpawnBackFlowerRibbon(parent, ground + back * 5.35f + right * 0.95f + Vector3.up * 0.04f, -right, 9);
            SpawnBackCurvedPath(parent, ground + back * 3.8f, right, back, faceCenter);
        }

        static void SpawnBackFlowerRibbon(Transform parent, Vector3 start, Vector3 dir, int seed)
        {
            Color[] colors =
            {
                new Color(0.92f, 0.58f, 0.38f),
                new Color(0.96f, 0.76f, 0.34f),
                new Color(0.78f, 0.56f, 0.78f)
            };

            for (int i = 0; i < 6; i++)
            {
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "BackdropBackFlowerRibbon";
                flower.transform.SetParent(parent, false);
                flower.transform.position = start + dir.normalized * (i * 0.22f) + Vector3.up * 0.03f;
                flower.transform.localScale = new Vector3(0.09f, 0.065f, 0.09f);
                ApplyColor(flower, colors[(seed + i) % colors.Length]);
            }
        }

        static void SpawnBackCurvedPath(Transform parent, Vector3 center, Vector3 right, Vector3 back, Quaternion rotation)
        {
            for (int i = -5; i <= 5; i++)
            {
                float x = i * 0.46f;
                float z = Mathf.Abs(i) * 0.12f;
                CreatePrimitive(parent, "BackdropBackPathStone", PrimitiveType.Cylinder,
                    center + right * x + back * z + Vector3.up * 0.04f,
                    rotation * Quaternion.Euler(90f, i * 11f, 0f), new Vector3(0.32f, 0.014f, 0.22f), new Color(0.68f, 0.62f, 0.51f));
            }
        }

        static void SpawnCircularWalkway(Transform parent, Vector3 ground, Vector3 right, Vector3 forward)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                Vector3 dir = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                CreatePrimitive(parent, "BackdropRoundWalkway", PrimitiveType.Cube,
                    ground + dir * 3.15f + Vector3.up * 0.04f, rot,
                    new Vector3(0.88f, 0.012f, 0.30f), new Color(0.66f, 0.59f, 0.47f));
            }
        }

        static void SpawnGroundFlowerDots(Transform parent, Vector3 center, Vector3 tangent, Vector3 dir, int seed)
        {
            Color[] colors =
            {
                new Color(0.92f, 0.58f, 0.38f),
                new Color(0.96f, 0.76f, 0.34f),
                new Color(0.78f, 0.56f, 0.78f)
            };

            for (int i = 0; i < 5; i++)
            {
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "BackdropRingFlower";
                flower.transform.SetParent(parent, false);
                flower.transform.position = center + tangent * ((i - 2) * 0.18f) + dir * ((i % 2) * 0.12f);
                flower.transform.localScale = new Vector3(0.09f, 0.065f, 0.09f);
                ApplyColor(flower, colors[(seed + i) % colors.Length]);
            }
        }

        enum ImportedAssetTone
        {
            Tree,
            Leaf,
            Wood
        }

        static void SpawnSafeParkAsset(Transform parent, string resourcePath, string name, Vector3 position,
            Quaternion rotation, float scale, ImportedAssetTone tone)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
                return;

            var instance = Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale *= scale;
            ApplySafeImportedMaterials(instance, tone);
            RemoveAssetColliders(instance);
        }

        static void ApplySafeImportedMaterials(GameObject root, ImportedAssetTone tone)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = new Material(Shader.Find("Standard"));
                string n = renderer.gameObject.name.ToLowerInvariant();

                if (tone == ImportedAssetTone.Wood)
                    material.color = new Color(0.52f, 0.34f, 0.18f);
                else if (n.Contains("trunk") || n.Contains("stem"))
                    material.color = new Color(0.43f, 0.29f, 0.18f);
                else if (tone == ImportedAssetTone.Leaf)
                    material.color = new Color(0.36f, 0.58f, 0.28f);
                else
                    material.color = new Color(0.42f, 0.66f, 0.34f);

                material.SetFloat("_Glossiness", 0.06f);
                renderer.material = material;
            }
        }

        static void RemoveAssetColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        static void SpawnFence(Transform parent, Vector3 center, Vector3 right, Quaternion rotation)
        {
            for (int i = -2; i <= 2; i++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "FencePost";
                post.transform.SetParent(parent, false);
                post.transform.position = center + right * (i * 1.05f) + Vector3.up * 0.30f;
                post.transform.rotation = rotation;
                post.transform.localScale = new Vector3(0.08f, 0.60f, 0.08f);

                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "FenceRail";
                rail.transform.SetParent(parent, false);
                rail.transform.position = center + right * (i * 1.05f) + Vector3.up * 0.42f;
                rail.transform.rotation = rotation;
                rail.transform.localScale = new Vector3(1.02f, 0.06f, 0.06f);

                Color wood = new Color(0.46f, 0.30f, 0.16f);
                ApplyColor(post, wood);
                ApplyColor(rail, wood);
            }
        }

        static void SpawnBadukSelectionProps(Transform parent, Vector3 center, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            Vector3 tableCenter = center + forward * 0.95f + Vector3.up * 0.42f;
            var table = CreatePrimitive(parent, "SelectionBadukTable", PrimitiveType.Cube,
                tableCenter, rotation, new Vector3(1.8f, 0.08f, 0.96f), new Color(0.52f, 0.31f, 0.16f));

            var board = CreatePrimitive(parent, "SelectionBadukBoard", PrimitiveType.Cube,
                tableCenter + Vector3.up * 0.06f, rotation, new Vector3(0.86f, 0.035f, 0.64f), new Color(0.86f, 0.66f, 0.38f));

            SpawnBoardGrid(parent, board.transform.position + Vector3.up * 0.025f, right, forward, rotation);
            SpawnMiniStones(parent, board.transform.position + Vector3.up * 0.052f, right, forward);
            SpawnTeaCups(parent, tableCenter + Vector3.up * 0.08f, right, forward, rotation);
            SpawnSideBench(parent, center + forward * 1.55f - right * 2.25f + Vector3.up * 0.24f, rotation);
            SpawnSideBench(parent, center + forward * 1.55f + right * 2.25f + Vector3.up * 0.24f, rotation);
            SpawnSmallSign(parent, center + forward * 1.45f + right * 0.02f + Vector3.up * 0.92f, rotation);
        }

        static void SpawnForegroundBadukScene(Transform parent, Vector3 anchorPosition, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            Vector3 floor = anchorPosition + forward * 2.05f + Vector3.down * 1.18f;
            Vector3 tableCenter = floor + Vector3.up * 0.46f;

            CreatePrimitive(parent, "ForegroundBadukTable", PrimitiveType.Cube,
                tableCenter, rotation, new Vector3(1.55f, 0.075f, 0.82f), new Color(0.48f, 0.27f, 0.13f));
            CreatePrimitive(parent, "ForegroundBadukBoard", PrimitiveType.Cube,
                tableCenter + Vector3.up * 0.07f, rotation, new Vector3(0.74f, 0.035f, 0.55f), new Color(0.86f, 0.66f, 0.38f));

            Vector3 boardTop = tableCenter + Vector3.up * 0.11f;
            SpawnBoardGrid(parent, boardTop + Vector3.up * 0.015f, right, forward, rotation);
            SpawnMiniStones(parent, boardTop + Vector3.up * 0.04f, right, forward);

            SpawnSelectionSpectator(parent, floor - right * 1.10f + forward * 0.08f, tableCenter, rotation, 0);
            SpawnSelectionSpectator(parent, floor + right * 1.10f + forward * 0.08f, tableCenter, rotation, 1);
            SpawnSelectionSpectator(parent, floor - right * 0.72f + forward * 0.72f, tableCenter, rotation, 2);
            SpawnSelectionSpectator(parent, floor + right * 0.74f + forward * 0.72f, tableCenter, rotation, 3);
        }

        static void SpawnSelectionSpectator(Transform parent, Vector3 footPosition, Vector3 lookTarget, Quaternion baseRotation, int index)
        {
            Vector3 toTarget = Vector3.ProjectOnPlane(lookTarget - footPosition, Vector3.up);
            Quaternion rotation = toTarget.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toTarget.normalized, Vector3.up)
                : baseRotation;

            Color[] clothes =
            {
                new Color(0.28f, 0.35f, 0.47f),
                new Color(0.46f, 0.34f, 0.24f),
                new Color(0.34f, 0.41f, 0.32f),
                new Color(0.52f, 0.45f, 0.36f)
            };

            Color cloth = clothes[index % clothes.Length];
            float height = 0.72f + (index % 2) * 0.04f;
            float legHeight = height * 0.38f;
            float bodyHeight = height * 0.44f;
            float headRadius = height * 0.10f;
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;

            var body = CreatePrimitive(parent, "SelectionSpectatorBody", PrimitiveType.Capsule,
                footPosition + Vector3.up * (legHeight + bodyHeight * 0.5f),
                rotation, new Vector3(0.18f, bodyHeight * 0.5f, 0.16f), cloth);
            var head = CreatePrimitive(parent, "SelectionSpectatorHead", PrimitiveType.Sphere,
                footPosition + Vector3.up * (legHeight + bodyHeight + headRadius),
                rotation, Vector3.one * (headRadius * 2f), new Color(0.96f, 0.78f, 0.66f));

            CreatePrimitive(parent, "SelectionSpectatorHair", PrimitiveType.Sphere,
                head.transform.position + Vector3.up * (headRadius * 0.45f) - forward * (headRadius * 0.04f),
                rotation, new Vector3(headRadius * 1.85f, headRadius * 0.70f, headRadius * 1.65f), new Color(0.62f, 0.62f, 0.60f));
            CreatePrimitive(parent, "SelectionSpectatorLeftEye", PrimitiveType.Sphere,
                head.transform.position + Vector3.up * (headRadius * 0.12f) + forward * (headRadius * 0.92f) - right * (headRadius * 0.35f),
                rotation, Vector3.one * (headRadius * 0.16f), new Color(0.07f, 0.06f, 0.05f));
            CreatePrimitive(parent, "SelectionSpectatorRightEye", PrimitiveType.Sphere,
                head.transform.position + Vector3.up * (headRadius * 0.12f) + forward * (headRadius * 0.92f) + right * (headRadius * 0.35f),
                rotation, Vector3.one * (headRadius * 0.16f), new Color(0.07f, 0.06f, 0.05f));
            CreatePrimitive(parent, "SelectionSpectatorSmile", PrimitiveType.Cube,
                head.transform.position - Vector3.up * (headRadius * 0.28f) + forward * (headRadius * 0.96f),
                rotation, new Vector3(headRadius * 0.62f, headRadius * 0.055f, headRadius * 0.035f), new Color(0.44f, 0.12f, 0.10f));

            CreatePrimitive(parent, "SelectionSpectatorArmL", PrimitiveType.Cube,
                footPosition + Vector3.up * (legHeight + bodyHeight * 0.54f) - right * 0.14f + forward * 0.03f,
                rotation * Quaternion.Euler(12f, 0f, 12f), new Vector3(0.04f, bodyHeight * 0.48f, 0.04f), cloth);
            CreatePrimitive(parent, "SelectionSpectatorArmR", PrimitiveType.Cube,
                footPosition + Vector3.up * (legHeight + bodyHeight * 0.54f) + right * 0.14f + forward * 0.03f,
                rotation * Quaternion.Euler(8f, 0f, -16f), new Vector3(0.04f, bodyHeight * 0.48f, 0.04f), cloth);

            CreatePrimitive(parent, "SelectionSpectatorLegL", PrimitiveType.Cube,
                footPosition + Vector3.up * (legHeight * 0.5f) - right * 0.04f,
                rotation, new Vector3(0.045f, legHeight, 0.045f), new Color(0.25f, 0.27f, 0.29f));
            CreatePrimitive(parent, "SelectionSpectatorLegR", PrimitiveType.Cube,
                footPosition + Vector3.up * (legHeight * 0.5f) + right * 0.04f,
                rotation, new Vector3(0.045f, legHeight, 0.045f), new Color(0.25f, 0.27f, 0.29f));

        }

        static void SpawnBoardGrid(Transform parent, Vector3 center, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            Color lineColor = new Color(0.16f, 0.11f, 0.07f);
            for (int i = -2; i <= 2; i++)
            {
                CreatePrimitive(parent, "SelectionBoardGridX", PrimitiveType.Cube,
                    center + right * (i * 0.145f), rotation,
                    new Vector3(0.008f, 0.008f, 0.55f), lineColor);
                CreatePrimitive(parent, "SelectionBoardGridZ", PrimitiveType.Cube,
                    center + forward * (i * 0.105f), rotation,
                    new Vector3(0.72f, 0.008f, 0.008f), lineColor);
            }
        }

        static void SpawnMiniStones(Transform parent, Vector3 center, Vector3 right, Vector3 forward)
        {
            Vector2[] black =
            {
                new(-1, -1), new(0, -1), new(1, 0), new(-1, 1)
            };
            Vector2[] white =
            {
                new(1, -1), new(0, 0), new(-1, 0), new(1, 1)
            };

            foreach (var point in black)
                CreatePrimitive(parent, "SelectionBlackStone", PrimitiveType.Sphere,
                    center + right * (point.x * 0.145f) + forward * (point.y * 0.105f),
                    Quaternion.identity, new Vector3(0.055f, 0.020f, 0.055f), new Color(0.05f, 0.05f, 0.055f));

            foreach (var point in white)
                CreatePrimitive(parent, "SelectionWhiteStone", PrimitiveType.Sphere,
                    center + right * (point.x * 0.145f) + forward * (point.y * 0.105f),
                    Quaternion.identity, new Vector3(0.055f, 0.020f, 0.055f), new Color(0.94f, 0.92f, 0.86f));
        }

        static void SpawnTeaCups(Transform parent, Vector3 tableCenter, Vector3 right, Vector3 forward, Quaternion rotation)
        {
            CreatePrimitive(parent, "SelectionTeaCupA", PrimitiveType.Cylinder,
                tableCenter - right * 0.66f + forward * 0.25f, rotation,
                new Vector3(0.10f, 0.065f, 0.10f), new Color(0.76f, 0.36f, 0.18f));
            CreatePrimitive(parent, "SelectionTeaCupB", PrimitiveType.Cylinder,
                tableCenter + right * 0.66f + forward * 0.22f, rotation,
                new Vector3(0.10f, 0.065f, 0.10f), new Color(0.18f, 0.38f, 0.44f));
        }

        static void SpawnSideBench(Transform parent, Vector3 center, Quaternion rotation)
        {
            CreatePrimitive(parent, "SelectionBenchSeat", PrimitiveType.Cube,
                center, rotation, new Vector3(0.72f, 0.08f, 0.24f), new Color(0.44f, 0.27f, 0.12f));
            CreatePrimitive(parent, "SelectionBenchBack", PrimitiveType.Cube,
                center + Vector3.up * 0.22f, rotation, new Vector3(0.72f, 0.22f, 0.06f), new Color(0.38f, 0.22f, 0.10f));
        }

        static void SpawnSmallSign(Transform parent, Vector3 center, Quaternion rotation)
        {
            var sign = CreatePrimitive(parent, "SelectionBadukSign", PrimitiveType.Cube,
                center, rotation, new Vector3(1.28f, 0.38f, 0.035f), new Color(0.92f, 0.86f, 0.70f));

            var canvasGO = new GameObject("SelectionBadukSignText");
            canvasGO.transform.SetParent(parent, false);
            canvasGO.transform.position = sign.transform.position - (rotation * Vector3.forward) * 0.024f;
            canvasGO.transform.rotation = rotation;
            canvasGO.transform.localScale = Vector3.one * 0.0012f;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;
            scaler.referencePixelsPerUnit = 140f;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(canvasGO.transform, false);
            var rt = textGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(760, 180);

            var text = textGO.GetComponent<Text>();
            text.text = "탑골공원 바둑 쉼터";
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Droid Sans Fallback", "sans-serif" }, 34);
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.12f, 0.08f);
        }

        static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position,
            Quaternion rotation, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;
            ApplyColor(obj, color);
            return obj;
        }

        static void ApplyColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", 0.08f);
            renderer.material = material;
            Object.Destroy(obj.GetComponent<Collider>());
        }

        static void ApplyLobbySkyMood()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.90f, 0.92f);
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 38f;
            RenderSettings.ambientLight = new Color(0.76f, 0.74f, 0.62f);
        }

        static void SoftenTreePalette(GameObject tree)
        {
            var renderers = tree.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                if (material == null)
                    continue;

                Color original = material.color;
                bool looksLikeLeaf = original.g >= original.r && original.g >= original.b;
                bool looksLikeWood = original.r >= original.g && original.g >= original.b;

                if (looksLikeLeaf)
                    material.color = Color.Lerp(original, new Color(0.48f, 0.72f, 0.42f, original.a), 0.45f);
                else if (looksLikeWood)
                    material.color = Color.Lerp(original, new Color(0.50f, 0.34f, 0.20f, original.a), 0.35f);

                material.SetFloat("_Glossiness", 0.05f);
            }
        }

        static void SpawnSunLight(Transform parent, Vector3 pos, Vector3 forward, Vector3 right)
        {
            var lightObj = new GameObject("BackdropSunlight");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.position = pos;
            lightObj.transform.rotation = Quaternion.LookRotation((forward - right * 0.35f - Vector3.up * 0.18f).normalized, Vector3.up);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.78f);
            light.intensity = 0.72f;
        }

        static void SpawnStylizedSun(Transform parent, Vector3 horizonBase, Vector3 right, Quaternion rotation)
        {
            Vector3 sunPos = horizonBase - right.normalized * 5.0f + Vector3.up * 2.45f;
            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "BackdropSun";
            sun.transform.SetParent(parent, false);
            sun.transform.position = sunPos;
            sun.transform.rotation = rotation;
            sun.transform.localScale = new Vector3(0.38f, 0.38f, 0.04f);
            ApplyColor(sun, new Color(1.0f, 0.86f, 0.46f));
        }
    }
}
