// Assets/Editor/SceneBuilder.cs
// Unity 메뉴: SilverCare → Build Scenes
// Unity 2022.3 + XR Interaction Toolkit 2.x 필수
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public static class SceneBuilder
{
    [MenuItem("SilverCare/Build Scenes (씬 자동 생성)")]
    public static void BuildAllScenes()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("플레이 중",
                "Stop 버튼을 눌러 플레이를 종료한 다음 실행하세요.", "확인");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        BuildLobbyScene();
        BuildBadukDesktopScene();
        BuildBadukVRScene();
        BuildCardMatchScene();
        BuildQuizScene();
        BuildSongGuessScene();
        BuildGoStopScene();
        BuildGolfScene();
        RegisterBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 완료 후 MainLobby를 열어서 Play 버튼이 로비부터 시작하도록
        EditorSceneManager.OpenScene("Assets/Scenes/MainLobby.unity");

        EditorUtility.DisplayDialog("완료!",
            "전체 8개 씬 생성 완료\nAssets/Scenes/ 폴더를 확인하세요.", "확인");
    }

    // ── 공통: 퍼시스턴트 Managers 오브젝트 ───────────────────────
    static void BuildManagers()
    {
        var mgr = new GameObject("Managers");

        mgr.AddComponent<SilverCare.Common.GameSceneManager>();

        var ttsManager = mgr.AddComponent<SilverCare.Common.TTSManager>();

        mgr.AddComponent<SilverCare.Common.AudioManager>();
        var bgmSrc = new GameObject("BGMSource").AddComponent<AudioSource>();
        bgmSrc.transform.SetParent(mgr.transform);
        bgmSrc.playOnAwake = false;
        var sfxSrc = new GameObject("SFXSource").AddComponent<AudioSource>();
        sfxSrc.transform.SetParent(mgr.transform);
        sfxSrc.playOnAwake = false;

        var ttsSrc = new GameObject("TTSSource").AddComponent<AudioSource>();
        ttsSrc.transform.SetParent(mgr.transform);
        ttsSrc.playOnAwake = false;

        // TTSManager에 AudioSource 연결
        var ttsMgrSerialized = new UnityEditor.SerializedObject(ttsManager);
        ttsMgrSerialized.FindProperty("audioSource").objectReferenceValue = ttsSrc;
        ttsMgrSerialized.ApplyModifiedProperties();

        mgr.AddComponent<SilverCare.Common.PlayerDataManager>();
    }

    // ── MainLobby 씬 ─────────────────────────────────────────────
    static void BuildLobbyScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 기본 카메라 제거 (XR Origin의 카메라로 대체)
#pragma warning disable CS0618
        var defaultCam = Object.FindObjectOfType<Camera>();
#pragma warning restore CS0618
        if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

        // 싱글턴 매니저들 (DontDestroyOnLoad)
        BuildManagers();

        // VR 로비 UI (World Space Canvas)
        var lobbyGO = new GameObject("LobbyManager");
        lobbyGO.AddComponent<VRLobby>();

        // XR Origin + XR Device Simulator (PC 테스트용)
        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainLobby.unity");
        Debug.Log("[SceneBuilder] MainLobby 생성 완료");
    }

    // ── Baduk Desktop 씬 ─────────────────────────────────────────
    static void BuildBadukDesktopScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var go = new GameObject("BadukManager");
        go.AddComponent<Baduk.BadukBoard>();
        go.AddComponent<Baduk.BadukProblemLoader>();
        go.AddComponent<Baduk.BadukAnswerChecker>();
        go.AddComponent<Baduk.BadukHintSystem>();
        go.AddComponent<Baduk.BadukTTSConnector>();
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<Baduk.BadukDesktopInput>();
        go.AddComponent<Baduk.BadukDesktopUI>();
        go.AddComponent<Baduk.BadukGameManager>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Baduk.unity");
        Debug.Log("[SceneBuilder] Baduk (Desktop) 씬 생성 완료");
    }

    // ── Baduk VR 씬 ──────────────────────────────────────────────
    static void BuildBadukVRScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        var defaultCam = Object.FindObjectOfType<Camera>();
