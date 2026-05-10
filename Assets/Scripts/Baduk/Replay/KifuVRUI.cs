// Assets/Scripts/Baduk/Replay/KifuVRUI.cs
// World Space Canvas 기반 복기 UI (BadukVRUI 패턴)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Baduk.Data;

namespace Baduk.Replay
{
    public class KifuVRUI : MonoBehaviour
    {
        // ── 외부 콜백 ────────────────────────────────────
        public System.Action<Kifu> OnKifuSelected;
        public System.Action OnPlayPause;
        public System.Action OnNext;
        public System.Action OnPrev;
        public System.Action OnRestart;
        public System.Action OnBack;
        public System.Action<float> OnSpeedChanged;

        [Header("UI 위치")]
        [SerializeField] Vector3 panelOffset = new Vector3(0f, 1.8f, 0f);

        Canvas     _canvas;
        GameObject _selectPanel;
        GameObject _replayPanel;

        Text _titleText, _playersText, _progressText, _commentText;
        Button _btnPlayPause, _btnNext, _btnPrev, _btnRestart, _btnBack;
        Button _btnSpeed05, _btnSpeed1, _btnSpeed2;

        bool _built;

        // ── 외부 호출 메서드 ─────────────────────────────

        public void ShowKifuSelect(List<Kifu> kifus)
        {
            EnsureBuilt();
            PlaceCanvasInFrontOfCamera();
            BuildKifuSelectButtons(kifus);
            _selectPanel.SetActive(true);
            _replayPanel.SetActive(false);
        }

        public void ShowReplay(Kifu kifu)
        {
            EnsureBuilt();
            PlaceCanvasInFrontOfCamera();
            _selectPanel.SetActive(false);
            _replayPanel.SetActive(true);

            _titleText.text   = kifu.title ?? "기보";
            _playersText.text = $"흑: {kifu.black_player ?? "?"}    백: {kifu.white_player ?? "?"}";
            _commentText.text = "";
            UpdateProgress(0, kifu.moves?.Count ?? 0);
            UpdatePlayPauseLabel(false);
        }

        public void UpdateProgress(int current, int total)
        {
            if (_progressText != null) _progressText.text = $"{current} / {total} 수";
        }

        public void UpdatePlayPauseLabel(bool isPlaying)
        {
            if (_btnPlayPause == null) return;
            var t = _btnPlayPause.GetComponentInChildren<Text>();
            if (t != null) t.text = isPlaying ? "⏸ 일시정지" : "▶ 재생";
        }

        public void ShowComment(string text)
        {
            if (_commentText != null) _commentText.text = text;
        }

        // ── 빌드 ─────────────────────────────────────────

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
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void PlaceCanvasInFrontOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || _canvas == null) return;
            _canvas.worldCamera = cam;

            Vector3 camPos = cam.transform.position;
            Vector3 flatFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (flatFwd == Vector3.zero) flatFwd = Vector3.forward;

            // 수평 forward 방향, 카메라 높이 그대로(상하 오프셋 X) — 씬 카메라 높이가 낮아도
            // 음수 Y로 떨어지지 않음. 보드 시점에선 상단 시야, 정면 시점에선 눈높이.
            var rt = _canvas.GetComponent<RectTransform>();
            rt.position = camPos + flatFwd * 2.0f;
            rt.rotation = Quaternion.LookRotation(flatFwd, Vector3.up);
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("KifuVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(800, 500);
            rt.localScale = Vector3.one * 0.002f;
            rt.position   = panelOffset;
            rt.rotation   = Quaternion.Euler(15, 180, 0);

            _selectPanel = BuildSelectPanel(rt);
            _replayPanel = BuildReplayPanel(rt);
            _replayPanel.SetActive(false);
        }

        // ── 기보 선택 패널 ───────────────────────────────
        GameObject BuildSelectPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "SelectPanel", new Color(0.1f, 0.1f, 0.18f, 0.95f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            CreateText(prt, "Title", "기보 복기", 40, FontStyle.Bold,
                new Vector2(0, 215), new Vector2(700, 60), Color.white);
            CreateText(prt, "Sub", "감상할 기보를 고르세요", 24, FontStyle.Normal,
                new Vector2(0, 165), new Vector2(700, 40), new Color(0.85f, 0.85f, 0.85f));

            // 기보 목록 영역 (BuildKifuSelectButtons에서 채움)
            var listGO = new GameObject("KifuList", typeof(RectTransform));
            listGO.transform.SetParent(prt, false);
            var lrt = listGO.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, -10);
            lrt.sizeDelta        = new Vector2(700, 240);

            var backBtn = CreateButton(prt, "로비로 돌아가기", 26,
                new Vector2(0, -215), new Vector2(380, 60));
            backBtn.onClick.AddListener(() => OnBack?.Invoke());

