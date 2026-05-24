// Unity 복사 경로: Assets/Scripts/Baduk/BadukAnswerChecker.cs
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukAnswerChecker : MonoBehaviour
    {
        private BadukProblem _currentProblem;
        private int _currentAnswerStep = 0;   // 다중 정답 추적용

        public void SetProblem(BadukProblem problem)
        {
            _currentProblem = problem;
            _currentAnswerStep = 0;
        }

        /// <summary>
        /// 플레이어가 클릭한 위치를 정답과 비교
        /// </summary>
        public ProblemResult CheckMove(int row, int col)
        {
            if (_currentProblem == null || _currentProblem.answer == null)
                return ProblemResult.Wrong;

            List<StonePosition> answers = _currentProblem.answer;

            // 단일 정답 또는 다중 정답의 현재 스텝 확인
            if (_currentAnswerStep >= answers.Count)
                return ProblemResult.Wrong;

            StonePosition expected = answers[_currentAnswerStep];

            if (expected.row == row && expected.col == col)
            {
                _currentAnswerStep++;

                // 모든 정답 스텝 완료
                if (_currentAnswerStep >= answers.Count)
                {
                    _currentAnswerStep = 0;
                    return ProblemResult.Correct;
                }
                // 연속 정답 중 중간 단계
                return ProblemResult.PartialCorrect;
            }

            // 오답: 스텝 리셋
            _currentAnswerStep = 0;
            return ProblemResult.Wrong;
        }

        /// <summary>정답 초기화 (다시 풀기용)</summary>
        public void ResetAnswer() => _currentAnswerStep = 0;

        /// <summary>현재 진행 중인 정답 스텝 수 (UI 표시용)</summary>
        public int CurrentAnswerStep => _currentAnswerStep;

        /// <summary>총 정답 스텝 수</summary>
        public int TotalAnswerSteps => _currentProblem?.answer?.Count ?? 0;
        public bool TryGetExpectedMove(out StonePosition expectedMove)
        {
            expectedMove = null;
            if (_currentProblem?.answer == null) return false;
            if (_currentAnswerStep < 0 || _currentAnswerStep >= _currentProblem.answer.Count) return false;

            expectedMove = _currentProblem.answer[_currentAnswerStep];
            return expectedMove != null;
        }
    }
}
