// Assets/Scripts/Baduk/BadukVRUI.cs
// VR 전용 - World Space Canvas 기반 UI
// 코드로 Canvas + UI 요소를 자동 생성합니다.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Baduk.Data;

namespace Baduk
{
    public class BadukVRUI : MonoBehaviour, IBadukUI
    {
        public System.Action OnNext  { get; set; }
        public System.Action OnPrev  { get; set; }
        public System.Action OnHint  { get; set; }
        public System.Action OnRetry { get; set; }
        public System.Action OnBack  { get; set; }
        public System.Action<int> OnDifficultySelected { get; set; }

        [Header("UI 위치 (보드 기준)")]
        [SerializeField] Vector3 panelOffset = new Vector3(0f, 1.8f, 0f); // 카메라 앞 1.5m

        Canvas       _canvas;
        GameObject   _difficultyPanel;
        GameObject   _gamePanel;

        // 게임 UI 요소
        Text _titleText, _descText, _msgText, _progressText;
        Button _btnPrev, _btnHint, _btnNext, _btnRetry, _btnBack;

        bool _answered;
        bool _built;

        // ── IBadukUI 구현 ────────────────────────────────

        public void ShowDifficultySelect()
        {
            EnsureBuilt();
            PlaceCanvasInFrontOfCamera();
            _difficultyPanel.SetActive(true);
            _gamePanel.SetActive(false);
        }

        public void ShowProblem(BadukProblem problem, int idx, int total)
        {
            EnsureBuilt();
            PlaceCanvasInFrontOfCamera();
            _difficultyPanel.SetActive(false);
            _gamePanel.SetActive(true);

            string stars = new string('★', problem.difficulty) + new string('☆', 3 - problem.difficulty);
            _titleText.text    = $"#{problem.id}  {problem.title}  {stars}";
            _descText.text     = problem.description;
            _msgText.text      = "바둑판을 가리켜 돌을 놓으세요.";
            _msgText.color     = Color.white;
            _progressText.text = $"{idx} / {total}";

            _answered = false;
            _btnRetry.gameObject.SetActive(false);
        }

        public void ShowResult(ProblemResult result, string explanation = "")
        {
            switch (result)
            {
                case ProblemResult.Correct:
                    _msgText.text  = $"정답!  {explanation}";
                    _msgText.color = new Color(0.2f, 1f, 0.2f);
                    _answered = true;
                    _btnRetry.gameObject.SetActive(true);
                    break;
                case ProblemResult.Wrong:
                    _msgText.text  = "틀렸습니다. 힌트 버튼을 눌러보세요.";
                    _msgText.color = new Color(1f, 0.4f, 0.4f);
                    break;
                case ProblemResult.PartialCorrect:
                    _msgText.text  = "잘했어요! 계속 두세요.";
                    _msgText.color = new Color(0.4f, 1f, 1f);
                    break;
            }
        }

        public void ShowHintText(string hint)
        {
            _msgText.text  = $"힌트: {hint}";
            _msgText.color = new Color(1f, 1f, 0.3f);
        }

        // ── UI 자동 생성 ─────────────────────────────────

        void EnsureBuilt()
        {
            if (_built) return;
            EnsureEventSystem();
            BuildUI();
            _built = true;
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
            if (SilverCare.Common.GameSceneManager.Instance != null)
                SilverCare.Common.GameSceneManager.Instance.LoadScene(SilverCare.Common.GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }

        void PlaceCanvasInFrontOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || _canvas == null) return;

            // 마우스 클릭이 되려면 worldCamera 필수
            _canvas.worldCamera = cam;

            Vector3 camPos = cam.transform.position;
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;

