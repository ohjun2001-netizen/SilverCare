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
            SelectionBackdropUtility.ClearAllBackdrops();
            _selectionPanel.SetActive(true);
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);
            // Golf는 자체 코스 프리뷰 환경을 사용 — ShowNatureBackdrop의 sky/mountain이 마젠타로 표시되므로 미호출

            var buttons = _selectionPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                if (i < 3)
                {
                    int index = i;
                    buttons[i].onClick.AddListener(() => onSelect(index));
                }
                else
                {
                    buttons[i].onClick.AddListener(() => onLobby?.Invoke());
                }
            }
        }

        public void HideCourseSelection()
        {
            _selectionPanel.SetActive(false);
            _gamePanel.SetActive(true);
            _resultPanel.SetActive(false);
            SelectionBackdropUtility.ClearAllBackdrops();
        }

        public void ShowCourseInfo(int current, int total)
        {
            if (_courseText != null)
                _courseText.text = $"Course {current} / {total}";
        }

        public void UpdateStroke(int stroke)
        {
            if (_strokeText != null)
                _strokeText.text = $"Stroke {stroke}";
            SetSwingUIActive(false);
        }

        public void ShowShotFeedback(float normalizedPower)
        {
            if (_feedbackText == null)
                return;

            string message = normalizedPower >= 0.85f ? "Nice Shot!" : "Good Swing!";
            Color color = normalizedPower >= 0.85f ? new Color(1f, 0.92f, 0.40f) : new Color(0.82f, 0.95f, 1f);

            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(PlayFeedback(message, color));
        }

        public void ShowShotDistance(float distance)
        {
            if (_distanceText != null)
                _distanceText.text = $"Distance {distance:0.0}m";
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
                _powerText.text = $"Power {Mathf.RoundToInt(clamped * 100f)}%";
            if (_powerFill != null)
                _powerFill.fillAmount = clamped;
        }

        public void ShowResult(int stroke, int score, Action onReselect, Action onLobby)
        {
            _selectionPanel.SetActive(false);
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(true);

            if (_resultText != null)
                _resultText.text = $"Strokes {stroke}\nScore {score}";

            var buttons = _resultPanel.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
            {
                buttons[0].onClick.RemoveAllListeners();
                buttons[0].onClick.AddListener(() => onReselect?.Invoke());
            }

            if (buttons.Length > 1)
            {
                buttons[1].onClick.RemoveAllListeners();
                buttons[1].onClick.AddListener(() => onLobby?.Invoke());
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

        void BuildCanvas()
        {
            var go = new GameObject("GolfUI_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            XRUIUtility.ConfigureWorldCanvas(go, _canvas);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 420f);
            rt.localScale = Vector3.one * 0.003f;

            _selectionPanel = BuildSelectionPanel(go.transform);
            _gamePanel = BuildGamePanel(go.transform);
            _resultPanel = BuildResultPanel(go.transform);

            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);
        }

        GameObject BuildSelectionPanel(Transform parent)
        {
            var panel = MakePanel(parent, "SelectionPanel", new Vector2(490f, 380f), new Color(0.05f, 0.11f, 0.08f, 0.90f));
            MakeLabel(panel.transform, "Title", new Vector2(0f, 152f), new Vector2(460f, 56f), 30, Color.white, "Golf Course Select");

            string[] labels =
            {
                "Course 1  |  Straight  (No obstacles)",
                "Course 2  |  Bumper Slalom  (x3)",
                "Course 3  |  Obstacle Run  (Windmill + x5)"
            };

            float[] yPos = { 84f, 4f, -76f };
            Color[] colors =
            {
                new Color(0.22f, 0.59f, 0.30f),
                new Color(0.72f, 0.53f, 0.14f),
                new Color(0.72f, 0.28f, 0.22f)
            };

            for (int i = 0; i < 3; i++)
                MakeButton(panel.transform, $"CourseBtn{i + 1}", new Vector2(0f, yPos[i]), new Vector2(420f, 60f), colors[i], labels[i], 20);

            MakeButton(panel.transform, "LobbyBtn", new Vector2(0f, -165f), new Vector2(220f, 46f), new Color(0.24f, 0.28f, 0.32f), "Back To Lobby", 18);
            return panel;
        }

        GameObject BuildGamePanel(Transform parent)
        {
            var panel = MakePanel(parent, "GamePanel", new Vector2(300f, 205f), new Color(0.02f, 0.04f, 0.06f, 0.18f));
            _courseText = MakeLabel(panel.transform, "CourseText", new Vector2(0f, 76f), new Vector2(280f, 30f), 16, Color.white, "Course 1 / 3");
            _strokeText = MakeLabel(panel.transform, "StrokeText", new Vector2(0f, 48f), new Vector2(280f, 32f), 19, new Color(1f, 0.90f, 0.38f), "Stroke 0");
            _distanceText = MakeLabel(panel.transform, "DistanceText", new Vector2(0f, 21f), new Vector2(280f, 28f), 15, new Color(0.83f, 0.97f, 1f), "Distance 0.0m");
            _powerText = MakeLabel(panel.transform, "PowerText", new Vector2(0f, -8f), new Vector2(280f, 24f), 13, new Color(1f, 0.93f, 0.45f), "Power 0%");
            _powerFill = MakePowerBar(panel.transform, new Vector2(0f, -30f), new Vector2(210f, 14f));
            _feedbackText = MakeLabel(panel.transform, "FeedbackText", new Vector2(0f, -58f), new Vector2(280f, 30f), 18, new Color(1f, 0.94f, 0.42f), string.Empty);
            _swingGuide = MakeLabel(panel.transform, "SwingGuide", new Vector2(0f, -83f), new Vector2(290f, 22f), 11, new Color(0.94f, 0.86f, 0.54f), "Hold Space to charge, release to shoot").gameObject;

            MakeButton(panel.transform, "BackToSelectBtn", new Vector2(0f, -111f), new Vector2(142f, 26f), new Color(0.16f, 0.20f, 0.23f), "Course Select", 12);
            return panel;
        }

        GameObject BuildResultPanel(Transform parent)
        {
            var panel = MakePanel(parent, "ResultPanel", new Vector2(490f, 380f), new Color(0.06f, 0.08f, 0.15f, 0.92f));
            MakeLabel(panel.transform, "ResultTitle", new Vector2(0f, 146f), new Vector2(460f, 56f), 28, Color.white, "Result");
            _resultText = MakeLabel(panel.transform, "ResultText", new Vector2(0f, 68f), new Vector2(460f, 70f), 25, new Color(0.80f, 1f, 0.82f), string.Empty);

            MakeButton(panel.transform, "ReselectBtn", new Vector2(-112f, -66f), new Vector2(180f, 54f), new Color(0.20f, 0.50f, 0.88f), "Retry", 18);
            MakeButton(panel.transform, "LobbyBtn", new Vector2(112f, -66f), new Vector2(180f, 54f), new Color(0.35f, 0.38f, 0.42f), "Lobby", 18);
            return panel;
        }

        void PlaceCanvas()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null)
                return;

            _canvas.worldCamera = cam;
            XRUIUtility.PlaceCanvasFacingCamera(_canvas, 2.35f, 0.75f);
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

        static Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
        }
    }
}
