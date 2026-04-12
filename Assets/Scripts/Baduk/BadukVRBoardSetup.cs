// Assets/Scripts/Baduk/BadukVRBoardSetup.cs
// VR 전용 - 교차점에 XR Interactable 부착
// 패키지 미설치 시 빈 스텁으로 컴파일됩니다.
using UnityEngine;

namespace Baduk
{
#if UNITY_XR_INTERACTION_TOOLKIT
    using UnityEngine.XR.Interaction.Toolkit;

    public class BadukVRBoardSetup : MonoBehaviour
    {
        public void AttachInteractables()
        {
#pragma warning disable CS0618
            var colliders = Object.FindObjectsOfType<SphereCollider>();
#pragma warning restore CS0618
            foreach (var col in colliders)
            {
                if (!col.gameObject.name.StartsWith("I_")) continue;
                if (col.GetComponent<XRSimpleInteractable>() != null) continue;

                var interactable = col.gameObject.AddComponent<XRSimpleInteractable>();
                interactable.selectMode = InteractableSelectMode.Single;
            }
        }
    }
#else
    public class BadukVRBoardSetup : MonoBehaviour
    {
        public void AttachInteractables()
        {
            Debug.LogWarning("[BadukVRBoardSetup] XR Interaction Toolkit 미설치 - Interactable 부착 생략");
        }
    }
#endif
}
