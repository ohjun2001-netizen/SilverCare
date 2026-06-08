using System;
using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.Golf
{
    public class GolfUIManager : MonoBehaviour
    {
        Canvas _canvas;
        GameObject _selectionPanel;
        GameObject _gamePanel;
        GameObject _resultPanel;
        Text _courseText;
        Text _strokeText;
        Text _resultText;
        Text _distanceText;
        Text _feedbackText;
        Text _powerText;
        Image _powerFill;
        GameObject _swingGuide;
        Coroutine _feedbackRoutine;

        void Awake() => BuildCanvas();
        void Start() => PlaceCanvas();

        public void ShowCourseSelection(Action<int> onSelect, Action onLobby = null)
        {
            PlaceCanvas(); // 결과 화면에서 홀 근처로 옮겼던 캔버스를 정면으로 복원
            SelectionBackdropUtility.ClearAllBackdrops();
            _selectionPanel.SetActive(true);
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);
            SelectionBackdropUtility.ShowNatureBackdrop(_canvas.transform, "GolfCourseSelect");

            var buttons = _selectionPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                button.onClick.RemoveAllListeners();

                if (button.name == "LobbyBtn")
                {
                    button.onClick.AddListener(() => onLobby?.Invoke());
                }
                else if (button.name.StartsWith("CourseBtn"))
                {
                    string suffix = button.name.Replace("CourseBtn", "");
                    if (int.TryParse(suffix, out int parsedIndex))
                    {
                        int index = Mathf.Clamp(parsedIndex - 1, 0, 2);
                        button.onClick.AddListener(() => onSelect(index));
                    }
                }
            }
        }

        public void HideCourseSelection()
        {
            PlaceCanvas(); // 결과(홀 근처)에서 게임 시작 시 HUD를 정면으로 복원
            _selectionPanel.SetActive(false);
            _gamePanel.SetActive(true);
            _resultPanel.SetActive(false);
            SelectionBackdropUtility.ClearBackdrop("GolfCourseSelect");
        }

        public void ShowCourseInfo(int current, int total)
        {
            if (_courseText != null)
                _courseText.text = $"코스 {current} / {total}";
        }

        public void UpdateStroke(int stroke)
        {
            if (_strokeText != null)
                _strokeText.text = $"타수 {stroke}";
            SetSwingUIActive(false);
        }

        public void ShowShotFeedback(float normalizedPower)
        {
            if (_feedbackText == null)
                return;

            string message = normalizedPower >= 0.85f ? "멋진 샷!" : "좋은 스윙!";
            Color color = normalizedPower >= 0.85f ? new Color(1f, 0.92f, 0.40f) : new Color(0.82f, 0.95f, 1f);

            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(PlayFeedback(message, color));
        }

        public void ShowShotDistance(float distance)
        {
            if (_distanceText != null)
                _distanceText.text = $"거리 {distance:0.0}m";
        }

        public void SetSwingUIActive(bool on)
        {
            if (_swingGuide != null)
                _swingGuide.SetActive(on);
        }

        public void ShowPower(float normalizedPower)
        {
            float clamped = Mathf.Clamp01(normalizedPower);
            if (_powerText != null)
                _powerText.text = $"파워 {Mathf.RoundToInt(clamped * 100f)}%";
            if (_powerFill != null)
                _powerFill.fillAmount = clamped;
        }

        public void ShowResult(int stroke, int score,
            Action onRetry, Action onSelectDifficulty, Action onLobby)
        {
            _selectionPanel.SetActive(false);
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(true);

            // 코스 지형/장식에 VR 레이저가 막히지 않도록 결과 패널은 플레이어 정면(검증된 위치)에 둔다.
            PlaceCanvas();

            if (_resultText != null)
                _resultText.text = $"타수 {stroke}\n점수 {score}";

            // 버튼 순서: 다시 하기 / 난이도 선택 / 로비
            // 클릭 시 결과 패널을 먼저 확실히 닫아 잔상을 방지한다.
            var buttons = _resultPanel.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
            {
                buttons[0].onClick.RemoveAllListeners();
                buttons[0].onClick.AddListener(() => { _resultPanel.SetActive(false); onRetry?.Invoke(); });
            }
            if (buttons.Length > 1)
            {
                buttons[1].onClick.RemoveAllListeners();
                buttons[1].onClick.AddListener(() => { _resultPanel.SetActive(false); onSelectDifficulty?.Invoke(); });
            }
            if (buttons.Length > 2)
            {
                buttons[2].onClick.RemoveAllListeners();
                buttons[2].onClick.AddListener(() => { _resultPanel.SetActive(false); onLobby?.Invoke(); });
            }
        }

        public void ShowFinalResult(List<int> strokes, int score)
        {
        }

        public void SetBackToSelectCallback(Action onBack)
        {
            var buttons = _gamePanel.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name != "BackToSelectBtn")
                    continue;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onBack?.Invoke());
                break;
            }
        }

        static Font _koreanFont;

        static Font GetFont()
        {
            if (_koreanFont == null)
                _koreanFont = Resources.Load<Font>("Fonts/Paperlogy-5Medium");
            return _koreanFont != null ? _koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void BuildCanvas()
        {
            var go = new GameObject("GolfUI_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            XRUIUtility.ConfigureWorldCanvas(go, _canvas);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900f, 520f);
            rt.localScale = Vector3.one * 0.003f;

            _selectionPanel = BuildSelectionPanel(go.transform);
            _gamePanel = BuildGamePanel(go.transform);
            _resultPanel = BuildResultPanel(go.transform);

            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);
        }

        GameObject BuildSelectionPanel(Transform parent)
        {
            var panel = MakePanel(parent, "SelectionPanel", new Vector2(900f, 520f), new Color(0.94f, 0.90f, 0.78f, 0.94f));
            AddOutline(panel, new Color(0.55f, 0.49f, 0.35f, 0.42f), new Vector2(2f, -2f));

            MakePanel(panel.transform, "Accent", new Vector2(840f, 6f), new Color(0.08f, 0.41f, 0.33f), new Vector2(0f, 240f));
            MakeLabel(panel.transform, "Title", new Vector2(0f, 190f), new Vector2(820f, 60f), 42,
                new Color(0.08f, 0.41f, 0.33f), "VR 골프");
            MakeLabel(panel.transform, "Subtitle", new Vector2(0f, 142f), new Vector2(820f, 42f), 24,
                new Color(0.36f, 0.30f, 0.20f), "코스를 고르고 가볍게 퍼팅해보세요.");

            MakeCourseButton(panel.transform, "CourseBtn1", "쉬움", "처음 시작하는 직선 코스", new Vector2(-220f, 55f));
            MakeCourseButton(panel.transform, "CourseBtn2", "보통", "조금 더 생각하는 경사 코스", new Vector2(220f, 55f));
            MakeCourseButton(panel.transform, "CourseBtn3", "어려움", "집중해서 치는 언덕 코스", new Vector2(0f, -70f));

            // 조작법 안내 (코스 버튼과 하단 구분선 사이 빈 공간 활용)
            MakeLabel(panel.transform, "ControlsHint", new Vector2(0f, -133f), new Vector2(840f, 26f), 15,
                new Color(0.28f, 0.48f, 0.40f),
                "왼쪽 조이스틱: 이동   |   오른쪽 조이스틱 좌우: 시점 변경");

            // 하단 악센트 + 안내문으로 빈 공간을 채워 단조로움을 줄인다.
            MakePanel(panel.transform, "BottomAccent", new Vector2(840f, 4f), new Color(0.08f, 0.41f, 0.33f, 0.55f), new Vector2(0f, -150f));
            MakeLabel(panel.transform, "BottomHint", new Vector2(0f, -176f), new Vector2(820f, 30f), 18,
                new Color(0.40f, 0.34f, 0.24f), "천천히, 편안하게 한 홀씩 즐겨보세요.");

            MakeButton(panel.transform, "LobbyBtn", new Vector2(0f, -222f), new Vector2(420f, 60f),
                new Color(0.34f, 0.38f, 0.40f), "로비로 돌아가기", 24);
            return panel;
        }

        GameObject BuildGamePanel(Transform parent)
        {
            var panel = MakePanel(parent, "GamePanel", new Vector2(300f, 205f), new Color(0.02f, 0.04f, 0.06f, 0.18f));
            _courseText = MakeLabel(panel.transform, "CourseText", new Vector2(0f, 76f), new Vector2(280f, 30f), 16, Color.white, "코스 1 / 3");
            _strokeText = MakeLabel(panel.transform, "StrokeText", new Vector2(0f, 48f), new Vector2(280f, 32f), 19, new Color(1f, 0.90f, 0.38f), "타수 0");
            _distanceText = MakeLabel(panel.transform, "DistanceText", new Vector2(0f, 21f), new Vector2(280f, 28f), 15, new Color(0.83f, 0.97f, 1f), "거리 0.0m");
            _powerText = MakeLabel(panel.transform, "PowerText", new Vector2(0f, -8f), new Vector2(280f, 24f), 13, new Color(1f, 0.93f, 0.45f), "파워 0%");
            _powerFill = MakePowerBar(panel.transform, new Vector2(0f, -30f), new Vector2(210f, 14f));
            _feedbackText = MakeLabel(panel.transform, "FeedbackText", new Vector2(0f, -58f), new Vector2(280f, 30f), 18, new Color(1f, 0.94f, 0.42f), string.Empty);
            _swingGuide = MakeLabel(panel.transform, "SwingGuide", new Vector2(0f, -83f), new Vector2(290f, 22f), 11, new Color(0.94f, 0.86f, 0.54f), "퍼터를 스윙하여 공을 치세요").gameObject;

            MakeButton(panel.transform, "BackToSelectBtn", new Vector2(0f, -111f), new Vector2(142f, 26f), new Color(0.16f, 0.20f, 0.23f), "코스 선택", 12);
            return panel;
        }

        GameObject BuildResultPanel(Transform parent)
        {
            var panel = MakePanel(parent, "ResultPanel", new Vector2(490f, 380f), new Color(0.06f, 0.08f, 0.15f, 0.92f));
            MakeLabel(panel.transform, "ResultTitle", new Vector2(0f, 146f), new Vector2(460f, 56f), 28, Color.white, "결과");
            _resultText = MakeLabel(panel.transform, "ResultText", new Vector2(0f, 68f), new Vector2(460f, 70f), 25, new Color(0.80f, 1f, 0.82f), string.Empty);

            MakeButton(panel.transform, "RetryBtn", new Vector2(-160f, -66f), new Vector2(150f, 54f), new Color(0.20f, 0.50f, 0.88f), "다시 하기", 17);
            MakeButton(panel.transform, "SelectBtn", new Vector2(0f, -66f), new Vector2(150f, 54f), new Color(0.22f, 0.62f, 0.40f), "난이도 선택", 16);
            MakeButton(panel.transform, "LobbyBtn", new Vector2(160f, -66f), new Vector2(150f, 54f), new Color(0.35f, 0.38f, 0.42f), "로비", 17);
            return panel;
        }

        void PlaceCanvas()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null)
                return;

            _canvas.worldCamera = cam;
            XRUIUtility.PlaceCanvasFacingCamera(_canvas, 2.35f, 0.75f);
            _canvas.GetComponent<RectTransform>().localScale = Vector3.one * 0.003f; // 기본 스케일 복원
        }

        IEnumerator PlayFeedback(string message, Color color)
        {
            _feedbackText.text = message;
            _feedbackText.color = color;
            yield return new WaitForSeconds(1.1f);
            _feedbackText.text = string.Empty;
            _feedbackRoutine = null;
        }

        static GameObject MakePanel(Transform parent, string name, Vector2 size, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = bg;
            return go;
        }

        static GameObject MakePanel(Transform parent, string name, Vector2 size, Color bg, Vector2 pos)
        {
            var go = MakePanel(parent, name, size, bg);
            go.GetComponent<RectTransform>().anchoredPosition = pos;
            return go;
        }

        static Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var label = go.AddComponent<Text>();
            label.font = GetFont();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            return label;
        }

        static Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, string text, FontStyle style)
        {
            var label = MakeLabel(parent, name, pos, size, fontSize, color, text);
            label.fontStyle = style;
            return label;
        }

        static Image MakePowerBar(Transform parent, Vector2 pos, Vector2 size)
        {
            var bg = new GameObject("PowerBar");
            bg.transform.SetParent(parent, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.sizeDelta = size;
            bgRt.anchoredPosition = pos;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.82f, 0.16f, 0.95f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            return fillImage;
        }

        static void MakeButton(Transform parent, string name, Vector2 pos, Vector2 size, Color color, string label, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = color * 1.25f,
                pressedColor = color * 0.75f,
                selectedColor = color * 1.18f,
                disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            if (button.GetComponent<XRButtonHoverFeedback>() == null)
                button.gameObject.AddComponent<XRButtonHoverFeedback>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.sizeDelta = size;
            var text = labelGo.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
        }

        static void MakeCourseButton(Transform parent, string name, string title, string desc, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 94f);
            rt.anchoredPosition = pos;

            var color = new Color(0.06f, 0.25f, 0.30f);
            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, new Color(0.84f, 0.93f, 0.56f), 0.34f),
                pressedColor = Color.Lerp(color, Color.black, 0.20f),
                selectedColor = Color.Lerp(color, new Color(0.84f, 0.93f, 0.56f), 0.34f),
                disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            AddOutline(go, new Color(0.92f, 0.86f, 0.62f, 0.55f), new Vector2(2f, -2f));

            if (button.GetComponent<XRButtonHoverFeedback>() == null)
                button.gameObject.AddComponent<XRButtonHoverFeedback>();

            MakeLabel(go.transform, "Title", new Vector2(0f, 18f), new Vector2(360f, 38f), 30, Color.white, title, FontStyle.Bold);
            MakeLabel(go.transform, "Desc", new Vector2(0f, -23f), new Vector2(360f, 30f), 19,
                new Color(0.92f, 0.98f, 0.91f), desc);
        }

        static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null)
                return;

            var outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }
    }
}