            var rt = _canvas.GetComponent<RectTransform>();
            rt.position = camPos + flatForward * 2.5f + Vector3.up * 1.2f;
            rt.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        void BuildUI()
        {
            // World Space Canvas
            var canvasGO = new GameObject("BadukVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta    = new Vector2(800, 500);
            rt.localScale   = Vector3.one * 0.002f;
            rt.position     = panelOffset;
            rt.rotation     = Quaternion.Euler(15, 180, 0);

            // 난이도 선택 패널
            _difficultyPanel = BuildDifficultyPanel(rt);
            // 게임 패널
            _gamePanel = BuildGamePanel(rt);
            _gamePanel.SetActive(false);
        }

        // ── 난이도 선택 패널 ──────────────────────────────
        GameObject BuildDifficultyPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "DifficultyPanel", new Color(0.1f, 0.1f, 0.18f, 0.95f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            // 타이틀
            CreateText(prt, "Title", "바둑 사활문제", 36, FontStyle.Bold,
                new Vector2(0, 160), new Vector2(600, 50), Color.white);
            CreateText(prt, "Sub", "난이도를 선택하세요", 24, FontStyle.Normal,
                new Vector2(0, 110), new Vector2(600, 40), new Color(0.8f, 0.8f, 0.8f));

            // 버튼
            (int d, string l)[] opts = { (1, "초급  ★☆☆"), (2, "중급  ★★☆"), (3, "고급  ★★★"), (0, "전체 문제") };
            for (int i = 0; i < opts.Length; i++)
            {
                int diff = opts[i].d;
                var btn = CreateButton(prt, opts[i].l, 28,
                    new Vector2(0, 50 - i * 65), new Vector2(350, 55));
                btn.onClick.AddListener(() => OnDifficultySelected?.Invoke(diff));
            }

            var backBtn = CreateButton(prt, "로비로 돌아가기", 24,
                new Vector2(0, -230), new Vector2(350, 55));
            backBtn.onClick.AddListener(() => LoadLobby());

            return panel;
        }

        // ── 게임 패널 ────────────────────────────────────
        GameObject BuildGamePanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "GamePanel", new Color(0, 0, 0, 0f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            // 상단 바
            var topBar = CreatePanel(prt, "TopBar", new Color(0, 0, 0, 0.82f));
            var tbrt = topBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = Vector2.one;
            tbrt.pivot = new Vector2(0.5f, 1);
            tbrt.offsetMin = new Vector2(0, -120); tbrt.offsetMax = Vector2.zero;

            _titleText = CreateText(tbrt, "Title", "", 28, FontStyle.Bold,
                new Vector2(0, 15), new Vector2(750, 45), Color.white);
            _descText = CreateText(tbrt, "Desc", "", 22, FontStyle.Normal,
                new Vector2(0, -30), new Vector2(750, 45), new Color(0.92f, 0.92f, 0.92f));

            // 하단 바
            var botBar = CreatePanel(prt, "BotBar", new Color(0, 0, 0, 0.82f));
            var bbrt = botBar.GetComponent<RectTransform>();
            bbrt.anchorMin = Vector2.zero; bbrt.anchorMax = new Vector2(1, 0);
            bbrt.pivot = new Vector2(0.5f, 0);
            bbrt.offsetMin = Vector2.zero; bbrt.offsetMax = new Vector2(0, 150);

            _msgText = CreateText(bbrt, "Msg", "", 24, FontStyle.Bold,
                new Vector2(0, 45), new Vector2(750, 55), Color.white);

            // 버튼 행: 5개 버튼 균등 배치 (겹침 방지)
            float btnY = -35f, btnW = 130f, gap = 10f;
            float totalW = btnW * 5 + gap * 4;
            float startX = -totalW / 2f;

            _btnPrev  = CreateButton(bbrt, "◀ 이전",   22, new Vector2(startX,                       btnY), new Vector2(btnW, 50));
            _btnHint  = CreateButton(bbrt, "힌트",      22, new Vector2(startX + (btnW + gap),         btnY), new Vector2(btnW, 50));
            _btnNext  = CreateButton(bbrt, "다음 ▶",   22, new Vector2(startX + (btnW + gap) * 2,     btnY), new Vector2(btnW, 50));
            _btnRetry = CreateButton(bbrt, "다시 풀기", 22, new Vector2(startX + (btnW + gap) * 3,     btnY), new Vector2(btnW, 50));
            _btnBack  = CreateButton(bbrt, "나가기",    22, new Vector2(startX + (btnW + gap) * 4,     btnY), new Vector2(btnW, 50));

            _btnPrev.onClick.AddListener(() => OnPrev?.Invoke());
            _btnHint.onClick.AddListener(() => OnHint?.Invoke());
            _btnNext.onClick.AddListener(() => OnNext?.Invoke());
            _btnRetry.onClick.AddListener(() => OnRetry?.Invoke());
            _btnBack.onClick.AddListener(() => OnBack?.Invoke());

            // 진행도: 버튼 위 오른쪽
            _progressText = CreateText(bbrt, "Progress", "", 20, FontStyle.Normal,
                new Vector2(320f, 20f), new Vector2(120, 36), new Color(0.8f, 0.8f, 0.8f));

            return panel;
        }

        // ── 헬퍼 ─────────────────────────────────────────

        GameObject CreatePanel(RectTransform parent, string name, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;
            return go;
        }

        Text CreateText(RectTransform parent, string name, string text,
            int fontSize, FontStyle style, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var t = go.GetComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.color     = color;
            t.font      = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Truncate;
            return t;
        }

        Button CreateButton(RectTransform parent, string label, int fontSize,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            var t = textGO.GetComponent<Text>();
            t.text      = label;
            t.fontSize  = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color     = Color.white;
            t.font      = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.alignment = TextAnchor.MiddleCenter;

            return go.GetComponent<Button>();
        }
    }
}
