// Assets/Scripts/Baduk/BadukDesktopInput.cs
// Desktop 전용 - 마우스 클릭 + 카메라 자동 조정
using UnityEngine;
using SilverCare.Common;

namespace Baduk
{
    public class BadukDesktopInput : MonoBehaviour, IBadukInput
    {
        public System.Action<int, int> OnIntersectionClicked { get; set; }

        bool _inputEnabled;
        BadukBoard _board;

        public void EnableInput()  => _inputEnabled = true;
        public void DisableInput() => _inputEnabled = false;

        public void OnBoardReady(int r0, int c0, int r1, int c1)
        {
            _board = GetComponent<BadukBoard>();
            float cx   = (c1 - c0) * BadukBoard.CELL / 2f;
            float cz   = -(r1 - r0) * BadukBoard.CELL / 2f;
            float size = Mathf.Max(r1 - r0, c1 - c0) * BadukBoard.CELL;
            float dist = size * 1.2f + 3f;
            Camera.main.transform.position = new Vector3(cx, dist, cz + dist * 0.35f);
            Camera.main.transform.LookAt(new Vector3(cx, 0f, cz));
            Camera.main.backgroundColor = new Color(0.78f, 0.88f, 0.92f);
            Camera.main.clearFlags      = CameraClearFlags.SolidColor;
        }

        void Update()
        {
            if (!_inputEnabled) return;
            if (!XRPointerInput.TryGetSelectionHit(20f, out RaycastHit hit)) return;

            if (TryParseIntersection(hit.collider.name))
                return;

            if (_board != null && _board.TryWorldToIntersection(hit.point, out int row, out int col))
                OnIntersectionClicked?.Invoke(row, col);
        }

        bool TryParseIntersection(string name)
        {
            string[] p = name.Split('_');
            if (p.Length < 3 || p[0] != "I") return false;

            int row = int.Parse(p[1]);
            int col = int.Parse(p[2]);
            OnIntersectionClicked?.Invoke(row, col);
            return true;
        }
    }
}
