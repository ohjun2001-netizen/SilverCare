// 모든 씬 XR Origin에 붙여두면 XRInteractorLineVisual 참조 누락을 런타임에 자동 수정
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SilverCare.Common
{
    public class XRLineVisualFixer : MonoBehaviour
    {
        void Awake()
        {
            foreach (var visual in FindObjectsOfType<XRInteractorLineVisual>(true))
            {
                var ray = visual.GetComponent<XRRayInteractor>();
                if (ray == null) continue;

                var field = typeof(XRInteractorLineVisual)
                    .GetField("m_LineRenderable",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;

                field.SetValue(visual, ray);
                visual.enabled = false;
                visual.enabled = true; // OnEnable 재실행
            }
        }
    }
}
