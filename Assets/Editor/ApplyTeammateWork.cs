// Assets/Editor/ApplyTeammateWork.cs
// Unity 메뉴: SilverCare → Apply Teammate Work
// 양석원(LobbyManager+Golf UI), 박건영(CardMatch) 작업을 씬에 적용
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ApplyTeammateWork
{
    [MenuItem("SilverCare/Apply Teammate Work (동료 작업 적용)")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("플레이 중", "Stop 후 실행하세요.", "확인");
            return;
        }

        bool lobbyOk = SetupLobbyScene();
        bool golfOk  = SetupGolfScene();

        // CardMatch는 SilverCare → Full Test Setup 에서 처리됨
        // (텍스처·프리팹 생성이 필요하기 때문에 별도 메뉴로 분리)

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/MainLobby.unity");

        string result = $"MainLobby : {(lobbyOk ? "완료" : "스킵(이미 존재)")}\n" +
                        $"Golf      : {(golfOk  ? "완료" : "스킵(이미 존재)")}\n\n" +
                        "CardMatch → SilverCare/Full Test Setup 으로 실행하세요.";
        EditorUtility.DisplayDialog("Apply Teammate Work 완료", result, "확인");
    }

    // ── 1. MainLobby: 양석원 LobbyManager 적용 ───────────────────
    static bool SetupLobbyScene()
    {
        string path = "Assets/Scenes/MainLobby.unity";
        if (!File.Exists(Path.GetFullPath(path)))
        {
            Debug.LogWarning("[ApplyTeammateWork] MainLobby.unity 없음 — SceneBuilder 먼저 실행하세요.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(path);

        if (Object.FindObjectOfType<SilverCare.Lobby.LobbyManager>() != null)
        {
            EditorSceneManager.SaveScene(scene);
            return false;
        }

        // LobbyManager — VRLobby GO에 추가 (별도 캔버스 없이 컴포넌트만)
        var vrLobbyGO = GameObject.Find("LobbyManager") ?? new GameObject("LobbyController");
        vrLobbyGO.AddComponent<SilverCare.Lobby.LobbyManager>();
        // 추천 텍스트/점수는 VRLobby가 직접 표시하므로 별도 캔버스 불필요

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyTeammateWork] MainLobby 세팅 완료");
        return true;
    }

    // ── 2. Golf: 양석원 GolfUIManager + BallController 적용 ──────
    static bool SetupGolfScene()
    {
        string path = "Assets/Scenes/Golf.unity";
        if (!File.Exists(Path.GetFullPath(path)))
        {
            Debug.LogWarning("[ApplyTeammateWork] Golf.unity 없음 — SceneBuilder 먼저 실행하세요.");
            return false;
        }

        var scene    = EditorSceneManager.OpenScene(path);
        var golfMgrGO = GameObject.Find("GolfManager");
        if (golfMgrGO == null)
        {
            Debug.LogWarning("[ApplyTeammateWork] GolfManager 오브젝트 없음");
            return false;
        }

        var golfMgr = golfMgrGO.GetComponent<SilverCare.Golf.GolfGameManager>();
        if (golfMgr == null) { EditorSceneManager.SaveScene(scene); return false; }

        // 이미 적용됐는지 확인
        if (Object.FindObjectOfType<SilverCare.Golf.GolfUIManager>() != null)
        {
            EditorSceneManager.SaveScene(scene);
            return false;
        }

        EnsureEventSystem();

        // ── 골프공 ────────────────────────────────────────────────
        var ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballGO.name = "GolfBall";
        ballGO.transform.position   = new Vector3(0f, 0.5f, 2f);
        ballGO.transform.localScale = Vector3.one * 0.3f;
        ballGO.AddComponent<Rigidbody>();
        var ballCtrl = ballGO.AddComponent<SilverCare.Golf.BallController>();

        // ── Golf UI (Screen Space Overlay) ───────────────────────
        var uiRoot  = new GameObject("GolfUI");
        var canvas  = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiRoot.AddComponent<CanvasScaler>();
        uiRoot.AddComponent<GraphicRaycaster>();
        var uiRT = uiRoot.GetComponent<RectTransform>();

        var golfUI  = uiRoot.AddComponent<SilverCare.Golf.GolfUIManager>();

        var courseText = MakeText(uiRT, "CourseInfoText", "코스 1 / 3",
            new Vector2(0, -30), new Vector2(400, 50), 28, TextAnchor.UpperCenter, topAnchor: true);
        var strokeText = MakeText(uiRT, "StrokeText",     "현재 타수: 0타",
            new Vector2(0, -90), new Vector2(400, 44), 24, TextAnchor.UpperCenter, topAnchor: true);
        var resultText = MakeText(uiRT, "ResultText",     "게임 종료",
            new Vector2(0, 0),   new Vector2(500, 60), 28, TextAnchor.MiddleCenter);
        resultText.gameObject.SetActive(false);

        // 스윙 가이드 패널
        var swingPanel = new GameObject("SwingGuidePanel");
        swingPanel.transform.SetParent(uiRoot.transform, false);
        var spRT = swingPanel.AddComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0.5f, 0f); spRT.anchorMax = new Vector2(0.5f, 0f);
        spRT.pivot     = new Vector2(0.5f, 0f);
        spRT.anchoredPosition = new Vector2(0, 40);
        spRT.sizeDelta = new Vector2(500, 60);
        swingPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        MakeText(spRT, "GuideLabel", "컨트롤러를 스윙하세요!",
            Vector2.zero, new Vector2(480, 54), 24);

        // ── GolfUIManager 참조 연결 ───────────────────────────────
        Apply(golfUI, so =>
        {
            so.FindProperty("courseInfoText").objectReferenceValue  = courseText;
            so.FindProperty("strokeText").objectReferenceValue      = strokeText;
            so.FindProperty("resultText").objectReferenceValue      = resultText;
            so.FindProperty("swingGuidePanel").objectReferenceValue = swingPanel;
        });

        // ── GolfGameManager 참조 연결 ─────────────────────────────
        Apply(golfMgr, so =>
        {
            so.FindProperty("ball").objectReferenceValue =
                ballCtrl;
            so.FindProperty("courseManager").objectReferenceValue =
                golfMgrGO.GetComponent<SilverCare.Golf.GolfCourseManager>();
            so.FindProperty("golfUI").objectReferenceValue =
                golfUI;
        });

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyTeammateWork] Golf 씬 세팅 완료");
        return true;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────

    static void Apply(Object target, System.Action<SerializedObject> action)
    {
        var so = new SerializedObject(target);
        action(so);
        so.ApplyModifiedProperties();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static GameObject NewPanel(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.16f, 0.9f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize,
        TextAnchor anchor = TextAnchor.MiddleCenter, bool topAnchor = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (topAnchor)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text      = content;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = anchor;
        t.font      = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }

    static InputField MakeInputField(RectTransform parent, string name,
        string placeholder, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.24f);

        var ph = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        ph.transform.SetParent(go.transform, false);
        Fill(ph.GetComponent<RectTransform>());
        var phT = ph.GetComponent<Text>();
        phT.text      = placeholder;
        phT.fontStyle = FontStyle.Italic;
        phT.color     = new Color(0.55f, 0.55f, 0.55f);
        phT.fontSize  = 18;
        phT.font      = Font.CreateDynamicFontFromOSFont("Arial", 18);

        var txt = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txt.transform.SetParent(go.transform, false);
        Fill(txt.GetComponent<RectTransform>());
        var txtT = txt.GetComponent<Text>();
        txtT.fontSize = 20;
        txtT.color    = Color.white;
        txtT.font     = Font.CreateDynamicFontFromOSFont("Arial", 20);

        var input = go.GetComponent<InputField>();
        input.textComponent = txtT;
        input.placeholder   = phT;
        return input;
    }

    static Button MakeButton(RectTransform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.18f, 0.50f, 0.28f);

        var txt = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txt.transform.SetParent(go.transform, false);
        Fill(txt.GetComponent<RectTransform>());
        var t = txt.GetComponent<Text>();
        t.text      = label;
        t.fontSize  = 20;
        t.color     = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Font.CreateDynamicFontFromOSFont("Arial", 20);

        return go.GetComponent<Button>();
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