            return panel;
        }

        void BuildKifuSelectButtons(List<Kifu> kifus)
        {
            // SelectPanel/KifuList 찾기
            var listTr = _selectPanel.transform.Find("KifuList") as RectTransform;
            if (listTr == null) return;

            // 기존 버튼 제거
            for (int i = listTr.childCount - 1; i >= 0; i--)
                Destroy(listTr.GetChild(i).gameObject);

            if (kifus == null || kifus.Count == 0)
            {
                CreateText(listTr, "Empty", "사용 가능한 기보가 없습니다.", 24, FontStyle.Normal,
                    Vector2.zero, new Vector2(640, 40), new Color(1f, 0.6f, 0.6f));
                return;
            }

            int n = Mathf.Min(kifus.Count, 5);
            float spacing = 62f;
            float startY = (n - 1) * spacing / 2f;
            listTr.sizeDelta = new Vector2(700, n * spacing + 16);
            for (int i = 0; i < n; i++)
            {
                var k = kifus[i];
                var btn = CreateButton(listTr, k.title, 26,
                    new Vector2(0, startY - i * spacing), new Vector2(640, 60));
                btn.onClick.AddListener(() => OnKifuSelected?.Invoke(k));
            }
        }

        // ── 복기 패널 ────────────────────────────────────
        GameObject BuildReplayPanel(RectTransform parent)
        {
            var panel = CreatePanel(parent, "ReplayPanel", new Color(0, 0, 0, 0f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            // 상단 바
            var topBar = CreatePanel(prt, "TopBar", new Color(0, 0, 0, 0.82f));
            var tbrt = topBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = Vector2.one;
            tbrt.pivot = new Vector2(0.5f, 1);
            tbrt.offsetMin = new Vector2(0, -120); tbrt.offsetMax = Vector2.zero;

            _titleText   = CreateText(tbrt, "Title", "", 26, FontStyle.Bold,
                new Vector2(0, 15), new Vector2(750, 45), Color.white);
            _playersText = CreateText(tbrt, "Players", "", 20, FontStyle.Normal,
                new Vector2(0, -28), new Vector2(750, 32), new Color(0.85f, 0.85f, 0.85f));
            _progressText = CreateText(tbrt, "Progress", "", 18, FontStyle.Normal,
                new Vector2(320, -55), new Vector2(150, 28), new Color(0.7f, 0.7f, 0.7f));

            // 하단 바
            var botBar = CreatePanel(prt, "BotBar", new Color(0, 0, 0, 0.82f));
            var bbrt = botBar.GetComponent<RectTransform>();
            bbrt.anchorMin = Vector2.zero; bbrt.anchorMax = new Vector2(1, 0);
            bbrt.pivot = new Vector2(0.5f, 0);
            bbrt.offsetMin = Vector2.zero; bbrt.offsetMax = new Vector2(0, 180);

            // NPC 자막
            _commentText = CreateText(bbrt, "Comment", "", 22, FontStyle.Italic,
                new Vector2(0, 130), new Vector2(750, 36), new Color(1f, 0.95f, 0.6f));

            // 컨트롤 버튼 행 1: 이전/재생/다음/다시
            float btnY = 70f, btnW = 130f, gap = 10f;
            float totalW1 = btnW * 4 + gap * 3;
            float startX1 = -totalW1 / 2f;

            _btnPrev      = CreateButton(bbrt, "◀ 이전",     22, new Vector2(startX1,                     btnY), new Vector2(btnW, 50));
            _btnPlayPause = CreateButton(bbrt, "▶ 재생",     22, new Vector2(startX1 + (btnW + gap),       btnY), new Vector2(btnW, 50));
            _btnNext      = CreateButton(bbrt, "다음 ▶",     22, new Vector2(startX1 + (btnW + gap) * 2,   btnY), new Vector2(btnW, 50));
            _btnRestart   = CreateButton(bbrt, "↺ 처음부터", 20, new Vector2(startX1 + (btnW + gap) * 3,   btnY), new Vector2(btnW, 50));

            _btnPrev.onClick.AddListener(()      => OnPrev?.Invoke());
            _btnPlayPause.onClick.AddListener(() => OnPlayPause?.Invoke());
            _btnNext.onClick.AddListener(()      => OnNext?.Invoke());
            _btnRestart.onClick.AddListener(()   => OnRestart?.Invoke());

            // 컨트롤 버튼 행 2: 속도 + 나가기
            float btn2Y = 10f, btn2W = 110f;
            _btnSpeed05 = CreateButton(bbrt, "0.5x", 20, new Vector2(-260, btn2Y), new Vector2(btn2W, 45));
            _btnSpeed1  = CreateButton(bbrt, "1x",   20, new Vector2(-130, btn2Y), new Vector2(btn2W, 45));
            _btnSpeed2  = CreateButton(bbrt, "2x",   20, new Vector2(   0, btn2Y), new Vector2(btn2W, 45));
            _btnBack    = CreateButton(bbrt, "나가기", 20, new Vector2( 200, btn2Y), new Vector2(btn2W * 1.4f, 45));

            _btnSpeed05.onClick.AddListener(() => OnSpeedChanged?.Invoke(0.5f));
            _btnSpeed1.onClick.AddListener(()  => OnSpeedChanged?.Invoke(1f));
            _btnSpeed2.onClick.AddListener(()  => OnSpeedChanged?.Invoke(2f));
            _btnBack.onClick.AddListener(()    => OnBack?.Invoke());

            return panel;
        }

        // ── 헬퍼 (BadukVRUI와 동일 패턴) ──────────────────
        GameObject CreatePanel(RectTransform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
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
            t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.color = color;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Truncate;
            return t;
        }

        Button CreateButton(Transform parent, string label, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            var t = textGO.GetComponent<Text>();
            t.text = label; t.fontSize = fontSize; t.fontStyle = FontStyle.Bold; t.color = Color.white;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.alignment = TextAnchor.MiddleCenter;
            return go.GetComponent<Button>();
        }
    }
}
