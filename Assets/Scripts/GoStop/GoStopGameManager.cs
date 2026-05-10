// Assets/Scripts/GoStop/GoStopGameManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SilverCare.Common;

namespace SilverCare.GoStop
{
    public class GoStopGameManager : BaseGameManager
    {
        // ── 카드 크기·간격 상수 ──────────────────────────────
        const float CW = 0.38f, CH = 0.54f;   // 카드 가로·세로
        const float SX = 0.42f, SY = 0.62f;   // 그리드 간격
        const float DIST = 2.5f;               // 카메라 ~ 카드 거리

        // ── 12월 색상 팔레트 ─────────────────────────────────
        static readonly Color[] MonthColors =
        {
            new(0.15f,0.50f,0.15f), new(0.85f,0.55f,0.70f), new(0.90f,0.75f,0.80f),
            new(0.55f,0.25f,0.75f), new(0.30f,0.45f,0.80f), new(0.85f,0.25f,0.65f),
            new(0.80f,0.15f,0.15f), new(0.85f,0.50f,0.10f), new(0.60f,0.75f,0.20f),
            new(0.60f,0.10f,0.10f), new(0.30f,0.30f,0.35f), new(0.10f,0.25f,0.60f),
        };

        // ── 게임 데이터 ──────────────────────────────────────
        GoStopDeck            _deck;
        GoStopScoreCalculator _scorer;

        List<HwatooCard> _playerHand     = new();
        List<HwatooCard> _cpuHand        = new();
        List<HwatooCard> _floorCards     = new();
        List<HwatooCard> _deckRemaining  = new();
        List<HwatooCard> _playerCaptured = new();
        List<HwatooCard> _cpuCaptured    = new();
        int  _goCount;
        bool _waitingForCard;
        bool _isProcessing;

        // ── 3D 뷰 ────────────────────────────────────────────
        Transform _cardRoot;
        readonly Dictionary<HwatooCard, GameObject> _views = new();
        readonly Dictionary<HwatooCard, Texture2D>  _texCache = new();

        // ── UI ───────────────────────────────────────────────
        Canvas _gameCanvas;
        Canvas _goStopCanvas;
        Text   _scoreText, _statusText;

        // ── 카메라 기준 벡터 (SpawnCards 후 유지) ────────────
        Vector3    _fwd, _right, _spawnCenter;
        Quaternion _cardRot;

        // ── BaseGameManager ───────────────────────────────────

        protected override void InitGame() { gameTitle = "고스톱"; }

        protected override void StartGame()
        {
            _deck   = GetComponent<GoStopDeck>()   ?? gameObject.AddComponent<GoStopDeck>();
            _scorer = GetComponent<GoStopScoreCalculator>() ?? gameObject.AddComponent<GoStopScoreCalculator>();
            _deck.Initialize();

            SetupSpawnVectors();
            DealCards();
            BuildGameUI();
            RefreshAllCardViews();
            SetStatus("패를 골라 내어 보세요");
            _waitingForCard = true;
        }

        protected override void EndGame() { /* ShowFinalResult handles it */ }

        // ── 입력 ─────────────────────────────────────────────

        void Update()
        {
            if (!_isPlaying || _isProcessing || !_waitingForCard) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 20f)) return;

