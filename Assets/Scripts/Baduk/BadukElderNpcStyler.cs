using UnityEngine;

namespace Baduk
{
    public class BadukElderNpcStyler : MonoBehaviour
    {
        static readonly string[] DisableNames =
        {
            "cigarette", "crowbar", "fireaxe", "glock", "phone",
            "cap", "cap2", "cap3", "chain1", "chain2", "chain3",
            "banker_suit", "cock_suit", "cock_suit_hat", "farmer_suit", "farmer_suit_hat",
            "fireman_suit", "fireman_suit_hat", "mechanic_suit", "mechanic_suit_hat",
            "nurse_suit", "police_suit", "police_suit_hat", "roober_suit", "roober_suit_hat",
            "security_guard_suit", "security_guard_suit_hat", "seller_suit", "worker_suit", "worker_suit_hat",
            "shortpants", "tank_top", "t_shirt", "scarf",
            "beard_a", "beard_b", "beard_c", "beard_d",
            "hair_a", "hair_b", "hair_c", "hair_d", "hair_e",
            "glasses", "jacket", "pullover", "shirt", "trousers",
            "shoes1", "shoes2", "shoes3"
        };

        static readonly Color[] CardiganPalette =
        {
            new Color(0.84f, 0.84f, 0.70f),
            new Color(0.59f, 0.69f, 0.82f),
            new Color(0.73f, 0.77f, 0.72f),
            new Color(0.69f, 0.66f, 0.61f)
        };

        static readonly Color[] ShirtPalette =
        {
            new Color(0.94f, 0.94f, 0.91f),
            new Color(0.88f, 0.91f, 0.94f),
            new Color(0.92f, 0.90f, 0.86f)
        };

        static readonly Color[] TrouserPalette =
        {
            new Color(0.36f, 0.39f, 0.46f),
            new Color(0.43f, 0.42f, 0.39f),
            new Color(0.30f, 0.35f, 0.31f)
        };

        public void ApplyStyle(int variationSeed)
        {
            var random = new System.Random(variationSeed);
            bool female = random.NextDouble() > 0.52;

            foreach (string name in DisableNames)
                SetActive(name, false);

            SetSkinTone(random);
            SetHair(female, random);
            SetFace(random);
            SetClothes(random);
            SetShoes(random);
            SetGlasses(random.NextDouble() > 0.35);
        }

        void SetSkinTone(System.Random random)
        {
            Color skin = Color.Lerp(new Color(0.98f, 0.84f, 0.73f), new Color(0.93f, 0.77f, 0.66f), (float)random.NextDouble() * 0.28f);
            ForceSolidSkin(FindDeep("skin_head"), skin);
            ForceSolidSkin(FindDeep("skin_body"), skin);
            ForceSolidSkin(FindDeep("human_mesh"), skin);
            ForceSolidSkin(FindDeep("human_head_mesh"), skin);
            TintSkinLikeRenderers(skin);
        }

        void SetHair(bool female, System.Random random)
        {
            string hairName = female
                ? (random.NextDouble() > 0.5 ? "hair_c" : "hair_d")
                : (random.NextDouble() > 0.5 ? "hair_b" : "hair_e");

            SetActive(hairName, true);

            Transform hair = FindDeep(hairName);
            if (hair != null)
            {
                Color gray = Color.Lerp(new Color(0.58f, 0.58f, 0.58f), new Color(0.84f, 0.84f, 0.82f), (float)random.NextDouble() * 0.65f);
                foreach (var renderer in hair.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.materials)
                    {
                        material.color = gray;
                        material.SetFloat("_Glossiness", 0.18f);
                    }
                }
            }
        }

        void SetFace(System.Random random)
        {
            var headMesh = FindDeep("human_head_mesh");
            if (headMesh != null)
                TintRenderer(headMesh, Color.Lerp(new Color(0.97f, 0.95f, 0.92f), new Color(0.91f, 0.88f, 0.84f), (float)random.NextDouble() * 0.35f));
        }

        void SetClothes(System.Random random)
        {
            bool wearCardigan = random.NextDouble() > 0.25;

            SetActive("trousers", true);
            TintRenderer(FindDeep("trousers"), TrouserPalette[random.Next(TrouserPalette.Length)]);

            SetActive("shirt", true);
            TintRenderer(FindDeep("shirt"), ShirtPalette[random.Next(ShirtPalette.Length)]);

            if (wearCardigan)
            {
                SetActive("jacket", true);
                TintRenderer(FindDeep("jacket"), CardiganPalette[random.Next(CardiganPalette.Length)]);
            }
            else
            {
                SetActive("pullover", true);
                TintRenderer(FindDeep("pullover"), CardiganPalette[random.Next(CardiganPalette.Length)]);
            }
        }

        void SetShoes(System.Random random)
        {
            string shoeName = random.NextDouble() > 0.45 ? "shoes1" : "shoes2";
            SetActive(shoeName, true);
            TintRenderer(FindDeep(shoeName), new Color(0.23f, 0.23f, 0.24f));
        }

        void SetGlasses(bool enabled)
        {
            SetActive("glasses", enabled);
            if (!enabled) return;

            Transform glasses = FindDeep("glasses");
            if (glasses == null) return;

            foreach (var renderer in glasses.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    material.color = new Color(0.26f, 0.22f, 0.18f);
                    material.SetFloat("_Glossiness", 0.45f);
                }
            }
        }

        void SetActive(string name, bool active)
        {
            Transform target = FindDeep(name);
            if (target != null)
                target.gameObject.SetActive(active);
        }

        void TintRenderer(Transform target, Color color)
        {
            if (target == null) return;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    material.color = color;
                    material.SetFloat("_Glossiness", 0.1f);
                }
            }
        }

        void TintSkinLikeRenderers(Color skinColor)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                string rendererName = renderer.name.ToLowerInvariant();
                bool likelySkinObject =
                    rendererName.Contains("human_mesh") ||
                    rendererName.Contains("human_head_mesh") ||
                    rendererName.Contains("skin_");

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var material = renderer.materials[i];
                    string materialName = material != null ? material.name.ToLowerInvariant() : string.Empty;
                    bool likelySkinMaterial =
                        materialName.Contains("skin") ||
                        materialName.Contains("body") ||
                        materialName.Contains("head") ||
                        materialName.Contains("human");

                    if (!likelySkinObject && !likelySkinMaterial)
                        continue;

                    material.color = skinColor;
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", skinColor);
                    material.SetFloat("_Glossiness", 0.08f);
                }
            }
        }

        void ForceSolidSkin(Transform target, Color skinColor)
        {
            if (target == null) return;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                var materials = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = skinColor;
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", skinColor);
                    material.SetFloat("_Glossiness", 0.08f);
                    materials[i] = material;
                }

                renderer.materials = materials;
            }
        }

        Transform FindDeep(string name)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var transform in transforms)
            {
                if (string.Equals(transform.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return transform;
            }

            return null;
        }
    }
}
