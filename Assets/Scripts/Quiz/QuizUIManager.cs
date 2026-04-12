// Assets/Scripts/Quiz/QuizUIManager.cs
// 담당: 박건영
// TODO: TextMeshPro 설치 후 4지선다 버튼 UI 연결
using UnityEngine;

namespace SilverCare.Quiz
{
    public class QuizUIManager : MonoBehaviour
    {
        public void ShowQuestion(QuizQuestion q, int current, int total)
        {
            // TODO: 박건영 - 문제 텍스트, 보기 버튼 갱신
            Debug.Log($"[QuizUI] {current}/{total} : {q.question}");
        }

        public void ShowAnswerResult(bool correct, int correctIndex)
        {
            // TODO: 박건영 - 정답 초록, 오답 빨강 하이라이트
            Debug.Log($"[QuizUI] 결과: {(correct ? "정답" : "오답")}");
        }

        public void ShowFinalResult(int score, int total)
        {
            // TODO: 박건영 - 결과 패널 표시
            Debug.Log($"[QuizUI] 최종: {score}점");
        }
    }
}
