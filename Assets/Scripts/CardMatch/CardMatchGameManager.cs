// Assets/Scripts/CardMatch/CardMatchGameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SilverCare.Common;

namespace SilverCare.CardMatch
{
    public class CardMatchGameManager : BaseGameManager
    {
        [Header("카드 설정")]
        [SerializeField] Texture[] cardFrontTextures;
        [SerializeField] int   columnCount  = 4;
        [SerializeField] float spacingX     = 1.05f;
        [SerializeField] float spacingY     = 1.45f;
        [SerializeField] float spawnDist    = 2.2f;

        // 난이도별 쌍 수
        static readonly int[] DiffPairs = { 4, 6, 8 };
        static readonly string[] DiffLabels = { "쉬움  (4쌍·8장)", "보통  (6쌍·12장)", "어려움  (8쌍·16장)" };

        int _activePairCount;

        readonly List<CardController> _cards = new();
        CardController _firstFlipped;
        int  _matchedPairs;
        bool _isChecking;
        Transform _cardRoot;

        Canvas _diffCanvas;
        Canvas _gameCanvas;
        Text   _scoreText, _statusText;

        // ── BaseGameManager 구현 ─────────────────────────────

        protected override void InitGame()
        {
            gameTitle = "카드 짝맞추기";
        }

        protected override void StartGame()
        {
            ShowDifficultyPanel();
        }

        protected override void EndGame()
        {
            ShowResultOverlay();
        }

        // ── 입력 — 카메라 레이캐스트 (PC/VR 공용) ───────────

        void Update()
        {
            if (!_isPlaying || _isChecking) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                var card = hit.collider.GetComponent<CardController>();
                if (card != null) HandleCardClick(card);
            }
        }

        // ── 난이도 선택 패널 ─────────────────────────────────

