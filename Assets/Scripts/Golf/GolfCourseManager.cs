// Assets/Scripts/Golf/GolfCourseManager.cs
// 담당: 양석원
// TODO: 3개 코스 씬/프리팹 로드, 홀 위치, 장애물 배치
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfCourseManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] coursePrefabs;   // 코스 3개 프리팹
        [SerializeField] private Transform    courseRoot;
        [SerializeField] private Transform[]  ballStartPositions;

        private GameObject _currentCourse;

        public void LoadCourse(int index)
        {
            if (_currentCourse != null) Destroy(_currentCourse);
            if (index >= coursePrefabs.Length) return;

            _currentCourse = Instantiate(coursePrefabs[index], courseRoot);
            Debug.Log($"[GolfCourse] 코스 {index + 1} 로드");
        }

        public Vector3 GetBallStartPosition(int courseIndex)
        {
            if (ballStartPositions != null && courseIndex < ballStartPositions.Length)
                return ballStartPositions[courseIndex].position;
            return Vector3.zero;
        }
    }
}
