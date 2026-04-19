// Assets/Scripts/Golf/GolfUIManager.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.Golf
{
    public class GolfUIManager : MonoBehaviour
    { 
        [Header("UI References")]
        [SerializeField] private Text courseInfoText;
        [SerializeField] private Text strokeText;
        [SerializeField] private Text resultText;
        [SerializeField] private GameObject swingGuidePanel;

        public void ShowCourseInfo(int current, int total)
        {
            if (courseInfoText != null)
            {
                courseInfoText.text = $"코스 {current} / {total}";
            }
        }

        public void UpdateStroke(int stroke)
        {
            if (strokeText != null)
            {
                strokeText.text = $"현재 타수: {stroke}타";
            }
            SetSwingUIActive(false); // 타격 즉시 스윙 가이드 숨김
        }

        public void ShowFinalResult(List<int> strokes, int score)
        {
            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text = $"최종 점수: {score}점\n수고하셨습니다!";
            }
        }

        public void SetSwingUIActive(bool isActive)
        {
            if (swingGuidePanel != null)
            {
                swingGuidePanel.SetActive(isActive);
            }
        }

        /*
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
        */
    }
}
