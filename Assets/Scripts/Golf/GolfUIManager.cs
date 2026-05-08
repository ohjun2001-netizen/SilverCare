// Assets/Scripts/Golf/GolfUIManager.cs
// 담당: 양석원
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.Golf
{
    public class GolfUIManager : MonoBehaviour
    {
        private Canvas     _canvas;
        private GameObject _selectionPanel;
        private GameObject _gamePanel;
        private GameObject _resultPanel;
        private Text       _courseText;
        private Text       _strokeText;
        private Text       _resultText;
        private GameObject _swingGuide;

        void Awake() => BuildCanvas();
        void Start()  => PlaceCanvas();

        // ── 코스 선택 화면 ──────────────────────────────────────────
        public void ShowCourseSelection(Action<int> onSelect, Action onLobby = null)
        {
            _selectionPanel.SetActive(true);
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);

            // 버튼 순서: CourseBtn1, CourseBtn2, CourseBtn3, LobbyBtn
            var btns = _selectionPanel.GetComponentsInChildren<Button>();
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].onClick.RemoveAllListeners();
                if (i < 3)
                {
                    int idx = i;
                    btns[i].onClick.AddListener(() => onSelect(idx));
                }
                else
                {
                    btns[i].onClick.AddListener(() => onLobby?.Invoke());
                }
            }
        }

        public void HideCourseSelection()
        {
            _selectionPanel.SetActive(false);
            _gamePanel.SetActive(true);
        }

        // ── 게임 중 UI ──────────────────────────────────────────────
        public void ShowCourseInfo(int current, int total)
        {
            if (_courseText) _courseText.text = $"코스 {current} / {total}";
        }

        public void UpdateStroke(int stroke)
        {
            if (_strokeText) _strokeText.text = $"{stroke}타";
            SetSwingUIActive(false);
        }

        public void SetSwingUIActive(bool on)
        {
            if (_swingGuide) _swingGuide.SetActive(on);
        }

        // ── 결과 화면 ──────────────────────────────────────────────
        public void ShowResult(int stroke, int score, Action onReselect, Action onLobby)
        {
            _gamePanel.SetActive(false);
            _resultPanel.SetActive(true);
            _resultText.text = $"{stroke}타 완료\n점수: {score}점";

            var btns = _resultPanel.GetComponentsInChildren<Button>();
            if (btns.Length > 0) { btns[0].onClick.RemoveAllListeners(); btns[0].onClick.AddListener(() => onReselect()); }
            if (btns.Length > 1) { btns[1].onClick.RemoveAllListeners(); btns[1].onClick.AddListener(() => onLobby()); }
        }

        // 기존 호환 (EndGame 에서 호출)
        public void ShowFinalResult(List<int> strokes, int score) { }

        // ── 빌드 ────────────────────────────────────────────────────
        void BuildCanvas()
        {
            var go = new GameObject("GolfUI_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(500f, 400f);
            rt.localScale = Vector3.one * 0.003f;

            _selectionPanel = BuildSelectionPanel(go.transform);
            _gamePanel      = BuildGamePanel(go.transform);
            _resultPanel    = BuildResultPanel(go.transform);

            _gamePanel.SetActive(false);
            _resultPanel.SetActive(false);
        }

        GameObject BuildSelectionPanel(Transform parent)
        {
            var panel = MakePanel(parent, "SelectionPanel", new Color(0.05f, 0.12f, 0.05f, 0.92f));

            MakeLabel(panel.transform, "Title", new Vector2(0f, 150f), 32,
                Color.white, "⛳  코스 선택");

            string[] labels = {
                "1번 홀  |  직선 코스  (초급)",
                "2번 홀  |  장애물 1개  (중급)",
                "3번 홀  |  장애물 2개  (고급)"
            };
            float[] yPos = { 80f, 0f, -80f };
            Color[] colors = {
                new Color(0.2f, 0.7f, 0.3f),
                new Color(0.8f, 0.6f, 0.1f),
                new Color(0.8f, 0.2f, 0.2f)
            };
            for (int i = 0; i < 3; i++)
                MakeButton(panel.transform, $"CourseBtn{i + 1}",
                    new Vector2(0f, yPos[i]), new Vector2(420f, 60f),
                    colors[i], labels[i], 22);

            MakeButton(panel.transform, "LobbyBtn",
                new Vector2(0f, -165f), new Vector2(200f, 45f),
                new Color(0.35f, 0.35f, 0.35f), "◀  돌아가기", 20);

            return panel;
        }

        GameObject BuildGamePanel(Transform parent)
        {
            var panel = MakePanel(parent, "GamePanel", new Color(0f, 0f, 0f, 0.5f));

            _courseText = MakeLabel(panel.transform, "CourseText",
                new Vector2(0f, 140f), 26, Color.white, "코스 1 / 3");
            _strokeText = MakeLabel(panel.transform, "StrokeText",
                new Vector2(0f, 80f), 30, Color.yellow, "0타");
            _swingGuide = MakeLabel(panel.transform, "SwingGuide",
                new Vector2(0f, 10f), 20,
                new Color(1f, 0.85f, 0.4f), "Space 키  /  VR 컨트롤러 스윙").gameObject;

            MakeButton(panel.transform, "BackToSelectBtn",
                new Vector2(0f, -90f), new Vector2(240f, 50f),
                new Color(0.3f, 0.3f, 0.3f), "◀  코스 선택", 20);

            return panel;
        }

        public void SetBackToSelectCallback(Action onBack)
        {
            var btns = _gamePanel.GetComponentsInChildren<Button>();
            foreach (var btn in btns)
            {
                if (btn.name == "BackToSelectBtn")
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => onBack?.Invoke());
                    break;
                }
            }
        }

        GameObject BuildResultPanel(Transform parent)
        {
            var panel = MakePanel(parent, "ResultPanel", new Color(0.05f, 0.05f, 0.15f, 0.92f));

            MakeLabel(panel.transform, "ResultTitle",
                new Vector2(0f, 140f), 28, Color.white, "결과");

            _resultText = MakeLabel(panel.transform, "ResultText",
                new Vector2(0f, 60f), 26, Color.green, "");

            MakeButton(panel.transform, "ReselectBtn",
                new Vector2(-110f, -60f), new Vector2(180f, 55f),
                new Color(0.2f, 0.5f, 0.9f), "다시 선택", 20);
            MakeButton(panel.transform, "LobbyBtn",
                new Vector2(110f, -60f), new Vector2(180f, 55f),
                new Color(0.4f, 0.4f, 0.4f), "로비로", 20);

            return panel;
        }

        void PlaceCanvas()
        {
            Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null) return;
            _canvas.worldCamera = cam;
            var rt  = _canvas.GetComponent<RectTransform>();
            var fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            rt.position = cam.transform.position + fwd * 2f + Vector3.up * 0.2f;
            rt.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ── 유틸 ────────────────────────────────────────────────────
        static GameObject MakePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(490f, 370f);
            var img = go.AddComponent<Image>();
            img.color = bg;
            return go;
        }

        static Text MakeLabel(Transform parent, string name, Vector2 pos,
            int fontSize, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 50f);
            rt.anchoredPosition = pos;
            var t = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text      = text;
            return t;
        }

        static void MakeButton(Transform parent, string name, Vector2 pos, Vector2 size,
            Color color, string label, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            var cb  = new ColorBlock
            {
                normalColor      = color,
                highlightedColor = color * 1.3f,
                pressedColor     = color * 0.7f,
                selectedColor    = color,
                colorMultiplier  = 1f,
                fadeDuration     = 0.1f
            };
            btn.colors = cb;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.sizeDelta = size;
            var t = labelGO.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.text      = label;
        }
    }
}
