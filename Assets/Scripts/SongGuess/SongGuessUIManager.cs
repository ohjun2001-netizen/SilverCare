// Assets/Scripts/SongGuess/SongGuessUIManager.cs
// 담당: 조성우
// TODO: TextMeshPro 설치 후 UI 연결
using UnityEngine;

namespace SilverCare.SongGuess
{
    public class SongGuessUIManager : MonoBehaviour
    {
        public void ShowRoundStart(int current, int total)
        {
            Debug.Log($"[SongGuessUI] {current}/{total} 라운드 시작");
        }

        public void ShowAnswerInput()
        {
            Debug.Log("[SongGuessUI] 정답 입력 대기");
        }

        public void ShowResult(bool correct, SongData song)
        {
            Debug.Log($"[SongGuessUI] {(correct ? "정답" : "오답")}: {song.title} - {song.artist}");
        }

        public void ShowFinalResult(int score)
        {
            Debug.Log($"[SongGuessUI] 최종 점수: {score}점");
        }
    }
}
