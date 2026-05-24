// Assets/Scripts/Baduk/BadukPanelBillboard.cs
using UnityEngine;

namespace Baduk
{
    /// <summary>
    /// 월드공간 설명판이 항상 카메라를 정면으로, 수평(월드 업) 기준으로 바라보게 한다.
    /// 위치는 가능하면 바둑판(책상) 중심에 맞춰 잡아, 시선이 어디를 향하든 판 위쪽 중앙에
    /// 오도록 한다. 보드가 아직 준비되지 않았으면 카메라 정면을 임시로 사용한다.
    /// </summary>
    public class BadukPanelBillboard : MonoBehaviour
    {
        public float distance = 1.75f;     // 보드가 없을 때(난이도 선택 등) 카메라 정면 거리
        public float heightOffset = 0.05f; // 눈높이 기준 위/아래 보정
        public float backOffset = 0.35f;   // 보드 중심에서 플레이어 반대쪽(뒤)으로 밀어 판을 가리지 않게

        public BadukBoard board;           // 정렬 기준이 되는 바둑판 (같은 오브젝트의 컴포넌트)
        public bool anchorToBoard;         // true면 보드 중심 기준, false면 카메라 정면

        bool _positioned;

        public void RequestReposition() => _positioned = false;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = cam;

            if (!_positioned)
            {
                if (anchorToBoard && TryGetBoardCenter(out Vector3 boardCenter))
                {
                    // 카메라 → 보드 수평 방향(=플레이어가 보는 쪽). 그 뒤로 살짝 밀어 판 위/뒤에 띄운다.
                    Vector3 toBoard = boardCenter - cam.transform.position;
                    toBoard.y = 0f;
                    Vector3 backDir = toBoard.sqrMagnitude > 1e-4f ? toBoard.normalized : Vector3.forward;

                    Vector3 pos = boardCenter + backDir * backOffset;
                    pos.y = cam.transform.position.y + heightOffset; // 눈높이 → 책상보다 위로 떠 보임
                    transform.position = pos;
                    _positioned = true;
                }
                else
                {
                    // 보드 준비 전: 카메라 정면. 보드가 잡히면 다음 프레임에 보드 기준으로 다시 잡는다.
                    Vector3 flat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                    if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
                    flat.Normalize();

                    Vector3 pos = cam.transform.position + flat * distance;
                    pos.y = cam.transform.position.y + heightOffset;
                    transform.position = pos;
                    if (!anchorToBoard) _positioned = true; // 보드를 안 쓸 땐 한 번만
                }
            }

            // 매 프레임 카메라를 향해 똑바로(월드 업) 정렬 → 절대 기울어지지 않음
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        bool TryGetBoardCenter(out Vector3 center)
        {
            center = default;
            if (board == null) return false;

            // 보드 로컬 원점은 (R0,C0) 교차점. 중심은 (w/2, 0, -h/2). 월드 스케일/회전 포함해 변환.
            float w = (board.C1 - board.C0) * BadukBoard.CELL;
            float h = (board.R1 - board.R0) * BadukBoard.CELL;
            if (w <= 0f && h <= 0f) return false;

            center = board.transform.TransformPoint(new Vector3(w / 2f, 0f, -h / 2f));
            return true;
        }
    }
}
