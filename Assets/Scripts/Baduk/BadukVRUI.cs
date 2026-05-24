using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk
{
    public class BadukVRUI : MonoBehaviour, IBadukUI
    {
        public System.Action OnNext { get; set; }
        public System.Action OnPrev { get; set; }
        public System.Action OnHint { get; set; }
        public System.Action OnRetry { get; set; }
        public System.Action OnBack { get; set; }
        public System.Action OnConfirmPlacement { get; set; }
        public System.Action OnCancelPlacement { get; set; }
        public System.Action<int> OnDifficultySelected { get; set; }

        [Header("UI Position")]
        [SerializeField] Vector3 panelOffset = new(0f, 1.65f, 0f);

        readonly Color _panel = new(0.96f, 0.94f, 0.88f, 0.98f);
        readonly Color _ink = new(0.10f, 0.13f, 0.16f);
        readonly Color _muted = new(0.36f, 0.40f, 0.44f);
        readonly Color _accent = new(0.08f, 0.38f, 0.42f);

        Canvas _canvas;
        GameObject _difficultyPanel;
        GameObject _gamePanel;

        Text _titleText;
        Text _descText;
        Text _msgText;
        Text _progressText;
        Button _btnRetry;
        GameObject _confirmPanel;
        Text _confirmText;
        BadukPanelBillboard _billboard;

        bool _built;
        bool _placementLocked;

        public void ShowDifficultySelect()
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            HidePlacementConfirm();
            SelectionBackdropUtility.ShowNatureBackdrop(_canvas.transform, "BadukDifficulty");
            _billboard.anchorToBoard = false;   // 난이도 선택은 보드가 없으니 카메라 정면
            _billboard.RequestReposition();
            _difficultyPanel.SetActive(true);
            _gamePanel.SetActive(false);
        }

        public void ShowProblem(BadukProblem problem, int idx, int total)
        {
            EnsureBuilt();
            EnsureCanvasPlacement();
            HidePlacementConfirm();
            SelectionBackdropUtility.ClearBackdrop("BadukDifficulty");
            _billboard.anchorToBoard = true;    // 문제 화면은 바둑판 중심에 맞춰 정렬
            _billboard.RequestReposition();
            _difficultyPanel.SetActive(false);
            _gamePanel.SetActive(true);

            _titleText.text = $"{problem.title}  ({GetDifficultyLabel(problem.difficulty)})";
            _descText.text = string.IsNullOrWhiteSpace(problem.description)
                ? "바둑판에서 가장 좋은 자리를 찾아보세요."
                : problem.description;
            _msgText.text = "컨트롤러로 바둑판 교차점을 가리킨 뒤 선택하세요.";
            _msgText.color = _accent;
            _progressText.text = $"{idx} / {total}";
            _btnRetry.gameObject.SetActive(false);
        }

        public void ShowResult(ProblemResult result, string explanation = "")
        {
            HidePlacementConfirm();

            switch (result)
            {
                case ProblemResult.Correct:
                    _msgText.text = string.IsNullOrWhiteSpace(explanation)
                        ? "정답입니다. 잘 찾으셨어요."
                        : $"정답입니다. {explanation}";
                    _msgText.color = new Color(0.10f, 0.55f, 0.22f);
                    _btnRetry.gameObject.SetActive(true);
                    break;

                case ProblemResult.Wrong:
                    _msgText.text = "아쉽지만 틀렸습니다. 힌트를 보고 다시 시도해 보세요.";
                    _msgText.color = new Color(0.78f, 0.20f, 0.16f);
                    break;

                case ProblemResult.PartialCorrect:
                    _msgText.text = "좋습니다. 이어서 다음 수를 찾아보세요.";
                    _msgText.color = new Color(0.08f, 0.42f, 0.64f);
                    break;
            }
        }

        public void ShowHintText(string hint)
        {
            // 확인창을 강제로 닫지 않는다. (자동 힌트 타이머가 떠 있는 확인창을 닫아버리는 버그 방지)
            // 확인창이 떠 있으면 힌트는 그 뒤에서 갱신만 되고, 확인/다시 선택 후 보이게 된다.
            _msgText.text = string.IsNullOrWhiteSpace(hint)
                ? "힌트가 없습니다. 빈 자리를 천천히 살펴보세요."
                : $"힌트: {hint}";
            _msgText.color = new Color(0.58f, 0.38f, 0.05f);
        }

        public void ShowGuideMessage(string message)
        {
            _msgText.text = string.IsNullOrWhiteSpace(message)
                ? "바둑돌을 둘 자리를 눌러보세요."
                : message;
            _msgText.color = new Color(0.70f, 0.46f, 0.08f);
        }

        public void ShowPlacementConfirm(string message)
        {
            EnsureBuilt();
            if (_confirmPanel == null)
                return;

            _confirmPanel.SetActive(true);
            if (_confirmText != null)
                _confirmText.text = string.IsNullOrWhiteSpace(message) ? "여기에 바둑돌을 두시겠어요?" : message;
        }

        public void HidePlacementConfirm()
        {
            if (_confirmPanel != null)
                _confirmPanel.SetActive(false);
        }

        void EnsureBuilt()
        {
            if (_built) return;
            EnsureEventSystem();
            BuildUI();
            _built = true;
        }

        void EnsureCanvasPlacement()
        {
            if (_placementLocked) return;
            if (ApplyCameraVisuals())
                _placementLocked = true;
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        static void LoadLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }

        bool ApplyCameraVisuals()
        {
            var cam = Camera.main;
            if (cam == null || _canvas == null) return false;

            _canvas.worldCamera = cam;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.88f, 0.92f);
            RenderSettings.ambientLight = new Color(0.64f, 0.58f, 0.48f);
            // 위치/회전은 BadukPanelBillboard가 매 프레임 카메라 기준으로 정렬한다.
            return true;
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("BadukVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;
            scaler.referencePixelsPerUnit = 140f;
            XRUIUtility.ConfigureWorldCanvas(canvasGO, _canvas);

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(920, 560);
            rt.localScale = Vector3.one * 0.0018f;
            rt.position = panelOffset;
            rt.rotation = Quaternion.identity;

            // 카메라를 향해 수평으로 항상 똑바로 보이게 정렬 (삐뚤어짐 방지 + 늦게 잡히는 카메라 대응)
            // 위치는 바둑판(책상) 중심에 맞춰, 시선이 어디를 향하든 판 위쪽 중앙에 오게 한다.
            _billboard = canvasGO.AddComponent<BadukPanelBillboard>();
            _billboard.distance = 1.75f;
            _billboard.heightOffset = 0.40f;   // 눈높이보다 위로
            _billboard.backOffset = 0.75f;     // 바둑판 중심에서 더 뒤로
            _billboard.board = GetComponent<BadukBoard>();

            _difficultyPanel = BuildDifficultyPanel(rt);
            _gamePanel = BuildGamePanel(rt);
            _gamePanel.SetActive(false);
        }

        GameObject BuildDifficultyPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "DifficultyPanel", _panel);
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            CreatePanel(prt, "Accent", _accent, new Vector2(0, 240), new Vector2(840, 6));
            CreateText(prt, "Title", "바둑 사활문제", 42, FontStyle.Bold,
                new Vector2(0, 190), new Vector2(820, 60), _accent);
            CreateText(prt, "Sub", "난이도를 고르고 바둑판에서 좋은 자리를 찾아보세요.", 24, FontStyle.Normal,
                new Vector2(0, 142), new Vector2(820, 42), _muted);

            MakeDifficultyButton(prt, 1, "쉬움", "처음 시작하는 문제", new Vector2(-220, 55));
            MakeDifficultyButton(prt, 2, "보통", "조금 더 생각하는 문제", new Vector2(220, 55));
            MakeDifficultyButton(prt, 3, "어려움", "집중해서 푸는 문제", new Vector2(-220, -70));
            MakeDifficultyButton(prt, 0, "전체 문제", "모든 난이도를 한꺼번에 보기", new Vector2(220, -70));

            var backBtn = CreateButton(prt, "로비로 돌아가기", 24,
                new Vector2(0, -205), new Vector2(420, 64), new Color(0.34f, 0.38f, 0.40f));
            backBtn.onClick.AddListener(LoadLobby);

            return panel;
        }

        void MakeDifficultyButton(RectTransform parent, int difficulty, string title, string desc, Vector2 pos)
        {
            var btn = CreateButton(parent, "", 24, pos, new Vector2(400, 94), _accent);
            var rt = btn.GetComponent<RectTransform>();
            CreateText(rt, "Title", title, 30, FontStyle.Bold,
                new Vector2(0, 18), new Vector2(360, 38), Color.white);
            CreateText(rt, "Desc", desc, 19, FontStyle.Normal,
                new Vector2(0, -23), new Vector2(360, 30), new Color(0.94f, 0.98f, 0.96f));
            btn.onClick.AddListener(() => OnDifficultySelected?.Invoke(difficulty));
        }

        GameObject BuildGamePanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "GamePanel", new Color(0, 0, 0, 0));
            Stretch(panel);
            var prt = panel.GetComponent<RectTransform>();

            var top = CreatePanel(prt, "TopPanel", _panel, new Vector2(0, 128), new Vector2(860, 126));
            var topRt = top.GetComponent<RectTransform>();

            _titleText = CreateText(topRt, "Title", "", 31, FontStyle.Bold,
                new Vector2(-52, 26), new Vector2(690, 46), _accent);
            _progressText = CreateText(topRt, "Progress", "", 22, FontStyle.Bold,
                new Vector2(332, 26), new Vector2(120, 38), _muted);
            _descText = CreateText(topRt, "Desc", "", 22, FontStyle.Normal,
                new Vector2(0, -24), new Vector2(790, 58), _ink);

            var bottom = CreatePanel(prt, "BottomPanel", _panel, new Vector2(0, -44), new Vector2(860, 150));
            var botRt = bottom.GetComponent<RectTransform>();

            _msgText = CreateText(botRt, "Message", "", 24, FontStyle.Bold,
                new Vector2(0, 36), new Vector2(790, 56), _accent);

            _btnRetry = CreateButton(botRt, "다시 풀기", 22, new Vector2(-325, -38), new Vector2(140, 56), new Color(0.35f, 0.52f, 0.28f));
            var prev = CreateButton(botRt, "이전", 22, new Vector2(-175, -38), new Vector2(130, 56), new Color(0.30f, 0.36f, 0.42f));
            var hint = CreateButton(botRt, "힌트", 22, new Vector2(-35, -38), new Vector2(130, 56), new Color(0.58f, 0.42f, 0.10f));
            var next = CreateButton(botRt, "다음", 22, new Vector2(105, -38), new Vector2(130, 56), _accent);
            var back = CreateButton(botRt, "난이도 선택", 20, new Vector2(285, -38), new Vector2(190, 56), new Color(0.34f, 0.38f, 0.40f));

            _btnRetry.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, -32);
            prev.GetComponent<RectTransform>().anchoredPosition = new Vector2(-135, -32);
            hint.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, -32);
            next.GetComponent<RectTransform>().anchoredPosition = new Vector2(145, -32);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(325, -32);

            _btnRetry.onClick.AddListener(() => OnRetry?.Invoke());
            prev.onClick.AddListener(() => OnPrev?.Invoke());
            hint.onClick.AddListener(() => OnHint?.Invoke());
            next.onClick.AddListener(() => OnNext?.Invoke());
            back.onClick.AddListener(() => OnBack?.Invoke());

            // 착수 확인 모달: 게임 패널 전체를 덮는 딤 오버레이로 뒤쪽 버튼(다음/이전/힌트 등) 클릭을 차단한다.
            _confirmPanel = CreatePanel(prt, "ConfirmPanel", new Color(0.05f, 0.06f, 0.09f, 0.66f));
            Stretch(_confirmPanel);
            _confirmPanel.GetComponent<Image>().raycastTarget = true; // 레이저가 뒤 버튼으로 새지 않게 막음

            var box = CreatePanel(_confirmPanel.GetComponent<RectTransform>(), "ConfirmBox",
                new Color(0.07f, 0.08f, 0.11f, 0.99f), new Vector2(0, 20), new Vector2(640, 240));
            var boxRt = box.GetComponent<RectTransform>();

            _confirmText = CreateText(boxRt, "ConfirmText", "여기에 바둑돌을 두시겠어요?", 28, FontStyle.Bold,
                new Vector2(0, 56), new Vector2(560, 72), Color.white);
            CreateText(boxRt, "ConfirmSub", "확인을 누르면 착수하고, 다시 선택을 누르면 취소됩니다.", 19, FontStyle.Normal,
                new Vector2(0, 6), new Vector2(560, 40), new Color(0.88f, 0.90f, 0.92f));

            var confirmBtn = CreateButton(boxRt, "확인", 22, new Vector2(-100, -64), new Vector2(170, 60), new Color(0.21f, 0.52f, 0.29f));
            var cancelBtn = CreateButton(boxRt, "다시 선택", 22, new Vector2(100, -64), new Vector2(170, 60), new Color(0.48f, 0.23f, 0.16f));
            confirmBtn.onClick.AddListener(() => OnConfirmPlacement?.Invoke());
            cancelBtn.onClick.AddListener(() => OnCancelPlacement?.Invoke());
            _confirmPanel.SetActive(false);

            return panel;
        }

        static string GetDifficultyLabel(int difficulty)
        {
            return difficulty switch
            {
                1 => "쉬움",
                2 => "보통",
                3 => "어려움",
                _ => "전체",
            };
        }

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

        Text CreateText(RectTransform parent, string name, string text, int fontSize, FontStyle style, Vector2 pos, Vector2 size, Color color)
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
            var go = new GameObject(label == "" ? "Button" : label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            if (!string.IsNullOrEmpty(label))
                CreateText(rt, "Text", label, fontSize, FontStyle.Bold, Vector2.zero, size, Color.white);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
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
