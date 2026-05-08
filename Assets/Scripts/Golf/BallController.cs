// Assets/Scripts/Golf/BallController.cs
// 담당: 양석원
// TODO: VR 컨트롤러 스윙 세기/방향 감지, Rigidbody 물리 적용
using UnityEngine;

namespace SilverCare.Golf
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float maxSwingForce = 20f;
        [SerializeField] private LayerMask holeTriggerLayer;

        [Header("Swing Detection")]
        [SerializeField] private float swingThreshold = 1.5f;  // 컨트롤러 속도 임계값

        private Rigidbody _rb;
        private GolfGameManager _manager;
        private bool _isMoving = false;

        void Awake()
        {
            _rb      = GetComponent<Rigidbody>();
            _manager = FindObjectOfType<GolfGameManager>();
        }

        void Update()
        {
            // 공 정지 감지
            if (_isMoving && _rb.velocity.magnitude < 0.05f)
            {
                _isMoving = false;
                // TODO: 양석원 - 스윙 UI 다시 활성화
                var uiManager = FindObjectOfType<GolfUIManager>();
                if (uiManager != null)
                {
                    uiManager.SetSwingUIActive(true);
                }
            }
        }

        /// <summary>VR 컨트롤러 스윙 감지 후 호출 (방향, 세기)</summary>
        public void ApplySwing(Vector3 direction, float force)
        {
            if (_isMoving) return;

            float clampedForce = Mathf.Clamp(force, 0, maxSwingForce);
            _rb.AddForce(direction.normalized * clampedForce, ForceMode.Impulse);
            _isMoving = true;

            _manager?.OnSwingCompleted();
        }

        void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & holeTriggerLayer) != 0)
            {
                _rb.velocity = Vector3.zero;
                _manager?.OnHoleIn();
            }
        }

        public void ResetBall(Vector3 position)
        {
            transform.position = position;
            _rb.velocity        = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _isMoving           = false;
        }
    }
}
