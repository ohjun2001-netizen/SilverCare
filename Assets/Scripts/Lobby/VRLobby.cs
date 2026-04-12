// Assets/Scripts/Lobby/VRLobby.cs
// VR 로비 - World Space Canvas + GraphicRaycaster + worldCamera
// 마우스 클릭으로 동작 (Quest 연결 시 XR Ray도 자동 작동)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SilverCare.Common;

public class VRLobby : MonoBehaviour
{
    Canvas _canvas;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
        PlaceCanvasInFrontOfCamera();
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
    }

    void PlaceCanvasInFrontOfCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || _canvas == null) return;

        _canvas.worldCamera = cam;

        Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (flatForward == Vector3.zero) flatForward = Vector3.forward;

        var rt = _canvas.GetComponent<RectTransform>();
        rt.position = cam.transform.position + flatForward * 2f + Vector3.up * 0.1f;
        rt.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("LobbyCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var rt = _canvas.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(900, 600);
        rt.localScale = Vector3.one * 0.002f;

        var bg = CreatePanel(rt, "Background", new Color(0.08f, 0.08f, 0.16f, 0.97f));
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

        CreateText(bgrt, "Title", "SilverCare - 어르신 미니게임",
            36, FontStyle.Bold, new Vector2(0, 240), new Vector2(860, 55), Color.white);
        CreateText(bgrt, "Sub", "즐기고 싶은 게임을 선택하세요",
            22, FontStyle.Normal, new Vector2(0, 190), new Vector2(860, 36),
            new Color(0.75f, 0.75f, 0.75f));

        (string scene, string label)[] games =
        {
            ("Baduk",      "바둑 사활문제"),
            ("BadukVR",    "바둑 (VR)"),
            ("CardMatch",  "카드 맞추기"),
            ("Quiz",       "퀴즈"),
            ("SongGuess",  "노래 맞추기"),
            ("GoStop",     "고스톱"),
            ("Golf",       "골프"),
        };

        float btnW = 360f, btnH = 68f, gapX = 20f, gapY = 14f, startY = 120f;

        for (int i = 0; i < games.Length; i++)
        {
            int row = i / 2, col = i % 2;
            float xOffset = (games.Length % 2 != 0 && i == games.Length - 1)
                ? 0f : (col == 0 ? -1f : 1f) * (btnW / 2f + gapX / 2f);
            float yPos = startY - row * (btnH + gapY);
            string sceneName = games[i].scene;
            var btn = CreateButton(bgrt, games[i].label, 26,
                new Vector2(xOffset, yPos), new Vector2(btnW, btnH));
            btn.onClick.AddListener(() => GoTo(sceneName));
        }
    }

    static void GoTo(string sceneName)
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    Text CreateText(RectTransform parent, string name, string text,
        int fontSize, FontStyle style, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.color = color;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }

    Button CreateButton(RectTransform parent, string label, int fontSize,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.38f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = textGO.GetComponent<Text>();
        t.text = label; t.fontSize = fontSize; t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.alignment = TextAnchor.MiddleCenter;

        var colors = go.GetComponent<Button>().colors;
        colors.normalColor      = new Color(0.18f, 0.22f, 0.38f);
        colors.highlightedColor = new Color(0.28f, 0.38f, 0.65f);
        colors.pressedColor     = new Color(0.10f, 0.14f, 0.28f);
        go.GetComponent<Button>().colors = colors;
        return go.GetComponent<Button>();
    }
}
