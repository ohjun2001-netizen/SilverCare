// Assets/Scripts/Baduk/BadukVRBoardSetup.cs
// VR-only helper that attaches XR interactables to board intersections.
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Baduk
{
    public class BadukVRBoardSetup : MonoBehaviour
    {
        public void AttachInteractables()
        {
#pragma warning disable CS0618
            var colliders = Object.FindObjectsOfType<SphereCollider>();
#pragma warning restore CS0618

            foreach (var collider in colliders)
            {
                if (!collider.gameObject.name.StartsWith("I_"))
                    continue;

                if (collider.GetComponent<XRSimpleInteractable>() != null)
                    continue;

                var interactable = collider.gameObject.AddComponent<XRSimpleInteractable>();
                interactable.selectMode = InteractableSelectMode.Single;
            }
        }
    }
}
