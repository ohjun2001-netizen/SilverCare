// Assets/Scripts/Lobby/GameSelectUI.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결 (Package Manager → TextMeshPro)
using UnityEngine;
using UnityEngine.UI;
using SilverCare.Common;

namespace SilverCare.Lobby
{
    public class GameSelectUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text recommendationText;
        [SerializeField] private Text highScoreText;

        public void RefreshHighScores()
        {
            int high = PlayerDataManager.Instance?.GetHighScore("Baduk") ?? 0;
            if (highScoreText != null)
            {
                highScoreText.text = $"바둑 최고 점수: {high}";
            }
        }

        public void ApplyRecommendation(string gender, int age)
        {
            if (recommendationText == null) return;

            // 나이 기반 단순 추천 로직
            if (age >= 70)
            {
                recommendationText.text = "치매 예방을 위해 '카드 맞추기'를 추천합니다.";
            }
            else
            {
                recommendationText.text = "인지력 강화를 위해 '바둑 사활문제'를 추천합니다.";
            }
        }
    }
}
