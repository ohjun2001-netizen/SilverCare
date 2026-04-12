// Assets/Scripts/Lobby/GameSelectUI.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결 (Package Manager → TextMeshPro)
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Lobby
{
    public class GameSelectUI : MonoBehaviour
    {
        public void RefreshHighScores()
        {
            // TODO: 양석원 - 각 게임 버튼에 최고점수 표시
            // int high = PlayerDataManager.Instance?.GetHighScore("Baduk") ?? 0;
        }

        public void ApplyRecommendation(string gender, int age)
        {
            // TODO: 양석원 - 나이/성별별 게임 추천 배너 표시
        }
    }
}
