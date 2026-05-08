// Assets/Scripts/Golf/GolfGameManager.cs
// 담당: 양석원
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Golf
{
    public class GolfGameManager : BaseGameManager
    {
        [Header("Golf Settings")]
        [SerializeField] private BallController    ball;
        [SerializeField] private GolfCourseManager courseManager;
        [SerializeField] private GolfUIManager     golfUI;

        private int _selectedCourse = 0;
        private int _strokeCount    = 0;

        protected override void InitGame()
        {
            gameTitle = "골프";
            if (courseManager == null)
                courseManager = GetComponent<GolfCourseManager>()
                             ?? FindObjectOfType<GolfCourseManager>();
            if (golfUI == null)
                golfUI = GetComponent<GolfUIManager>()
                      ?? FindObjectOfType<GolfUIManager>();
        }

        protected override void StartGame()
        {
            StartCoroutine(ReadyThenSelectCourse());
        }

        protected override void EndGame() { }

        // ── 코스 선택 후 호출 ───────────────────────────────────────
        void OnCourseSelected(int courseIndex)
        {
            _selectedCourse = courseIndex;
            _strokeCount    = 0;

            golfUI?.HideCourseSelection();
            golfUI?.SetBackToSelectCallback(ShowCourseSelection);

            if (ball == null) ball = CreateBall();
            ball.gameObject.SetActive(true);

            PositionCourseNearPlayer();
            courseManager?.LoadCourse(courseIndex);
            golfUI?.ShowCourseInfo(courseIndex + 1, 3);
            golfUI?.SetSwingUIActive(true);

            if (ball != null && courseManager != null)
                ball.ResetBall(courseManager.GetTeePosition(courseIndex));

            TTSManager.Instance?.Speak($"{courseIndex + 1}번 홀입니다. 스윙하세요.");
        }

        // ── BallController에서 호출 ─────────────────────────────────
        public void OnHoleIn()
        {
            _score = Mathf.Max(0, 500 - _strokeCount * 30);
            TTSManager.Instance?.Speak($"홀인! {_strokeCount}타로 성공했습니다.");
            AudioManager.Instance?.PlayGameClear();
            PlayerDataManager.Instance?.SaveScore(gameTitle, _score);

            golfUI?.ShowResult(_strokeCount, _score,
                onReselect: ShowCourseSelection,
                onLobby:    GoToLobby);
        }

        public void OnSwingCompleted()
        {
            _strokeCount++;
            golfUI?.UpdateStroke(_strokeCount);
        }

        public void OnBallOutOfBounds()
        {
            _strokeCount++;  // 벌타
            golfUI?.UpdateStroke(_strokeCount);
            TTSManager.Instance?.Speak("아웃! 원위치로 돌아갑니다.");
            if (ball != null && courseManager != null)
                ball.ResetBall(courseManager.GetTeePosition(_selectedCourse));
        }

        // ── 내부 ────────────────────────────────────────────────────
        IEnumerator ReadyThenSelectCourse()
        {
            yield return null;
            yield return null;
            ShowCourseSelection();
        }

        void ShowCourseSelection()
        {
            // 이전 코스 정리
            courseManager?.LoadCourse(-1); // -1 = 현재 코스 파괴만
            if (ball != null) ball.gameObject.SetActive(false);

            golfUI?.ShowCourseSelection(OnCourseSelected, GoToLobby);
        }

        void PositionCourseNearPlayer()
        {
            if (courseManager == null) return;
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null) return;

            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 origin = new Vector3(cam.transform.position.x, 0f,
                                         cam.transform.position.z + 0.8f);
            courseManager.transform.SetPositionAndRotation(
                origin, Quaternion.LookRotation(fwd, Vector3.up));
        }

        BallController CreateBall()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GolfBall";
            go.transform.localScale = Vector3.one * 0.12f;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass                   = 1f;
            rb.drag                   = 0.4f;
            rb.angularDrag            = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;

            var pm = new PhysicMaterial("BallMat");
            pm.bounciness        = 0.3f;
            pm.dynamicFriction   = 0.3f;
            pm.staticFriction    = 0.3f;
            pm.frictionCombine   = PhysicMaterialCombine.Average;
            pm.bounceCombine     = PhysicMaterialCombine.Average;
            go.GetComponent<SphereCollider>().sharedMaterial = pm;

            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            return go.AddComponent<BallController>();
        }
    }
}
