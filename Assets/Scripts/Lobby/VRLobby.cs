// Assets/Scripts/Lobby/VRLobby.cs
// World Space Canvas — XR Ray + 마우스 모두 지원
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SilverCare.Common;

public class VRLobby : MonoBehaviour
{
    Canvas _canvas;
    Text   _recText;
    Text   _scoreText;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
        PlaceCanvas();
        StartCoroutine(UpdateInfoTexts());
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void PlaceCanvas()
    {
        Camera cam = Camera.main;
        if (cam == null || _canvas == null) return;

        _canvas.worldCamera = cam;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        var rt = _canvas.GetComponent<RectTransform>();
        Vector3 pos = cam.transform.position + forward * 2f;
        pos.y = 1.5f;
        rt.position = pos;
        rt.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // LobbyManager.Start()가 먼저 완료된 뒤 PlayerDataManager를 읽음
    IEnumerator UpdateInfoTexts()
    {
        yield return null;

        var profile = PlayerDataManager.Instance?.Profile;

        if (_recText != null)
        {
            if (profile == null)
                _recText.text = "프로필을 설정하면 맞춤 게임을 추천해 드립니다.";
            else if (profile.age >= 70)
                _recText.text = "치매 예방을 위해 '카드 맞추기'를 추천합니다.";
            else
                _recText.text = "인지력 강화를 위해 '바둑 사활문제'를 추천합니다.";
        }

        if (_scoreText != null)
        {
            int high = PlayerDataManager.Instance?.GetHighScore("Baduk") ?? 0;
            _scoreText.text = high > 0 ? $"바둑 최고 점수: {high}" : "";
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("LobbyCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var rt = _canvas.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(800, 600);
        rt.localScale = Vector3.one * 0.002f;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(rt, false);
        bg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.97f);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

        // ── 타이틀 ───────────────────────────────────────────────
        MakeText(bgrt, "Title", "SilverCare - 어르신 미니게임",
            36, FontStyle.Bold, new Vector2(0, 250), new Vector2(780, 52), Color.white);
        MakeText(bgrt, "Sub", "즐기고 싶은 게임을 선택하세요",
            22, FontStyle.Normal, new Vector2(0, 202), new Vector2(780, 36),
            new Color(0.75f, 0.75f, 0.75f));

        // ── 게임 버튼 (4행 2열) ──────────────────────────────────
        (string scene, string label)[] games =
        {
            ("BadukVR",         "바둑 사활문제"),
            ("BadukReplay",     "바둑 복기 (탑골공원)"),
            ("BadukPrediction", "다음 수 맞히기"),
            ("CardMatch",       "카드 맞추기"),
            ("Quiz",            "퀴즈"),
            ("SongGuess",       "노래 맞추기"),
            ("GoStop",          "고스톱"),
            ("Golf",            "골프"),
        };

        float btnW = 360f, btnH = 60f, gapX = 20f, gapY = 10f, startY = 128f;

        for (int i = 0; i < games.Length; i++)
        {
            int row = i / 2, col = i % 2;
            float xOffset = (col == 0 ? -1f : 1f) * (btnW / 2f + gapX / 2f);
            float yPos = startY - row * (btnH + gapY);
            string sceneName = games[i].scene;
            MakeButton(bgrt, games[i].label, 24,
                new Vector2(xOffset, yPos), new Vector2(btnW, btnH))
                .onClick.AddListener(() => GoTo(sceneName));
        }

        // ── 구분선 ───────────────────────────────────────────────
        var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sep.transform.SetParent(bgrt, false);
        sep.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.55f, 0.6f);
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchoredPosition = new Vector2(0, -135);
        sepRT.sizeDelta = new Vector2(740, 2);

        // ── 게임 추천 텍스트 ─────────────────────────────────────
        _recText = MakeText(bgrt, "RecommendationText", "",
            20, FontStyle.Normal, new Vector2(0, -168), new Vector2(760, 36),
            new Color(0.95f, 0.88f, 0.45f));

        // ── 최고 점수 ────────────────────────────────────────────
        _scoreText = MakeText(bgrt, "HighScoreText", "",
            18, FontStyle.Normal, new Vector2(0, -208), new Vector2(760, 30),
            new Color(0.6f, 0.85f, 0.65f));
    }

    static void GoTo(string sceneName)
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    static Text MakeText(RectTransform parent, string name, string content,
        int fontSize, FontStyle style, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = content; t.fontSize = fontSize; t.fontStyle = style; t.color = color;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }

    static Button MakeButton(RectTransform parent, string label, int fontSize,
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

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.18f, 0.22f, 0.38f);
        colors.highlightedColor = new Color(0.28f, 0.38f, 0.65f);
        colors.pressedColor     = new Color(0.10f, 0.14f, 0.28f);
        btn.colors = colors;
        return btn;
    }
}
