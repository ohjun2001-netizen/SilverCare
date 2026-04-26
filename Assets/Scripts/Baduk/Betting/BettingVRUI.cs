// Assets/Scripts/Baduk/Betting/BettingVRUI.cs
// 베팅 모드 UI - 기보 선택 → 베팅 → 재생 → 결과 4단계
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Baduk.Data;

namespace Baduk.Betting
{
    public class BettingVRUI : MonoBehaviour
    {
        // ── 외부 콜백 ────────────────────────────────────
        public System.Action<Kifu> OnKifuSelected;
        public System.Action<BettingChoice, int> OnBetPlaced;  // (선택, 금액)
        public System.Action OnPlayPause;
        public System.Action OnRestart;
        public System.Action OnBack;
        public System.Action<float> OnSpeedChanged;

        Canvas _canvas;
        GameObject _selectPanel, _betPanel, _replayPanel, _resultPanel;
        Text _coinsText;

        // 베팅 패널 상태
        BettingChoice _selectedChoice = BettingChoice.None;
        int _selectedAmount = 0;
        Text _betChoiceLabel, _betAmountLabel, _betPotentialLabel;
        Button _btnConfirmBet;

        // 재생 패널
        Text _titleText, _playersText, _progressText, _commentText;
        Button _btnPlayPause;

        // 결과 패널
        Text _resultText, _payoutText;

        bool _built;

        // ── 외부 호출 ────────────────────────────────────

        public void ShowKifuSelect(List<Kifu> kifus, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_selectPanel);
            BuildKifuButtons(kifus);
        }

