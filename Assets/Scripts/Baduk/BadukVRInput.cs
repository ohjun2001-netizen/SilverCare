// Assets/Scripts/Baduk/BadukVRInput.cs
// VR 전용 - XR Interaction Toolkit 필요
// PC 테스트용 마우스 클릭 폴백 내장
using UnityEngine;
using UnityEngine.SceneManagement;

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
            => SetupSittingBoard(boardObj, cx, cy);

        static void SetupSittingBoard(Transform boardObj, float cx, float cy)
        {
            string sn = SceneManager.GetActiveScene().name;
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.0f, 0);
            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);

            if (sn == "BadukReplay" || sn == "BadukPrediction")
            {
                // 수평 배치 (기보 관전 — 테이블 위)
                float scale = boardWorldMax > 0f ? Mathf.Min(1f, 0.45f / boardWorldMax) : 1f;
                boardObj.localScale = Vector3.one * scale;
                boardObj.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
                float tableY = camPos.y - 0.35f;
                Vector3 boardCenter = camPos + flatForward * 0.55f;
                boardCenter.y = tableY;
                boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);
                BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
            }
            else
            {
                // 수직 배치 (사활 — 정면에 세우기)
                float scale = boardWorldMax > 0f ? Mathf.Min(1f, 0.55f / boardWorldMax) : 1f;
                boardObj.localScale = Vector3.one * scale;
                // local +Z → world up, local +X → world right, local +Y → 플레이어 방향
                boardObj.rotation = Quaternion.LookRotation(Vector3.up, -flatForward);
                Vector3 boardCenter = camPos + flatForward * 1.5f;
                boardCenter.y = camPos.y;
                boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);
            }
        }

        static void SpawnTable(Vector3 center, float halfW, float halfD, float tableY)
        {
            const string TABLE_NAME = "BadukTable";
            var old = GameObject.Find(TABLE_NAME);
            if (old != null) Object.Destroy(old);

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = TABLE_NAME;
            Object.Destroy(table.GetComponent<Collider>());

            float pad   = 0.08f; // 바둑판 여백
            float thick = 0.05f; // 상판 두께
            table.transform.position   = new Vector3(center.x, tableY - thick * 0.5f, center.z);
            table.transform.localScale = new Vector3(halfW * 2f + pad, thick, halfD * 2f + pad);

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var woodColor = new Color(0.80f, 0.62f, 0.32f);
            mat.color = woodColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", woodColor);
            table.GetComponent<Renderer>().material = mat;
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
                SetupSittingBoard(boardObj, cx, cy);
        }

        static void SetupSittingBoard(Transform boardObj, float cx, float cy)
        {
            string sn = SceneManager.GetActiveScene().name;
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.0f, 0);
            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);

            if (sn == "BadukReplay" || sn == "BadukPrediction")
            {
                float scale = boardWorldMax > 0f ? Mathf.Min(1f, 0.45f / boardWorldMax) : 1f;
                boardObj.localScale = Vector3.one * scale;
                boardObj.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
                float tableY = camPos.y - 0.35f;
                Vector3 boardCenter = camPos + flatForward * 0.55f;
                boardCenter.y = tableY;
                boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);
                BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
            }
            else
            {
                float scale = boardWorldMax > 0f ? Mathf.Min(1f, 0.55f / boardWorldMax) : 1f;
                boardObj.localScale = Vector3.one * scale;
                boardObj.rotation = Quaternion.LookRotation(Vector3.up, -flatForward);
                Vector3 boardCenter = camPos + flatForward * 1.5f;
                boardCenter.y = camPos.y;
                boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);
            }
        }

        static void SpawnTable(Vector3 center, float halfW, float halfD, float tableY)
        {
            const string TABLE_NAME = "BadukTable";
            var old = GameObject.Find(TABLE_NAME);
            if (old != null) Object.Destroy(old);

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = TABLE_NAME;
            Object.Destroy(table.GetComponent<Collider>());

            float pad   = 0.08f;
            float thick = 0.05f;
            table.transform.position   = new Vector3(center.x, tableY - thick * 0.5f, center.z);
            table.transform.localScale = new Vector3(halfW * 2f + pad, thick, halfD * 2f + pad);

            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.55f, 0.33f, 0.10f);
            table.GetComponent<Renderer>().material = mat;
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
