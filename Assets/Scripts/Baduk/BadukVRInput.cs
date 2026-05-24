// Assets/Scripts/Baduk/BadukVRInput.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using SilverCare.Common;

namespace Baduk
{
#if UNITY_XR_INTERACTION_TOOLKIT
    using UnityEngine.XR.Interaction.Toolkit;

    public class BadukVRInput : MonoBehaviour, IBadukInput
    {
        public System.Action<int, int> OnIntersectionClicked { get; set; }

        [Header("XR Settings")]
        [SerializeField] XRRayInteractor rayInteractor;

        bool _inputEnabled;
        BadukBoard _board;

        public void EnableInput() => _inputEnabled = true;
        public void DisableInput() => _inputEnabled = false;

        public void OnBoardReady(int r0, int c0, int r1, int c1)
        {
            float cx = (c1 - c0) * BadukBoard.CELL / 2f;
            float cy = (r1 - r0) * BadukBoard.CELL / 2f;
            _board = GetComponent<BadukBoard>();
            var boardObj = _board?.transform;
            if (boardObj != null)
                SetupDeskBoard(boardObj, cx, cy);
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

        void Update()
        {
            if (!_inputEnabled) return;
            if (XRPointerInput.TryGetSelectionHit(20f, out RaycastHit hit))
                TryHandleHit(hit);
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            if (!_inputEnabled) return;
            var go = args.interactableObject?.transform?.gameObject;
            if (go == null) return;
            TryParseIntersection(go.name);
        }

        void TryHandleHit(RaycastHit hit)
        {
            if (hit.collider == null)
                return;

            if (TryParseIntersection(hit.collider.gameObject.name))
                return;

            if (_board == null)
                _board = GetComponent<BadukBoard>();

            if (_board != null && _board.TryWorldToIntersection(hit.point, out int row, out int col))
                OnIntersectionClicked?.Invoke(row, col);
        }

        bool TryParseIntersection(string name)
        {
            string[] parts = name.Split('_');
            if (parts.Length < 3 || parts[0] != "I") return false;
            if (int.TryParse(parts[1], out int row) && int.TryParse(parts[2], out int col))
            {
                OnIntersectionClicked?.Invoke(row, col);
                return true;
            }

            return false;
        }

        void FindRayInteractor()
        {
#pragma warning disable CS0618
            var interactors = Object.FindObjectsOfType<XRRayInteractor>();
#pragma warning restore CS0618
            if (interactors.Length > 0) rayInteractor = interactors[0];
            else Debug.LogWarning("[BadukVRInput] XRRayInteractor not found. Falling back to pointer ray.");
        }

        static void SetupDeskBoard(Transform boardObj, float cx, float cy)
        {
            Camera cam = Camera.main;
            bool isReplayStyle = SceneManager.GetActiveScene().name is "BadukReplay" or "BadukPrediction";
            float boardSizeTarget = isReplayStyle ? 0.48f : 0.92f;
            float boardDistance = isReplayStyle ? 0.52f : 0.20f;
            float tableHeightOffset = isReplayStyle ? 0.32f : 0.62f;
            BadukDeskLayoutUtility.ApplyDeskLayout(
                boardObj,
                cx,
                cy,
                boardSizeTarget,
                boardDistance,
                tableHeightOffset,
                SceneManager.GetActiveScene().path,
                cam,
                out Vector3 boardCenter,
                out float tableY);

            float scale = boardObj.localScale.x;
            BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
        }
    }
#else
    public class BadukVRInput : MonoBehaviour, IBadukInput
    {
        public System.Action<int, int> OnIntersectionClicked { get; set; }

        bool _inputEnabled;
        BadukBoard _board;

        public void EnableInput() => _inputEnabled = true;
        public void DisableInput() => _inputEnabled = false;

        public void OnBoardReady(int r0, int c0, int r1, int c1)
        {
            float cx = (c1 - c0) * BadukBoard.CELL / 2f;
            float cy = (r1 - r0) * BadukBoard.CELL / 2f;
            _board = GetComponent<BadukBoard>();
            var boardObj = _board?.transform;
            if (boardObj != null)
                SetupDeskBoard(boardObj, cx, cy);
        }

        static void SetupDeskBoard(Transform boardObj, float cx, float cy)
        {
            Camera cam = Camera.main;
            bool isReplayStyle = SceneManager.GetActiveScene().name is "BadukReplay" or "BadukPrediction";
            float boardSizeTarget = isReplayStyle ? 0.48f : 0.92f;
            float boardDistance = isReplayStyle ? 0.52f : 0.20f;
            float tableHeightOffset = isReplayStyle ? 0.32f : 0.62f;
            BadukDeskLayoutUtility.ApplyDeskLayout(
                boardObj,
                cx,
                cy,
                boardSizeTarget,
                boardDistance,
                tableHeightOffset,
                SceneManager.GetActiveScene().path,
                cam,
                out Vector3 boardCenter,
                out float tableY);

            float scale = boardObj.localScale.x;
            BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY, boardObj.rotation);
        }

        void Update()
        {
            if (!_inputEnabled) return;
            if (XRPointerInput.TryGetSelectionHit(20f, out RaycastHit hit))
            {
                string[] parts = hit.collider.gameObject.name.Split('_');
                if (parts.Length >= 3 &&
                    parts[0] == "I" &&
                    int.TryParse(parts[1], out int row) &&
                    int.TryParse(parts[2], out int col))
                {
                    OnIntersectionClicked?.Invoke(row, col);
                    return;
                }

                if (_board == null)
                    _board = GetComponent<BadukBoard>();

                if (_board != null && _board.TryWorldToIntersection(hit.point, out int nearestRow, out int nearestCol))
                    OnIntersectionClicked?.Invoke(nearestRow, nearestCol);
            }
        }

        void Awake()
        {
            Debug.LogWarning("[BadukVRInput] XR Interaction Toolkit not installed. Using pointer fallback.");
        }
    }
#endif
}
