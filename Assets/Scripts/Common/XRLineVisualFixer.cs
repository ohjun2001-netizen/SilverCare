using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace SilverCare.Common
{
    public class XRLineVisualFixer : MonoBehaviour
    {
        readonly Dictionary<int, GameObject> _endMarkers = new();

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshNow();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            foreach (var marker in _endMarkers.Values)
            {
                if (marker != null)
                    Destroy(marker);
            }

            _endMarkers.Clear();
        }

        void Update()
        {
            UpdateEndMarkers();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshNow();
        }

        public void RefreshNow()
        {
            foreach (var visual in FindObjectsOfType<XRInteractorLineVisual>(true))
            {
                var ray = visual.GetComponent<XRRayInteractor>();
                if (ray == null)
                    continue;

                var field = typeof(XRInteractorLineVisual)
                    .GetField("m_LineRenderable", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    field.SetValue(visual, ray);

                ApplyLaserVisual(visual);
                EnsureEndMarker(ray);

                visual.enabled = false;
                visual.enabled = true;
            }
        }

        void UpdateEndMarkers()
        {
            foreach (var ray in FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray == null)
                    continue;

                var marker = EnsureEndMarker(ray);
                if (marker == null)
                    continue;

                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    marker.SetActive(true);
                    marker.transform.position = hit.point + hit.normal * 0.003f;
                    marker.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
                else
                {
                    marker.SetActive(false);
                }
            }
        }

        GameObject EnsureEndMarker(XRRayInteractor ray)
        {
            int id = ray.GetInstanceID();
            if (_endMarkers.TryGetValue(id, out GameObject existing) && existing != null)
                return existing;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{ray.name}_LaserMarker";
            marker.transform.localScale = Vector3.one * 0.028f;
            marker.layer = ray.gameObject.layer;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = MakeMarkerMaterial();

            marker.SetActive(false);
            _endMarkers[id] = marker;
            return marker;
        }

        static void ApplyLaserVisual(XRInteractorLineVisual visual)
        {
            visual.lineWidth = 0.014f;

            var validGrad = new Gradient();
            validGrad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0.2f),
                    new GradientColorKey(new Color(0.2f, 0.75f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0.75f, 1f)
                });
            visual.validColorGradient = validGrad;

            var invalidGrad = new Gradient();
            invalidGrad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 1f)
                });
            visual.invalidColorGradient = invalidGrad;

            var lr = visual.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.widthMultiplier = 1.25f;
                lr.numCapVertices = 8;
                lr.material = MakeLaserMaterial();
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
            }
        }

        static Material MakeLaserMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader)
            {
                color = new Color(0.7f, 0.95f, 1f, 0.98f)
            };
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3100;
            return mat;
        }

        static Material MakeMarkerMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader)
            {
                color = new Color(0.85f, 1f, 1f, 0.95f)
            };
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3101;
            return mat;
        }
    }
}
