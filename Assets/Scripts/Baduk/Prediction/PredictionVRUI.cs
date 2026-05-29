using System.Collections.Generic;
using Baduk.Data;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baduk.Prediction
{
    public class PredictionVRUI : MonoBehaviour
    {
        public System.Action<Kifu> OnKifuSelected;
        public System.Action OnPlayPause;
        public System.Action<float> OnSpeedChanged;
        public System.Action<int> OnPredictionSubmit;
        public System.Action OnRestart;
        public System.Action OnBack;
        public System.Action OnBackToSelect;

        readonly Color _panel = new(0.96f, 0.94f, 0.88f, 0.98f);
        readonly Color _ink = new(0.10f, 0.13f, 0.16f);
        readonly Color _muted = new(0.36f, 0.40f, 0.44f);
        readonly Color _accent = new(0.08f, 0.38f, 0.42f);
        readonly Color _note = new(0.58f, 0.38f, 0.05f);

        Canvas _canvas;
        GameObject _selectPanel;
        GameObject _replayPanel;
        GameObject _predictPanel;
        GameObject _resultPanel;
        RectTransform _kifuListRoot;

        Text _titleText;
        Text _playersText;
        Text _progressText;
        Text _commentText;
        Button _btnPlayPause;
        BadukPanelBillboard _billboard;

        Text _predictQuestion;
        Text _predictResultLabel;
        Text _predictExplain;
        RectTransform _predictCandidatesRoot;
        readonly List<Button> _candidateButtons = new();

        Text _resultHeadline;
        Text _resultDetail;

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
            SelectionBackdropUtility.ShowNatureBackdrop(_canvas.transform, "BadukPredictionSelect");
            ShowOnly(_selectPanel);
            BuildKifuButtons(kifus);
        }

        public void ShowReplay(Kifu kifu)
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            SelectionBackdropUtility.ClearBackdrop("BadukPredictionSelect");
            _billboard.anchorToBoard = true;
            _billboard.RequestReposition();
            ShowOnly(_replayPanel);

            _titleText.text = string.IsNullOrWhiteSpace(kifu.title) ? "수 예측하기" : kifu.title;
            _playersText.text = $"흑 {Safe(kifu.black_player)}    백 {Safe(kifu.white_player)}";
            _commentText.text = string.IsNullOrWhiteSpace(kifu.description)
                ? "재생 버튼을 눌러 다음 수 예측을 시작해보세요."
                : kifu.description;
            UpdateProgress(0, kifu.moves?.Count ?? 0);
            UpdatePlayPauseLabel(false);
        }

        public void ShowPredictionOverlay(PredictionPoint point)
        {
            EnsureBuilt();
            _predictPanel.SetActive(true);
            _predictResultLabel.text = "";
            _predictExplain.text = "";
            _predictQuestion.text = string.IsNullOrWhiteSpace(point?.question)
                ? "다음 수는 어디일까요?"
                : point.question;
            BuildCandidateButtons(point);
        }

        public void ShowPredictionResult(bool correct, PredictionPoint point, int chosenCandidateIndex)
        {
            _predictResultLabel.text = correct ? "정답입니다." : "다시 생각해보세요.";
            _predictResultLabel.color = correct
                ? new Color(0.10f, 0.55f, 0.22f)
                : new Color(0.78f, 0.20f, 0.16f);
            _predictExplain.text = string.IsNullOrWhiteSpace(point?.explanation)
                ? "설명을 준비 중입니다."
                : point.explanation;

            for (int i = 0; i < _candidateButtons.Count; i++)
            {
                var image = _candidateButtons[i].GetComponent<Image>();
                if (i == point.correct_index)
                    image.color = new Color(0.22f, 0.55f, 0.28f);
                else if (i == chosenCandidateIndex && !correct)
                    image.color = new Color(0.63f, 0.24f, 0.20f);
                else
                    image.color = _accent;

                _candidateButtons[i].interactable = false;
            }
        }

        public void HidePredictionOverlay()
        {
            if (_predictPanel != null)
                _predictPanel.SetActive(false);
        }

        public void ShowResult(int correct, int total)
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            ShowOnly(_resultPanel);
            _resultHeadline.text = total <= 0
                ? "수 예측이 끝났습니다."
                : $"{correct} / {total} 문제를 맞혔습니다.";
            _resultDetail.text = correct == total && total > 0
                ? "모든 수를 정확히 읽어냈어요."
                : "다시 도전해서 흐름을 익혀보세요.";
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
                text.text = isPlaying ? "일시 정지" : "재생";
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
            var canvasGO = new GameObject("PredictionVRCanvas");
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
            _predictPanel = BuildPredictionOverlay(rt);
            _resultPanel = BuildResultPanel(rt);

            ShowOnly(_selectPanel);
            _predictPanel.SetActive(false);
        }

        void ShowOnly(GameObject panel)
        {
            _selectPanel.SetActive(panel == _selectPanel);
            _replayPanel.SetActive(panel == _replayPanel);
            _resultPanel.SetActive(panel == _resultPanel);
            _predictPanel.SetActive(false);
        }

        GameObject BuildSelectPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "SelectPanel", _panel);
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            CreatePanel(prt, "Accent", _accent, new Vector2(0, 240), new Vector2(840, 6));
            CreateText(prt, "Title", "수 예측하기", 42, FontStyle.Bold,
                new Vector2(0, 190), new Vector2(820, 60), _accent);
            CreateText(prt, "Sub", "관찰할 기보를 고르고 다음 수를 맞혀보세요.", 24, FontStyle.Normal,
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

        void BuildKifuButtons(List<Kifu> kifus)
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
                int predictionCount = kifu.prediction_points?.Count ?? 0;
                string label = string.IsNullOrWhiteSpace(kifu.title) ? $"기보 {i + 1}" : kifu.title;
                if (predictionCount > 0)
                    label = $"{label}  ({predictionCount}문제)";

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
                new Vector2(0, 36), new Vector2(790, 56), _note);

            _btnPlayPause = CreateButton(botRt, "재생", 20, new Vector2(-180, -30), new Vector2(120, 46), _accent);
            var speed05 = CreateButton(botRt, "느리게", 18, new Vector2(-60, -30), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));
            var speed1 = CreateButton(botRt, "보통", 18, new Vector2(60, -30), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));
            var speed2 = CreateButton(botRt, "빠르게", 18, new Vector2(180, -30), new Vector2(110, 46), new Color(0.48f, 0.35f, 0.16f));
            var back = CreateButton(botRt, "목록", 18, new Vector2(310, -30), new Vector2(110, 46), new Color(0.34f, 0.38f, 0.40f));

            var restart = CreateButton(botRt, "다시 보기", 18, new Vector2(0, -78), new Vector2(150, 38), new Color(0.35f, 0.52f, 0.28f));

            _btnPlayPause.onClick.AddListener(() => OnPlayPause?.Invoke());
            speed05.onClick.AddListener(() => OnSpeedChanged?.Invoke(0.5f));
            speed1.onClick.AddListener(() => OnSpeedChanged?.Invoke(1f));
            speed2.onClick.AddListener(() => OnSpeedChanged?.Invoke(2f));
            back.onClick.AddListener(() => OnBackToSelect?.Invoke());
            restart.onClick.AddListener(() => OnRestart?.Invoke());

            return panel;
        }

        GameObject BuildPredictionOverlay(RectTransform parent)
        {
            var panel = CreatePanel(parent, "PredictPanel", new Color(0.08f, 0.10f, 0.12f, 0.96f));
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            var modal = CreatePanel(prt, "Modal", _panel, new Vector2(0, 12), new Vector2(760, 390));
            var modalRt = modal.GetComponent<RectTransform>();

            _predictQuestion = CreateText(modalRt, "Question", "", 30, FontStyle.Bold,
                new Vector2(0, 138), new Vector2(680, 60), _accent);

            var list = new GameObject("Candidates", typeof(RectTransform));
            list.transform.SetParent(modalRt, false);
            _predictCandidatesRoot = list.GetComponent<RectTransform>();
            _predictCandidatesRoot.anchoredPosition = new Vector2(0, 16);
            _predictCandidatesRoot.sizeDelta = new Vector2(640, 170);

            _predictResultLabel = CreateText(modalRt, "Result", "", 28, FontStyle.Bold,
                new Vector2(0, -92), new Vector2(640, 42), _ink);
            _predictExplain = CreateText(modalRt, "Explain", "", 20, FontStyle.Normal,
                new Vector2(0, -140), new Vector2(640, 72), _muted);

            return panel;
        }

        void BuildCandidateButtons(PredictionPoint point)
        {
            _candidateButtons.Clear();
            if (_predictCandidatesRoot == null)
                return;

            for (int i = _predictCandidatesRoot.childCount - 1; i >= 0; i--)
                Destroy(_predictCandidatesRoot.GetChild(i).gameObject);

            if (point?.candidates == null || point.candidates.Count == 0)
            {
                CreateText(_predictCandidatesRoot, "Empty", "선택지가 없습니다.", 22, FontStyle.Bold,
                    Vector2.zero, new Vector2(520, 36), new Color(0.78f, 0.20f, 0.16f));
                return;
            }

            int count = Mathf.Min(point.candidates.Count, 4);
            float spacing = 62f;
            float startY = (count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int index = i;
                string label = FormatCandidateLabel(point.candidates[i], i);
                var btn = CreateButton(_predictCandidatesRoot, label, 22,
                    new Vector2(0, startY - i * spacing), new Vector2(560, 50), _accent);
                btn.onClick.AddListener(() => OnPredictionSubmit?.Invoke(index));
                _candidateButtons.Add(btn);
            }
        }

        GameObject BuildResultPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "ResultPanel", _panel);
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            CreatePanel(prt, "Accent", _accent, new Vector2(0, 240), new Vector2(840, 6));
            _resultHeadline = CreateText(prt, "Headline", "", 40, FontStyle.Bold,
                new Vector2(0, 120), new Vector2(820, 56), _accent);
            _resultDetail = CreateText(prt, "Detail", "", 24, FontStyle.Normal,
                new Vector2(0, 48), new Vector2(820, 42), _muted);

            var retry = CreateButton(prt, "다시 하기", 24,
                new Vector2(-170, -92), new Vector2(220, 62), new Color(0.35f, 0.52f, 0.28f));
            var backToList = CreateButton(prt, "목록으로", 24,
                new Vector2(95, -92), new Vector2(220, 62), _accent);
            var lobby = CreateButton(prt, "로비로", 24,
                new Vector2(0, -182), new Vector2(220, 58), new Color(0.34f, 0.38f, 0.40f));

            retry.onClick.AddListener(() => OnRestart?.Invoke());
            backToList.onClick.AddListener(() => OnBackToSelect?.Invoke());
            lobby.onClick.AddListener(() => OnBack?.Invoke());

            return panel;
        }

        static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        static string FormatCandidateLabel(StonePosition stone, int index)
        {
            if (stone == null)
                return $"후보 {index + 1}";

            return $"후보 {index + 1}  ({stone.row + 1}, {stone.col + 1})";
        }

        GameObject CreatePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        GameObject CreatePanel(RectTransform parent, string name, Color color, Vector2 anchoredPos, Vector2 size)
        {
            var go = CreatePanel(parent, name, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return go;
        }

        Text CreateText(RectTransform parent, string name, string text, int fontSize, FontStyle style,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = CreateUIFont(fontSize);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        Button CreateButton(RectTransform parent, string title, int fontSize, Vector2 anchoredPos, Vector2 size, Color? fill = null)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = fill ?? _accent;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = img.color;
            colors.highlightedColor = img.color * 1.08f;
            colors.pressedColor = img.color * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
            btn.colors = colors;
            if (btn.GetComponent<XRButtonHoverFeedback>() == null)
                btn.gameObject.AddComponent<XRButtonHoverFeedback>();

            CreateText(rt, "Label", title, fontSize, FontStyle.Bold, Vector2.zero, size - new Vector2(18, 12), Color.white);
            return btn;
        }

        static Font CreateUIFont(int fontSize)
        {
            string[] fontNames =
            {
                "Malgun Gothic",
                "Noto Sans CJK KR",
                "Noto Sans KR",
                "Droid Sans Fallback",
                "sans-serif"
            };

            return Font.CreateDynamicFontFromOSFont(fontNames, fontSize);
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