            var view = hit.collider.GetComponent<HandCardMarker>();
            if (view != null) OnPlayerPickCard(view.Card);
        }

        // ── 패 배분 ──────────────────────────────────────────

        void DealCards()
        {
            // Initialize()는 StartGame()에서 이미 호출됨
            _deck.Shuffle();

            _playerHand.Clear(); _cpuHand.Clear();
            _floorCards.Clear(); _deckRemaining.Clear();
            _playerCaptured.Clear(); _cpuCaptured.Clear();
            _goCount = 0;

            // 플레이어 10장, CPU 10장, 바닥 8장, 나머지 덱
            for (int i = 0; i < 10; i++) _playerHand.Add(_deck.Draw());
            for (int i = 0; i < 10; i++) _cpuHand.Add(_deck.Draw());
            for (int i = 0; i < 8;  i++) _floorCards.Add(_deck.Draw());
            while (_deck.RemainingCount > 0) _deckRemaining.Add(_deck.Draw());
        }

        // ── 플레이어 턴 ──────────────────────────────────────

        void OnPlayerPickCard(HwatooCard card)
        {
            _waitingForCard = false;
            _isProcessing   = true;
            StartCoroutine(PlayerTurnCo(card));
        }

        IEnumerator PlayerTurnCo(HwatooCard card)
        {
            // 1) 패 내기
            SetStatus("패를 냅니다...");
            yield return PlayCardCo(card, _playerHand, _playerCaptured);

            // 2) 덱에서 1장 뒤집기
            yield return DrawCo(_playerCaptured);

            // 3) 점수 확인 → 고/스톱 여부
            int score = _scorer.Calculate(_playerCaptured, _goCount);
            if (score >= 3)
            {
                ShowGoStopPanel(score);
                yield break;  // 버튼 클릭 후 OnGoStopDecision() 재개
            }

            // 4) CPU 턴
            yield return CPUTurnCo();

            // 5) 패 소진 → 게임 종료
            if (_playerHand.Count == 0)
            {
                yield return new WaitForSeconds(0.3f);
                OnGameClear();
                yield break;
            }

            SetStatus("패를 골라 내어 보세요");
            _waitingForCard = true;
            _isProcessing   = false;
        }

        // ── CPU 턴 ───────────────────────────────────────────

        IEnumerator CPUTurnCo()
        {
            SetStatus("상대 차례...");
            yield return new WaitForSeconds(1.2f);

            if (_cpuHand.Count == 0) yield break;

            // 매칭 가능한 패 우선, 없으면 첫 번째 패
            HwatooCard toPlay = _cpuHand.FirstOrDefault(c => _floorCards.Any(f => f.month == c.month))
                             ?? _cpuHand[0];

            yield return PlayCardCo(toPlay, _cpuHand, _cpuCaptured);
            yield return DrawCo(_cpuCaptured);
        }

        // ── 카드 내기 (공용) ──────────────────────────────────
        // 바닥에서 같은 월 찾아 처리, 없으면 바닥에 추가

        IEnumerator PlayCardCo(HwatooCard card, List<HwatooCard> hand, List<HwatooCard> captured)
        {
            hand.Remove(card);
            RemoveView(card);

            var matches = _floorCards.Where(f => f.month == card.month).ToList();
            yield return new WaitForSeconds(0.5f);

            if (matches.Count == 0)
            {
                _floorCards.Add(card);
            }
            else
            {
                captured.Add(card);
                foreach (var m in matches)
                {
                    _floorCards.Remove(m);
                    RemoveView(m);
                    captured.Add(m);
                }
                AudioManager.Instance?.PlayCorrect();
            }

            RefreshAllCardViews();
            RefreshScore();
        }

        // ── 덱에서 1장 뒤집기 (공용) ─────────────────────────

        IEnumerator DrawCo(List<HwatooCard> captured)
        {
            if (_deckRemaining.Count == 0) yield break;

            var drawn = _deckRemaining[0];
            _deckRemaining.RemoveAt(0);
            yield return new WaitForSeconds(0.5f);

            var matches = _floorCards.Where(f => f.month == drawn.month).ToList();

            if (matches.Count == 0)
            {
                _floorCards.Add(drawn);
            }
            else
            {
                captured.Add(drawn);
                foreach (var m in matches)
                {
                    _floorCards.Remove(m);
                    RemoveView(m);
                    captured.Add(m);
                }
                AudioManager.Instance?.PlayCorrect();
            }

            RefreshAllCardViews();
            RefreshScore();
        }

        // ── 고/스톱 결정 ──────────────────────────────────────

        void ShowGoStopPanel(int score)
        {
            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);

            var go = new GameObject("GoStopPanel");
            _goStopCanvas = go.AddComponent<Canvas>();
            _goStopCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = _goStopCanvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(500, 220);
            rt.localScale = Vector3.one * 0.003f;
            rt.position   = _spawnCenter + _fwd * 0.1f + Vector3.up * 0.6f;
            rt.rotation   = Quaternion.LookRotation(_fwd, Vector3.up);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            MakeUIText(go.transform, "msg",
                $"현재 점수: {score}점\n고 하시겠습니까?", 28,
                new Vector2(0, 50), new Vector2(480, 90));

            MakeDiffButton(go.transform, "고!", 30,
                new Vector2(-110, -50), new Vector2(180, 70),
                new Color(0.15f, 0.55f, 0.25f), () => OnGoStopDecision(true));

            MakeDiffButton(go.transform, "스톱!", 30,
                new Vector2(110, -50), new Vector2(180, 70),
                new Color(0.65f, 0.15f, 0.15f), () => OnGoStopDecision(false));
        }

        public void OnGoStopDecision(bool isGo)
        {
            if (_goStopCanvas != null) { Destroy(_goStopCanvas.gameObject); _goStopCanvas = null; }

            if (isGo)
            {
                _goCount++;
                TTSManager.Instance?.Speak("고!");
                StartCoroutine(AfterGoCo());
            }
            else
            {
                TTSManager.Instance?.Speak("스톱!");
                ShowFinalResult();
            }
        }

        IEnumerator AfterGoCo()
        {
            yield return CPUTurnCo();

            if (_playerHand.Count == 0) { ShowFinalResult(); yield break; }

            SetStatus("패를 골라 내어 보세요");
            _waitingForCard = true;
            _isProcessing   = false;
        }

        // ── 결과 표시 ────────────────────────────────────────

        void ShowFinalResult()
        {
            _isPlaying = false;
            int pScore = _scorer.Calculate(_playerCaptured, _goCount);
            int cScore = _scorer.Calculate(_cpuCaptured, 0);
            _score = pScore;

            string breakdown = _scorer.ScoreBreakdown(_playerCaptured, _goCount);
            string result = pScore > cScore ? "승리!" : pScore == cScore ? "무승부" : "패배";

            if (_scoreText  != null) _scoreText.gameObject.SetActive(false);
            if (_statusText != null) _statusText.gameObject.SetActive(false);

            var rt = _gameCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, 240);

            MakeUIText(_gameCanvas.transform, "result",
                $"게임 {result}\n{breakdown}\n상대 점수: {cScore}점", 26,
                new Vector2(0, 60), new Vector2(800, 110)).color = new Color(1f, 0.9f, 0.3f);

            MakeDiffButton(_gameCanvas.transform, "다시 하기", 26,
                new Vector2(-200, -60), new Vector2(240, 60),
                new Color(0.22f, 0.22f, 0.38f), HandleRestart);
            MakeDiffButton(_gameCanvas.transform, "로비로", 26,
                new Vector2(60, -60), new Vector2(240, 60),
                new Color(0.22f, 0.22f, 0.38f), HandleGoLobby);
        }

        void HandleRestart()
        {
            if (_cardRoot     != null) { Destroy(_cardRoot.gameObject);     _cardRoot     = null; }
            if (_gameCanvas   != null) { Destroy(_gameCanvas.gameObject);   _gameCanvas   = null; }
            if (_goStopCanvas != null) { Destroy(_goStopCanvas.gameObject); _goStopCanvas = null; }
            _views.Clear(); _texCache.Clear();
            _isProcessing = false; _waitingForCard = false;
            StartGame();
        }

        void HandleGoLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(lobbySceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        // ── 3D 카드 뷰 생성/갱신 ────────────────────────────

        void SetupSpawnVectors()
        {
            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.up * 1.6f;
            _fwd   = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (_fwd.sqrMagnitude < 0.01f) _fwd = Vector3.forward;
            _right      = Vector3.Cross(Vector3.up, _fwd).normalized;
            _cardRot    = Quaternion.LookRotation(-_fwd, Vector3.up);
            _spawnCenter = camPos + _fwd * DIST;
            _spawnCenter.y = camPos.y;
        }

        void RefreshAllCardViews()
        {
            // 뷰가 없는 카드에 새 뷰 생성, 위치 재계산
            if (_cardRoot == null) _cardRoot = new GameObject("GoStopCards").transform;

            // 플레이어 패 (하단, 앞면, 클릭 가능)
            PlaceRow(_playerHand, _spawnCenter + Vector3.up * -1.0f, true);

            // 바닥 패 (중앙, 앞면)
            PlaceGrid(_floorCards, _spawnCenter + Vector3.up * 0.15f);

            // CPU 패 (상단, 뒷면)
            PlaceRow(_cpuHand, _spawnCenter + Vector3.up * 1.3f, false);
        }

        void PlaceRow(List<HwatooCard> cards, Vector3 rowCenter, bool faceUp)
        {
            if (cards.Count == 0) return;
            float totalW = (cards.Count - 1) * SX;
            for (int i = 0; i < cards.Count; i++)
            {
                float x  = i * SX - totalW / 2f;
                Vector3 pos = rowCenter + _right * x;
                EnsureView(cards[i], pos, faceUp, isHandCard: faceUp);
            }
        }

        void PlaceGrid(List<HwatooCard> cards, Vector3 gridCenter)
        {
            if (cards.Count == 0) return;
            int cols   = Mathf.Min(cards.Count, 5);
            int rows   = Mathf.CeilToInt((float)cards.Count / cols);
            float totW = (cols - 1) * SX;
            float totH = (rows - 1) * SY;

            for (int i = 0; i < cards.Count; i++)
            {
                int col = i % cols, row = i / cols;
                float x = col * SX - totW / 2f;
                float y = row * SY - totH / 2f;
                Vector3 pos = gridCenter + _right * x + Vector3.up * y;
                EnsureView(cards[i], pos, faceUp: true, isHandCard: false);
            }
        }

        void EnsureView(HwatooCard card, Vector3 pos, bool faceUp, bool isHandCard)
        {
            if (_views.TryGetValue(card, out var go))
            {
                go.transform.position = pos;
                return;
            }

            var root = new GameObject($"GSCard_{card.cardName}");
            root.transform.SetPositionAndRotation(pos, _cardRot);
            root.transform.SetParent(_cardRoot);

            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.transform.SetParent(root.transform, false);
            face.transform.localScale    = new Vector3(-CW, CH, 1f);
            face.transform.localPosition = new Vector3(0, 0, 0.01f);
            Destroy(face.GetComponent<MeshCollider>());

            if (faceUp)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = GetCardTex(card);
                mat.color       = Color.white;
                face.GetComponent<Renderer>().material = mat;
            }
            else
            {
                face.GetComponent<Renderer>().material = MakeColorMat(new Color(0.18f, 0.38f, 0.72f));
            }

            if (isHandCard)
            {
                root.AddComponent<BoxCollider>().size = new Vector3(CW + 0.04f, CH + 0.04f, 0.05f);
                root.AddComponent<HandCardMarker>().Card = card;
            }

            _views[card] = root;
        }

        void RemoveView(HwatooCard card)
        {
            if (_views.TryGetValue(card, out var go))
            {
                Destroy(go);
                _views.Remove(card);
            }
        }

        // ── 텍스처 로드: Resources/GoStop/card_XX 우선, 없으면 폴백 ─

        Texture2D GetCardTex(HwatooCard card)
        {
            if (_texCache.TryGetValue(card, out var tex)) return tex;

            // Resources/GoStop/card_00 ~ card_47 PNG 로드 시도
            string resPath = $"GoStop/card_{card.index:D2}";
            var sprite = Resources.Load<Sprite>(resPath);
            if (sprite != null)
            {
                tex = sprite.texture;
                _texCache[card] = tex;
                return tex;
            }

            // 폴백: 월별 색상 + 번호 텍스처
            var t = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Color bg = MonthColors[card.month - 1];
            var px = new Color[128 * 128];
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            t.SetPixels(px);

            DrawRect(t, 3, 3, 122, 122, Color.white);
            DrawRect(t, 6, 6, 116, 116, bg);
            DrawNumber(t, card.month, 20, 65, Color.white);

            string sym = card.cardType switch
            {
                CardType.Gwang => "光",
                CardType.Yul   => "10",
                CardType.Tti   => "띠",
                _              => "피",
            };
            DrawLabel(t, sym, 68, 10, new Color(1f, 1f, 0.6f));

            t.Apply();
            _texCache[card] = t;
            return t;
        }

        // ── UI 구성 ──────────────────────────────────────────

        void BuildGameUI()
        {
            if (_gameCanvas != null) Destroy(_gameCanvas.gameObject);

            var go = new GameObject("GoStopUI");
            _gameCanvas = go.AddComponent<Canvas>();
            _gameCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = _gameCanvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(820, 110);
            rt.localScale = Vector3.one * 0.003f;
            rt.position   = _spawnCenter + Vector3.up * 2.2f;
            rt.rotation   = Quaternion.LookRotation(_fwd, Vector3.up);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            _scoreText  = MakeUIText(go.transform, "Score",
                "광:0 열끗:0 띠:0 피:0  → 0점", 24,
                new Vector2(-75, 24), new Vector2(580, 38));
            _statusText = MakeUIText(go.transform, "Status", "", 20,
                new Vector2(-75, -22), new Vector2(580, 34));
            _statusText.color = new Color(0.9f, 0.9f, 0.65f);

            MakeDiffButton(go.transform, "나가기", 20,
                new Vector2(340, 0), new Vector2(100, 90),
                new Color(0.55f, 0.12f, 0.12f), HandleGoLobby);
        }

        void RefreshScore()
        {
            if (_scoreText == null) return;
            int g = _playerCaptured.Count(c => c.cardType == CardType.Gwang);
            int y = _playerCaptured.Count(c => c.cardType == CardType.Yul);
            int ti = _playerCaptured.Count(c => c.cardType == CardType.Tti);
            int p = _playerCaptured.Count(c => c.cardType == CardType.Pi);
            int score = _scorer.Calculate(_playerCaptured, _goCount);
            _scoreText.text = $"광:{g} 열끗:{y} 띠:{ti} 피:{p}  → {score}점  (덱:{_deckRemaining.Count}장)";
            _score = score;
        }

        void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        // ── UI 헬퍼 ──────────────────────────────────────────

        static Text MakeUIText(Transform parent, string name, string content, int size,
                               Vector2 pos, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = sd;
            var t = go.GetComponent<Text>();
            t.text = content; t.fontSize = size; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Truncate;
            return t;
        }

        static void MakeDiffButton(Transform parent, string label, int fontSize,
                                    Vector2 pos, Vector2 size, Color color,
                                    System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var textGO = new GameObject("L", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = textGO.GetComponent<Text>();
            t.text = label; t.fontSize = fontSize; t.color = Color.white;
            t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        static Material MakeColorMat(Color c)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = c;
            return mat;
        }

        // ── 픽셀 드로잉 (폴백 텍스처) ────────────────────────

        static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color col)
        {
            int x1 = Mathf.Max(0, x), x2 = Mathf.Min(tex.width, x + w);
            int y1 = Mathf.Max(0, y), y2 = Mathf.Min(tex.height, y + h);
            for (int yy = y1; yy < y2; yy++)
                for (int xx = x1; xx < x2; xx++)
                    tex.SetPixel(xx, yy, col);
        }

        // 월 숫자 (큼직하게)
        static void DrawNumber(Texture2D tex, int n, int x, int y, Color col)
        {
            bool[][] seg = {
                new[]{true, true, true, false,true, true, true},
                new[]{false,false,true, false,false,true, false},
                new[]{true, false,true, true, true, false,true},
                new[]{true, false,true, true, false,true, true},
                new[]{false,true, true, true, false,true, false},
                new[]{true, true, false,true, false,true, true},
                new[]{true, true, false,true, true, true, true},
                new[]{true, false,true, false,false,true, false},
                new[]{true, true, true, true, true, true, true},
                new[]{true, true, true, true, false,true, true},
            };
            string s = n.ToString();
            int dw = 22, dh = 40, th = 5, gap = 6;
            int totalW = s.Length * dw + (s.Length - 1) * gap;
            int sx = x - totalW / 2;
            for (int i = 0; i < s.Length; i++)
            {
                int d = s[i] - '0';
                int ox = sx + i * (dw + gap);
                int midY = y + dh / 2 - th / 2, topY = y + dh - th;
                int rx = ox + dw - th, midLY = y + dh / 2;
                bool[] sg = seg[d];
                if (sg[0]) DrawRect(tex, ox,  topY,  dw, th,         col);
                if (sg[1]) DrawRect(tex, ox,  midLY, th, dh / 2 - th,col);
                if (sg[2]) DrawRect(tex, rx,  midLY, th, dh / 2 - th,col);
                if (sg[3]) DrawRect(tex, ox,  midY,  dw, th,         col);
                if (sg[4]) DrawRect(tex, ox,  y + th,th, dh / 2 - th,col);
                if (sg[5]) DrawRect(tex, rx,  y + th,th, dh / 2 - th,col);
                if (sg[6]) DrawRect(tex, ox,  y,     dw, th,         col);
            }
        }

        // 타입 레이블 (우하단 작은 글씨 — 흰 사각형 배경 위)
        static void DrawLabel(Texture2D tex, string s, int x, int y, Color col)
        {
            DrawRect(tex, x - 2, y - 2, 32, 20, new Color(0, 0, 0, 0.5f));
            // 최소 픽셀 표시 (2×3 dot per char)
            for (int i = 0; i < Mathf.Min(s.Length, 2); i++)
                DrawRect(tex, x + i * 9, y + 3, 7, 12, col);
        }
    }

    // 플레이어 패 카드임을 표시하는 마커
    public class HandCardMarker : MonoBehaviour
    {
        public HwatooCard Card;
    }
}
