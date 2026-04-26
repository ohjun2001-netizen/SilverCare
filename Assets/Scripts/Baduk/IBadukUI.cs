// Assets/Scripts/Baduk/IBadukUI.cs
using Baduk.Data;

namespace Baduk
{
    /// <summary>UI 추상화 - Desktop(OnGUI)과 VR(World Canvas) 공통 인터페이스</summary>
    public interface IBadukUI
    {
        System.Action OnNext  { get; set; }
        System.Action OnPrev  { get; set; }
        System.Action OnHint  { get; set; }
        System.Action OnRetry { get; set; }
        System.Action OnBack  { get; set; }
        System.Action<int> OnDifficultySelected { get; set; }

        void ShowDifficultySelect();
        void ShowProblem(BadukProblem problem, int idx, int total);
        void ShowResult(ProblemResult result, string explanation = "");
        void ShowHintText(string hint);
    }
}
