// Assets/Scripts/Golf/GolfUIManager.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfUIManager : MonoBehaviour
    {
        public void ShowCourseInfo(int current, int total)
        {
            Debug.Log($"[GolfUI] {current}/{total} 홀");
        }

        public void UpdateStroke(int stroke)
        {
            Debug.Log($"[GolfUI] {stroke}타");
        }

        public void ShowFinalResult(List<int> strokes, int score)
        {
            Debug.Log($"[GolfUI] 최종 점수: {score}점");
        }
    }
}
