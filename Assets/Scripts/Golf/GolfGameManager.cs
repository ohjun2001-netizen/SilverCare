// Assets/Scripts/Golf/GolfGameManager.cs
using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace SilverCare.Golf
{
    public class GolfGameManager : BaseGameManager
    {
        const float BallDiameter = 0.0637f;
        const float FollowBallDistance = 1.5f;
        const float FollowBallMoveSeconds = 0.65f;

        [Header("Golf Settings")]
        [SerializeField] private BallController ball;
        [SerializeField] private GolfCourseManager courseManager;
        [SerializeField] private GolfUIManager golfUI;
        [SerializeField] private VRPutter putter;

        // ballPrefab 제거 — NormalizeChildVisual 위치 오류로 공 비가시 → 흰 구체 폴백 사용

        int _selectedCourse;
        int _strokeCount;
        bool _resultShown;
        bool _acceptHoleIn;

        // 스윙해서 친 공만 홀인 인정 — 시작/재시작 직후의 비정상 홀 진입 무시.
        public bool CanHoleIn => _acceptHoleIn && !_resultShown;
        Vector3 _fixedBallStartPosition;
        Coroutine _startPositionRoutine;
        Coroutine _followBallRoutine;

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
            if (_startPositionRoutine != null)
            {
                StopCoroutine(_startPositionRoutine);
                _startPositionRoutine = null;
            }
            if (_followBallRoutine != null)
            {
                StopCoroutine(_followBallRoutine);
                _followBallRoutine = null;
            }

            // 코스 제거 — 난이도 선택 배경에 골프 맵이 남는 잔상 방지(index<0이면 ClearCurrent만).
            courseManager?.LoadCourse(-1);

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
            _resultShown = false;
            _acceptHoleIn = false; // 첫 스윙 전까지 홀인 무시

            // 공 따라가기 이동 중지 — 다시 하기 시 잔여 이동/위치 어긋남 방지.
            if (_followBallRoutine != null)
            {
                StopCoroutine(_followBallRoutine);
                _followBallRoutine = null;
            }

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
            _fixedBallStartPosition = GetBallRestingPosition(courseIndex);
            golfUI?.ShowCourseInfo(courseIndex + 1, 3);
            golfUI?.UpdateStroke(0);
            golfUI?.ShowShotDistance(0f);
            golfUI?.SetSwingUIActive(true);

            if (ball != null && courseManager != null)
            {
                ball.ResetBall(_fixedBallStartPosition);
                RestartBallAtCourseStart();
            }

            AudioManager.Instance?.PlayGameStart();
        }

        void RestartBallAtCourseStart()
        {
            if (_startPositionRoutine != null)
                StopCoroutine(_startPositionRoutine);

            _startPositionRoutine = StartCoroutine(ForceBallToCourseStartAfterLayout());
        }

        IEnumerator ForceBallToCourseStartAfterLayout()
        {
            yield return null;
            ForceBallToCourseStart();
            yield return new WaitForEndOfFrame();
            ForceBallToCourseStart();
            _startPositionRoutine = null;
        }

        void ForceBallToCourseStart()
        {
            if (ball == null || courseManager == null)
                return;

            _fixedBallStartPosition = GetBallRestingPosition(_selectedCourse);
            ball.ResetBall(_fixedBallStartPosition);
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
            _acceptHoleIn = true; // 공을 친 뒤부터 홀인 인정
            golfUI?.UpdateStroke(_strokeCount);
        }

        public void OnBallStopped(Vector3 ballPosition, Vector3 shotStartPosition)
        {
            MovePlayerNearBall(ballPosition, shotStartPosition);
        }

        public void OnHoleIn()
        {
            if (_resultShown)
                return; // 이미 결과 표시됨 — 재시작 직후 재홀인 등으로 중복 호출 방지
            _resultShown = true;

            _score = Mathf.Max(100, 650 - _strokeCount * 40);
            AudioManager.Instance?.PlayGameClear();
            PlayerDataManager.Instance?.SaveScore(gameTitle, _score);

            bool firstClear = StoryProgressManager.Instance != null &&
                              StoryProgressManager.Instance.TryMarkActivityCleared(
                                  StoryProgressManager.StoryActivity.Golf);
            if (firstClear)
                StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.StoryActivity.Golf);

            golfUI?.ShowResult(_strokeCount, _score, RestartCourse, ShowCourseSelection, GoToLobby);
        }

        // 같은 난이도(코스)로 다시 시작
        void RestartCourse()
        {
            OnCourseSelected(_selectedCourse);
        }

        public void OnBallOutOfBounds()
        {
            _strokeCount++;
            golfUI?.UpdateStroke(_strokeCount);

            if (ball == null)
                return;

            // 코스마다 시작 티 위치로 고정 복귀시켜 재실행/낙하 후 위치가 흔들리지 않게 한다.
            ball.ResetBall(_fixedBallStartPosition != Vector3.zero
                ? _fixedBallStartPosition
                : GetBallRestingPosition(_selectedCourse));
        }

        Vector3 GetBallRestingPosition(int courseIndex)
        {
            // 공 바닥이 티매트 윗면에 정확히 닿도록 실제 콜라이더 반지름만큼 띄운다.
            float radius = ball != null ? ball.Radius : BallDiameter * 0.5f;
            return courseManager.GetTeeMatCenterPosition(courseIndex) + Vector3.up * radius;
        }

        void MovePlayerNearBall(Vector3 ballPosition, Vector3 shotStartPosition)
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null)
                return;

            Vector3 travel = ballPosition - shotStartPosition;
            travel.y = 0f;
            if (travel.sqrMagnitude < 0.001f)
            {
                travel = cam.transform.forward;
                travel.y = 0f;
            }
            if (travel.sqrMagnitude < 0.001f)
                travel = Vector3.forward;
            travel.Normalize();

            Vector3 desiredEyePosition = ballPosition - travel * FollowBallDistance;
            desiredEyePosition.y = cam.transform.position.y;

            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                Vector3 cameraOffset = cam.transform.position - xrOrigin.transform.position;
                Vector3 targetOrigin = desiredEyePosition - cameraOffset;
                targetOrigin.y = xrOrigin.transform.position.y;
                Quaternion targetRotation = BuildOriginRotationFacingBall(xrOrigin.transform, cam.transform, ballPosition);
                StartFollowBallMove(xrOrigin.transform, targetOrigin, targetRotation);
                return;
            }

            Transform cameraRoot = cam.transform.parent != null ? cam.transform.parent : cam.transform;
            Vector3 rootOffset = cam.transform.position - cameraRoot.position;
            Vector3 targetRoot = desiredEyePosition - rootOffset;
            targetRoot.y = cameraRoot.position.y;
            Quaternion rootRotation = BuildOriginRotationFacingBall(cameraRoot, cam.transform, ballPosition);
            StartFollowBallMove(cameraRoot, targetRoot, rootRotation);
        }

        void StartFollowBallMove(Transform root, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (root == null)
                return;

            if (_followBallRoutine != null)
                StopCoroutine(_followBallRoutine);

            _followBallRoutine = StartCoroutine(SmoothMoveRoot(root, targetPosition, targetRotation));
        }

        IEnumerator SmoothMoveRoot(Transform root, Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 startPosition = root.position;
            Quaternion startRotation = root.rotation;
            float elapsed = 0f;

            while (elapsed < FollowBallMoveSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / FollowBallMoveSeconds);
                root.position = Vector3.Lerp(startPosition, targetPosition, t);
                root.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            root.SetPositionAndRotation(targetPosition, targetRotation);
            _followBallRoutine = null;
        }

        static Quaternion BuildOriginRotationFacingBall(Transform root, Transform cameraTransform, Vector3 ballPosition)
        {
            Vector3 toBallFromCamera = ballPosition - cameraTransform.position;
            toBallFromCamera.y = 0f;
            if (toBallFromCamera.sqrMagnitude < 0.001f)
                return root.rotation;
            toBallFromCamera.Normalize();

            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.001f)
                cameraForward = root.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.001f)
                return root.rotation;
            cameraForward.Normalize();

            Quaternion delta = Quaternion.FromToRotation(cameraForward, toBallFromCamera);
            return delta * root.rotation;
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