#pragma warning restore CS0618
        if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

        var go = new GameObject("BadukManager");
        go.AddComponent<Baduk.BadukBoard>();
        go.AddComponent<Baduk.BadukProblemLoader>();
        go.AddComponent<Baduk.BadukAnswerChecker>();
        go.AddComponent<Baduk.BadukHintSystem>();
        go.AddComponent<Baduk.BadukTTSConnector>();
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<Baduk.BadukVRInput>();
        go.AddComponent<Baduk.BadukVRUI>();
        go.AddComponent<Baduk.BadukVRBoardSetup>();
        go.AddComponent<Baduk.BadukGameManager>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BadukVR.unity");
        Debug.Log("[SceneBuilder] BadukVR 씬 생성 완료");
    }

    // ── CardMatch 씬 ─────────────────────────────────────────────
    static void BuildCardMatchScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        Object.DestroyImmediate(Object.FindObjectOfType<Camera>()?.gameObject);
#pragma warning restore CS0618

        var go = new GameObject("CardMatchManager");
        go.AddComponent<SilverCare.CardMatch.CardMatchGameManager>();
        go.AddComponent<SilverCare.CardMatch.CardController>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CardMatch.unity");
        Debug.Log("[SceneBuilder] CardMatch 씬 생성 완료");
    }

    // ── Quiz 씬 ──────────────────────────────────────────────────
    static void BuildQuizScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        Object.DestroyImmediate(Object.FindObjectOfType<Camera>()?.gameObject);
#pragma warning restore CS0618

        var go = new GameObject("QuizManager");
        go.AddComponent<SilverCare.Quiz.QuizGameManager>();
        go.AddComponent<SilverCare.Quiz.QuizUIManager>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Quiz.unity");
        Debug.Log("[SceneBuilder] Quiz 씬 생성 완료");
    }

    // ── SongGuess 씬 ─────────────────────────────────────────────
    static void BuildSongGuessScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        Object.DestroyImmediate(Object.FindObjectOfType<Camera>()?.gameObject);
#pragma warning restore CS0618

        var go = new GameObject("SongGuessManager");
        go.AddComponent<SilverCare.SongGuess.SongGuessGameManager>();
        go.AddComponent<SilverCare.SongGuess.SongPlayer>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SongGuess.unity");
        Debug.Log("[SceneBuilder] SongGuess 씬 생성 완료");
    }

    // ── GoStop 씬 ────────────────────────────────────────────────
    static void BuildGoStopScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        Object.DestroyImmediate(Object.FindObjectOfType<Camera>()?.gameObject);
#pragma warning restore CS0618

        var go = new GameObject("GoStopManager");
        go.AddComponent<SilverCare.GoStop.GoStopGameManager>();
        go.AddComponent<SilverCare.GoStop.GoStopDeck>();
        go.AddComponent<SilverCare.GoStop.GoStopScoreCalculator>();
        go.AddComponent<SilverCare.GoStop.GoStopUIManager>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GoStop.unity");
        Debug.Log("[SceneBuilder] GoStop 씬 생성 완료");
    }

    // ── Golf 씬 ──────────────────────────────────────────────────
    static void BuildGolfScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

#pragma warning disable CS0618
        Object.DestroyImmediate(Object.FindObjectOfType<Camera>()?.gameObject);
