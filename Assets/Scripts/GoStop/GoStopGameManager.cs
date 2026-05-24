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
        const float CardW = 0.38f;
        const float CardH = 0.54f;
        const float GapX = 0.42f;
        const float GapY = 0.62f;
        const float TableDistance = 2.5f;

        readonly List<HwatooCard> _playerHand = new();
        readonly List<HwatooCard> _cpuHand = new();
        readonly List<HwatooCard> _floorCards = new();
        readonly List<HwatooCard> _deckRemaining = new();
        readonly List<HwatooCard> _playerCaptured = new();
        readonly List<HwatooCard> _cpuCaptured = new();
        readonly HashSet<int> _playerShakenMonths = new();
        readonly HashSet<int> _cpuShakenMonths = new();
        readonly Dictionary<HwatooCard, GameObject> _cardViews = new();
        readonly Dictionary<int, Texture2D> _textureCache = new();

        GoStopDeck _deck;
        GoStopScoreCalculator _scorer;

        Transform _cardRoot;
        Transform _playerCapturedRoot;
        Transform _cpuCapturedRoot;
        Canvas _gameCanvas;
        Canvas _goStopCanvas;
        Text _statusText;
        Text _scoreText;
        Text _deckText;

        Vector3 _tableCenter;
        Vector3 _right;
        Vector3 _up;
        Quaternion _cardRotation;

        int _goCount;
        int _cpuGoCount;
        int _playerBombCount;
        int _cpuBombCount;
        bool _waitingForCard;
        bool _isProcessing;

        protected override void InitGame()
        {
            gameTitle = "고스톱";
        }

        protected override void StartGame()
        {
            _deck = GetComponent<GoStopDeck>() ?? gameObject.AddComponent<GoStopDeck>();
            _scorer = GetComponent<GoStopScoreCalculator>() ?? gameObject.AddComponent<GoStopScoreCalculator>();
            _deck.Initialize();

            SetupTable();
            DealCards();
            BuildGameUI();
            RefreshAllViews();

            _waitingForCard = true;
            SetStatus("내 차례입니다. 낼 패를 선택하세요.");
        }

        protected override void EndGame()
        {
            PlayerDataManager.Instance?.SaveScore(gameTitle, _score);
        }

        void Update()
        {
            if (!_isPlaying || !_waitingForCard || _isProcessing) return;
            if (!XRPointerInput.TryGetSelectionHit(100f, out var hit)) return;

            var marker = hit.collider.GetComponentInParent<HandCardMarker>();
            if (marker != null && marker.Card != null && _playerHand.Contains(marker.Card))
            {
                StartCoroutine(PlayerTurn(marker.Card));
            }
        }

        void SetupTable()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                cam.tag = "MainCamera";
                go.transform.position = new Vector3(0, 1.6f, -4f);
                go.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            }

            XRUIUtility.GetSceneViewAnchor(cam, out Vector3 anchorPosition, out Vector3 anchorForward);
            _tableCenter = anchorPosition + anchorForward * TableDistance;
            _right = Vector3.Cross(Vector3.up, anchorForward).normalized;
            _up = Vector3.up;
            _cardRotation = Quaternion.LookRotation(anchorForward, Vector3.up);

            ClearCardViews();
            _cardRoot = new GameObject("GoStop Cards").transform;
        }

        void DealCards()
        {
            _playerHand.Clear();
            _cpuHand.Clear();
            _floorCards.Clear();
            _deckRemaining.Clear();
            _playerCaptured.Clear();
            _cpuCaptured.Clear();
            _playerShakenMonths.Clear();
            _cpuShakenMonths.Clear();

            _goCount = 0;
            _cpuGoCount = 0;
            _playerBombCount = 0;
            _cpuBombCount = 0;

            _deck.Shuffle();

            for (int i = 0; i < 10; i++)
            {
                _playerHand.Add(_deck.Draw());
                _cpuHand.Add(_deck.Draw());
            }

            for (int i = 0; i < 8; i++)
            {
                _floorCards.Add(_deck.Draw());
            }

            HwatooCard card;
            while ((card = _deck.Draw()) != null)
            {
                _deckRemaining.Add(card);
            }
        }

        IEnumerator PlayerTurn(HwatooCard selected)
        {
            _waitingForCard = false;
            _isProcessing = true;

            var report = PlayTurn(selected, _playerHand, _playerCaptured, _cpuCaptured, true);
            RefreshAllViews();
            SetStatus(report);
            yield return new WaitForSeconds(0.75f);

            var score = CurrentPlayerScore(false);
            if (score.finalScore >= 3)
            {
                ShowGoStopPanel(score.finalScore);
                _isProcessing = false;
                yield break;
            }

            yield return CpuTurn();
            if (!_isPlaying) yield break;

            if (IsRoundOver())
            {
                ShowFinalResult();
            }
            else
            {
                _waitingForCard = true;
                _isProcessing = false;
                SetStatus("내 차례입니다. 낼 패를 선택하세요.");
            }
        }

        IEnumerator CpuTurn()
        {
            _isProcessing = true;
            yield return new WaitForSeconds(0.5f);

            var selected = PickCpuCard();
            if (selected == null)
            {
                ShowFinalResult();
                yield break;
            }

            var report = PlayTurn(selected, _cpuHand, _cpuCaptured, _playerCaptured, false);
            RefreshAllViews();
            SetStatus("상대: " + report);
            yield return new WaitForSeconds(0.75f);

            var cpuScore = CurrentCpuScore(false);
            if (cpuScore.finalScore >= 3)
            {
                _cpuGoCount = 0;
                ShowFinalResult();
            }
        }

        HwatooCard PickCpuCard()
        {
            var bomb = FindBombMonth(_cpuHand);
            if (bomb > 0) return _cpuHand.First(c => c.month == bomb);

            return _cpuHand.FirstOrDefault(c => _floorCards.Any(f => f.month == c.month))
                ?? _cpuHand.FirstOrDefault();
        }

        string PlayTurn(
            HwatooCard selected,
            List<HwatooCard> hand,
            List<HwatooCard> captured,
            List<HwatooCard> opponentCaptured,
            bool isPlayer)
        {
            AutoShake(hand, isPlayer);

            int month = selected.month;
            int bombMonth = FindBombMonthForCard(selected, hand);
            if (bombMonth > 0)
            {
                var bombCards = hand.Where(c => c.month == bombMonth).Take(3).ToList();
                var floorCard = _floorCards.First(c => c.month == bombMonth);
                foreach (var c in bombCards) hand.Remove(c);
                _floorCards.Remove(floorCard);
                captured.AddRange(bombCards);
                captured.Add(floorCard);

                if (isPlayer) _playerBombCount++;
                else _cpuBombCount++;

                StealOnePi(opponentCaptured, captured);
                CheckSweep(opponentCaptured, captured);
                return $"{bombMonth}월 폭탄, 4장을 먹었습니다.";
            }

            hand.Remove(selected);
            var firstMatches = _floorCards.Where(c => c.month == month).ToList();
            var drawn = DrawFromDeck();
            var gained = 0;
            var messages = new List<string> { $"{month}월 패를 냈습니다" };

            if (firstMatches.Count == 0)
            {
                _floorCards.Add(selected);

                if (drawn != null && drawn.month == selected.month)
                {
                    _floorCards.Remove(selected);
                    captured.Add(selected);
                    captured.Add(drawn);
                    gained += 2;
                    StealOnePi(opponentCaptured, captured);
                    messages.Add("쪽");
                }
                else
                {
                    gained += ResolveDrawnCard(drawn, captured, opponentCaptured);
                }
            }
            else if (firstMatches.Count == 1)
            {
                if (drawn != null && drawn.month == selected.month)
                {
                    _floorCards.Add(selected);
                    _floorCards.Add(drawn);
                    messages.Add("뻑");
                }
                else
                {
                    _floorCards.Remove(firstMatches[0]);
                    captured.Add(selected);
                    captured.Add(firstMatches[0]);
                    gained += 2;
                    gained += ResolveDrawnCard(drawn, captured, opponentCaptured);
                }
            }
            else if (firstMatches.Count == 2)
            {
                if (drawn != null && drawn.month == selected.month)
                {
                    foreach (var c in firstMatches) _floorCards.Remove(c);
                    captured.Add(selected);
                    captured.Add(drawn);
                    captured.AddRange(firstMatches);
                    gained += 4;
                    StealOnePi(opponentCaptured, captured);
                    messages.Add("따닥");
                }
                else
                {
                    var picked = firstMatches[0];
                    _floorCards.Remove(picked);
                    captured.Add(selected);
                    captured.Add(picked);
                    gained += 2;
                    messages.Add("같은 월 바닥패 1장을 자동 선택");
                    gained += ResolveDrawnCard(drawn, captured, opponentCaptured);
                }
            }
            else
            {
                foreach (var c in firstMatches) _floorCards.Remove(c);
                captured.Add(selected);
                captured.AddRange(firstMatches);
                gained += firstMatches.Count + 1;
                StealOnePi(opponentCaptured, captured);
                messages.Add("묶인 패를 모두 가져옴");
                gained += ResolveDrawnCard(drawn, captured, opponentCaptured);
            }

            if (gained > 0) CheckSweep(opponentCaptured, captured);
            return string.Join(", ", messages);
        }

        int ResolveDrawnCard(HwatooCard drawn, List<HwatooCard> captured, List<HwatooCard> opponentCaptured)
        {
            if (drawn == null) return 0;

            var matches = _floorCards.Where(c => c.month == drawn.month).ToList();
            if (matches.Count == 0)
            {
                _floorCards.Add(drawn);
                return 0;
            }

            if (matches.Count == 1 || matches.Count == 2)
            {
                var picked = matches[0];
                _floorCards.Remove(picked);
                captured.Add(drawn);
                captured.Add(picked);
                return 2;
            }

            foreach (var c in matches) _floorCards.Remove(c);
            captured.Add(drawn);
            captured.AddRange(matches);
            StealOnePi(opponentCaptured, captured);
            return matches.Count + 1;
        }

        HwatooCard DrawFromDeck()
        {
            if (_deckRemaining.Count == 0) return null;
            var card = _deckRemaining[0];
            _deckRemaining.RemoveAt(0);
            return card;
        }

        void AutoShake(List<HwatooCard> hand, bool isPlayer)
        {
            var shaken = isPlayer ? _playerShakenMonths : _cpuShakenMonths;
            foreach (var group in hand.GroupBy(c => c.month))
            {
                if (group.Count() >= 3 && shaken.Add(group.Key))
                {
                    SetStatus((isPlayer ? "나" : "상대") + $" {group.Key}월 흔들기");
                }
            }
        }

        int FindBombMonth(List<HwatooCard> hand)
        {
            foreach (var group in hand.GroupBy(c => c.month))
            {
                if (group.Count() >= 3 && _floorCards.Count(c => c.month == group.Key) == 1)
                    return group.Key;
            }
            return 0;
        }

        int FindBombMonthForCard(HwatooCard selected, List<HwatooCard> hand)
        {
            return hand.Count(c => c.month == selected.month) >= 3
                && _floorCards.Count(c => c.month == selected.month) == 1
                ? selected.month
                : 0;
        }

        void StealOnePi(List<HwatooCard> from, List<HwatooCard> to)
        {
            var pi = from.FirstOrDefault(c => c.cardType == CardType.Pi && !c.isDoublePi)
                ?? from.FirstOrDefault(c => c.cardType == CardType.Pi);
            if (pi == null) return;

            from.Remove(pi);
            to.Add(pi);
        }

        void CheckSweep(List<HwatooCard> opponentCaptured, List<HwatooCard> captured)
        {
            if (_floorCards.Count == 0)
            {
                StealOnePi(opponentCaptured, captured);
            }
        }

        GoStopScoreResult CurrentPlayerScore(bool final)
            => _scorer.CalculateDetailed(
                _playerCaptured,
                _cpuCaptured,
                _goCount,
                _playerShakenMonths.Count,
                _playerBombCount,
                _cpuGoCount,
                final);

        GoStopScoreResult CurrentCpuScore(bool final)
            => _scorer.CalculateDetailed(
                _cpuCaptured,
                _playerCaptured,
                _cpuGoCount,
                _cpuShakenMonths.Count,
                _cpuBombCount,
                _goCount,
                final);

        bool IsRoundOver()
        {
            return _playerHand.Count == 0 || _cpuHand.Count == 0 || _deckRemaining.Count == 0;
        }

        void ShowGoStopPanel(int score)
        {
            _waitingForCard = false;
            SetStatus($"{score}점입니다. 고 또는 스톱을 선택하세요.");

            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);

            _goStopCanvas = CreateCanvas("고스톱 선택 Canvas");
            MakePanel(_goStopCanvas.transform, new Vector2(0, 0), new Vector2(520, 260), new Color(0, 0, 0, 0.85f));
            MakeText(_goStopCanvas.transform, "ChoiceText", $"{score}점\n고 / 스톱 선택", 34, new Vector2(0, 60), new Vector2(480, 110));
            MakeButton(_goStopCanvas.transform, "고", new Vector2(-120, -55), new Vector2(170, 70), new Color(0.1f, 0.5f, 0.2f), OnGo);
            MakeButton(_goStopCanvas.transform, "스톱", new Vector2(120, -55), new Vector2(170, 70), new Color(0.65f, 0.15f, 0.12f), ShowFinalResult);
        }

        void OnGo()
        {
            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);
            _goStopCanvas = null;
            _goCount++;
            RefreshScoreText();
            StartCoroutine(ContinueAfterGo());
        }

        IEnumerator ContinueAfterGo()
        {
            SetStatus($"{_goCount}고입니다. 상대 차례입니다.");
            yield return CpuTurn();
            if (!_isPlaying) yield break;

            if (IsRoundOver())
            {
                ShowFinalResult();
                yield break;
            }

            _waitingForCard = true;
            _isProcessing = false;
            SetStatus("내 차례입니다. 낼 패를 선택하세요.");
        }

        void ShowFinalResult()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _waitingForCard = false;
            _isProcessing = false;

            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);
            _goStopCanvas = null;

            var player = CurrentPlayerScore(true);
            var cpu = CurrentCpuScore(true);
            bool playerWins = player.finalScore >= cpu.finalScore;
            _score = playerWins ? player.finalScore : 0;
            EndGame();

            if (_gameCanvas != null) Destroy(_gameCanvas.gameObject);
            _gameCanvas = CreateCanvas("GoStop Result Canvas");
            MakePanel(_gameCanvas.transform, Vector2.zero, new Vector2(760, 460), new Color(0, 0, 0, 0.88f));

            string title = playerWins ? "승리" : "패배";
            string body =
                $"{title}\n\n" +
                $"나: {player.finalScore}점 ({player.multiplierLabel})\n" +
                $"상대: {cpu.finalScore}점 ({cpu.multiplierLabel})\n\n" +
                $"고 {_goCount}, 흔들기 {_playerShakenMonths.Count}, 폭탄 {_playerBombCount}";

            MakeText(_gameCanvas.transform, "ResultText", body, 30, new Vector2(0, 45), new Vector2(700, 300));
            MakeButton(_gameCanvas.transform, "로비", new Vector2(0, -160), new Vector2(220, 70), new Color(0.18f, 0.36f, 0.65f), GoToLobbySafe);
        }

        void RefreshAllViews()
        {
            RefreshCardViews();
            RefreshScoreText();
        }

        void RefreshCardViews()
        {
            foreach (var view in _cardViews.Values)
            {
                if (view != null) Destroy(view);
            }
            _cardViews.Clear();

            if (_playerCapturedRoot != null) Destroy(_playerCapturedRoot.gameObject);
            if (_cpuCapturedRoot != null) Destroy(_cpuCapturedRoot.gameObject);
            _playerCapturedRoot = new GameObject("Player Captured").transform;
            _cpuCapturedRoot = new GameObject("CPU Captured").transform;
            _playerCapturedRoot.SetParent(_cardRoot, false);
            _cpuCapturedRoot.SetParent(_cardRoot, false);

            RenderRow(_playerHand, _tableCenter - _up * 1.25f, true, true);
            RenderRow(_cpuHand, _tableCenter + _up * 1.25f, false, false);
            RenderGrid(_floorCards, _tableCenter, true);
            RenderCaptured(_playerCaptured, _tableCenter - _up * 0.72f, _playerCapturedRoot);
            RenderCaptured(_cpuCaptured, _tableCenter + _up * 0.72f, _cpuCapturedRoot);
        }

        void RenderRow(List<HwatooCard> cards, Vector3 center, bool faceUp, bool interactive)
        {
            float start = -(cards.Count - 1) * GapX * 0.5f;
            for (int i = 0; i < cards.Count; i++)
            {
                var pos = center + _right * (start + i * GapX);
                CreateCardView(cards[i], pos, faceUp, interactive);
            }
        }

        void RenderGrid(List<HwatooCard> cards, Vector3 center, bool faceUp)
        {
            int columns = Mathf.Min(8, Mathf.Max(1, cards.Count));
            for (int i = 0; i < cards.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                float x = (col - (columns - 1) * 0.5f) * GapX;
                float y = (0.5f - row) * GapY;
                CreateCardView(cards[i], center + _right * x + _up * y, faceUp, false);
            }
        }

        void RenderCaptured(List<HwatooCard> cards, Vector3 center, Transform parent)
        {
            var ordered = cards.OrderBy(c => c.cardType).ThenBy(c => c.month).ThenBy(c => c.index).ToList();
            int columns = 16;
            for (int i = 0; i < ordered.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                float x = (col - (columns - 1) * 0.5f) * (GapX * 0.55f);
                float y = -row * (GapY * 0.45f);
                var view = CreateCardView(ordered[i], center + _right * x + _up * y, true, false, 0.58f);
                view.transform.SetParent(parent, true);
            }
        }

        GameObject CreateCardView(HwatooCard card, Vector3 position, bool faceUp, bool interactive, float scale = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = faceUp ? $"card_{card.index:D2}_{card.cardName}" : "card_back";
            go.transform.SetParent(_cardRoot, true);
            go.transform.position = position;
            go.transform.rotation = _cardRotation;
            go.transform.localScale = new Vector3(CardW * scale, CardH * scale, 1f);

            var mat = new Material(Shader.Find("Sprites/Default"));
            if (faceUp)
            {
                mat.mainTexture = GetCardTexture(card.index);
                mat.color = Color.white;
            }
            else
            {
                mat.color = new Color(0.08f, 0.18f, 0.42f);
            }
            go.GetComponent<MeshRenderer>().material = mat;

            if (interactive)
            {
                var marker = go.AddComponent<HandCardMarker>();
                marker.Card = card;
            }

            _cardViews[card] = go;
            return go;
        }

        Texture2D GetCardTexture(int index)
        {
            if (_textureCache.TryGetValue(index, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>($"GoStop/card_{index:D2}");
            var texture = sprite != null ? sprite.texture : Texture2D.whiteTexture;
            _textureCache[index] = texture;
            return texture;
        }

        void BuildGameUI()
        {
            if (_gameCanvas != null) Destroy(_gameCanvas.gameObject);
            _gameCanvas = CreateCanvas("고스톱 UI Canvas");

            _statusText = MakeText(_gameCanvas.transform, "Status", "고스톱", 28, new Vector2(0, 250), new Vector2(900, 60));
            _scoreText = MakeText(_gameCanvas.transform, "Score", "", 20, new Vector2(-405, 145), new Vector2(340, 150));
            _deckText = MakeText(_gameCanvas.transform, "Deck", "", 22, new Vector2(405, 165), new Vector2(320, 110));
            MakeButton(_gameCanvas.transform, "로비", new Vector2(505, -275), new Vector2(140, 50), new Color(0.16f, 0.28f, 0.45f), GoToLobbySafe);
        }

        Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1280, 720);
            rt.localScale = Vector3.one * 0.0018f;
            XRUIUtility.ConfigureWorldCanvas(go, canvas);
            XRUIUtility.PlaceCanvasInFront(canvas, 2.15f, 1.45f);
            return canvas;
        }

        void RefreshScoreText()
        {
            if (_scorer == null) return;

            var player = CurrentPlayerScore(false);
            _score = player.finalScore;
            int gwangCount = _playerCaptured.Count(c => c.cardType == CardType.Gwang);
            int yulCount = _playerCaptured.Count(c => c.cardType == CardType.Yul);
            int ttiCount = _playerCaptured.Count(c => c.cardType == CardType.Tti);
            int piCount = CountPiForDisplay(_playerCaptured);

            if (_scoreText != null)
            {
                _scoreText.text =
                    $"내 점수 {player.finalScore}점\n" +
                    $"광 {gwangCount}  열끗 {yulCount}\n" +
                    $"띠 {ttiCount}  피 {piCount}\n" +
                    $"고 {_goCount}  흔들기 {_playerShakenMonths.Count}  폭탄 {_playerBombCount}";
            }

            if (_deckText != null)
            {
                _deckText.text =
                    $"더미 {_deckRemaining.Count}장\n" +
                    $"바닥 {_floorCards.Count}장\n" +
                    $"내가 먹은 패 {_playerCaptured.Count}장";
            }
        }

        static int CountPiForDisplay(List<HwatooCard> cards)
        {
            return cards.Where(c => c.cardType == CardType.Pi).Sum(c => c.isDoublePi ? 2 : 1);
        }

        void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
        }

        static void MakePanel(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
        }

        static Text MakeText(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var uiText = go.GetComponent<Text>();
            uiText.text = text;
            uiText.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
            uiText.fontSize = fontSize;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        static void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var text = MakeText(go.transform, "Label", label, 26, Vector2.zero, size);
            text.fontStyle = FontStyle.Bold;

            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        void ClearCardViews()
        {
            if (_cardRoot != null) Destroy(_cardRoot.gameObject);
            _cardViews.Clear();
        }

        void GoToLobbySafe()
        {
            if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadScene(lobbySceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }

    public class HandCardMarker : MonoBehaviour
    {
        public HwatooCard Card;
    }
}
