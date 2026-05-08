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
        [SerializeField] int   pairCount    = 4;
        [SerializeField] int   columnCount  = 4;
        [SerializeField] float spacingX     = 0.85f;
        [SerializeField] float spacingY     = 1.0f;
        [SerializeField] float spawnDist    = 1.8f;
        [SerializeField] float heightOffset = -0.2f; // 눈높이 기준 오프셋 (음수=아래)

        // 게임 상태
        readonly List<CardController> _cards = new();
        CardController _firstFlipped;
        int  _matchedPairs;
        bool _isChecking;
        Transform _cardRoot;

        // UI
        Canvas _canvas;
        Text   _scoreText, _statusText;

        // ── BaseGameManager 구현 ─────────────────────────────

        protected override void InitGame()
        {
            gameTitle = "카드 짝맞추기";
            if (cardFrontTextures == null || cardFrontTextures.Length < pairCount)
                cardFrontTextures = MakeFallbackTextures(pairCount);
        }

        protected override void StartGame()
        {
            _matchedPairs = 0;
            _score        = 0;
            _firstFlipped = null;
            _isChecking   = false;

            BuildUI();
            StartCoroutine(SpawnAfterDelay());
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
                if (_matchedPairs >= pairCount)
                    OnGameClear();
                else
                    SetStatus($"맞았습니다! 카드를 골라보세요");
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

            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            Vector3 camPos  = cam != null ? cam.transform.position : Vector3.up * 1.6f;
            Vector3 forward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (forward == Vector3.zero) forward = Vector3.forward;
            camPos.y = Mathf.Max(camPos.y, 1.0f);

            // 카드가 카메라를 향하도록 회전
            Quaternion cardRot = Quaternion.LookRotation(-forward, Vector3.up);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized; // 그리드 가로 축

            int total  = pairCount * 2;
            int rows   = Mathf.CeilToInt((float)total / columnCount);
            float totW = (columnCount - 1) * spacingX;
            float totH = (rows - 1) * spacingY;
            Vector3 center = camPos + forward * spawnDist;
            center.y = camPos.y + heightOffset;

            // 셔플
            var data = new List<(int id, Texture tex)>();
            for (int i = 0; i < pairCount; i++)
            {
                data.Add((i, cardFrontTextures[i]));
                data.Add((i, cardFrontTextures[i]));
            }
            for (int i = 0; i < data.Count; i++)
            {
                int r = Random.Range(i, data.Count);
                (data[i], data[r]) = (data[r], data[i]);
            }

            for (int i = 0; i < data.Count; i++)
            {
                int col = i % columnCount;
                int row = i / columnCount;
                float x = col * spacingX - totW / 2f;
                float y = row * spacingY - totH / 2f;
                Vector3 pos = center + right * x + Vector3.up * y;

                var card = CreateCard(pos, cardRot);
                card.name = $"Card_{data[i].id}_{i}";
                card.Init(data[i].id, data[i].tex);
                _cards.Add(card);
            }

            // UI 캔버스를 카드 그리드 위에 배치
            // Canvas 는 local+Z 가 카메라 반대 방향(forward)일 때 텍스트가 올바로 보임
            Vector3 canvasPos = center + Vector3.up * (totH / 2f + 0.75f);
            _canvas.transform.position = canvasPos;
            _canvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        CardController CreateCard(Vector3 pos, Quaternion rot)
        {
            var root = new GameObject("Card");
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.SetParent(_cardRoot);

            // 앞면
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "FrontFace";
            front.transform.SetParent(root.transform, false);
            front.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            front.transform.localScale = new Vector3(0.9f, 1.2f, 1f);
            Destroy(front.GetComponent<MeshCollider>());

            // 뒷면은 카메라 쪽을 향하도록 180도 뒤집어 준다.
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "BackFace";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            back.transform.localScale = new Vector3(0.9f, 1.2f, 1f);
            Destroy(back.GetComponent<MeshCollider>());
            back.GetComponent<Renderer>().material = MakeColorMat(new Color(0.85f, 0.30f, 0.10f));

            root.AddComponent<BoxCollider>().size = new Vector3(0.95f, 0.95f, 0.1f);

            var cc = root.AddComponent<CardController>();
            cc.SetFaces(front, back);
            return cc;
        }

        static Shader FindCardShader(params string[] names)
        {
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader != null) return shader;
            }
            return null;
        }

        static Material MakeColorMat(Color color)
        {
            var shader = FindCardShader("Unlit/Color", "Sprites/Default", "Universal Render Pipeline/Unlit", "Standard");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        // ── UI ───────────────────────────────────────────────

        void BuildUI()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);

            var go = new GameObject("CardMatchUI");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(700, 110);
            rt.localScale = Vector3.one * 0.003f;

            // 씬 로드 전까지 화면 밖에 숨김
            go.transform.position = new Vector3(0f, -100f, 0f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            _scoreText  = MakeText(_canvas.transform, "Score",  "점수: 0  |  0 / 4 쌍", 28,
                new Vector2(0, 22), new Vector2(680, 44));
            _statusText = MakeText(_canvas.transform, "Status", "", 22,
                new Vector2(0, -22), new Vector2(680, 36));
            _statusText.color = new Color(0.9f, 0.9f, 0.65f);
        }

        void ShowResultOverlay()
        {
            if (_scoreText  != null) _scoreText.gameObject.SetActive(false);
            if (_statusText != null) _statusText.gameObject.SetActive(false);

            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 200);

            MakeText(_canvas.transform, "Clear",
                $"게임 클리어!  점수: {_score}", 38,
                new Vector2(0, 55), new Vector2(680, 60)).color = new Color(1f, 0.9f, 0.3f);

            MakeButton(_canvas.transform, "다시 하기",
                new Vector2(-150, -30), new Vector2(250, 60), HandleRestart);
            MakeButton(_canvas.transform, "로비로",
                new Vector2( 150, -30), new Vector2(250, 60), HandleGoLobby);
        }

        void RefreshScore()
        {
            if (_scoreText != null)
                _scoreText.text = $"점수: {_score}  |  {_matchedPairs} / {pairCount} 쌍";
        }

        void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        void HandleRestart()
        {
            _isPlaying = true;
            StartGame();
        }

        void HandleGoLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(lobbySceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        Text MakeText(Transform parent, string name, string content, int size,
                      Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = sizeDelta;
            var t = go.GetComponent<Text>();
            t.text      = content;
            t.fontSize  = size;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font      = Font.CreateDynamicFontFromOSFont("Arial", size);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Truncate;
            return t;
        }

        void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
                        System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.38f);

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = textGO.GetComponent<Text>();
            t.text      = label;
            t.fontSize  = 26;
            t.color     = Color.white;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.font      = Font.CreateDynamicFontFromOSFont("Arial", 26);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }



        Texture2D[] MakeFallbackTextures(int count)
        {
            Color[] palette = {
                new Color(0.85f, 0.15f, 0.15f),
                new Color(0.15f, 0.35f, 0.85f),
                new Color(0.15f, 0.65f, 0.22f),
                new Color(0.85f, 0.60f, 0.10f),
                new Color(0.70f, 0.15f, 0.75f),
                new Color(0.10f, 0.70f, 0.75f),
                new Color(0.85f, 0.85f, 0.10f),
                new Color(0.50f, 0.32f, 0.18f),
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
            // 카드 테두리
            DrawRect(tex, 6, 6, tex.width - 12, tex.height - 12, Color.white);
            DrawRect(tex, 10, 10, tex.width - 20, tex.height - 20, new Color(0.15f, 0.15f, 0.15f, 0.35f));

            string digits = number.ToString();
            int digitWidth = 28;
            int digitHeight = 52;
            int thickness = 6;
            int gap = 10;
            int totalWidth = digits.Length * digitWidth + (digits.Length - 1) * gap;
            int startX = (tex.width - totalWidth) / 2;
            int startY = (tex.height - digitHeight) / 2;

            for (int i = 0; i < digits.Length; i++)
            {
                DrawDigit(tex, digits[i] - '0', startX + i * (digitWidth + gap), startY, digitWidth, digitHeight, thickness, Color.white);
            }
        }

        static void DrawDigit(Texture2D tex, int digit, int x, int y, int w, int h, int t, Color color)
        {
            bool[][] segments =
            {
                new[] { true,  true,  true,  false, true,  true,  true  }, // 0
                new[] { false, false, true,  false, false, true,  false }, // 1
                new[] { true,  false, true,  true,  true,  false, true  }, // 2
                new[] { true,  false, true,  true,  false, true,  true  }, // 3
                new[] { false, true,  true,  true,  false, true,  false }, // 4
                new[] { true,  true,  false, true,  false, true,  true  }, // 5
                new[] { true,  true,  false, true,  true,  true,  true  }, // 6
                new[] { true,  false, true,  false, false, true,  false }, // 7
                new[] { true,  true,  true,  true,  true,  true,  true  }, // 8
                new[] { true,  true,  true,  true,  false, true,  true  }, // 9
            };

            if (digit < 0 || digit > 9) return;

            bool[] s = segments[digit];
            int midY = y + h / 2 - t / 2;
            int topY = y + h - t;
            int botY = y;
            int leftX = x;
            int rightX = x + w - t;
            int midLeftY = y + h / 2;

            if (s[0]) DrawRect(tex, x, topY, w, t, color);              // top
            if (s[1]) DrawRect(tex, leftX, midLeftY, t, h / 2 - t, color); // upper-left
            if (s[2]) DrawRect(tex, rightX, midLeftY, t, h / 2 - t, color); // upper-right
            if (s[3]) DrawRect(tex, x, midY, w, t, color);              // middle
            if (s[4]) DrawRect(tex, leftX, botY + t, t, h / 2 - t, color);  // lower-left
            if (s[5]) DrawRect(tex, rightX, botY + t, t, h / 2 - t, color); // lower-right
            if (s[6]) DrawRect(tex, x, botY, w, t, color);              // bottom
        }

        static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            int xMax = Mathf.Min(tex.width, x + w);
            int yMax = Mathf.Min(tex.height, y + h);
            int xMin = Mathf.Max(0, x);
            int yMin = Mathf.Max(0, y);

            for (int yy = yMin; yy < yMax; yy++)
            {
                for (int xx = xMin; xx < xMax; xx++)
                {
                    tex.SetPixel(xx, yy, color);
                }
            }
        }
    }
}
