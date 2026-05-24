// 모든 씬 XR Origin에 붙여두면 XRInteractorLineVisual 참조 누락을 런타임에 자동 수정 + 레이저 시각 설정
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

namespace SilverCare.Common
{
    public class XRLineVisualFixer : MonoBehaviour
    {
        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshNow();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
                if (ray == null) continue;

                var field = typeof(XRInteractorLineVisual)
                    .GetField("m_LineRenderable", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;

                field.SetValue(visual, ray);
                ApplyLaserVisual(visual);
                visual.enabled = false;
                visual.enabled = true;
            }
        }

        static void ApplyLaserVisual(XRInteractorLineVisual visual)
        {
            visual.lineWidth = 0.008f;

            var validGrad = new Gradient();
            validGrad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.5f, 0.9f, 1f), 0f),
                    new GradientColorKey(Color.white,                0.15f),
                    new GradientColorKey(new Color(0.3f, 0.75f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.9f, 0.4f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            visual.validColorGradient = validGrad;

            var invalidGrad = new Gradient();
            invalidGrad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0.15f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.8f, 0.4f),
                    new GradientAlphaKey(0.2f, 1f)
                }
            );
            visual.invalidColorGradient = invalidGrad;

            var lr = visual.GetComponent<LineRenderer>();
            if (lr != null)
                lr.material = MakeLaserMaterial();
        }

        static Material MakeLaserMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
