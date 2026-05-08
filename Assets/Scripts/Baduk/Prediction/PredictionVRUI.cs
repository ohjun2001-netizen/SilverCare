// Assets/Scripts/Baduk/Prediction/PredictionVRUI.cs
// 다음 수 맞히기 모드 UI - 기보 선택 / 재생 / 예측 오버레이 / 최종 결과
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Baduk.Data;

namespace Baduk.Prediction
{
    public class PredictionVRUI : MonoBehaviour
    {
        // ── 외부 콜백 ────────────────────────────────────
        public System.Action<Kifu>  OnKifuSelected;
        public System.Action        OnPlayPause;
        public System.Action<float> OnSpeedChanged;
        public System.Action<int>   OnPredictionSubmit;   // chosen candidate index
        public System.Action        OnRestart;
        public System.Action        OnBack;

        Canvas _canvas;
        GameObject _selectPanel, _replayPanel, _predictPanel, _resultPanel;
        Text _coinsText;

        // 재생 패널
        Text _titleText, _playersText, _progressText, _commentText;
        Button _btnPlayPause;

        // 예측 오버레이
        Text _predictQuestion, _predictResultLabel, _predictExplain;
        Transform _predictCandidatesContainer;
        readonly List<Button> _candidateButtons = new();

        // 결과 패널
        Text _resultHeadline, _resultDetail;

        bool _built;

        // ── 외부 호출 ────────────────────────────────────

        public void ShowKifuSelect(List<Kifu> kifus, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_selectPanel);
            BuildKifuButtons(kifus);
        }

