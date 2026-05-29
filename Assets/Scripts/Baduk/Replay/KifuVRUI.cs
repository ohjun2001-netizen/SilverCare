using System.Collections.Generic;
using Baduk.Data;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baduk.Replay
{
    public class KifuVRUI : MonoBehaviour
    {
        public System.Action<Kifu> OnKifuSelected;
        public System.Action OnPlayPause;
        public System.Action OnNext;
        public System.Action OnPrev;
        public System.Action OnRestart;
        public System.Action OnBack;
        public System.Action<float> OnSpeedChanged;

        readonly Color _panel = new(0.96f, 0.94f, 0.88f, 0.98f);
        readonly Color _ink = new(0.10f, 0.13f, 0.16f);
        readonly Color _muted = new(0.36f, 0.40f, 0.44f);
        readonly Color _accent = new(0.08f, 0.38f, 0.42f);

        Canvas _canvas;
        GameObject _selectPanel;
        GameObject _replayPanel;
        RectTransform _kifuListRoot;

        Text _titleText;
        Text _playersText;
        Text _progressText;
        Text _commentText;
        Button _btnPlayPause;
        BadukPanelBillboard _billboard;

        bool _built;
        bool _placementLocked;

        public void ShowKifuSelect(List<Kifu> kifus)
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            BadukRoomEnvironment.Cleanup();
            SelectionBackdropUtility.ClearAllBackdrops();
            _billboard.anchorToBoard = false;
            _billboard.ForceReposition();
            SelectionBackdropUtility.ShowNatureBackdrop(_canvas.transform, "BadukReplaySelect");
            BuildKifuSelectButtons(kifus);
            _selectPanel.SetActive(true);
            _replayPanel.SetActive(false);
        }

        public void ShowReplay(Kifu kifu)
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            SelectionBackdropUtility.ClearBackdrop("BadukReplaySelect");
            _billboard.anchorToBoard = true;
            _billboard.RequestReposition();
            _selectPanel.SetActive(false);
            _replayPanel.SetActive(true);

            _titleText.text = string.IsNullOrWhiteSpace(kifu.title) ? "바둑 복기" : kifu.title;
            _playersText.text = $"흑  {Safe(kifu.black_player)}    백  {Safe(kifu.white_player)}";
            _commentText.text = "재생 버튼을 눌러 수순을 시작하세요.";
            UpdateProgress(0, kifu.moves?.Count ?? 0);
            UpdatePlayPauseLabel(false);
        }

        public void UpdateProgress(int current, int total)
        {
            if (_progressText != null)
                _progressText.text = $"{current} / {total} 수";
        }

        public void UpdatePlayPauseLabel(bool isPlaying)
        {
            if (_btnPlayPause == null)
                return;

            var text = _btnPlayPause.GetComponentInChildren<Text>();
            if (text != null)
                text.text = isPlaying ? "일시정지" : "재생";
        }

        public void ShowComment(string text)
        {
            if (_commentText != null)
                _commentText.text = string.IsNullOrWhiteSpace(text) ? "설명 문구가 없습니다." : text;
        }

        void EnsureBuilt()
        {
            if (_built)
                return;

            EnsureEventSystem();
            BuildUI();
            _built = true;
        }

        void EnsureCanvasPlacement()
        {
            if (_placementLocked)
                return;

            if (ApplyCameraVisuals())
                _placementLocked = true;
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        bool ApplyCameraVisuals()
        {
            var cam = Camera.main;
            if (cam == null || _canvas == null)
                return false;

            _canvas.worldCamera = cam;
            return true;
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("KifuVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;
            scaler.referencePixelsPerUnit = 140f;
            XRUIUtility.ConfigureWorldCanvas(canvasGO, _canvas);

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(920, 560);
            rt.localScale = Vector3.one * 0.0018f;

            _billboard = canvasGO.AddComponent<BadukPanelBillboard>();
            _billboard.distance = 1.75f;
            _billboard.heightOffset = 0.40f;
            _billboard.backOffset = 0.75f;
            _billboard.board = GetComponent<BadukBoard>();

            _selectPanel = BuildSelectPanel(rt);
            _replayPanel = BuildReplayPanel(rt);
            _replayPanel.SetActive(false);
        }

        GameObject BuildSelectPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "SelectPanel", _panel);
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            CreatePanel(prt, "Accent", _accent, new Vector2(0, 240), new Vector2(840, 6));
            CreateText(prt, "Title", "바둑 복기", 42, FontStyle.Bold,
                new Vector2(0, 190), new Vector2(820, 60), _accent);
            CreateText(prt, "Sub", "보고 싶은 기보를 선택하세요.", 24, FontStyle.Normal,
                new Vector2(0, 142), new Vector2(820, 42), _muted);

            var list = new GameObject("KifuList", typeof(RectTransform));
            list.transform.SetParent(prt, false);
            _kifuListRoot = list.GetComponent<RectTransform>();
            _kifuListRoot.anchoredPosition = new Vector2(0, -25);
            _kifuListRoot.sizeDelta = new Vector2(760, 280);

            var backBtn = CreateButton(prt, "로비로 돌아가기", 24,
                new Vector2(0, -220), new Vector2(420, 64), new Color(0.34f, 0.38f, 0.40f));
            backBtn.onClick.AddListener(() => OnBack?.Invoke());

            return panel;
        }

        void BuildKifuSelectButtons(List<Kifu> kifus)
        {
            if (_kifuListRoot == null)
                return;

            for (int i = _kifuListRoot.childCount - 1; i >= 0; i--)
                Destroy(_kifuListRoot.GetChild(i).gameObject);

            if (kifus == null || kifus.Count == 0)
            {
                CreateText(_kifuListRoot, "Empty", "사용 가능한 기보가 없습니다.", 26, FontStyle.Bold,
                    Vector2.zero, new Vector2(720, 60), new Color(0.78f, 0.20f, 0.16f));
                return;
            }

            int count = Mathf.Min(kifus.Count, 4);
            float spacing = 72f;
            float startY = (count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var kifu = kifus[i];
                string label = string.IsNullOrWhiteSpace(kifu.title) ? $"기보 {i + 1}" : kifu.title;
                var btn = CreateButton(_kifuListRoot, label, 24,
                    new Vector2(0, startY - i * spacing), new Vector2(720, 62), _accent);
                btn.onClick.AddListener(() => OnKifuSelected?.Invoke(kifu));
            }
        }

        GameObject BuildReplayPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "ReplayPanel", new Color(0f, 0f, 0f, 0f));
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            var top = CreatePanel(prt, "TopPanel", _panel, new Vector2(0, 128), new Vector2(860, 126));
            var topRt = top.GetComponent<RectTransform>();

            _titleText = CreateText(topRt, "Title", "", 31, FontStyle.Bold,
                new Vector2(-52, 26), new Vector2(690, 46), _accent);
            _progressText = CreateText(topRt, "Progress", "", 22, FontStyle.Bold,
                new Vector2(332, 26), new Vector2(120, 38), _muted);
            _playersText = CreateText(topRt, "Players", "", 22, FontStyle.Normal,
                new Vector2(0, -24), new Vector2(790, 58), _ink);

            var bottom = CreatePanel(prt, "BottomPanel", _panel, new Vector2(0, -44), new Vector2(860, 150));
            var botRt = bottom.GetComponent<RectTransform>();

            _commentText = CreateText(botRt, "Comment", "", 23, FontStyle.Bold,
                new Vector2(0, 36), new Vector2(790, 56), new Color(0.58f, 0.38f, 0.05f));

            var prev = CreateButton(botRt, "이전", 20, new Vector2(-300, -18), new Vector2(110, 50), new Color(0.30f, 0.36f, 0.42f));
            _btnPlayPause = CreateButton(botRt, "재생", 20, new Vector2(-180, -18), new Vector2(110, 50), _accent);
            var next = CreateButton(botRt, "다음", 20, new Vector2(-60, -18), new Vector2(110, 50), _accent);
            var restart = CreateButton(botRt, "처음부터", 18, new Vector2(70, -18), new Vector2(130, 50), new Color(0.35f, 0.52f, 0.28f));
            var back = CreateButton(botRt, "목록", 18, new Vector2(210, -18), new Vector2(110, 50), new Color(0.34f, 0.38f, 0.40f));

            var speed05 = CreateButton(botRt, "느리게", 18, new Vector2(-120, -80), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));
            var speed1 = CreateButton(botRt, "보통", 18, new Vector2(0, -80), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));
            var speed2 = CreateButton(botRt, "빠르게", 18, new Vector2(120, -80), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));

            prev.onClick.AddListener(() => OnPrev?.Invoke());
            _btnPlayPause.onClick.AddListener(() => OnPlayPause?.Invoke());
            next.onClick.AddListener(() => OnNext?.Invoke());
            restart.onClick.AddListener(() => OnRestart?.Invoke());
            back.onClick.AddListener(() => OnBack?.Invoke());
            speed05.onClick.AddListener(() => OnSpeedChanged?.Invoke(0.5f));
            speed1.onClick.AddListener(() => OnSpeedChanged?.Invoke(1f));
            speed2.onClick.AddListener(() => OnSpeedChanged?.Invoke(2f));

            return panel;
        }

        static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        GameObject CreatePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        GameObject CreatePanel(RectTransform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = CreatePanel(parent, name, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        Text CreateText(RectTransform parent, string name, string text, int fontSize,
            FontStyle style, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        Button CreateButton(RectTransform parent, string label, int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            CreateText(rt, "Text", label, fontSize, FontStyle.Bold, Vector2.zero, size, Color.white);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            if (btn.GetComponent<XRButtonHoverFeedback>() == null)
                btn.gameObject.AddComponent<XRButtonHoverFeedback>();
            return btn;
        }

        static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