        void ShowDifficultyPanel()
        {
            _isPlaying = false;

            if (_diffCanvas != null) Destroy(_diffCanvas.gameObject);

            var go = new GameObject("DifficultyPanel");
            _diffCanvas = go.AddComponent<Canvas>();
            _diffCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = _diffCanvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(600, 400);
            rt.localScale = Vector3.one * 0.003f;

            PlaceCanvasInFront(_diffCanvas, 0f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            MakeDiffText(_diffCanvas.transform, "제목", "카드 짝맞추기", 36,
                new Vector2(0, 145), new Vector2(560, 52), Color.white);
            MakeDiffText(_diffCanvas.transform, "서브", "난이도를 선택하세요", 22,
                new Vector2(0, 95), new Vector2(560, 36), new Color(0.8f, 0.8f, 0.8f));

            Color[] btnColors =
            {
                new Color(0.15f, 0.55f, 0.25f),
                new Color(0.70f, 0.50f, 0.05f),
                new Color(0.65f, 0.15f, 0.15f),
            };
            float[] yPos = { 25f, -50f, -125f };
            for (int i = 0; i < DiffLabels.Length; i++)
            {
                int idx = i;
                MakeDiffButton(_diffCanvas.transform, DiffLabels[i], 24,
                    new Vector2(0, yPos[i]), new Vector2(480, 58), btnColors[i],
                    () => OnDifficultySelected(DiffPairs[idx]));
            }

            MakeDiffButton(_diffCanvas.transform, "로비로 돌아가기", 20,
                new Vector2(0, -195), new Vector2(280, 46), new Color(0.3f, 0.3f, 0.3f),
                HandleGoLobby);
        }

        void OnDifficultySelected(int pairs)
        {
            _activePairCount = pairs;

            if (_diffCanvas != null) { Destroy(_diffCanvas.gameObject); _diffCanvas = null; }

            if (cardFrontTextures == null || cardFrontTextures.Length < _activePairCount)
                cardFrontTextures = MakeFallbackTextures(_activePairCount);

            _matchedPairs = 0;
            _score        = 0;
            _firstFlipped = null;
            _isChecking   = false;
            _isPlaying    = true;

            BuildGameUI();
            StartCoroutine(SpawnAfterDelay());
        }

        // ── 카드 짝맞추기 로직 ───────────────────────────────

        void HandleCardClick(CardController card)
        {
            if (card.IsMatched || card.IsFaceUp) return;
            card.FlipUp();

            if (_firstFlipped == null)
            {
                _firstFlipped = card;
                SetStatus("두 번째 카드를 고르세요");
            }
            else
            {
                StartCoroutine(CheckMatch(card));
            }
        }

        IEnumerator CheckMatch(CardController second)
        {
            _isChecking = true;
            SetStatus("확인 중...");
            yield return new WaitForSeconds(1.0f);

            if (_firstFlipped.CardId == second.CardId)
            {
                _firstFlipped.SetMatched();
                second.SetMatched();
                _matchedPairs++;
                _score += 100;
                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.Speak("짝이 맞았습니다!");
                RefreshScore();
                if (_matchedPairs >= _activePairCount)
                    OnGameClear();
                else
                    SetStatus("맞았습니다! 카드를 골라보세요");
            }
            else
            {
                _firstFlipped.FlipDown();
                second.FlipDown();
                _score = Mathf.Max(0, _score - 10);
                AudioManager.Instance?.PlayWrong();
                RefreshScore();
                SetStatus("아쉽네요. 다시 고르세요");
            }

            _firstFlipped = null;
            _isChecking   = false;
        }

        // ── 카드 스폰 ────────────────────────────────────────

        IEnumerator SpawnAfterDelay()
        {
            yield return null;
            yield return null;
            SpawnCards();
            SetStatus("카드를 골라보세요");
            RefreshScore();
        }

        void SpawnCards()
        {
            if (_cardRoot != null) Destroy(_cardRoot.gameObject);
            _cards.Clear();
            _cardRoot = new GameObject("CardGrid").transform;

            Camera cam     = Camera.main ?? FindObjectOfType<Camera>();
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.up * 1.6f;
            Vector3 forward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

            Quaternion baseRot = Quaternion.LookRotation(-forward, Vector3.up);
            Vector3    right   = Vector3.Cross(Vector3.up, forward).normalized;

            int total = _activePairCount * 2;

            var data = new List<(int id, Texture tex)>();
            for (int i = 0; i < _activePairCount; i++)
            {
                data.Add((i, cardFrontTextures[i]));
                data.Add((i, cardFrontTextures[i]));
            }
            Shuffle(data);

            int   cols = columnCount;
            int   rows = Mathf.CeilToInt((float)total / cols);
            float totW = (cols - 1) * spacingX;

            // 행 수에 따라 세로 간격 동적 조정 — 전체 높이를 2.2m 이내로 제한
            const float maxTotH = 2.2f;
            float effSpacingY = rows > 1 ? Mathf.Min(spacingY, maxTotH / (rows - 1)) : spacingY;
            float cardH       = Mathf.Min(1.2f, effSpacingY * 0.88f);
            float totH        = (rows - 1) * effSpacingY;

            // 그리드 중심 = 카메라 눈높이 (항상 고정)
            Vector3 center = camPos + forward * spawnDist;
            center.y = Mathf.Clamp(camPos.y, 0.8f, 2.4f);

            for (int i = 0; i < total; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = col * spacingX - totW / 2f;
                float y = row * effSpacingY - totH / 2f;
                Vector3 pos = center + right * x + Vector3.up * y;

                var card = CreateCard(pos, baseRot, cardH);
                card.name = $"Card_{data[i].id}_{i}";
                card.Init(data[i].id, data[i].tex);
                _cards.Add(card);
            }

            Debug.Log($"[CardMatch] rows:{rows} effSpY:{effSpacingY:F2} cardH:{cardH:F2} center:{center:F1}");
            // 카드 상단 끝(totH/2 + cardH/2) 위로 0.3m 띄워서 UI 배치
            _gameCanvas.transform.position = center + Vector3.up * (totH / 2f + cardH / 2f + 0.3f);
            _gameCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        CardController CreateCard(Vector3 pos, Quaternion rot, float cardH = 1.2f)
        {
            var root = new GameObject("Card");
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.SetParent(_cardRoot);

            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "FrontFace";
            front.transform.SetParent(root.transform, false);
            front.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            front.transform.localScale    = new Vector3(-0.9f, cardH, 1f);
            Destroy(front.GetComponent<MeshCollider>());

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "BackFace";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            back.transform.localScale    = new Vector3(0.9f, cardH, 1f);
            Destroy(back.GetComponent<MeshCollider>());
            back.GetComponent<Renderer>().material = MakeColorMat(new Color(0.18f, 0.38f, 0.72f));

            root.AddComponent<BoxCollider>().size = new Vector3(0.95f, cardH + 0.05f, 0.1f);

            var cc = root.AddComponent<CardController>();
            cc.SetFaces(front, back);
            return cc;
        }

        static Material MakeColorMat(Color c)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = c;
            return mat;
        }

        // ── 게임 UI ─────────────────────────────────────────

        void BuildGameUI()
        {
            if (_gameCanvas != null) Destroy(_gameCanvas.gameObject);

            var go = new GameObject("CardMatchUI");
            _gameCanvas = go.AddComponent<Canvas>();
            _gameCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = _gameCanvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(820, 120);
            rt.localScale = Vector3.one * 0.003f;
            go.transform.position = new Vector3(0f, -100f, 0f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            _scoreText  = MakeText(_gameCanvas.transform, "Score",
                $"점수: 0  |  0 / {_activePairCount} 쌍", 28,
                new Vector2(-75, 26), new Vector2(580, 44));
            _statusText = MakeText(_gameCanvas.transform, "Status", "", 22,
                new Vector2(-75, -22), new Vector2(580, 36));
            _statusText.color = new Color(0.9f, 0.9f, 0.65f);

            MakeDiffButton(_gameCanvas.transform, "나가기", 22,
                new Vector2(340, 0), new Vector2(110, 100),
                new Color(0.55f, 0.12f, 0.12f), HandleGoLobby);
        }

        void ShowResultOverlay()
        {
            if (_scoreText  != null) _scoreText.gameObject.SetActive(false);
            if (_statusText != null) _statusText.gameObject.SetActive(false);

            _gameCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 200);

            MakeText(_gameCanvas.transform, "Clear",
                $"게임 클리어!  점수: {_score}", 38,
                new Vector2(0, 55), new Vector2(800, 60)).color = new Color(1f, 0.9f, 0.3f);

            MakeButton(_gameCanvas.transform, "다시 하기",
                new Vector2(-200, -30), new Vector2(250, 60), HandleRestart);
            MakeButton(_gameCanvas.transform, "로비로",
                new Vector2(  60, -30), new Vector2(250, 60), HandleGoLobby);
        }

        void RefreshScore()
        {
            if (_scoreText != null)
                _scoreText.text = $"점수: {_score}  |  {_matchedPairs} / {_activePairCount} 쌍";
        }

        void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        void HandleRestart()
        {
            if (_cardRoot != null) { Destroy(_cardRoot.gameObject); _cardRoot = null; }
            if (_gameCanvas != null) { Destroy(_gameCanvas.gameObject); _gameCanvas = null; }
            _cards.Clear();
            ShowDifficultyPanel();
        }

        void HandleGoLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(lobbySceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        // ── 캔버스 배치 ──────────────────────────────────────

        void PlaceCanvasInFront(Canvas canvas, float yOffset)
        {
            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            if (cam == null) return;

            Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

            var rt = canvas.GetComponent<RectTransform>();
            rt.position = cam.transform.position + fwd * 2f + Vector3.up * (0.1f + yOffset);
            rt.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ── 유틸 ─────────────────────────────────────────────

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── UI 헬퍼 ──────────────────────────────────────────

        Text MakeText(Transform parent, string name, string content, int size,
                      Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = sizeDelta;
            var t = go.GetComponent<Text>();
            t.text               = content;
            t.fontSize           = size;
            t.color              = Color.white;
            t.alignment          = TextAnchor.MiddleCenter;
            t.font               = Font.CreateDynamicFontFromOSFont("Arial", size);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Truncate;
            return t;
        }

        static Text MakeDiffText(Transform parent, string name, string content, int size,
                                  Vector2 pos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = sizeDelta;
            var t = go.GetComponent<Text>();
            t.text               = content;
            t.fontSize           = size;
            t.color              = color;
            t.alignment          = TextAnchor.MiddleCenter;
            t.font               = Font.CreateDynamicFontFromOSFont("Arial", size);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        static void MakeDiffButton(Transform parent, string label, int fontSize,
                                    Vector2 pos, Vector2 size, Color color,
                                    System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.GetComponent<Image>().color = color;

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = textGO.GetComponent<Text>();
            t.text      = label;
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.font      = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
                        System.Action onClick)
        {
            MakeDiffButton(parent, label, 26, pos, size, new Color(0.22f, 0.22f, 0.38f), onClick);
        }

        // ── 폴백 텍스처 ──────────────────────────────────────

        static Texture2D[] MakeFallbackTextures(int count)
        {
            Color[] palette = {
                new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.35f, 0.85f),
                new Color(0.15f, 0.65f, 0.22f), new Color(0.85f, 0.60f, 0.10f),
                new Color(0.70f, 0.15f, 0.75f), new Color(0.10f, 0.70f, 0.75f),
                new Color(0.85f, 0.85f, 0.10f), new Color(0.50f, 0.32f, 0.18f),
            };
            var result = new Texture2D[count];
            for (int i = 0; i < count; i++)
            {
                var tex    = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color c    = palette[i % palette.Length];
                var pixels = new Color[128 * 128];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = c;
                tex.SetPixels(pixels);
                DrawCardNumber(tex, i + 1);
                tex.Apply();
                result[i] = tex;
            }
            return result;
        }

        static void DrawCardNumber(Texture2D tex, int number)
        {
            DrawRect(tex, 6, 6, tex.width - 12, tex.height - 12, Color.white);
            DrawRect(tex, 10, 10, tex.width - 20, tex.height - 20,
                     new Color(0.15f, 0.15f, 0.15f, 0.35f));

            string digits   = number.ToString();
            int dw = 28, dh = 52, th = 6, gap = 10;
            int totalW = digits.Length * dw + (digits.Length - 1) * gap;
            int sx = (tex.width  - totalW) / 2;
            int sy = (tex.height - dh)     / 2;
            for (int i = 0; i < digits.Length; i++)
                DrawDigit(tex, digits[i] - '0', sx + i * (dw + gap), sy, dw, dh, th, Color.white);
        }

        static void DrawDigit(Texture2D tex, int d, int x, int y, int w, int h, int t, Color col)
        {
            bool[][] seg = {
                new[]{true, true, true, false,true, true, true }, // 0
                new[]{false,false,true, false,false,true, false}, // 1
                new[]{true, false,true, true, true, false,true }, // 2
                new[]{true, false,true, true, false,true, true }, // 3
                new[]{false,true, true, true, false,true, false}, // 4
                new[]{true, true, false,true, false,true, true }, // 5
                new[]{true, true, false,true, true, true, true }, // 6
                new[]{true, false,true, false,false,true, false}, // 7
                new[]{true, true, true, true, true, true, true }, // 8
                new[]{true, true, true, true, false,true, true }, // 9
            };
            if (d < 0 || d > 9) return;
            bool[] s   = seg[d];
            int midY   = y + h / 2 - t / 2;
            int topY   = y + h - t;
            int rx     = x + w - t;
            int midLY  = y + h / 2;
            if (s[0]) DrawRect(tex, x,  topY,  w, t,         col);
            if (s[1]) DrawRect(tex, x,  midLY, t, h / 2 - t, col);
            if (s[2]) DrawRect(tex, rx, midLY, t, h / 2 - t, col);
            if (s[3]) DrawRect(tex, x,  midY,  w, t,         col);
            if (s[4]) DrawRect(tex, x,  y + t, t, h / 2 - t, col);
            if (s[5]) DrawRect(tex, rx, y + t, t, h / 2 - t, col);
            if (s[6]) DrawRect(tex, x,  y,     w, t,         col);
        }

        static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            int x1 = Mathf.Max(0, x), x2 = Mathf.Min(tex.width,  x + w);
            int y1 = Mathf.Max(0, y), y2 = Mathf.Min(tex.height, y + h);
            for (int yy = y1; yy < y2; yy++)
                for (int xx = x1; xx < x2; xx++)
                    tex.SetPixel(xx, yy, color);
        }
    }
}
