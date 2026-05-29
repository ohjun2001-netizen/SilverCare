// Assets/Scripts/Golf/GolfGameManager.cs
using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfGameManager : BaseGameManager
    {
        const float BallDiameter = 0.09f;

        [Header("Golf Settings")]
        [SerializeField] private BallController ball;
        [SerializeField] private GolfCourseManager courseManager;
        [SerializeField] private GolfUIManager golfUI;
        [SerializeField] private VRPutter putter;

        // ballPrefab 제거 — NormalizeChildVisual 위치 오류로 공 비가시 → 흰 구체 폴백 사용

        int _selectedCourse;
        int _strokeCount;

        protected override void InitGame()
        {
            gameTitle = "Golf";

            if (courseManager == null)
                courseManager = GetComponent<GolfCourseManager>() ?? FindObjectOfType<GolfCourseManager>();
            if (golfUI == null)
                golfUI = GetComponent<GolfUIManager>() ?? FindObjectOfType<GolfUIManager>();

// Golf_ball_HighPOly: NormalizeChildVisual 위치 오류로 Course 2,3에서 비가시 → 미사용
// BallController.EnsureVisibleCore()의 흰 구체 폴백으로 대체
        }

        protected override void StartGame()
        {
            StartCoroutine(ReadyThenShowSelection());
        }

        protected override void EndGame()
        {
        }

        IEnumerator ReadyThenShowSelection()
        {
            yield return null;
            yield return null;
            ShowCourseSelection();
        }

        void ShowCourseSelection()
        {
            courseManager?.ShowPreviewEnvironment();
            if (ball != null)
                ball.gameObject.SetActive(false);
            if (putter != null)
                putter.gameObject.SetActive(false);

            golfUI?.ShowCourseSelection(OnCourseSelected, GoToLobby);
        }

        void OnCourseSelected(int courseIndex)
        {
            _selectedCourse = courseIndex;
            _strokeCount = 0;

            golfUI?.HideCourseSelection();
            golfUI?.SetBackToSelectCallback(ShowCourseSelection);

            if (ball == null)
                ball = CreateBall();
            if (putter == null)
                putter = CreatePutter();

            ball.gameObject.SetActive(true);
            putter.gameObject.SetActive(true);
            PositionCourseNearPlayer();
            courseManager?.LoadCourse(courseIndex);
            golfUI?.ShowCourseInfo(courseIndex + 1, 3);
            golfUI?.UpdateStroke(0);
            golfUI?.ShowShotDistance(0f);
            golfUI?.SetSwingUIActive(true);

            if (ball != null && courseManager != null)
                ball.ResetBall(GetBallRestingPosition(courseIndex));

            AudioManager.Instance?.PlayGameStart();
        }

        void PositionCourseNearPlayer()
        {
            if (courseManager == null)
                return;

            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null)
                return;

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 origin = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z) + forward * 1.5f;
            courseManager.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(forward, Vector3.up));
        }

        public void OnSwingCompleted()
        {
            _strokeCount++;
            golfUI?.UpdateStroke(_strokeCount);
        }

        public void OnHoleIn()
        {
            _score = Mathf.Max(100, 650 - _strokeCount * 40);
            AudioManager.Instance?.PlayGameClear();
            PlayerDataManager.Instance?.SaveScore(gameTitle, _score);

            golfUI?.ShowResult(_strokeCount, _score, ShowCourseSelection, GoToLobby);
        }

        public void OnBallOutOfBounds()
        {
            _strokeCount++;
            golfUI?.UpdateStroke(_strokeCount);

            if (ball == null)
                return;

            // 마지막으로 지면에 안착했던 위치로 복귀. 기록이 없으면 티 위치로.
            Vector3 safePos = ball.LastSafePosition;
            bool hasSafePos = safePos != Vector3.zero;
            ball.ResetBall(hasSafePos ? safePos : GetBallRestingPosition(_selectedCourse));
        }

        Vector3 GetBallRestingPosition(int courseIndex)
        {
            return courseManager.GetTeePosition(courseIndex) + Vector3.up * (BallDiameter * 0.5f);
        }

        BallController CreateBall()
        {
            var go = new GameObject("GolfBall");
            go.transform.localScale = Vector3.one;

            // ballPrefab 미사용 — BallController.EnsureVisibleCore()의 흰 구체로 표시

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.18f;
            rb.drag = 0.018f;
            rb.angularDrag = 0.08f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = true;
            rb.isKinematic = false;

            var sphereCol = go.GetComponent<SphereCollider>();
            if (sphereCol == null)
                sphereCol = go.AddComponent<SphereCollider>();
            sphereCol.radius = BallDiameter * 0.5f;

            if (sphereCol != null)
            {
                var mat = new PhysicMaterial("GolfBallMat");
                mat.dynamicFriction = 0.42f;
                mat.staticFriction = 0.38f;
                mat.bounciness = 0.18f;
                mat.frictionCombine = PhysicMaterialCombine.Average;
                mat.bounceCombine = PhysicMaterialCombine.Average;
                sphereCol.sharedMaterial = mat;
            }

            var existingBallCtrl = go.GetComponent<BallController>();
            return existingBallCtrl != null ? existingBallCtrl : go.AddComponent<BallController>();
        }

        void NormalizeChildVisual(Transform visualRoot, float targetDiameter)
        {
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            visualRoot.localScale = Vector3.one;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float currentDiameter = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (currentDiameter > 0.0001f)
                visualRoot.localScale *= targetDiameter / currentDiameter;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 worldOffset = bounds.center - visualRoot.parent.position;
            visualRoot.position -= worldOffset;
        }

        void RepairBallMaterials(GameObject visualRoot)
        {
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                Texture mainTexture = null;
                if (renderer.sharedMaterial != null)
                    mainTexture = renderer.sharedMaterial.mainTexture;

                var mat = new Material(Shader.Find("Standard"));
                mat.color = Color.white;
                mat.SetFloat("_Glossiness", 0.45f);
                if (mainTexture != null)
                    mat.mainTexture = mainTexture;
                renderer.sharedMaterial = mat;
            }
        }

        VRPutter CreatePutter()
        {
            var go = new GameObject("VRPutter");
            return go.AddComponent<VRPutter>();
        }
    }
}