#pragma warning restore CS0618

        var go = new GameObject("GolfManager");
        go.AddComponent<SilverCare.Golf.GolfGameManager>();
        go.AddComponent<SilverCare.Golf.GolfCourseManager>();

        BuildXROrigin(addSimulator: true);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Golf.unity");
        Debug.Log("[SceneBuilder] Golf 씬 생성 완료");
    }

    // ── XR Origin 생성 ────────────────────────────────────────────
    static void BuildXROrigin(bool addSimulator)
    {
        var originGO = new GameObject("XR Origin");
        originGO.transform.position = new Vector3(0f, 0f, -1.5f);
        var xrOrigin = originGO.AddComponent<XROrigin>();

        var offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform, false);
        xrOrigin.CameraFloorOffsetObject = offsetGO;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offsetGO.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        camGO.AddComponent<AudioListener>();
        xrOrigin.Camera = cam;

        // TrackedPoseDriver — XR 헤드셋 위치/회전을 카메라에 반영
        var tpd = camGO.AddComponent<TrackedPoseDriver>();
        var posAction = new InputAction("Position", InputActionType.Value,
            "<XRHMD>/centerEyePosition");
        var rotAction = new InputAction("Rotation", InputActionType.Value,
            "<XRHMD>/centerEyeRotation");
        tpd.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
        tpd.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);

        var rightGO = new GameObject("RightHand Controller");
        rightGO.transform.SetParent(offsetGO.transform, false);
        rightGO.transform.localPosition = new Vector3(0.2f, 1.3f, 0.3f);
        rightGO.AddComponent<ActionBasedController>();
        var ray = rightGO.AddComponent<XRRayInteractor>();
        ray.maxRaycastDistance = 10f;
        ray.enableUIInteraction = true;
        var lr = rightGO.AddComponent<LineRenderer>();
        lr.startWidth = 0.005f; lr.endWidth = 0.005f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor   = new Color(0f, 1f, 1f, 0.3f);
        rightGO.AddComponent<XRInteractorLineVisual>().lineLength = 10f;

        var leftGO = new GameObject("LeftHand Controller");
        leftGO.transform.SetParent(offsetGO.transform, false);
        leftGO.transform.localPosition = new Vector3(-0.2f, 1.3f, 0.3f);
        leftGO.AddComponent<ActionBasedController>();

        // 상하 이동 — PC: R(위) / F(아래)
        originGO.AddComponent<SilverCare.Common.VerticalMover>();

        // ── Locomotion System (이동 + 회전) ──
        var locoGO = new GameObject("Locomotion System");
        locoGO.transform.SetParent(originGO.transform, false);
        var locoSystem = locoGO.AddComponent<LocomotionSystem>();
        locoSystem.xrOrigin = xrOrigin;

        // 이동 — VR: 왼쪽 조이스틱 / PC: WASD
        var moveProvider = locoGO.AddComponent<ActionBasedContinuousMoveProvider>();
        moveProvider.system = locoSystem;
        moveProvider.moveSpeed = 2f;
        moveProvider.forwardSource = camGO.transform;
        var moveAction = new InputAction("Move", InputActionType.Value,
            binding: null, interactions: null, processors: null, expectedControlType: "Vector2");
        moveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveProvider.leftHandMoveAction = new InputActionProperty(moveAction);

        // 회전 — VR: 오른쪽 조이스틱 / PC: Q·E
        var turnProvider = locoGO.AddComponent<ActionBasedContinuousTurnProvider>();
        turnProvider.system = locoSystem;
        turnProvider.turnSpeed = 60f;
        var turnAction = new InputAction("Turn", InputActionType.Value,
            binding: null, interactions: null, processors: null, expectedControlType: "Vector2");
        turnAction.AddBinding("<XRController>{RightHand}/thumbstick");
        turnAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/e")
            .With("Down", "<Keyboard>/q")
            .With("Left", "<Keyboard>/q")
            .With("Right", "<Keyboard>/e");
        turnProvider.leftHandTurnAction = new InputActionProperty(turnAction);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        var floorMat = new Material(Shader.Find("Standard"));
        floorMat.color = new Color(0.15f, 0.15f, 0.2f);
        floor.GetComponent<MeshRenderer>().material = floorMat;

        // PC 테스트용 XR Device Simulator — Samples 프리팹 우선 사용 (Action Asset 포함)
        if (addSimulator)
        {
            bool placed = false;
            string[] guids = AssetDatabase.FindAssets("XR Device Simulator t:Prefab",
                new[] { "Assets/Samples" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                    Debug.Log($"[SceneBuilder] XR Device Simulator 프리팹 배치: {path}");
                    placed = true;
                }
            }
            if (!placed)
                Debug.LogWarning("[SceneBuilder] XR Device Simulator 프리팹을 찾지 못함. " +
                    "Package Manager → XR Interaction Toolkit → Samples → XR Device Simulator 임포트 필요");
        }

        Debug.Log("[SceneBuilder] XR Origin 생성 완료");
    }

    // ── Build Settings 전체 등록 ──────────────────────────────────
    static void RegisterBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainLobby.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/Baduk.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/BadukVR.unity",       true),
            new EditorBuildSettingsScene("Assets/Scenes/BadukReplay.unity",     true),
            new EditorBuildSettingsScene("Assets/Scenes/BadukPrediction.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CardMatch.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/Quiz.unity",       true),
            new EditorBuildSettingsScene("Assets/Scenes/SongGuess.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/GoStop.unity",     true),
            new EditorBuildSettingsScene("Assets/Scenes/Golf.unity",       true),
        };
        Debug.Log("[SceneBuilder] Build Settings 8개 씬 등록 완료");
    }
}
