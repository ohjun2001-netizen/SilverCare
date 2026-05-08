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
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.0f, 0);

            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            // 실제 바둑판 폭 기준 스케일 (최대 0.45m — 실제 바둑판 42cm 참고)
            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);
            float maxSize = 0.45f;
            float scale = boardWorldMax > 0f ? Mathf.Min(1f, maxSize / boardWorldMax) : 1f;
            boardObj.localScale = Vector3.one * scale;

            // 수평으로 눕히기: yaw만 → 보드가 XZ 평면에 수평으로 눕고
            // near row(높은 인덱스)가 플레이어 쪽을 향함
            boardObj.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

            // 착석 시 테이블 면 높이 (눈높이 -0.35m ≈ 0.65m)
            float tableY = camPos.y - 0.35f;

            // 보드 중심을 앞 0.55m (팔이 자연스럽게 닿는 거리)
            Vector3 boardCenter = camPos + flatForward * 0.55f;
            boardCenter.y = tableY;
            boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);

            // 리플레이/예측 씬: 전통 바둑방 환경 생성 / 그 외: 기본 테이블만
            string sn = SceneManager.GetActiveScene().name;
            if (sn == "BadukReplay" || sn == "BadukPrediction")
                BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
            else
                SpawnTable(boardCenter, cx * scale, cy * scale, tableY);
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
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1.0f, 0);

            Vector3 flatForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);
            float maxSize = 0.45f;
            float scale = boardWorldMax > 0f ? Mathf.Min(1f, maxSize / boardWorldMax) : 1f;
            boardObj.localScale = Vector3.one * scale;

            boardObj.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

            float tableY = camPos.y - 0.35f;
            Vector3 boardCenter = camPos + flatForward * 0.55f;
            boardCenter.y = tableY;
            boardObj.position = boardCenter - boardObj.rotation * (new Vector3(cx, 0f, -cy) * scale);

            string sn = SceneManager.GetActiveScene().name;
            if (sn == "BadukReplay" || sn == "BadukPrediction")
                BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
            else
                SpawnTable(boardCenter, cx * scale, cy * scale, tableY);
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