        public void ShowReplay(Kifu kifu, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_replayPanel);
            _titleText.text   = kifu.title ?? "기보";
            _playersText.text = $"흑: {kifu.black_player ?? "?"}    백: {kifu.white_player ?? "?"}";
            _commentText.text = kifu.description ?? "";
            UpdateProgress(0, kifu.moves?.Count ?? 0);
            UpdatePlayPauseLabel(false);
        }

        public void ShowPredictionOverlay(PredictionPoint point)
        {
            EnsureBuilt();
            _predictPanel.SetActive(true);
            _predictResultLabel.text = "";
            _predictExplain.text = "";
            _predictQuestion.text = string.IsNullOrEmpty(point?.question)
                ? "다음 수는 어디일까요?"
                : point.question;

            BuildCandidateButtons(point);
        }

        public void ShowPredictionResult(bool correct, PredictionPoint point, int chosenIndex, int reward)
        {
            if (correct)
            {
                _predictResultLabel.text = $"정답!  +{reward} 코인";
                _predictResultLabel.color = new Color(0.3f, 1f, 0.4f);
            }
            else
            {
                _predictResultLabel.text = "아쉽네요";
                _predictResultLabel.color = new Color(1f, 0.5f, 0.5f);
            }
            _predictExplain.text = point?.explanation ?? "";

            // 정답 버튼은 초록, 사용자가 고른 오답 버튼은 빨강
            for (int i = 0; i < _candidateButtons.Count; i++)
            {
                var img = _candidateButtons[i].GetComponent<Image>();
                if (i == point.correct_index)
                    img.color = new Color(0.25f, 0.6f, 0.3f);
                else if (i == chosenIndex && !correct)
                    img.color = new Color(0.6f, 0.25f, 0.25f);
                _candidateButtons[i].interactable = false;
            }
        }

        public void HidePredictionOverlay()
        {
            if (_predictPanel != null) _predictPanel.SetActive(false);
        }

        public void ShowResult(int correct, int total, int coinsEarned, int currentCoins)
        {
            EnsureBuilt(); PlaceCanvas();
            UpdateCoins(currentCoins);
            ShowOnly(_resultPanel);
            _resultHeadline.text = total == 0
                ? "관전 완료!"
                : $"{correct} / {total} 적중";
            _resultDetail.text = coinsEarned > 0
                ? $"획득 코인: +{coinsEarned}"
                : "획득 코인 없음 — 다시 도전해 보세요";
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

            Vector3 flatFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (flatFwd == Vector3.zero) flatFwd = Vector3.forward;

            var rt = _canvas.GetComponent<RectTransform>();
            // 수평 forward, 카메라 높이 유지 — 씬 카메라가 낮아도 음수 Y(지하)로 떨어지지 않음
            rt.position = cam.transform.position + flatFwd * 2.0f;
            rt.rotation = Quaternion.LookRotation(flatFwd, Vector3.up);
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("PredictionVRCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(800, 540);
            rt.localScale = Vector3.one * 0.002f;

            _coinsText = CreateText(rt, "Coins", "코인: 0", 22, FontStyle.Bold,
                new Vector2(280, 240), new Vector2(220, 36), new Color(1f, 0.85f, 0.3f));

            _selectPanel  = BuildSelectPanel(rt);
            _replayPanel  = BuildReplayPanel(rt);
            _predictPanel = BuildPredictionOverlay(rt);
            _resultPanel  = BuildResultPanel(rt);

            ShowOnly(_selectPanel);
            _predictPanel.SetActive(false);
        }

        void ShowOnly(GameObject panel)
        {
            _selectPanel.SetActive(panel == _selectPanel);
            _replayPanel.SetActive(panel == _replayPanel);
            _resultPanel.SetActive(panel == _resultPanel);
            _predictPanel.SetActive(false);
            _coinsText.gameObject.SetActive(true);
        }

        // ── 기보 선택 ────────────────────────────────────
        GameObject BuildSelectPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "SelectPanel", new Color(0.1f, 0.1f, 0.18f, 0.95f));
            FillParent(p);
            CreateText(p.GetComponent<RectTransform>(), "Title", "다음 수 맞히기", 40, FontStyle.Bold,
                new Vector2(0, 220), new Vector2(700, 60), Color.white);
            CreateText(p.GetComponent<RectTransform>(), "Sub", "관전할 명승부를 고르세요", 24, FontStyle.Normal,
                new Vector2(0, 170), new Vector2(700, 40), new Color(0.85f, 0.85f, 0.85f));

            var listGO = new GameObject("KifuList", typeof(RectTransform));
            listGO.transform.SetParent(p.transform, false);
            var lrt = listGO.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, -10);
            lrt.sizeDelta        = new Vector2(700, 240);

            var back = CreateButton(p.GetComponent<RectTransform>(), "로비로 돌아가기", 26,
                new Vector2(0, -230), new Vector2(380, 60));
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
                CreateText(listTr, "Empty", "기보가 없습니다.", 24, FontStyle.Normal,
                    Vector2.zero, new Vector2(640, 40), new Color(1f, 0.6f, 0.6f));
                return;
            }
            for (int i = 0; i < kifus.Count && i < 4; i++)
            {
                var k = kifus[i];
                int predCount = k.prediction_points?.Count ?? 0;
                string label = predCount > 0 ? $"{k.title}  (예측 {predCount})" : k.title;
                var btn = CreateButton(listTr, label, 26,
                    new Vector2(0, 90 - i * 70), new Vector2(640, 60));
                btn.onClick.AddListener(() => OnKifuSelected?.Invoke(k));
            }
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
            var back = CreateButton(bbrt, "그만 보기", 20, new Vector2(0, 0), new Vector2(200, 45));

            _btnPlayPause.onClick.AddListener(() => OnPlayPause?.Invoke());
            s05.onClick.AddListener(() => OnSpeedChanged?.Invoke(0.5f));
            s1.onClick.AddListener (() => OnSpeedChanged?.Invoke(1f));
            s2.onClick.AddListener (() => OnSpeedChanged?.Invoke(2f));
            back.onClick.AddListener(() => OnBack?.Invoke());

            return p;
        }

        // ── 예측 오버레이 ────────────────────────────────
        GameObject BuildPredictionOverlay(RectTransform parent)
        {
            var p = CreatePanel(parent, "PredictPanel", new Color(0.05f, 0.07f, 0.12f, 0.96f));
            FillParent(p);
            var prt = p.GetComponent<RectTransform>();

            _predictQuestion = CreateText(prt, "Q", "", 30, FontStyle.Bold,
                new Vector2(0, 200), new Vector2(720, 60), Color.white);

            var listGO = new GameObject("Candidates", typeof(RectTransform));
            listGO.transform.SetParent(prt, false);
            var lrt = listGO.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, 50);
            lrt.sizeDelta        = new Vector2(720, 220);
            _predictCandidatesContainer = lrt;

            _predictResultLabel = CreateText(prt, "ResultLabel", "", 32, FontStyle.Bold,
                new Vector2(0, -100), new Vector2(720, 44), Color.white);
            _predictExplain = CreateText(prt, "Explain", "", 20, FontStyle.Italic,
                new Vector2(0, -160), new Vector2(720, 80), new Color(0.95f, 0.95f, 0.7f));

            return p;
        }

        void BuildCandidateButtons(PredictionPoint point)
        {
            _candidateButtons.Clear();
            for (int i = _predictCandidatesContainer.childCount - 1; i >= 0; i--)
                Destroy(_predictCandidatesContainer.GetChild(i).gameObject);

            if (point?.candidates == null || point.candidates.Count == 0) return;

            int n = point.candidates.Count;
            float bw = 160f, gap = 18f;
            float startX = -((bw + gap) * (n - 1)) / 2f;

            for (int i = 0; i < n; i++)
            {
                var c = point.candidates[i];
                int idx = i;
                string label = $"{(char)('A' + i)}\n{FormatCoord(c.row, c.col)}";
                var btn = CreateButton(_predictCandidatesContainer, label, 22,
                    new Vector2(startX + i * (bw + gap), 0), new Vector2(bw, 110));
                btn.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f);
                btn.onClick.AddListener(() => OnPredictionSubmit?.Invoke(idx));
                _candidateButtons.Add(btn);
            }
        }

        // 좌표 표시: 시니어 친화적인 한국어 표기 (행/열, 1-based)
        static string FormatCoord(int row, int col)
            => $"{row + 1}행 {col + 1}열";

        // ── 결과 패널 ────────────────────────────────────
        GameObject BuildResultPanel(RectTransform parent)
        {
            var p = CreatePanel(parent, "ResultPanel", new Color(0.08f, 0.1f, 0.18f, 0.97f));
            FillParent(p);
            var prt = p.GetComponent<RectTransform>();

            _resultHeadline = CreateText(prt, "Result", "", 52, FontStyle.Bold,
                new Vector2(0, 80), new Vector2(700, 80), Color.white);
            _resultDetail   = CreateText(prt, "Detail", "", 28, FontStyle.Bold,
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
