// Assets/Scripts/Baduk/BadukVRInput.cs
// VR 전용 - XR Interaction Toolkit 필요
// PC 테스트용 마우스 클릭 폴백 내장
using UnityEngine;

namespace Baduk
{
#if UNITY_XR_INTERACTION_TOOLKIT
    using UnityEngine.XR.Interaction.Toolkit;

    public class BadukVRInput : MonoBehaviour, IBadukInput
    {
        public System.Action<int, int> OnIntersectionClicked { get; set; }

        [Header("XR 설정")]
        [SerializeField] XRRayInteractor rayInteractor;

        bool _inputEnabled;

        public void EnableInput()  => _inputEnabled = true;
        public void DisableInput() => _inputEnabled = false;

        public void OnBoardReady(int r0, int c0, int r1, int c1)
        {
            float cx = (c1 - c0) * BadukBoard.CELL / 2f;
            float cy = (r1 - r0) * BadukBoard.CELL / 2f;
            var boardObj = GetComponent<BadukBoard>()?.transform;
            if (boardObj != null)
                PlaceBoardInFront(boardObj, cx, cy);
        }

        void OnEnable()
        {
            if (rayInteractor == null) FindRayInteractor();
            if (rayInteractor != null)
                rayInteractor.selectEntered.AddListener(OnSelect);
        }

        void OnDisable()
        {
            if (rayInteractor != null)
                rayInteractor.selectEntered.RemoveListener(OnSelect);
        }

        // PC 테스트: 마우스 클릭 폴백 (XR Device Simulator에서 카메라 레이 사용)
        void Update()
        {
            if (!_inputEnabled) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                TryParseIntersection(hit.collider.gameObject.name);
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            if (!_inputEnabled) return;
            var go = args.interactableObject?.transform?.gameObject;
            if (go == null) return;
            TryParseIntersection(go.name);
        }

        void TryParseIntersection(string name)
        {
            string[] p = name.Split('_');
            if (p.Length < 3 || p[0] != "I") return;
            if (int.TryParse(p[1], out int row) && int.TryParse(p[2], out int col))
                OnIntersectionClicked?.Invoke(row, col);
        }

        void FindRayInteractor()
        {
#pragma warning disable CS0618
            var interactors = Object.FindObjectsOfType<XRRayInteractor>();
#pragma warning restore CS0618
            if (interactors.Length > 0) rayInteractor = interactors[0];
            else Debug.LogWarning("[BadukVRInput] XRRayInteractor를 찾을 수 없습니다. 마우스 폴백 사용.");
        }

        static void PlaceBoardInFront(Transform boardObj, float cx, float cy)
        {
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.6f, 0);

            // pitch 제거 — 수평 방향만 사용해서 보드가 항상 똑바로 세워짐
            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            // 보드가 카메라를 정면으로 바라보도록 회전 (항상 수직)
            Quaternion yawRot = Quaternion.LookRotation(flatForward, Vector3.up);
            boardObj.rotation = yawRot * Quaternion.Euler(-90, 0, 0);

            // 카메라 앞 20m, 눈높이에 보드 중심 배치
            Vector3 boardCenter = camPos + flatForward * 12f;
            boardCenter.y = 1.5f;
            boardObj.position = boardCenter - boardObj.rotation * new Vector3(cx, 0f, -cy);
        }
    }
#else
    // XR Interaction Toolkit 미설치 시 스텁 (보드 회전 + 마우스 폴백 동작)
    public class BadukVRInput : MonoBehaviour, IBadukInput
    {
        public System.Action<int, int> OnIntersectionClicked { get; set; }

        bool _inputEnabled;

        public void EnableInput()  => _inputEnabled = true;
        public void DisableInput() => _inputEnabled = false;

        public void OnBoardReady(int r0, int c0, int r1, int c1)
        {
            float cx = (c1 - c0) * BadukBoard.CELL / 2f;
            float cy = (r1 - r0) * BadukBoard.CELL / 2f;
            var boardObj = GetComponent<BadukBoard>()?.transform;
            if (boardObj != null)
                PlaceBoardInFront(boardObj, cx, cy);
        }

        static void PlaceBoardInFront(Transform boardObj, float cx, float cy)
        {
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.6f, 0);

            // pitch 제거 — 수평 방향만 사용해서 보드가 항상 똑바로 세워짐
            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            // 보드가 카메라를 정면으로 바라보도록 회전 (항상 수직)
            Quaternion yawRot = Quaternion.LookRotation(flatForward, Vector3.up);
            boardObj.rotation = yawRot * Quaternion.Euler(-90, 0, 0);

            // 카메라 앞 15m, 눈높이에 보드 중심 배치
            Vector3 boardCenter = camPos + flatForward * 12f;
            boardCenter.y = 1.5f;
            boardObj.position = boardCenter - boardObj.rotation * new Vector3(cx, 0f, -cy);
        }

        // PC 테스트: 마우스 클릭으로 돌 놓기
        void Update()
        {
            if (!_inputEnabled) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                string[] p = hit.collider.gameObject.name.Split('_');
                if (p.Length < 3 || p[0] != "I") return;
                if (int.TryParse(p[1], out int row) && int.TryParse(p[2], out int col))
                    OnIntersectionClicked?.Invoke(row, col);
            }
        }

        void Awake()
        {
            Debug.LogWarning("[BadukVRInput] XR Interaction Toolkit 미설치 — 마우스 클릭 폴백으로 돌 놓기.");
        }
    }
#endif
}
