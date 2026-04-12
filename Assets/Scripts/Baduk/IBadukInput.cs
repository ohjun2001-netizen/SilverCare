// Assets/Scripts/Baduk/IBadukInput.cs
namespace Baduk
{
    /// <summary>입력 추상화 - Desktop(마우스)과 VR(컨트롤러) 공통 인터페이스</summary>
    public interface IBadukInput
    {
        /// <summary>교차점 클릭 이벤트 (row, col)</summary>
        System.Action<int, int> OnIntersectionClicked { get; set; }

        /// <summary>입력 활성화</summary>
        void EnableInput();

        /// <summary>입력 비활성화</summary>
        void DisableInput();

        /// <summary>보드 세팅 후 카메라/환경 조정</summary>
        void OnBoardReady(int r0, int c0, int r1, int c1);
    }
}
