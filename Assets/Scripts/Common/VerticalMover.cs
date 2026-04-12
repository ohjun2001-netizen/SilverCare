// Assets/Scripts/Common/VerticalMover.cs
// R키: 위로, F키: 아래로 이동 (XR Origin 기준)
using UnityEngine;

namespace SilverCare.Common
{
    public class VerticalMover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 2f;

        void Update()
        {
            float vertical = 0f;
            if (Input.GetKey(KeyCode.R)) vertical = 1f;
            if (Input.GetKey(KeyCode.F)) vertical = -1f;

            if (vertical != 0f)
                transform.position += Vector3.up * vertical * moveSpeed * Time.deltaTime;
        }
    }
}