        public void ShowBetting(Kifu kifu, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_betPanel);
            _selectedChoice = BettingChoice.None;
            _selectedAmount = 0;
            UpdateBetSummary();
            var titleTr = _betPanel.transform.Find("BetTitle")?.GetComponent<Text>();
            if (titleTr != null) titleTr.text = $"내기: {kifu.title}";
        }

        public void ShowReplay(Kifu kifu, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_replayPanel);
            _titleText.text   = kifu.title ?? "기보";
            _playersText.text = $"흑: {kifu.black_player ?? "?"}    백: {kifu.white_player ?? "?"}";
            _commentText.text = "";
            UpdateProgress(0, kifu.moves?.Count ?? 0);
            UpdatePlayPauseLabel(false);
        }

        public void ShowResult(BettingResult result, int delta, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_resultPanel);
            switch (result)
            {
                case BettingResult.Win:
                    _resultText.text  = "적중!";
                    _resultText.color = new Color(0.3f, 1f, 0.4f);
                    _payoutText.text  = $"+{delta} 코인";
                    break;
                case BettingResult.Lose:
                    _resultText.text  = "아쉽네요";
                    _resultText.color = new Color(1f, 0.4f, 0.4f);
                    _payoutText.text  = $"-{delta} 코인";
                    break;
                case BettingResult.Draw:
                    _resultText.text  = "무승부";
                    _resultText.color = new Color(0.85f, 0.85f, 0.5f);
                    _payoutText.text  = "베팅금 환급";
                    break;
            }
        }

        public void UpdateProgress(int cur, int total)
        {
            if (_progressText != null) _progressText.text = $"{cur} / {total} 수";
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

        public void UpdateCoins(int coins)
        {
            if (_coinsText != null) _coinsText.text = $"코인: {coins}";
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

        void PlaceCanvas()
        {
            Camera cam = Camera.main;
            if (cam == null || _canvas == null) return;
            _canvas.worldCamera = cam;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (fwd == Vector3.zero) fwd = Vector3.forward;
            var rt = _canvas.GetComponent<RectTransform>();
            rt.position = cam.transform.position + fwd * 2.5f + Vector3.up * 1.2f;
            rt.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("BettingVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(800, 540);
            rt.localScale = Vector3.one * 0.002f;

            // 코인 표시 (모든 패널 상단 우측 공통)
            _coinsText = CreateText(rt, "Coins", "코인: 0", 22, FontStyle.Bold,
                new Vector2(280, 240), new Vector2(220, 36), new Color(1f, 0.85f, 0.3f));

            _selectPanel = BuildSelectPanel(rt);
            _betPanel    = BuildBetPanel(rt);
            _replayPanel = BuildReplayPanel(rt);
            _resultPanel = BuildResultPanel(rt);

            ShowOnly(_selectPanel);
        }

        void ShowOnly(GameObject panel)
        {
            _selectPanel.SetActive(panel == _selectPanel);
            _betPanel.SetActive(panel == _betPanel);
            _replayPanel.SetActive(panel == _replayPanel);
            _resultPanel.SetActive(panel == _resultPanel);
            _coinsText.gameObject.SetActive(true);
        }

        // ── 기보 선택 ────────────────────────────────────
        GameObject BuildSelectPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "SelectPanel", new Color(0.1f, 0.1f, 0.18f, 0.95f));
            FillParent(p);
            CreateText(p.GetComponent<RectTransform>(), "Title", "내기 바둑", 36, FontStyle.Bold,
                new Vector2(0, 200), new Vector2(600, 50), Color.white);
            CreateText(p.GetComponent<RectTransform>(), "Sub", "구경할 기보를 고르세요", 22, FontStyle.Normal,
                new Vector2(0, 150), new Vector2(600, 36), new Color(0.8f, 0.8f, 0.8f));

            var listGO = new GameObject("KifuList", typeof(RectTransform));
            listGO.transform.SetParent(p.transform, false);
            var lrt = listGO.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, -10);
            lrt.sizeDelta        = new Vector2(600, 280);

            var back = CreateButton(p.GetComponent<RectTransform>(), "로비로", 22,
                new Vector2(0, -230), new Vector2(300, 50));
            back.onClick.AddListener(() => OnBack?.Invoke());
            return p;
        }

        void BuildKifuButtons(List<Kifu> kifus)
        {
            var listTr = _selectPanel.transform.Find("KifuList") as RectTransform;
            if (listTr == null) return;
            for (int i = listTr.childCount - 1; i >= 0; i--) Destroy(listTr.GetChild(i).gameObject);
            if (kifus == null || kifus.Count == 0)
            {
                CreateText(listTr, "Empty", "기보가 없습니다.", 22, FontStyle.Normal,
                    Vector2.zero, new Vector2(600, 40), new Color(1f, 0.6f, 0.6f));
                return;
            }
            for (int i = 0; i < kifus.Count && i < 4; i++)
            {
                var k = kifus[i];
                var btn = CreateButton(listTr, k.title, 22,
                    new Vector2(0, 110 - i * 65), new Vector2(540, 55));
                btn.onClick.AddListener(() => OnKifuSelected?.Invoke(k));
            }
        }

        // ── 베팅 패널 ────────────────────────────────────
        GameObject BuildBetPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "BetPanel", new Color(0.1f, 0.13f, 0.2f, 0.95f));
            FillParent(p);
            var prt = p.GetComponent<RectTransform>();

            CreateText(prt, "BetTitle", "내기:", 28, FontStyle.Bold,
                new Vector2(0, 200), new Vector2(700, 44), Color.white);

            // 색 선택 (흑/백)
            CreateText(prt, "Q1", "누가 이길지 고르세요", 20, FontStyle.Normal,
                new Vector2(0, 150), new Vector2(600, 30), new Color(0.85f, 0.85f, 0.85f));
            var btnBlack = CreateButton(prt, "흑 (Black)", 24, new Vector2(-110, 90), new Vector2(180, 70));
            var btnWhite = CreateButton(prt, "백 (White)", 24, new Vector2( 110, 90), new Vector2(180, 70));
            btnBlack.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
            btnWhite.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f);
            foreach (var t in btnWhite.GetComponentsInChildren<Text>()) t.color = Color.black;
            btnBlack.onClick.AddListener(() => { _selectedChoice = BettingChoice.Black; UpdateBetSummary(); });
            btnWhite.onClick.AddListener(() => { _selectedChoice = BettingChoice.White; UpdateBetSummary(); });

            // 금액 선택
            CreateText(prt, "Q2", "베팅 금액", 20, FontStyle.Normal,
                new Vector2(0, 20), new Vector2(600, 30), new Color(0.85f, 0.85f, 0.85f));
            int[] amounts = { 100, 300, 500, 1000 };
            float bw = 130f, gap = 12f;
            float startX = -((bw + gap) * (amounts.Length - 1)) / 2f;
            for (int i = 0; i < amounts.Length; i++)
            {
                int a = amounts[i];
                var btn = CreateButton(prt, $"{a}", 22,
                    new Vector2(startX + i * (bw + gap), -30), new Vector2(bw, 50));
                btn.onClick.AddListener(() => { _selectedAmount = a; UpdateBetSummary(); });
            }

            // 요약
            _betChoiceLabel    = CreateText(prt, "Sum1", "선택: -", 20, FontStyle.Normal,
                new Vector2(-150, -100), new Vector2(280, 30), Color.white);
            _betAmountLabel    = CreateText(prt, "Sum2", "금액: -", 20, FontStyle.Normal,
                new Vector2( 150, -100), new Vector2(280, 30), Color.white);
            _betPotentialLabel = CreateText(prt, "Sum3", "적중 시 +0", 22, FontStyle.Bold,
                new Vector2(0, -135), new Vector2(500, 32), new Color(1f, 0.85f, 0.3f));

            // 확인/취소
            _btnConfirmBet = CreateButton(prt, "베팅 확정 ▶", 24, new Vector2(-110, -200), new Vector2(220, 60));
            _btnConfirmBet.onClick.AddListener(() =>
            {
                if (_selectedChoice == BettingChoice.None || _selectedAmount <= 0) return;
                OnBetPlaced?.Invoke(_selectedChoice, _selectedAmount);
            });
            var cancel = CreateButton(prt, "취소", 22, new Vector2(110, -200), new Vector2(160, 60));
            cancel.onClick.AddListener(() => OnBack?.Invoke());

            return p;
        }

        void UpdateBetSummary()
        {
            if (_betChoiceLabel != null)
                _betChoiceLabel.text = "선택: " + (_selectedChoice == BettingChoice.None ? "-" :
                    _selectedChoice == BettingChoice.Black ? "흑" : "백");
            if (_betAmountLabel != null)
                _betAmountLabel.text = "금액: " + (_selectedAmount > 0 ? _selectedAmount.ToString() : "-");
            if (_betPotentialLabel != null)
                _betPotentialLabel.text = $"적중 시 +{BettingPayout.CalculatePayout(_selectedAmount)}";
        }

        // ── 재생 패널 ────────────────────────────────────
        GameObject BuildReplayPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "ReplayPanel", new Color(0, 0, 0, 0f));
            FillParent(p);
            var prt = p.GetComponent<RectTransform>();

            var topBar = CreatePanel(prt, "TopBar", new Color(0, 0, 0, 0.82f));
            var tbrt = topBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = Vector2.one;
            tbrt.pivot = new Vector2(0.5f, 1);
            tbrt.offsetMin = new Vector2(0, -120); tbrt.offsetMax = Vector2.zero;

            _titleText    = CreateText(tbrt, "Title", "", 26, FontStyle.Bold,
                new Vector2(0, 15), new Vector2(750, 45), Color.white);
            _playersText  = CreateText(tbrt, "Players", "", 20, FontStyle.Normal,
                new Vector2(0, -28), new Vector2(750, 32), new Color(0.85f, 0.85f, 0.85f));
            _progressText = CreateText(tbrt, "Progress", "", 18, FontStyle.Normal,
                new Vector2(320, -55), new Vector2(150, 28), new Color(0.7f, 0.7f, 0.7f));

            var botBar = CreatePanel(prt, "BotBar", new Color(0, 0, 0, 0.82f));
            var bbrt = botBar.GetComponent<RectTransform>();
            bbrt.anchorMin = Vector2.zero; bbrt.anchorMax = new Vector2(1, 0);
            bbrt.pivot = new Vector2(0.5f, 0);
            bbrt.offsetMin = Vector2.zero; bbrt.offsetMax = new Vector2(0, 170);

            _commentText = CreateText(bbrt, "Comment", "", 22, FontStyle.Italic,
                new Vector2(0, 120), new Vector2(750, 36), new Color(1f, 0.95f, 0.6f));

            float btnY = 60f;
            _btnPlayPause = CreateButton(bbrt, "▶ 재생", 22, new Vector2(-200, btnY), new Vector2(160, 50));
            var s05 = CreateButton(bbrt, "0.5x", 20, new Vector2( -30, btnY), new Vector2(90, 50));
            var s1  = CreateButton(bbrt, "1x",   20, new Vector2(  70, btnY), new Vector2(90, 50));
            var s2  = CreateButton(bbrt, "2x",   20, new Vector2( 170, btnY), new Vector2(90, 50));
            var back = CreateButton(bbrt, "포기하기", 20, new Vector2(0, 0), new Vector2(200, 45));

            _btnPlayPause.onClick.AddListener(() => OnPlayPause?.Invoke());
            s05.onClick.AddListener(() => OnSpeedChanged?.Invoke(0.5f));
            s1.onClick.AddListener (() => OnSpeedChanged?.Invoke(1f));
            s2.onClick.AddListener (() => OnSpeedChanged?.Invoke(2f));
            back.onClick.AddListener(() => OnBack?.Invoke());

            return p;
        }

        // ── 결과 패널 ────────────────────────────────────
        GameObject BuildResultPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "ResultPanel", new Color(0.08f, 0.1f, 0.18f, 0.97f));
            FillParent(p);
            var prt = p.GetComponent<RectTransform>();

            _resultText = CreateText(prt, "Result", "", 56, FontStyle.Bold,
                new Vector2(0, 80), new Vector2(700, 80), Color.white);
            _payoutText = CreateText(prt, "Payout", "", 32, FontStyle.Bold,
                new Vector2(0, -10), new Vector2(700, 50), new Color(1f, 0.85f, 0.3f));

            var again = CreateButton(prt, "한판 더!", 24, new Vector2(-130, -150), new Vector2(220, 60));
            var lobby = CreateButton(prt, "로비로",   24, new Vector2( 130, -150), new Vector2(220, 60));
            again.onClick.AddListener(() => OnRestart?.Invoke());
            lobby.onClick.AddListener(() => OnBack?.Invoke());
            return p;
        }

        // ── 헬퍼 ─────────────────────────────────────────
        static void FillParent(GameObject panel)
        {
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        GameObject CreatePanel(RectTransform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        Text CreateText(Transform parent, string name, string text,
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
