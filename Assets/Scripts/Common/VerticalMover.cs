// Assets/Scripts/Common/VerticalMover.cs
// R키: 위로, F키: 아래로 이동 (에디터 테스트용)
// 오른쪽 조이스틱 Y축: 위아래 이동 (Quest 기기)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace SilverCare.Common
{
    public class VerticalMover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float joystickDeadzone = 0.25f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            foreach (var origin in Object.FindObjectsOfType<XROrigin>(true))
            {
                if (origin.GetComponent<VerticalMover>() == null)
                    origin.gameObject.AddComponent<VerticalMover>();
            }

            // XROrigin 없을 때 이름으로 폴백
            if (Object.FindObjectOfType<XROrigin>(true) == null)
            {
                var go = GameObject.Find("XR Origin");
                if (go != null && go.GetComponent<VerticalMover>() == null)
                    go.AddComponent<VerticalMover>();
            }
        }

        void Update()
        {
            float vertical = 0f;

            // 키보드 (에디터 테스트: R=위, F=아래)
            if (Input.GetKey(KeyCode.R)) vertical += 1f;
            if (Input.GetKey(KeyCode.F)) vertical -= 1f;

            // 오른쪽 조이스틱 Y축 (Quest 기기: 위아래 높낮이)
            var rightDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
            if (rightDevices.Count > 0 &&
                rightDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightAxis))
            {
                float joy = rightAxis.y;
                if (Mathf.Abs(joy) > joystickDeadzone)
                {
                    float normalized = (Mathf.Abs(joy) - joystickDeadzone) / (1f - joystickDeadzone);
                    vertical += Mathf.Sign(joy) * normalized;
                }
            }

            if (Mathf.Abs(vertical) > 0.001f)
                transform.position += Vector3.up * Mathf.Clamp(vertical, -1f, 1f) * moveSpeed * Time.deltaTime;
        }
    }
}
