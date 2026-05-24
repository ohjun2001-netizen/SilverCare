using UnityEngine;

namespace SilverCare.Common
{
    public static class SelectionBackdropUtility
    {
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

            var root = new GameObject(GetRootName(key));
            Vector3 forward = anchor.forward;
            Vector3 right = anchor.right;
            Vector3 center = anchor.position + forward * 5.6f;
            Vector3 ground = center + Vector3.down * 1.85f;
            Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);

            SpawnGround(root.transform, ground, facing);
            SpawnSky(root.transform, ground, facing);
            SpawnMountains(root.transform, ground + forward * 5.9f, right, facing);

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
            SpawnFence(root.transform, ground - forward * 1.8f, right, facing);
        }

        public static void ClearBackdrop(string key)
        {
            var root = GameObject.Find(GetRootName(key));
            if (root != null)
                Object.Destroy(root);
        }

        static string GetRootName(string key) => $"SelectionBackdrop_{key}";

        static void SpawnGround(Transform parent, Vector3 position, Quaternion rotation)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "BackdropGround";
            ground.transform.SetParent(parent, false);
            ground.transform.position = position - Vector3.up * 0.0125f;
            ground.transform.rotation = rotation;
            ground.transform.localScale = new Vector3(18.5f, 0.025f, 14f);

            var renderer = ground.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.42f, 0.62f, 0.34f);
            material.SetFloat("_Glossiness", 0.08f);
            renderer.material = material;
            Object.Destroy(ground.GetComponent<Collider>());
        }

        static void SpawnSky(Transform parent, Vector3 ground, Quaternion rotation)
        {
            var prefab = Resources.Load<GameObject>("StylizedNature/sky");
            if (prefab == null) return;

            var sky = Object.Instantiate(prefab, parent);
            sky.name = "BackdropSky";
            Vector3 forward = rotation * Vector3.forward;
            sky.transform.position = ground + Vector3.up * 2.05f + forward * 9.2f;
            sky.transform.rotation = rotation;
            sky.transform.localScale *= 8.5f;
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

                Color wood = new Color(0.34f, 0.20f, 0.10f);
                ApplyColor(post, wood);
                ApplyColor(rail, wood);
            }
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
    }
}
