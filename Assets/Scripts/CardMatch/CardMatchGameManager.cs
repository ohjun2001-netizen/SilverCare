using System;
using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.CardMatch
{
    public class CardMatchGameManager : BaseGameManager
    {
        const float TableTopHeight = 0.79f;
        const float TableForwardOffset = 0.24f;
        const float CardHeight = 1.02f;
        const float CardAspect = 0.68f;
        const float CardGapX = 0.16f;
        const float CardGapY = 0.22f;

        [Header("Card Settings")]
        [SerializeField] Texture[] cardFrontTextures;
        [SerializeField] int columnCount = 4;
        [SerializeField] float spacingX = 0.56f;
        [SerializeField] float spacingY = 1.06f;
        [SerializeField] float spawnDist = 2.2f;

        static readonly int[] DiffPairs = { 4, 6, 8 };
        static readonly string[] DiffLabels = { "쉬움  (4쌍)", "보통  (6쌍)", "어려움  (8쌍)" };

        static readonly MemoryTheme[] NostalgiaThemes =
        {
            new MemoryTheme("브라운관 TV", new Color(0.74f, 0.67f, 0.58f), new Color(0.30f, 0.24f, 0.18f), MemoryIcon.Television),
            new MemoryTheme("라디오", new Color(0.75f, 0.90f, 0.84f), new Color(0.48f, 0.34f, 0.18f), MemoryIcon.Radio),
            new MemoryTheme("다이얼 전화기", new Color(0.83f, 0.30f, 0.28f), new Color(0.41f, 0.11f, 0.12f), MemoryIcon.Telephone),
            new MemoryTheme("주전자", new Color(0.81f, 0.68f, 0.36f), new Color(0.40f, 0.25f, 0.10f), MemoryIcon.Kettle),
            new MemoryTheme("피아노", new Color(0.67f, 0.50f, 0.34f), new Color(0.26f, 0.17f, 0.08f), MemoryIcon.Piano),
            new MemoryTheme("재봉틀", new Color(0.34f, 0.36f, 0.38f), new Color(0.62f, 0.48f, 0.22f), MemoryIcon.SewingMachine),
            new MemoryTheme("꽃무늬 찻잔", new Color(0.94f, 0.84f, 0.88f), new Color(0.74f, 0.44f, 0.56f), MemoryIcon.Teacup),
            new MemoryTheme("벽시계", new Color(0.58f, 0.22f, 0.18f), new Color(0.86f, 0.72f, 0.28f), MemoryIcon.WallClock)
        };

        readonly List<CardController> _cards = new();

        CardController _firstFlipped;
        CardController _hoveredCard;
        int _activePairCount;
        int _matchedPairs;
        bool _isChecking;

        Transform _cardRoot;
        GameObject _memoryBackdropRoot;
        Transform _playBackdropAnchor;
        Canvas _diffCanvas;
        Canvas _gameCanvas;
        Text _scoreText;
        Text _statusText;
        Text _themeText;

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

        void Update()
        {
            if (!_isPlaying)
            {
                ClearCardHover();
                return;
            }

            UpdateCardHover();

            if (_isChecking)
                return;

            if (XRPointerInput.TryGetSelectionHit(20f, out RaycastHit hit))
            {
                var card = hit.collider.GetComponent<CardController>();
                if (card != null)
                    HandleCardClick(card);
            }
        }

        void UpdateCardHover()
        {
            CardController card = null;
            if (XRPointerInput.TryGetPointerRay(out Ray ray) && Physics.Raycast(ray, out RaycastHit hit, 20f))
                card = hit.collider.GetComponent<CardController>();

            if (card == null || card.IsMatched || card.IsFaceUp || !_cards.Contains(card))
            {
                ClearCardHover();
                return;
            }

            if (_hoveredCard == card)
                return;

            ClearCardHover();
            _hoveredCard = card;
            _hoveredCard.SetHover(true);
        }

        void ClearCardHover()
        {
            if (_hoveredCard == null)
                return;

            _hoveredCard.SetHover(false);
            _hoveredCard = null;
        }

        void ShowDifficultyPanel()
        {
            _isPlaying = false;
            ClearPlayObjects();

            if (_diffCanvas != null)
                Destroy(_diffCanvas.gameObject);

            var go = new GameObject("CardMatchDifficultyCanvas");
            _diffCanvas = go.AddComponent<Canvas>();
            _diffCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            XRUIUtility.ConfigureWorldCanvas(go, _diffCanvas);

            var rt = _diffCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(760f, 500f);
            rt.localScale = Vector3.one * 0.0027f;

            PlaceCanvasInFront(_diffCanvas, 0.02f);
            SelectionBackdropUtility.ShowNatureBackdrop(_diffCanvas.transform, "CardMatchDifficulty");
            BuildMemoryBackdrop(_diffCanvas.transform, false, Vector3.zero, Vector3.forward);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.96f, 0.92f, 0.84f, 0.93f);

            MakeAccent(go.transform, new Vector2(0f, 202f), new Vector2(650f, 6f), new Color(0.18f, 0.45f, 0.36f));
            MakeText(go.transform, "Title", "추억 카드 맞추기", 38, new Vector2(0f, 148f), new Vector2(620f, 58f), new Color(0.13f, 0.23f, 0.25f), FontStyle.Bold);
            MakeText(go.transform, "Sub", "익숙한 물건과 풍경을 떠올리며 같은 그림을 찾아보세요.", 23, new Vector2(0f, 102f), new Vector2(660f, 42f), new Color(0.33f, 0.35f, 0.30f), FontStyle.Normal);
            MakeText(go.transform, "Story", "브라운관 TV, 라디오, 주전자처럼 정겨운 물건들을 카드에 담았습니다. 천천히 살펴보며 기억 속 장면을 꺼내 보세요.", 20, new Vector2(0f, 48f), new Vector2(620f, 74f), new Color(0.37f, 0.31f, 0.24f), FontStyle.Normal);

            Color[] buttonColors =
            {
                new Color(0.26f, 0.55f, 0.38f),
                new Color(0.54f, 0.42f, 0.18f),
                new Color(0.42f, 0.31f, 0.16f)
            };
            float[] yPos = { -28f, -100f, -172f };
            for (int i = 0; i < DiffLabels.Length; i++)
            {
                int idx = i;
                MakeDiffButton(_diffCanvas.transform, DiffLabels[i], 24, new Vector2(0f, yPos[i]), new Vector2(460f, 56f), buttonColors[i], () => OnDifficultySelected(DiffPairs[idx]));
            }

            MakeDiffButton(_diffCanvas.transform, "로비로 돌아가기", 20, new Vector2(0f, -246f), new Vector2(250f, 44f), new Color(0.33f, 0.35f, 0.37f), HandleGoLobby);
        }

        void OnDifficultySelected(int pairs)
        {
            _activePairCount = pairs;

            if (_diffCanvas != null)
            {
                Destroy(_diffCanvas.gameObject);
                _diffCanvas = null;
            }

            SelectionBackdropUtility.ClearBackdrop("CardMatchDifficulty");
            SelectionBackdropUtility.ClearBackdrop("CardMatchPlay");
            ClearMemoryBackdrop();

            _matchedPairs = 0;
            _score = 0;
            _firstFlipped = null;
            _isChecking = false;
            _isPlaying = true;

            BuildGameUI();
            StartCoroutine(SpawnAfterDelay());
        }

        void HandleCardClick(CardController card)
        {
            if (card.IsMatched || card.IsFaceUp)
                return;

            if (_hoveredCard == card)
                ClearCardHover();

            card.FlipUp();

            if (_firstFlipped == null)
            {
                _firstFlipped = card;
                SetStatus("첫 번째 추억 카드를 골랐어요. 같은 그림을 찾아보세요.");
            }
            else
            {
                StartCoroutine(CheckMatch(card));
            }
        }

        IEnumerator CheckMatch(CardController second)
        {
            _isChecking = true;
            SetStatus("두 카드가 같은 그림인지 확인하고 있어요.");
            yield return new WaitForSeconds(1f);

            if (_firstFlipped.CardId == second.CardId)
            {
                _firstFlipped.SetMatched();
                second.SetMatched();
                _matchedPairs++;
                _score += 100;
                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.SpeakClip("card_match_success");
                RefreshScore();

                if (_matchedPairs >= _activePairCount)
                    OnGameClear();
                else
                    SetStatus("잘 맞추셨어요. 다음 추억 카드도 천천히 찾아보세요.");
            }
            else
            {
                _firstFlipped.FlipDown();
                second.FlipDown();
                _score = Mathf.Max(0, _score - 10);
                AudioManager.Instance?.PlayWrong();
                RefreshScore();
                SetStatus("조금 달랐어요. 다시 천천히 살펴보세요.");
            }

            _firstFlipped = null;
            _isChecking = false;
        }

        IEnumerator SpawnAfterDelay()
        {
            yield return null;
            yield return null;
            SpawnCards();
            SetStatus("카드를 한 장 눌러 익숙한 물건을 떠올려 보세요.");
            RefreshScore();
        }

        void SpawnCards()
        {
            if (_cardRoot != null)
                Destroy(_cardRoot.gameObject);

            _cards.Clear();
            _cardRoot = new GameObject("CardGrid").transform;

            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            XRUIUtility.RefreshSceneViewAnchor();
            XRUIUtility.GetSceneViewAnchor(cam, out Vector3 camPos, out Vector3 forward);

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 tableForward = forward.normalized;

            if (_playBackdropAnchor != null)
                Destroy(_playBackdropAnchor.gameObject);
            _playBackdropAnchor = new GameObject("CardMatchPlayBackdropAnchor").transform;
            _playBackdropAnchor.position = camPos + forward * 2.3f;
            _playBackdropAnchor.rotation = Quaternion.LookRotation(forward, Vector3.up);
            SelectionBackdropUtility.ShowNatureBackdrop(_playBackdropAnchor, "CardMatchPlay");

            BuildMemoryBackdrop(null, true, camPos + forward * (spawnDist + 2.8f) + Vector3.down * 1.02f, forward);

            int total = _activePairCount * 2;
            Texture[] fronts = BuildCardTextureSet(_activePairCount);
            var data = new List<(int id, Texture tex)>();
            for (int i = 0; i < _activePairCount; i++)
            {
                data.Add((i, fronts[i]));
                data.Add((i, fronts[i]));
            }
            Shuffle(data);

            int cols = Mathf.Max(2, columnCount);
            int rows = Mathf.CeilToInt((float)total / cols);
            float cardHeight = CardHeight;
            float cardWidth = cardHeight * CardAspect;
            float effectiveSpacingX = Mathf.Max(spacingX, cardWidth + CardGapX);
            float effectiveSpacingY = Mathf.Max(spacingY, cardHeight + CardGapY);
            float totalWidth = (cols - 1) * effectiveSpacingX;
            float totalDepth = (rows - 1) * effectiveSpacingY;

            Vector3 center;
            if (_memoryBackdropRoot != null)
            {
                center = _memoryBackdropRoot.transform.TransformPoint(new Vector3(0f, TableTopHeight, TableForwardOffset));
            }
            else
            {
                center = camPos + forward * spawnDist;
                center.y = Mathf.Clamp(camPos.y, 0.8f, 2.2f);
            }

            var positions = new List<Vector3>(total);
            for (int i = 0; i < total; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = col * effectiveSpacingX - totalWidth / 2f;
                float z = row * effectiveSpacingY - totalDepth / 2f;
                positions.Add(center + right * x + tableForward * z);
            }
            Shuffle(positions);

            for (int i = 0; i < total; i++)
            {
                Vector3 pos = positions[i];
                Quaternion cardRotation = Quaternion.LookRotation(Vector3.up, tableForward);

                var card = CreateCard(pos, cardRotation, cardWidth, cardHeight);
                card.name = $"MemoryCard_{data[i].id}_{i}";
                card.Init(data[i].id, data[i].tex);
                _cards.Add(card);
            }

            if (_gameCanvas != null)
            {
                _gameCanvas.transform.position = center + Vector3.up * 0.62f + tableForward * (totalDepth / 2f + 0.18f);
                _gameCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        CardController CreateCard(Vector3 position, Quaternion rotation, float cardWidth, float cardHeight)
        {
            var root = new GameObject("Card");
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.SetParent(_cardRoot);

            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "FrontFace";
            front.transform.SetParent(root.transform, false);
            front.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            front.transform.localScale = new Vector3(-cardWidth, cardHeight, 1f);
            Destroy(front.GetComponent<MeshCollider>());

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "BackFace";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            back.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);
            Destroy(back.GetComponent<MeshCollider>());

            var backMat = new Material(Shader.Find("Sprites/Default"));
            backMat.mainTexture = CreateCardBackTexture();
            back.GetComponent<Renderer>().material = backMat;

            root.AddComponent<BoxCollider>().size = new Vector3(cardWidth + 0.06f, cardHeight + 0.06f, 0.08f);

            var controller = root.AddComponent<CardController>();
            controller.SetFaces(front, back);
            return controller;
        }

        void BuildGameUI()
        {
            if (_gameCanvas != null)
                Destroy(_gameCanvas.gameObject);

            var go = new GameObject("CardMatchUI");
            _gameCanvas = go.AddComponent<Canvas>();
            _gameCanvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            XRUIUtility.ConfigureWorldCanvas(go, _gameCanvas);

            var rt = _gameCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(920f, 180f);
            rt.localScale = Vector3.one * 0.0025f;
            go.transform.position = new Vector3(0f, -100f, 0f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.96f, 0.93f, 0.87f, 0.92f);

            MakeAccent(go.transform, new Vector2(0f, 66f), new Vector2(760f, 4f), new Color(0.20f, 0.45f, 0.36f));
            _scoreText = MakeText(go.transform, "Score", $"맞춘 카드 0 / {_activePairCount}쌍", 28, new Vector2(-210f, 28f), new Vector2(340f, 42f), new Color(0.16f, 0.22f, 0.23f), FontStyle.Bold);
            _statusText = MakeText(go.transform, "Status", "", 20, new Vector2(-70f, -22f), new Vector2(500f, 40f), new Color(0.48f, 0.36f, 0.21f), FontStyle.Normal);
            _themeText = MakeText(go.transform, "Theme", "", 22, new Vector2(170f, 28f), new Vector2(170f, 38f), new Color(0.24f, 0.47f, 0.41f), FontStyle.Bold);

            MakeDiffButton(go.transform, "나가기", 20, new Vector2(334f, 0f), new Vector2(150f, 56f), new Color(0.37f, 0.35f, 0.38f), HandleGoLobby);
        }

        void ShowResultOverlay()
        {
            if (_scoreText != null)
                _scoreText.gameObject.SetActive(false);
            if (_statusText != null)
                _statusText.gameObject.SetActive(false);
            if (_themeText != null)
                _themeText.gameObject.SetActive(false);

            _gameCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, 240f);

            MakeText(_gameCanvas.transform, "Clear", $"추억 카드 맞추기 완료\n점수: {_score}", 34, new Vector2(0f, 44f), new Vector2(780f, 88f), new Color(0.18f, 0.26f, 0.24f), FontStyle.Bold);
            MakeText(_gameCanvas.transform, "ClearSub", "익숙한 그림을 하나씩 떠올리며 아주 잘 맞춰 주셨어요.", 21, new Vector2(0f, -18f), new Vector2(720f, 38f), new Color(0.49f, 0.36f, 0.23f), FontStyle.Normal);

            MakeButton(_gameCanvas.transform, "다시 하기", new Vector2(-126f, -74f), new Vector2(190f, 54f), HandleRestart);
            MakeButton(_gameCanvas.transform, "로비로", new Vector2(126f, -74f), new Vector2(190f, 54f), HandleGoLobby);
        }

        void RefreshScore()
        {
            if (_scoreText != null)
                _scoreText.text = $"맞춘 카드 {_matchedPairs} / {_activePairCount}쌍";
        }

        void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        void HandleRestart()
        {
            ClearPlayObjects();
            ShowDifficultyPanel();
        }

        void HandleGoLobby()
        {
            ClearPlayObjects();
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(lobbySceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        void ClearPlayObjects()
        {
            ClearCardHover();

            if (_cardRoot != null)
            {
                Destroy(_cardRoot.gameObject);
                _cardRoot = null;
            }

            if (_gameCanvas != null)
            {
                Destroy(_gameCanvas.gameObject);
                _gameCanvas = null;
            }

            if (_playBackdropAnchor != null)
            {
                Destroy(_playBackdropAnchor.gameObject);
                _playBackdropAnchor = null;
            }

            _cards.Clear();
            SelectionBackdropUtility.ClearBackdrop("CardMatchPlay");
            ClearMemoryBackdrop();
        }

        void BuildMemoryBackdrop(Transform anchor, bool worldSpace, Vector3 worldPosition, Vector3 lookForward)
        {
            ClearMemoryBackdrop();

            _memoryBackdropRoot = new GameObject(worldSpace ? "CardMatchPlayBackdrop" : "CardMatchMemoryBackdrop");

            if (!worldSpace && anchor != null)
            {
                _memoryBackdropRoot.transform.SetParent(anchor, false);
                _memoryBackdropRoot.transform.position = anchor.position + anchor.forward * 5.0f + Vector3.down * 1.0f;
                _memoryBackdropRoot.transform.rotation = Quaternion.LookRotation(anchor.forward, Vector3.up);
            }
            else
            {
                _memoryBackdropRoot.transform.position = worldPosition;
                if (lookForward == Vector3.zero)
                    lookForward = Vector3.forward;
                _memoryBackdropRoot.transform.rotation = Quaternion.LookRotation(lookForward, Vector3.up);
            }

            Transform root = _memoryBackdropRoot.transform;
            CreateGround(root, new Vector3(0f, -0.05f, 0f), new Vector3(12.5f, 0.05f, 10.0f), new Color(0.56f, 0.75f, 0.47f));
            CreateSea(root, new Vector3(0f, -0.15f, 5.7f), new Vector3(14f, 0.02f, 5f));
            CreateTable(root, new Vector3(0f, 0f, TableForwardOffset), GetMemoryTableSize());
            CreateTree(root, new Vector3(-3.8f, 0f, 2.4f));
            CreateTree(root, new Vector3(3.8f, 0f, 2.4f));
            CreateTree(root, new Vector3(-5.1f, 0f, -0.6f), 0.82f);
            CreateTree(root, new Vector3(5.1f, 0f, -0.6f), 0.82f);
            CreateFlowerCluster(root, new Vector3(-2.2f, 0f, -0.2f), new Color(0.95f, 0.74f, 0.56f));
            CreateFlowerCluster(root, new Vector3(2.1f, 0f, -0.15f), new Color(0.92f, 0.65f, 0.74f));
        }

        void ClearMemoryBackdrop()
        {
            if (_memoryBackdropRoot != null)
            {
                Destroy(_memoryBackdropRoot);
                _memoryBackdropRoot = null;
            }
        }

        Texture[] BuildCardTextureSet(int count)
        {
            var textures = new Texture[count];
            var resourceTextures = LoadBundledMemoryTextures();
            for (int i = 0; i < count; i++)
            {
                if (cardFrontTextures != null && i < cardFrontTextures.Length && cardFrontTextures[i] != null)
                    textures[i] = cardFrontTextures[i];
                else if (i < resourceTextures.Length && resourceTextures[i] != null)
                    textures[i] = resourceTextures[i];
                else
                    textures[i] = CreateMemoryCardTexture(NostalgiaThemes[i % NostalgiaThemes.Length]);
            }
            return textures;
        }

        static Texture2D[] LoadBundledMemoryTextures()
        {
            string[] resourceNames =
            {
                "CardMatchMemory/tv",
                "CardMatchMemory/radio",
                "CardMatchMemory/phone",
                "CardMatchMemory/kettle",
                "CardMatchMemory/piano",
                "CardMatchMemory/sewing",
                "CardMatchMemory/teacup",
                "CardMatchMemory/clock"
            };

            var textures = new Texture2D[resourceNames.Length];
            for (int i = 0; i < resourceNames.Length; i++)
                textures[i] = Resources.Load<Texture2D>(resourceNames[i]);

            return textures;
        }

        Texture2D CreateCardBackTexture()
        {
            var tex = new Texture2D(256, 384, TextureFormat.RGBA32, false);
            Fill(tex, new Color(0.25f, 0.48f, 0.54f));
            DrawRect(tex, 18, 18, 220, 348, new Color(0.90f, 0.93f, 0.88f));
            DrawRect(tex, 34, 34, 188, 316, new Color(0.18f, 0.41f, 0.46f));
            DrawCircle(tex, 128, 192, 56, new Color(0.90f, 0.87f, 0.68f));
            DrawRect(tex, 76, 182, 104, 20, new Color(0.26f, 0.30f, 0.24f));
            DrawRect(tex, 118, 146, 20, 92, new Color(0.26f, 0.30f, 0.24f));
            tex.Apply();
            return tex;
        }

        Texture2D CreateMemoryCardTexture(MemoryTheme theme)
        {
            var tex = new Texture2D(256, 384, TextureFormat.RGBA32, false);
            Fill(tex, new Color(0.97f, 0.93f, 0.86f));
            DrawRect(tex, 14, 14, 228, 356, new Color(0.89f, 0.81f, 0.68f));
            DrawRect(tex, 26, 26, 204, 332, new Color(0.98f, 0.96f, 0.91f));
            DrawRect(tex, 36, 252, 184, 76, theme.BannerColor);
            DrawRect(tex, 44, 52, 168, 170, new Color(1f, 0.99f, 0.95f));
            DrawRect(tex, 52, 44, 152, 8, new Color(0f, 0f, 0f, 0.08f));
            DrawMemoryIcon(tex, theme.Icon, 128, 136, theme.IconColor);
            DrawAccentStamps(tex, theme.AccentColor);
            tex.Apply();
            return tex;
        }

        static void DrawMemoryIcon(Texture2D tex, MemoryIcon icon, int cx, int cy, Color color)
        {
            switch (icon)
            {
                case MemoryIcon.Television:
                    DrawRect(tex, cx - 58, cy - 34, 116, 82, color);
                    DrawRect(tex, cx - 46, cy - 22, 66, 58, new Color(0.84f, 0.90f, 0.88f));
                    DrawRect(tex, cx + 28, cy - 22, 18, 58, new Color(0.22f, 0.16f, 0.12f));
                    DrawCircle(tex, cx + 37, cy + 16, 7, new Color(0.88f, 0.78f, 0.58f));
                    DrawCircle(tex, cx + 37, cy - 2, 7, new Color(0.88f, 0.78f, 0.58f));
                    DrawLine(tex, cx - 22, cy + 50, cx - 42, cy + 82, color, 4);
                    DrawLine(tex, cx + 8, cy + 50, cx + 28, cy + 82, color, 4);
                    break;

                case MemoryIcon.Radio:
                    DrawRect(tex, cx - 56, cy - 28, 112, 56, color);
                    DrawRect(tex, cx - 34, cy + 6, 68, 8, new Color(0.94f, 0.88f, 0.70f));
                    DrawCircle(tex, cx - 24, cy - 6, 12, new Color(0.94f, 0.88f, 0.70f));
                    DrawCircle(tex, cx + 24, cy - 6, 12, new Color(0.94f, 0.88f, 0.70f));
                    DrawLine(tex, cx - 10, cy + 28, cx + 56, cy + 70, color, 4);
                    break;

                case MemoryIcon.Telephone:
                    DrawRect(tex, cx - 50, cy - 42, 100, 62, color);
                    DrawArc(tex, cx, cy + 26, 48, 200, 340, color);
                    DrawCircle(tex, cx, cy - 4, 24, new Color(0.96f, 0.93f, 0.90f));
                    DrawCircle(tex, cx, cy - 4, 17, new Color(0.80f, 0.86f, 0.88f));
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 8f;
                        int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * 12f);
                        int py = cy - 4 + Mathf.RoundToInt(Mathf.Sin(angle) * 12f);
                        DrawCircle(tex, px, py, 2, color);
                    }
                    break;

                case MemoryIcon.Kettle:
                    DrawCircle(tex, cx, cy, 44, color);
                    DrawRect(tex, cx - 46, cy - 6, 92, 36, color);
                    DrawArc(tex, cx + 42, cy + 8, 20, 300, 420, color);
                    DrawArc(tex, cx - 2, cy + 46, 24, 180, 360, color);
                    break;

                case MemoryIcon.Piano:
                    DrawRect(tex, cx - 58, cy - 30, 116, 72, color);
                    for (int i = -5; i <= 5; i++)
                        DrawRect(tex, cx + i * 10 - 4, cy - 24, 8, 46, new Color(0.98f, 0.96f, 0.92f));
                    DrawRect(tex, cx - 42, cy + 18, 84, 10, new Color(0.86f, 0.79f, 0.63f));
                    break;

                case MemoryIcon.SewingMachine:
                    DrawRect(tex, cx - 56, cy - 44, 112, 18, new Color(0.40f, 0.27f, 0.14f));
                    DrawRect(tex, cx - 28, cy - 18, 70, 18, color);
                    DrawArc(tex, cx - 8, cy + 10, 30, 90, 270, color);
                    DrawRect(tex, cx - 26, cy + 4, 12, 36, color);
                    DrawCircle(tex, cx + 42, cy + 8, 15, color);
                    DrawCircle(tex, cx + 42, cy + 8, 7, new Color(0.77f, 0.66f, 0.38f));
                    break;

                case MemoryIcon.Teacup:
                    DrawRect(tex, cx - 46, cy - 8, 92, 34, color);
                    DrawArc(tex, cx + 48, cy + 6, 18, 270, 420, color);
                    DrawRect(tex, cx - 58, cy - 18, 116, 10, new Color(0.82f, 0.63f, 0.70f));
                    DrawCircle(tex, cx - 18, cy + 8, 6, new Color(0.98f, 0.90f, 0.76f));
                    DrawCircle(tex, cx + 4, cy - 2, 6, new Color(0.98f, 0.90f, 0.76f));
                    break;

                case MemoryIcon.WallClock:
                    DrawRect(tex, cx - 34, cy - 58, 68, 124, color);
                    DrawArc(tex, cx, cy + 54, 32, 0, 180, color);
                    DrawCircle(tex, cx, cy + 16, 25, new Color(0.96f, 0.94f, 0.88f));
                    DrawLine(tex, cx, cy + 16, cx, cy + 31, color, 4);
                    DrawLine(tex, cx, cy + 16, cx + 12, cy + 8, color, 4);
                    DrawLine(tex, cx, cy - 6, cx, cy - 38, new Color(0.77f, 0.66f, 0.38f), 4);
                    DrawCircle(tex, cx, cy - 44, 10, new Color(0.83f, 0.72f, 0.35f));
                    break;
            }
        }

        static void DrawAccentStamps(Texture2D tex, Color accent)
        {
            DrawCircle(tex, 58, 320, 8, accent);
            DrawCircle(tex, 198, 320, 8, accent);
            DrawCircle(tex, 128, 338, 8, accent);
            DrawLine(tex, 52, 82, 84, 50, accent, 4);
            DrawLine(tex, 204, 82, 172, 50, accent, 4);
        }

        static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
        }

        static void DrawRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            int x1 = Mathf.Max(0, x);
            int x2 = Mathf.Min(tex.width, x + width);
            int y1 = Mathf.Max(0, y);
            int y2 = Mathf.Min(tex.height, y + height);

            for (int yy = y1; yy < y2; yy++)
            {
                for (int xx = x1; xx < x2; xx++)
                    tex.SetPixel(xx, yy, color);
            }
        }

        static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSquared)
                        tex.SetPixel(cx + x, cy + y, color);
                }
            }
        }

        static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))) * 2;
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                DrawCircle(tex, x, y, Mathf.Max(1, thickness / 2), color);
            }
        }

        static void DrawArc(Texture2D tex, int cx, int cy, int radius, int startDeg, int endDeg, Color color)
        {
            for (int angle = startDeg; angle <= endDeg; angle++)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * radius);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * radius);
                DrawCircle(tex, x, y, 4, color);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        void PlaceCanvasInFront(Canvas canvas, float yOffset)
        {
            XRUIUtility.PlaceCanvasFacingCamera(canvas, 2f, 0.1f + yOffset);
        }

        static Font _koreanFont;
        static Font GetFont()
        {
            if (_koreanFont == null)
                _koreanFont = Resources.Load<Font>("Fonts/Paperlogy-5Medium");
            return _koreanFont != null ? _koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Text MakeText(Transform parent, string name, string content, int size, Vector2 pos, Vector2 sizeDelta, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.font = GetFont();
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void MakeAccent(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
        }

        static void MakeDiffButton(Transform parent, string label, int fontSize, Vector2 pos, Vector2 size, Color color, Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(go.transform, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var labelText = labelObj.GetComponent<Text>();
            labelText.text = label;
            labelText.fontSize = fontSize;
            labelText.font = GetFont();
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            var button = go.GetComponent<Button>();
            if (button.GetComponent<XRButtonHoverFeedback>() == null)
                button.gameObject.AddComponent<XRButtonHoverFeedback>();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Action onClick)
        {
            MakeDiffButton(parent, label, 24, pos, size, new Color(0.28f, 0.39f, 0.48f), onClick);
        }

        static void CreateGround(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Ground";
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            obj.GetComponent<Renderer>().material.color = color;
        }

        static void CreateSea(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Sea";
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            obj.GetComponent<Renderer>().material.color = new Color(0.42f, 0.76f, 0.84f);
        }

        Vector2 GetMemoryTableSize()
        {
            int pairs = _activePairCount > 0 ? _activePairCount : DiffPairs[1];
            int total = pairs * 2;
            int cols = Mathf.Max(2, columnCount);
            int rows = Mathf.CeilToInt((float)total / cols);
            float cardWidth = CardHeight * CardAspect;
            float effectiveSpacingX = Mathf.Max(spacingX, cardWidth + CardGapX);
            float effectiveSpacingY = Mathf.Max(spacingY, CardHeight + CardGapY);
            float width = (cols - 1) * effectiveSpacingX + cardWidth + 0.78f;
            float depth = (rows - 1) * effectiveSpacingY + CardHeight + 0.86f;
            return new Vector2(Mathf.Max(3.9f, width), Mathf.Max(2.8f, depth));
        }

        static void CreateTable(Transform parent, Vector3 localPos, Vector2 tableSize)
        {
            var root = new GameObject("MemoryTable");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            float width = Mathf.Max(3.2f, tableSize.x);
            float depth = Mathf.Max(2.4f, tableSize.y);
            CreatePart(root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(width, 0.10f, depth), new Color(0.55f, 0.32f, 0.18f));
            CreatePart(root.transform, new Vector3(0f, 0.67f, 0f), new Vector3(width - 0.25f, 0.04f, depth - 0.25f), new Color(0.63f, 0.41f, 0.25f));

            float legX = width * 0.5f - 0.35f;
            float legZ = depth * 0.5f - 0.35f;
            CreatePart(root.transform, new Vector3(-legX, 0.34f, -legZ), new Vector3(0.12f, 0.68f, 0.12f), new Color(0.34f, 0.21f, 0.12f));
            CreatePart(root.transform, new Vector3(legX, 0.34f, -legZ), new Vector3(0.12f, 0.68f, 0.12f), new Color(0.34f, 0.21f, 0.12f));
            CreatePart(root.transform, new Vector3(-legX, 0.34f, legZ), new Vector3(0.12f, 0.68f, 0.12f), new Color(0.34f, 0.21f, 0.12f));
            CreatePart(root.transform, new Vector3(legX, 0.34f, legZ), new Vector3(0.12f, 0.68f, 0.12f), new Color(0.34f, 0.21f, 0.12f));
        }

        static void CreateTree(Transform parent, Vector3 localPos, float scale = 1f)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = Vector3.one * scale;

            CreatePart(root.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.20f, 1.5f, 0.20f), new Color(0.43f, 0.31f, 0.18f));
            CreatePart(root.transform, new Vector3(0f, 1.75f, 0f), new Vector3(1.2f, 0.75f, 1.2f), new Color(0.47f, 0.71f, 0.39f));
            CreatePart(root.transform, new Vector3(-0.42f, 1.48f, 0.08f), new Vector3(0.72f, 0.46f, 0.72f), new Color(0.53f, 0.77f, 0.44f));
            CreatePart(root.transform, new Vector3(0.46f, 1.52f, -0.05f), new Vector3(0.66f, 0.42f, 0.66f), new Color(0.40f, 0.67f, 0.35f));
        }

        static void CreateFlowerCluster(Transform parent, Vector3 localPos, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float offsetX = (i - 2.5f) * 0.09f;
                float offsetZ = (i % 2 == 0 ? -0.06f : 0.06f);
                CreatePart(parent, localPos + new Vector3(offsetX, 0.10f, offsetZ), new Vector3(0.10f, 0.14f, 0.10f), color);
            }
        }

        static void CreatePart(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            obj.GetComponent<Renderer>().material.color = color;
        }

        readonly struct MemoryTheme
        {
            public readonly string Label;
            public readonly Color BannerColor;
            public readonly Color AccentColor;
            public readonly MemoryIcon Icon;

            public MemoryTheme(string label, Color bannerColor, Color accentColor, MemoryIcon icon)
            {
                Label = label;
                BannerColor = bannerColor;
                AccentColor = accentColor;
                Icon = icon;
            }

            public Color IconColor => Color.Lerp(BannerColor, Color.black, 0.35f);
        }

        enum MemoryIcon
        {
            Television,
            Radio,
            Telephone,
            Kettle,
            Piano,
            SewingMachine,
            Teacup,
            WallClock
        }
    }
}
