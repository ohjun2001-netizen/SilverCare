// Assets/Scripts/Golf/GolfGameManager.cs
// 담당: 양석원
// TODO: 컨트롤러 스윙 감지, 공 물리, 3개 코스, 스코어 UI
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Golf
{
    public class GolfGameManager : BaseGameManager
    {
        [Header("Golf Settings")]
        [SerializeField] private BallController ball;
        [SerializeField] private GolfCourseManager courseManager;
        [SerializeField] private GolfUIManager golfUI;
        [SerializeField] private int totalCourses = 3;

        private int _currentCourse = 0;
        private int _strokeCount   = 0;
        private List<int> _courseStrokes = new();

        protected override void InitGame()
        {
            gameTitle = "골프 배팅";
        }

        protected override void StartGame()
        {
            _currentCourse = 0;
            _courseStrokes.Clear();
            LoadCourse(_currentCourse);
        }

        protected override void EndGame()
        {
            int total = 0;
            foreach (int s in _courseStrokes) total += s;
            _score = Mathf.Max(0, 1000 - total * 50);
            golfUI?.ShowFinalResult(_courseStrokes, _score);
        }

        /// <summary>BallController에서 홀인 감지 시 호출</summary>
        public void OnHoleIn()
        {
            _courseStrokes.Add(_strokeCount);
            TTSManager.Instance?.Speak($"홀인! {_strokeCount}타로 성공했습니다.");
            AudioManager.Instance?.PlayGameClear();

            _currentCourse++;
            if (_currentCourse >= totalCourses)
                OnGameClear();
            else
                Invoke(nameof(LoadNextCourse), 2f);
        }

        /// <summary>스윙 완료 시 BallController에서 호출</summary>
        public void OnSwingCompleted()
        {
            _strokeCount++;
            golfUI?.UpdateStroke(_strokeCount);
        }

        private void LoadCourse(int index)
        {
            _strokeCount = 0;
            courseManager?.LoadCourse(index);
            golfUI?.ShowCourseInfo(index + 1, totalCourses);
            TTSManager.Instance?.Speak($"{index + 1}번 홀입니다. 스윙하세요.");
        }

        private void LoadNextCourse() => LoadCourse(_currentCourse);
    }
}
