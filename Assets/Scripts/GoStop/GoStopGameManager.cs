// Assets/Scripts/GoStop/GoStopGameManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SilverCare.Common;

namespace SilverCare.GoStop
{
    public class GoStopGameManager : BaseGameManager
    {
        const float CardW = 0.38f;
        const float CardH = 0.54f;
        const float GapX = 0.52f;
        const float GapY = 0.78f;
        const float TableDistance = 3.0f;
        const float CardLift = 0.028f;
        const float CardHoverLift = 0.075f;
        const float CardHoverScale = 1.14f;
        const float TableLift = 0.30f;             // 테이블을 마루 위로 들어올려 다리가 보이게
        const float GoStopTableTopY = 0.286f + TableLift;

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
        Transform _environmentRoot;
        float _parkGroundY; // 공원 잔디 윗면 월드 높이 — 배경 나무를 여기에 스냅
        Transform _playerCapturedRoot;
        Transform _cpuCapturedRoot;
        Canvas _gameCanvas;
        Canvas _goStopCanvas;
        Text _statusText;
        Text _scoreText;
        Text _deckText;

        Vector3 _tableCenter;
        Vector3 _right;
        Vector3 _forwardOnTable;
        Quaternion _cardRotation;

        int _goCount;
        int _cpuGoCount;
        int _playerBombCount;
        int _cpuBombCount;
        int _lastGoMeldScore; // 마지막으로 고/스톱을 물은 카드점수 — 점수가 오를 때만 다시 묻기
        int _cpuLastGoMeld;   // CPU의 마지막 고 시점 카드점수
        bool _grantExtraTurn; // 폭탄 등으로 한 번 더 두는 턴
        HwatooCard _lastDrawnCard; // 직전에 더미에서 뒤집은 패(애니메이션용)
        string _lastTurnEvent;     // 직전 턴 특수 이벤트(쪽/뻑/따닥/폭탄/쓸) — 팝업 연출용
        bool _waitingForCard;
        bool _isProcessing;
        HwatooCard _hoveredCard;
        Transform _hoveredCardView;
        Vector3 _hoveredOriginalPosition;
        Vector3 _hoveredOriginalScale;

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
            if (!_isPlaying || !_waitingForCard || _isProcessing)
            {
                ClearCardHover();
                return;
            }

            UpdateCardHover();

            HandCardMarker marker = null;
            if (XRPointerInput.TryGetSelectionHit(100f, out var hit))
                marker = FindHandCardMarker(hit.collider);

            if (marker == null)
                marker = TryFindHandCardFromAllHits(100f);

            if (marker != null && marker.Card != null && _playerHand.Contains(marker.Card))
            {
                StartCoroutine(PlayerTurn(marker.Card));
            }
        }

        void UpdateCardHover()
        {
            var marker = TryFindHandCardUnderPointer(100f, requirePress: false);
            if (marker == null || marker.Card == null || !_playerHand.Contains(marker.Card))
            {
                ClearCardHover();
                return;
            }

            ApplyCardHover(marker.Card);
        }

        void ApplyCardHover(HwatooCard card)
        {
            if (_hoveredCard == card && _hoveredCardView != null)
                return;

            ClearCardHover();

            if (!_cardViews.TryGetValue(card, out GameObject view) || view == null)
                return;

            _hoveredCard = card;
            _hoveredCardView = view.transform;
            _hoveredOriginalPosition = _hoveredCardView.position;
            _hoveredOriginalScale = _hoveredCardView.localScale;
            _hoveredCardView.position = _hoveredOriginalPosition + Vector3.up * CardHoverLift;
            _hoveredCardView.localScale = _hoveredOriginalScale * CardHoverScale;
        }

        void ClearCardHover()
        {
            if (_hoveredCardView != null)
            {
                _hoveredCardView.position = _hoveredOriginalPosition;
                _hoveredCardView.localScale = _hoveredOriginalScale;
            }

            _hoveredCard = null;
            _hoveredCardView = null;
        }

        HandCardMarker FindHandCardMarker(Collider collider)
        {
            if (collider == null)
                return null;

            var marker = collider.GetComponentInParent<HandCardMarker>();
            if (marker != null)
                return marker;

            return collider.GetComponentInChildren<HandCardMarker>();
        }

        HandCardMarker TryFindHandCardFromAllHits(float maxDistance)
        {
            return TryFindHandCardUnderPointer(maxDistance, requirePress: true);
        }

        HandCardMarker TryFindHandCardUnderPointer(float maxDistance, bool requirePress)
        {
            if (requirePress && !XRPointerInput.WasSelectionPressedThisFrame())
                return null;

            if (!TryBuildSelectionRay(out Ray ray, requirePress))
                return null;

            var hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var candidate in hits)
            {
                var marker = FindHandCardMarker(candidate.collider);
                if (marker != null && marker.Card != null && _playerHand.Contains(marker.Card))
                    return marker;
            }

            return null;
        }

        bool TryBuildSelectionRay(out Ray ray, bool preferPressedMouse)
        {
            if (preferPressedMouse &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                Camera.main != null)
            {
                ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                return true;
            }

            if (XRPointerInput.TryGetPointerRay(out ray))
                return true;

            if (Mouse.current != null && Camera.main != null)
            {
                ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                return true;
            }

            return false;
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

            XRUIUtility.RefreshSceneViewAnchor();
            XRUIUtility.GetSceneViewAnchor(cam, out Vector3 anchorPosition, out Vector3 anchorForward);
            anchorForward.y = 0f;
            if (anchorForward.sqrMagnitude < 0.001f)
                anchorForward = Vector3.forward;
            anchorForward.Normalize();

            _tableCenter = anchorPosition + anchorForward * TableDistance;
            _right = Vector3.Cross(Vector3.up, anchorForward).normalized;
            _forwardOnTable = anchorForward;
            _cardRotation = Quaternion.LookRotation(Vector3.up, anchorForward);

            ClearCardViews();
            _cardRoot = new GameObject("GoStop Cards").transform;
            BuildRoomEnvironment(anchorPosition, anchorForward);
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
            _lastGoMeldScore = 0;
            _cpuLastGoMeld = 0;
            _grantExtraTurn = false;
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

            // 낸 패가 바닥 중앙으로 이동하는 연출
            yield return AnimatePlayCard(selected, _tableCenter - _forwardOnTable * 1.40f);
            var report = PlayTurn(selected, _playerHand, _playerCaptured, _cpuCaptured, true);
            yield return AnimateDrawCard(); // 더미에서 뒤집은 패 연출
            RefreshAllViews();
            PlayEventPopup();               // 쪽/뻑/따닥/폭탄/쓸 이펙트
            SetStatus(report);
            yield return new WaitForSeconds(0.65f);

            // 카드점수(meldScore)가 3점 이상이고 직전 고/스톱 때보다 올랐을 때만 다시 묻는다.
            var score = CurrentPlayerScore(false);
            if (score.meldScore >= 3 && score.meldScore > _lastGoMeldScore)
            {
                ShowGoStopPanel(score.finalScore);
                _isProcessing = false;
                yield break;
            }

            // 폭탄 등으로 한 번 더 두는 경우 — CPU에게 넘기지 않고 다시 내 차례
            if (_grantExtraTurn && _playerHand.Count > 0)
            {
                _waitingForCard = true;
                _isProcessing = false;
                SetStatus("폭탄! 한 번 더 낼 패를 선택하세요.");
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

            // 폭탄으로 인한 추가 턴을 위해 반복
            do
            {
                yield return new WaitForSeconds(0.5f);

                var selected = PickCpuCard();
                if (selected == null)
                {
                    ShowFinalResult();
                    yield break;
                }

                yield return AnimatePlayCard(selected, _tableCenter + _forwardOnTable * 1.40f);
                var report = PlayTurn(selected, _cpuHand, _cpuCaptured, _playerCaptured, false);
                yield return AnimateDrawCard();
                RefreshAllViews();
                PlayEventPopup();
                SetStatus("상대: " + report);
                yield return new WaitForSeconds(0.65f);

                var cpuScore = CurrentCpuScore(false);
                if (cpuScore.meldScore >= 3 && cpuScore.meldScore > _cpuLastGoMeld)
                {
                    if (CpuShouldStop(cpuScore))
                    {
                        ShowFinalResult(false);
                        yield break;
                    }
                    _cpuGoCount++;
                    _cpuLastGoMeld = cpuScore.meldScore;
                    SetStatus($"상대: {_cpuGoCount}고!");
                    TTSManager.Instance?.SpeakClip("gostop_go");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            while (_grantExtraTurn && _cpuHand.Count > 0 && !IsRoundOver());
        }

        // CPU 고/스톱 판단 — 점수가 충분하거나 남은 패가 적으면 스톱, 아니면 고.
        bool CpuShouldStop(GoStopScoreResult score)
        {
            if (score.meldScore >= 7) return true;       // 충분히 높음
            if (_deckRemaining.Count <= 8) return true;  // 남은 패 적음 — 안전하게 멈춤
            if (_cpuHand.Count <= 2) return true;        // 손패 거의 소진
            return false;                                // 그 외엔 고
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
            _grantExtraTurn = false;
            _lastDrawnCard = null;
            _lastTurnEvent = null;

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

                // 폭탄은 한 번 더 두는 턴 + 더미에서 한 장 뒤집어 처리
                _grantExtraTurn = true;
                var bombDrawn = DrawFromDeck();
                _lastDrawnCard = bombDrawn;
                ResolveDrawnCard(bombDrawn, captured, opponentCaptured);
                CheckSweep(opponentCaptured, captured);
                _lastTurnEvent = "폭탄";
                return $"{bombMonth}월 폭탄! 4장을 먹고 한 번 더 둡니다.";
            }

            hand.Remove(selected);
            var firstMatches = _floorCards.Where(c => c.month == month).ToList();
            var drawn = DrawFromDeck();
            _lastDrawnCard = drawn;
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
                    _lastTurnEvent = "쪽";
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
                    _lastTurnEvent = "뻑";
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
                    _lastTurnEvent = "따닥";
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
                _lastTurnEvent = "쓸"; // 바닥을 모두 쓸어담음
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
            MakeButton(_goStopCanvas.transform, "스톱", new Vector2(120, -55), new Vector2(170, 70), new Color(0.65f, 0.15f, 0.12f), OnStop);
        }

        void OnGo()
        {
            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);
            _goStopCanvas = null;
            TTSManager.Instance?.SpeakClip("gostop_go");
            _goCount++;
            _lastGoMeldScore = CurrentPlayerScore(false).meldScore; // 이 점수보다 올라야 다음 고/스톱
            RefreshScoreText();
            StartCoroutine(ContinueAfterGo());
        }

        void OnStop()
        {
            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);
            _goStopCanvas = null;
            TTSManager.Instance?.SpeakClip("gostop_stop");
            ShowFinalResult(true);
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

        void ShowFinalResult(bool? playerStopped = null)
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _waitingForCard = false;
            _isProcessing = false;

            if (_goStopCanvas != null) Destroy(_goStopCanvas.gameObject);
            _goStopCanvas = null;

            var player = CurrentPlayerScore(true);
            var cpu = CurrentCpuScore(true);

            // 나가리 — 라운드가 패 소진으로 끝났는데 양쪽 다 3점 미만이면 무승부.
            bool nagari = playerStopped == null && player.finalScore < 3 && cpu.finalScore < 3;
            bool playerWins = !nagari && (playerStopped ?? player.finalScore >= cpu.finalScore);
            _score = playerWins ? player.finalScore : 0;
            EndGame();

            bool firstClear = playerWins &&
                              StoryProgressManager.Instance != null &&
                              StoryProgressManager.Instance.TryMarkActivityCleared(
                                  StoryProgressManager.StoryActivity.GoStop);
            if (firstClear)
                StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.StoryActivity.GoStop);

            if (_gameCanvas != null) Destroy(_gameCanvas.gameObject);
            _gameCanvas = CreateCanvas("GoStop Result Canvas");
            MakePanel(_gameCanvas.transform, Vector2.zero, new Vector2(760, 460), new Color(0, 0, 0, 0.88f));

            string title = nagari ? "무승부 (나가리)" : playerWins ? "승리" : "패배";
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
            ClearCardHover();

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

            RenderRow(_playerHand, _tableCenter - _forwardOnTable * 1.40f, true, true);
            RenderRow(_cpuHand, _tableCenter + _forwardOnTable * 1.40f, false, false);
            RenderFloorAndDeck(_floorCards, _tableCenter);
            // 내가 먹은 패는 왼쪽, 상대가 먹은 패는 오른쪽
            RenderCaptured(_playerCaptured, _tableCenter + _right * 2.25f - _forwardOnTable * 0.55f, _playerCapturedRoot);
            RenderCaptured(_cpuCaptured, _tableCenter - _right * 2.25f + _forwardOnTable * 0.55f, _cpuCapturedRoot);
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

        // 클래식 고스톱 배치 — 중앙에 더미(스톡), 그 둘레로 바닥패를 격자로 깔아 겹치지 않게.
        void RenderFloorAndDeck(List<HwatooCard> cards, Vector3 center)
        {
            const float floorGapX = 0.56f;
            const float floorGapZ = 0.72f;

            // 바닥패 + 더미(1칸)를 담을 가장 정사각형에 가까운 홀수×홀수 격자 → 중앙칸이 생김.
            int total = cards.Count + 1;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(total));
            if (columns % 2 == 0) columns++;
            int rows = Mathf.CeilToInt(total / (float)columns);
            if (rows % 2 == 0) rows++;
            int centerCell = (rows / 2) * columns + (columns / 2);

            int ci = 0;
            for (int cell = 0; cell < rows * columns; cell++)
            {
                int row = cell / columns;
                int col = cell % columns;
                float x = (col - (columns - 1) * 0.5f) * floorGapX;
                float z = ((rows - 1) * 0.5f - row) * floorGapZ;
                Vector3 pos = center + _right * x + _forwardOnTable * z;

                if (cell == centerCell)
                {
                    RenderDeckPile(pos);
                    continue;
                }
                if (ci < cards.Count)
                    CreateCardView(cards[ci++], pos, true, false);
            }
        }

        // 중앙 더미 — 남은 패 수만큼 살짝 쌓아 입체감 있는 뒷면 더미로 표시.
        void RenderDeckPile(Vector3 pos)
        {
            int layers = Mathf.Clamp(_deckRemaining.Count, 1, 6);
            for (int i = 0; i < layers; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "deck_pile";
                go.transform.SetParent(_cardRoot, true);
                // 살짝씩 어긋나게 쌓아 더미 느낌
                Vector3 jitter = _right * (i * 0.006f) + _forwardOnTable * (i * 0.004f);
                go.transform.position = pos + jitter + Vector3.up * (CardLift + i * 0.006f);
                go.transform.rotation = _cardRotation;
                go.transform.localScale = new Vector3(CardW, CardH, 1f);

                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.66f, 0.10f, 0.10f); // 화투 뒷면 적색
                go.GetComponent<MeshRenderer>().material = mat;

                var mc = go.GetComponent<MeshCollider>();
                if (mc != null) Destroy(mc);
            }
        }

        // 낸 패가 손에서 바닥으로 살짝 회전하며 날아가 '톡' 내려놓이는 연출.
        IEnumerator AnimatePlayCard(HwatooCard card, Vector3 fallbackFrom)
        {
            bool hasView = _cardViews.TryGetValue(card, out var v) && v != null;
            Vector3 from = hasView ? v.transform.position : fallbackFrom;
            if (hasView) v.SetActive(false); // 원본 손패 숨겨 이중으로 보이지 않게(곧 RefreshAllViews로 갱신)
            Vector3 to = _tableCenter - _right * 0.5f + Vector3.up * 0.02f;
            float spin = Random.Range(-14f, 14f); // 매번 살짝 다른 각도로 던져 단조로움 완화
            yield return MoveGhostCard(card.index, true, from, to, 0.34f, spin);
        }

        // 더미에서 한 장 뽑아 '뒤집으며' 바닥으로 내려놓는 리빌 연출.
        IEnumerator AnimateDrawCard()
        {
            if (_lastDrawnCard == null)
                yield break;
            Vector3 from = _tableCenter + Vector3.up * 0.05f;     // 중앙 더미 위
            Vector3 to = _tableCenter + _right * 0.5f + Vector3.up * 0.02f;
            yield return FlipRevealCard(_lastDrawnCard.index, from, to, 0.42f);
        }

        // 임시 카드를 from→to로 띄워 이동(오버슈트 이징 + 도착 팝 + 회전).
        IEnumerator MoveGhostCard(int cardIndex, bool faceUp, Vector3 from, Vector3 to, float duration, float spin = 0f)
        {
            var go = MakeGhost(cardIndex, faceUp, out _);
            Quaternion baseRot = _cardRotation;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = EaseOutBack(k);                         // 살짝 지나쳤다 안착
                float arc = Mathf.Sin(k * Mathf.PI) * 0.16f;       // 떠오르는 포물선
                float pop = 1f + Mathf.Sin(k * Mathf.PI) * 0.14f;  // 가운데서 살짝 커졌다 작아짐
                go.transform.position = Vector3.Lerp(from, to, e) + Vector3.up * (CardHoverLift + arc);
                go.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, spin * (1f - k));
                go.transform.localScale = new Vector3(CardW * 1.08f * pop, CardH * 1.08f * pop, 1f);
                yield return null;
            }
            Destroy(go);
        }

        // 뒷면 → (가장자리에서 뒤집힘) → 앞면으로 리빌하며 이동.
        IEnumerator FlipRevealCard(int cardIndex, Vector3 from, Vector3 to, float duration)
        {
            var go = MakeGhost(cardIndex, false, out var rend); // 시작은 뒷면
            bool revealed = false;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float move = Mathf.SmoothStep(0f, 1f, k);
                float arc = Mathf.Sin(k * Mathf.PI) * 0.18f;
                go.transform.position = Vector3.Lerp(from, to, move) + Vector3.up * (CardHoverLift + arc);
                go.transform.rotation = _cardRotation;

                // 가로 스케일을 1→0(중간, 가장자리)→1로 줘 카드 뒤집힘 착시. 중간에 앞면으로 교체.
                float flip = Mathf.Abs(Mathf.Cos(k * Mathf.PI));
                if (!revealed && k >= 0.5f)
                {
                    rend.material.mainTexture = GetCardTexture(cardIndex);
                    rend.material.color = Color.white;
                    revealed = true;
                }
                go.transform.localScale = new Vector3(CardW * 1.1f * Mathf.Max(0.04f, flip), CardH * 1.1f, 1f);
                yield return null;
            }
            Destroy(go);
        }

        GameObject MakeGhost(int cardIndex, bool faceUp, out MeshRenderer rend)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "card_anim";
            go.transform.SetParent(_cardRoot, true);
            go.transform.rotation = _cardRotation;
            var mat = new Material(Shader.Find("Sprites/Default"));
            if (faceUp) { mat.mainTexture = GetCardTexture(cardIndex); mat.color = Color.white; }
            else mat.color = new Color(0.66f, 0.10f, 0.10f);
            rend = go.GetComponent<MeshRenderer>();
            rend.material = mat;
            var mc = go.GetComponent<MeshCollider>();
            if (mc != null) Destroy(mc);
            return go;
        }

        static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = k - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        // 쪽/뻑/따닥/폭탄/쓸 등의 특수 이벤트를 화면 중앙에 '팡' 띄웠다 사라지게.
        IEnumerator ShowEventPopup(string text, Color color)
        {
            if (_gameCanvas == null) yield break;

            var go = new GameObject("EventPopup", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(_gameCanvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(600f, 200f);

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = GetFont();
            label.fontSize = 96;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            go.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            float t = 0f;
            const float dur = 0.95f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float scale = k < 0.35f
                    ? Mathf.Lerp(0.3f, 1.25f, EaseOutBack(k / 0.35f))  // 팡 튀어나옴
                    : Mathf.Lerp(1.25f, 1.0f, (k - 0.35f) / 0.65f);     // 살짝 가라앉음
                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = new Vector2(0f, 40f + k * 60f);     // 위로 떠오름
                var c = label.color; c.a = k > 0.7f ? Mathf.Lerp(1f, 0f, (k - 0.7f) / 0.3f) : 1f;
                label.color = c;
                yield return null;
            }
            Destroy(go);
        }

        void PlayEventPopup()
        {
            if (string.IsNullOrEmpty(_lastTurnEvent))
                return;
            Color c = _lastTurnEvent switch
            {
                "쪽"   => new Color(0.45f, 0.92f, 1f),
                "뻑"   => new Color(1f, 0.42f, 0.40f),
                "따닥" => new Color(1f, 0.66f, 0.20f),
                "폭탄" => new Color(1f, 0.86f, 0.20f),
                "쓸"   => new Color(0.55f, 1f, 0.55f),
                _       => Color.white
            };
            StartCoroutine(ShowEventPopup(_lastTurnEvent + "!", c));
        }

        void RenderCaptured(List<HwatooCard> cards, Vector3 center, Transform parent)
        {
            var ordered = cards.OrderBy(c => c.cardType).ThenBy(c => c.month).ThenBy(c => c.index).ToList();
            const int columns = 4;            // 측면에 모아두는 컴팩트한 더미
            const float capScale = 0.5f;
            for (int i = 0; i < ordered.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int rowCount = Mathf.Min(columns, ordered.Count - row * columns);
                float x = (col - (rowCount - 1) * 0.5f) * (GapX * capScale);
                float z = -row * (GapY * capScale * 0.7f);
                var view = CreateCardView(ordered[i], center + _right * x + _forwardOnTable * z, true, false, capScale);
                view.transform.SetParent(parent, true);
            }
        }

        GameObject CreateCardView(HwatooCard card, Vector3 position, bool faceUp, bool interactive, float scale = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = faceUp ? $"card_{card.index:D2}_{card.cardName}" : "card_back";
            go.transform.SetParent(_cardRoot, true);
            go.transform.position = position + Vector3.up * CardLift;
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

            var meshCollider = go.GetComponent<MeshCollider>();
            if (meshCollider != null)
                Destroy(meshCollider);

            if (interactive)
            {
                var marker = go.AddComponent<HandCardMarker>();
                marker.Card = card;
                AddCardSelectionHitbox(go.transform, card);
            }

            _cardViews[card] = go;
            return go;
        }

        void AddCardSelectionHitbox(Transform cardTransform, HwatooCard card)
        {
            var hitbox = new GameObject("CardSelectionHitbox");
            hitbox.transform.SetParent(cardTransform, false);
            hitbox.transform.localPosition = new Vector3(0f, 0f, 0.045f);
            hitbox.transform.localRotation = Quaternion.identity;
            hitbox.transform.localScale = Vector3.one;

            var collider = hitbox.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.18f, 1.18f, 0.10f);
            collider.isTrigger = false;

            var marker = hitbox.AddComponent<HandCardMarker>();
            marker.Card = card;
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

            Color panelBg = new Color(0.08f, 0.10f, 0.14f, 0.92f);
            Color accent  = new Color(0.95f, 0.78f, 0.25f);

            // ── 상단 상태 배너 ──────────────────────────────
            MakeStyledPanel(_gameCanvas.transform, new Vector2(0, 285), new Vector2(760, 78), new Color(0.07f, 0.09f, 0.13f, 0.94f), accent);
            _statusText = MakeText(_gameCanvas.transform, "Status", "고스톱", 30, new Vector2(0, 285), new Vector2(720, 60));

            // ── 좌측 내 점수판 ──────────────────────────────
            MakeStyledPanel(_gameCanvas.transform, new Vector2(-450, 120), new Vector2(360, 230), panelBg, accent);
            MakeLabelStrip(_gameCanvas.transform, new Vector2(-450, 210), new Vector2(360, 44), accent, "내 점수");
            _scoreText = MakeText(_gameCanvas.transform, "Score", "", 22, new Vector2(-450, 95), new Vector2(330, 165));
            _scoreText.alignment = TextAnchor.UpperLeft;

            // ── 우측 패 현황판 ──────────────────────────────
            MakeStyledPanel(_gameCanvas.transform, new Vector2(450, 140), new Vector2(340, 190), panelBg, accent);
            MakeLabelStrip(_gameCanvas.transform, new Vector2(450, 210), new Vector2(340, 44), new Color(0.30f, 0.62f, 0.40f), "패 현황");
            _deckText = MakeText(_gameCanvas.transform, "Deck", "", 22, new Vector2(450, 120), new Vector2(310, 130));
            _deckText.alignment = TextAnchor.UpperLeft;

            MakeButton(_gameCanvas.transform, "로비로 나가기", new Vector2(530, 272), new Vector2(185, 65), new Color(0.50f, 0.18f, 0.14f), GoToLobbySafe);
        }

        // 둥근 느낌의 패널 + 외곽 강조선.
        static void MakeStyledPanel(Transform parent, Vector2 pos, Vector2 size, Color bg, Color border)
        {
            // 외곽선(살짝 큰 패널)
            var outline = new GameObject("PanelBorder", typeof(RectTransform), typeof(Image));
            outline.transform.SetParent(parent, false);
            var ort = outline.GetComponent<RectTransform>();
            ort.anchoredPosition = pos;
            ort.sizeDelta = size + new Vector2(6f, 6f);
            outline.GetComponent<Image>().color = new Color(border.r, border.g, border.b, 0.55f);

            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = bg;
        }

        // 패널 상단 제목 띠.
        static void MakeLabelStrip(Transform parent, Vector2 pos, Vector2 size, Color color, string label)
        {
            var go = new GameObject("LabelStrip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.9f);

            var text = MakeText(go.transform, "Label", label, 24, Vector2.zero, size);
            text.color = new Color(0.10f, 0.10f, 0.12f);
            text.fontStyle = FontStyle.Bold;
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

        static Font _koreanFont;
        static Font GetFont()
        {
            if (_koreanFont == null)
                _koreanFont = Resources.Load<Font>("Fonts/Paperlogy-5Medium");
            return _koreanFont != null ? _koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            uiText.font = GetFont();
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

            var button = go.GetComponent<Button>();
            if (button.GetComponent<XRButtonHoverFeedback>() == null)
                button.gameObject.AddComponent<XRButtonHoverFeedback>();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        void ClearCardViews()
        {
            if (_cardRoot != null) { Destroy(_cardRoot.gameObject); _cardRoot = null; }
            if (_environmentRoot != null) { Destroy(_environmentRoot.gameObject); _environmentRoot = null; }
            _cardViews.Clear();
        }

        void BuildRoomEnvironment(Vector3 anchorPosition, Vector3 anchorForward)
        {
            if (_environmentRoot != null)
                Destroy(_environmentRoot.gameObject);

            _environmentRoot = new GameObject("GoStop Environment").transform;
            Vector3 forward = anchorForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            // 환경 루트 Y를 월드 바닥(0)에 고정: 카메라 높이 기반으로 계산하면 테이블이 눈높이 근처에 떠버림
            _environmentRoot.position = new Vector3(_tableCenter.x, 0f, _tableCenter.z);
            _environmentRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // ── 1. 공원 배경 (바둑/사활과 동일) — 잔디 윗면을 마루 바닥(local y=0)에 정합 ──
            Quaternion roomRot = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 envPos = _environmentRoot.position;
            Vector3 parkCenter = new Vector3(envPos.x, 0f, envPos.z) + forward * 2.0f;
            _parkGroundY = envPos.y;
            Baduk.BadukRoomEnvironment.SpawnOutdoorBackground(_environmentRoot.gameObject, parkCenter, roomRot, envPos.y);
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.30f, 0.24f, 0.16f); // 저녁 따뜻한 앰비언트
            BuildParkSurround(); // 사방으로 넓은 들판 + 임포트 나무 — 돌아봐도 하늘색 바탕 안 보이게

            // ── 2. 한옥 대청마루 플랫폼 ──────────────────────────────────
            // 두꺼운 나무 기단 — 카메라 높이와 무관하게 항상 공원 지면까지 닿음
            Material parquet = Resources.Load<Material>("GoStopRoom/Parquet_1024");
            Material woodMat  = CreateWoodMaterial(new Color(0.40f, 0.24f, 0.10f), new Color(0.60f, 0.38f, 0.18f));
            Color woodDark    = new Color(0.30f, 0.17f, 0.07f);
            Color wallColor   = new Color(0.82f, 0.74f, 0.60f);

            CreateRoomPart("MaruBase",    new Vector3(0f, -1.0f, 0.2f),  new Vector3(9.2f, 2.0f, 8.6f), new Color(0.42f, 0.28f, 0.14f), woodMat);
            CreateRoomPart("MaruSurface", new Vector3(0f,  0.02f, 0.2f), new Vector3(8.8f, 0.04f, 8.2f), new Color(0.56f, 0.40f, 0.22f), parquet, false);

            // ── 3. 한옥 기둥 + 인방(보) — 3면 개방형 구조 ───────────────
            // 기둥 6개: 좌우 앞/뒤/중간
            float px = 3.9f, pzF = -3.6f, pzB = 3.4f, pzM = 0.3f;
            foreach (var lp in new[] {
                new Vector3(-px, 1.22f, pzF), new Vector3(px, 1.22f, pzF),
                new Vector3(-px, 1.22f, pzM), new Vector3(px, 1.22f, pzM),
                new Vector3(-px, 1.22f, pzB), new Vector3(px, 1.22f, pzB),
            })
                CreateRoomPart("Pillar", lp, new Vector3(0.14f, 2.44f, 0.14f), woodDark, woodMat, false);

            // 인방(상단 가로 보)
            CreateRoomPart("BeamLeft",  new Vector3(-px, 2.46f, 0.2f),  new Vector3(0.14f, 0.14f, 7.8f), woodDark, woodMat, false);
            CreateRoomPart("BeamRight", new Vector3( px, 2.46f, 0.2f),  new Vector3(0.14f, 0.14f, 7.8f), woodDark, woodMat, false);
            CreateRoomPart("BeamBack",  new Vector3(0f, 2.46f, pzB),    new Vector3(8.0f, 0.14f, 0.14f), woodDark, woodMat, false);
            CreateRoomPart("BeamFront", new Vector3(0f, 2.46f, pzF),    new Vector3(8.0f, 0.14f, 0.14f), woodDark, woodMat, false);

            // ── 천장 — 한옥 서까래 천장(막힘) : 뚫린 하늘 가림 ─────────────
            CreateRoomPart("Ceiling", new Vector3(0f, 2.66f, 0.2f), new Vector3(8.4f, 0.12f, 8.4f), new Color(0.34f, 0.22f, 0.11f), woodMat, false);
            // 서까래(세로 보) 디테일
            for (int i = -4; i <= 4; i++)
                CreateRoomPart("Rafter", new Vector3(i * 0.85f, 2.56f, 0.2f), new Vector3(0.06f, 0.06f, 8.0f), woodDark, woodMat, false);

            // ── 4. 뒷벽 — 한옥 미닫이 창호(살창) + 공원이 비치는 느낌 ──
            // 아랫단 낮은 벽
            CreateRoomPart("BackWallLow", new Vector3(0f, 0.55f, pzB - 0.05f), new Vector3(8.6f, 1.1f, 0.12f), wallColor, null, false);
            // 창호 틀 (위쪽 살창 구역)
            CreateRoomPart("ShojiFrL",   new Vector3(-2.8f, 1.7f, pzB - 0.04f), new Vector3(0.08f, 1.4f, 0.08f), woodDark, woodMat, false);
            CreateRoomPart("ShojiFrR",   new Vector3( 2.8f, 1.7f, pzB - 0.04f), new Vector3(0.08f, 1.4f, 0.08f), woodDark, woodMat, false);
            CreateRoomPart("ShojiTop",   new Vector3(0f,    2.42f, pzB - 0.04f), new Vector3(5.7f, 0.08f, 0.08f), woodDark, woodMat, false);
            // 살창 세로살 (6개)
            for (int i = -2; i <= 3; i++)
                CreateRoomPart("ShojiBar", new Vector3(i * 0.9f, 1.7f, pzB - 0.035f), new Vector3(0.04f, 1.36f, 0.04f), woodDark, woodMat, false);

            // ── 5. 측면 반벽 ────────────────────────────────────────────
            CreateRoomPart("WallLeft",  new Vector3(-4.32f, 1.0f, 0.2f), new Vector3(0.10f, 2.0f, 7.6f), wallColor, null, false);
            CreateRoomPart("WallRight", new Vector3( 4.32f, 1.0f, 0.2f), new Vector3(0.10f, 2.0f, 7.6f), wallColor, null, false);

            // ── 6. 실내 장식 ─────────────────────────────────────────────
            // 벽 그림(좌측)
            CreateRoomPart("WallFrame",    new Vector3(-3.2f, 1.72f, pzB - 0.08f), new Vector3(1.10f, 0.64f, 0.04f), woodDark, woodMat, false);
            CreateRoomPart("WallPainting", new Vector3(-3.2f, 1.72f, pzB - 0.11f), new Vector3(0.96f, 0.50f, 0.02f), new Color(0.65f, 0.80f, 0.55f), null, false);
            // 달력(우측)
            CreateRoomPart("WallCalendar",  new Vector3(3.0f, 1.66f, pzB - 0.08f), new Vector3(0.78f, 0.52f, 0.02f), new Color(0.96f, 0.94f, 0.86f), null, false);
            CreateRoomPart("CalendarHeader",new Vector3(3.0f, 1.88f, pzB - 0.10f), new Vector3(0.78f, 0.10f, 0.03f), new Color(0.72f, 0.12f, 0.10f), null, false);
            // 장식장(좌측 측면)
            CreateRoomPart("SideCabinet",    new Vector3(-3.5f, 0.48f, 2.5f), new Vector3(0.82f, 0.58f, 0.38f), new Color(0.40f, 0.24f, 0.09f), woodMat, false);
            CreateRoomPart("CabinetTopCloth",new Vector3(-3.5f, 0.79f, 2.5f), new Vector3(0.72f, 0.03f, 0.32f), new Color(0.60f, 0.12f, 0.10f), null, false);
            CreateRoomPart("OldRadioBody",   new Vector3(-3.5f, 0.92f, 2.48f),new Vector3(0.36f, 0.22f, 0.20f), new Color(0.50f, 0.36f, 0.15f), null, false);
            // 화분(우측 측면)
            CreateRoomPart("PlantPot",  new Vector3(3.8f, 0.18f, -2.6f), new Vector3(0.30f, 0.36f, 0.30f), new Color(0.44f, 0.28f, 0.14f), null, false);
            CreateRoomPart("PlantLeaf", new Vector3(3.8f, 0.62f, -2.6f), new Vector3(0.46f, 0.52f, 0.46f), new Color(0.18f, 0.44f, 0.16f), null, false);

            // ── 7. 테이블 + 소품 ─────────────────────────────────────────
            CreateGoStopTable(new Vector3(0f, 0.11f + TableLift, 0.12f));

            _tableCenter = _environmentRoot.TransformPoint(new Vector3(0f, GoStopTableTopY, 0.12f));

            CreateThermos(new Vector3(2.54f, 0.47f, 1.32f));
            CreatePaperCup(new Vector3(2.24f, 0.31f, 1.42f));
            CreatePaperCup(new Vector3(2.40f, 0.31f, 1.58f));
            CreateMixCoffeeBox(new Vector3(-2.46f, 0.30f, 1.38f));
            CreateTissueBox(new Vector3(-2.10f, 0.29f, 1.55f));
            CreateFloorSeatingAndProps();

            // ── 8. 조명 ──────────────────────────────────────────────────
            // 천장 한지 등(가운데)
            CreateRoomPart("CeilLampBody",  new Vector3(0f, 2.36f, 0.2f), new Vector3(0.44f, 0.22f, 0.44f), new Color(0.96f, 0.90f, 0.72f), null, false);
            CreateRoomPart("CeilLampCord",  new Vector3(0f, 2.60f, 0.2f), new Vector3(0.03f, 0.30f, 0.03f), woodDark, null, false);

            var ceilLight = new GameObject("CeilLight").AddComponent<Light>();
            ceilLight.transform.SetParent(_environmentRoot, false);
            ceilLight.transform.localPosition = new Vector3(0f, 2.2f, 0.2f);
            ceilLight.type = LightType.Point;
            ceilLight.range = 8.0f;
            ceilLight.intensity = 1.0f;
            ceilLight.color = new Color(1f, 0.88f, 0.66f);

            // 스탠드 조명 (좌측 장식장 옆)
            CreateRoomPart("LampBase",  new Vector3(-2.8f, 0.18f, 2.0f), new Vector3(0.22f, 0.06f, 0.22f), woodDark, null, false);
            CreateRoomPart("LampPole",  new Vector3(-2.8f, 0.72f, 2.0f), new Vector3(0.04f, 0.90f, 0.04f), woodDark, woodMat, false);
            CreateRoomPart("LampShade", new Vector3(-2.8f, 1.28f, 2.0f), new Vector3(0.40f, 0.30f, 0.40f), new Color(0.94f, 0.88f, 0.62f), null, false);

            var lampLight = new GameObject("LampLight").AddComponent<Light>();
            lampLight.transform.SetParent(_environmentRoot, false);
            lampLight.transform.localPosition = new Vector3(-2.8f, 1.1f, 2.0f);
            lampLight.type = LightType.Point;
            lampLight.range = 4.0f;
            lampLight.intensity = 1.8f;
            lampLight.color = new Color(1f, 0.80f, 0.45f);
        }

        // 사방을 넓은 들판 + 임포트 나무로 둘러싸 어느 방향을 봐도 하늘색 스카이박스가 드러나지 않게 한다.
        void BuildParkSurround()
        {
            // 1) 넓은 잔디 들판 — 공원 지면보다 살짝 아래(겹침 z-fighting 방지)
            CreateRoomPart("ParkFieldWide", new Vector3(0f, -0.05f, 0f), new Vector3(56f, 0.06f, 56f), new Color(0.34f, 0.50f, 0.26f), null, false);

            // 2) 둘러싼 나무 — BadukPark 임포트 프리팹(바둑 씬에서 Built-in 정상 렌더)으로 자연스럽게 배치
            string[] trees = { "BadukPark/OakTree", "BadukPark/PoplarTree", "BadukPark/ParkTree" };
            const int ringCount = 14;
            for (int i = 0; i < ringCount; i++)
            {
                float a = (i / (float)ringCount) * Mathf.PI * 2f;
                float r = 12.5f + ((i % 3) * 1.6f);                 // 반경에 변주
                float lx = Mathf.Sin(a) * r;
                float lz = Mathf.Cos(a) * r;
                string prefab = trees[i % trees.Length];
                float scale = 1.7f + ((i % 4) * 0.22f);
                float yaw = (i * 47f) % 360f;
                SpawnSceneryPrefab(prefab, lx, lz, scale, yaw);
            }

            // 3) 덤불로 나무 사이 틈 메우기 (낮은 녹지대)
            string[] bushes = { "BadukPark/BushA", "BadukPark/BushB" };
            const int bushCount = 14;
            for (int i = 0; i < bushCount; i++)
            {
                float a = ((i + 0.5f) / bushCount) * Mathf.PI * 2f;
                float r = 9.5f + ((i % 3) * 1.1f);
                SpawnSceneryPrefab(bushes[i % bushes.Length], Mathf.Sin(a) * r, Mathf.Cos(a) * r,
                    1.3f + ((i % 3) * 0.25f), (i * 63f) % 360f);
            }
        }

        // 임포트 자연물 프리팹을 _environmentRoot 로컬(x,z)에 배치하고 잔디 위로 스냅, 콜라이더 제거.
        GameObject SpawnSceneryPrefab(string resPath, float localX, float localZ, float scale, float yaw)
        {
            var prefab = Resources.Load<GameObject>(resPath);
            if (prefab == null) return null;

            Vector3 worldPos = _environmentRoot.TransformPoint(new Vector3(localX, 0f, localZ));
            Quaternion worldRot = _environmentRoot.rotation * Quaternion.Euler(0f, yaw, 0f);
            var inst = Instantiate(prefab, worldPos, worldRot, _environmentRoot);
            inst.name = "Bg_" + prefab.name;
            inst.transform.localScale = Vector3.one * scale;

            foreach (var col in inst.GetComponentsInChildren<Collider>())
                Destroy(col);

            // 렌더러 바운드 밑면을 잔디 높이(_parkGroundY)에 맞춰 스냅 — 떠 있거나 묻히지 않게.
            var renderers = inst.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                float delta = _parkGroundY - b.min.y;
                inst.transform.position += new Vector3(0f, delta, 0f);
            }
            return inst;
        }

        void CreateRoomPart(string name, Vector3 localPos, Vector3 localScale, Color color, Material material = null, bool collider = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(_environmentRoot, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            if (!collider)
            {
                var col = obj.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            var renderer = obj.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.material = material;
            else
                renderer.material.color = color;
        }

        void CreateGoStopTable(Vector3 localPos)
        {
            Material woodMat = CreateWoodMaterial(new Color(0.45f, 0.25f, 0.10f), new Color(0.68f, 0.42f, 0.18f));
            Material railMat = CreateWoodMaterial(new Color(0.30f, 0.13f, 0.04f), new Color(0.52f, 0.26f, 0.08f));
            CreateRoomPart("GoStopWoodTable", localPos, new Vector3(5.85f, 0.22f, 3.85f), new Color(0.43f, 0.24f, 0.10f), woodMat);

            var felt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            felt.name = "GoStopGreenFelt";
            felt.transform.SetParent(_environmentRoot, false);
            felt.transform.localPosition = localPos + new Vector3(0f, 0.13f, 0f);
            felt.transform.localScale = new Vector3(5.35f, 0.035f, 3.35f);
            var feltMat = new Material(Shader.Find("Standard"));
            feltMat.color = new Color(0.06f, 0.56f, 0.16f);
            feltMat.mainTexture = CreateGoStopFeltTexture();
            felt.GetComponent<MeshRenderer>().material = feltMat;

            Color railColor = new Color(0.34f, 0.17f, 0.06f);
            CreateRoomPart("GoStopRailFront", localPos + new Vector3(0f, 0.21f, -1.90f), new Vector3(5.95f, 0.16f, 0.16f), railColor, railMat);
            CreateRoomPart("GoStopRailBack", localPos + new Vector3(0f, 0.21f, 1.90f), new Vector3(5.95f, 0.16f, 0.16f), railColor, railMat);
            CreateRoomPart("GoStopRailLeft", localPos + new Vector3(-2.92f, 0.21f, 0f), new Vector3(0.16f, 0.16f, 3.85f), railColor, railMat);
            CreateRoomPart("GoStopRailRight", localPos + new Vector3(2.92f, 0.21f, 0f), new Vector3(0.16f, 0.16f, 3.85f), railColor, railMat);

            Color zoneColor = new Color(0.08f, 0.42f, 0.15f);
            CreateRoomPart("GoStopPlayerZone", localPos + new Vector3(0f, 0.149f, -1.17f), new Vector3(4.45f, 0.006f, 0.58f), zoneColor, null, false);
            CreateRoomPart("GoStopCpuZone", localPos + new Vector3(0f, 0.149f, 1.17f), new Vector3(4.45f, 0.006f, 0.58f), zoneColor, null, false);
            CreateRoomPart("GoStopFloorZone", localPos + new Vector3(0f, 0.150f, 0f), new Vector3(3.75f, 0.006f, 1.25f), new Color(0.07f, 0.48f, 0.16f), null, false);

            // 테이블 다리 — 슬랩 밑면에서 마루 바닥(local y≈0)까지 내려 실제 테이블처럼 보이게.
            const float maruTopY = 0.02f;                  // 마루 표면
            float tableBottom = localPos.y - 0.11f;        // 상판 슬랩 밑면
            float legH = Mathf.Max(0.08f, tableBottom - maruTopY);
            float legCy = maruTopY + legH * 0.5f;
            Color legColor = new Color(0.34f, 0.18f, 0.08f);
            Material legMat = CreateWoodMaterial(legColor, new Color(0.52f, 0.30f, 0.13f));
            float lx = 2.55f, lz1 = localPos.z - 1.62f, lz2 = localPos.z + 1.62f;
            foreach (var lp in new[] {
                new Vector3(-lx, legCy, lz1), new Vector3(lx, legCy, lz1),
                new Vector3(-lx, legCy, lz2), new Vector3(lx, legCy, lz2)
            })
                CreateRoomPart("TableLeg", new Vector3(lp.x, lp.y, lp.z), new Vector3(0.14f, legH, 0.14f), legColor, legMat, false);

            // 다리 사이 가로 보강대(에이프런) — 테이블 가구다움 강화
            CreateRoomPart("TableApronFront", new Vector3(0f, tableBottom - 0.05f, localPos.z - 1.62f), new Vector3(5.2f, 0.07f, 0.06f), legColor, legMat, false);
            CreateRoomPart("TableApronBack",  new Vector3(0f, tableBottom - 0.05f, localPos.z + 1.62f), new Vector3(5.2f, 0.07f, 0.06f), legColor, legMat, false);
            CreateRoomPart("TableApronLeft",  new Vector3(-lx, tableBottom - 0.05f, localPos.z), new Vector3(0.06f, 0.07f, 3.3f), legColor, legMat, false);
            CreateRoomPart("TableApronRight", new Vector3( lx, tableBottom - 0.05f, localPos.z), new Vector3(0.06f, 0.07f, 3.3f), legColor, legMat, false);
        }

        void CreateGoStopRoomDecor()
        {
            Material woodMat = CreateWoodMaterial(new Color(0.46f, 0.27f, 0.12f), new Color(0.66f, 0.44f, 0.22f));
            CreateRoomPart("WoodBaseboardBack", new Vector3(0f, 0.22f, 3.29f), new Vector3(8.7f, 0.18f, 0.05f), new Color(0.44f, 0.25f, 0.10f), woodMat, false);
            CreateRoomPart("WoodBaseboardLeft", new Vector3(-4.28f, 0.22f, 0f), new Vector3(0.05f, 0.18f, 7.1f), new Color(0.44f, 0.25f, 0.10f), woodMat, false);
            CreateRoomPart("WoodBaseboardRight", new Vector3(4.28f, 0.22f, 0f), new Vector3(0.05f, 0.18f, 7.1f), new Color(0.44f, 0.25f, 0.10f), woodMat, false);

            CreateRoomPart("WallFrame", new Vector3(-1.60f, 1.72f, 3.27f), new Vector3(1.10f, 0.64f, 0.04f), new Color(0.42f, 0.23f, 0.10f), woodMat, false);
            CreateRoomPart("WallPainting", new Vector3(-1.60f, 1.72f, 3.235f), new Vector3(0.96f, 0.50f, 0.025f), new Color(0.70f, 0.84f, 0.60f), null, false);
            CreateRoomPart("WallCalendar", new Vector3(1.32f, 1.66f, 3.235f), new Vector3(0.78f, 0.52f, 0.025f), new Color(0.96f, 0.94f, 0.86f), null, false);
            CreateRoomPart("CalendarHeader", new Vector3(1.32f, 1.88f, 3.215f), new Vector3(0.78f, 0.10f, 0.03f), new Color(0.72f, 0.12f, 0.10f), null, false);
            // 창문 — 야간 어두운 바깥 + 은은한 달빛
            CreateRoomPart("WindowFrame",  new Vector3(2.85f, 1.68f, 3.24f),  new Vector3(1.20f, 0.78f, 0.05f),  new Color(0.36f, 0.20f, 0.08f), woodMat, false);
            CreateRoomPart("WindowNight",  new Vector3(2.85f, 1.68f, 3.205f), new Vector3(1.02f, 0.62f, 0.025f), new Color(0.04f, 0.06f, 0.14f), null, false); // 야간 하늘
            CreateRoomPart("WindowMoon",   new Vector3(3.12f, 1.82f, 3.195f), new Vector3(0.16f, 0.16f, 0.02f),  new Color(0.88f, 0.90f, 0.78f), null, false); // 달
            CreateRoomPart("WindowCrossV", new Vector3(2.85f, 1.68f, 3.18f),  new Vector3(0.035f, 0.66f, 0.03f), new Color(0.34f, 0.19f, 0.07f), woodMat, false);
            CreateRoomPart("WindowCrossH", new Vector3(2.85f, 1.68f, 3.175f), new Vector3(1.08f, 0.035f, 0.03f), new Color(0.34f, 0.19f, 0.07f), woodMat, false);

            // TV 장식장 (좌측 벽)
            CreateRoomPart("TVUnit",       new Vector3(-3.6f, 0.32f, -0.2f),  new Vector3(1.60f, 0.50f, 0.40f),  new Color(0.32f, 0.18f, 0.07f), woodMat, false);
            CreateRoomPart("TVScreen",     new Vector3(-3.6f, 0.68f, -0.2f),  new Vector3(1.20f, 0.70f, 0.08f),  new Color(0.06f, 0.06f, 0.08f), null, false);
            CreateRoomPart("TVFrame",      new Vector3(-3.6f, 0.68f, -0.18f), new Vector3(1.28f, 0.78f, 0.06f),  new Color(0.14f, 0.12f, 0.10f), null, false);
            CreateRoomPart("TVGlow",       new Vector3(-3.6f, 0.68f, -0.22f), new Vector3(1.10f, 0.62f, 0.015f), new Color(0.28f, 0.36f, 0.54f), null, false); // 꺼진 화면 반사광

            // 벽시계 (우측 벽)
            CreateRoomPart("ClockBody",    new Vector3(4.28f, 1.82f,  0.8f),  new Vector3(0.04f, 0.34f, 0.34f),  new Color(0.55f, 0.40f, 0.20f), woodMat, false);
            CreateRoomPart("ClockFace",    new Vector3(4.26f, 1.82f,  0.8f),  new Vector3(0.025f, 0.28f, 0.28f), new Color(0.95f, 0.92f, 0.84f), null, false);
            CreateRoomPart("ClockRimTop",  new Vector3(4.25f, 1.82f,  0.8f),  new Vector3(0.02f, 0.32f, 0.32f),  new Color(0.42f, 0.26f, 0.10f), woodMat, false);

            // 화분 (우측 코너)
            CreateRoomPart("PlantPot",     new Vector3(3.72f, 0.18f, -2.8f),  new Vector3(0.30f, 0.36f, 0.30f),  new Color(0.44f, 0.30f, 0.16f), null, false);
            CreateRoomPart("PlantSoil",    new Vector3(3.72f, 0.38f, -2.8f),  new Vector3(0.26f, 0.04f, 0.26f),  new Color(0.28f, 0.20f, 0.12f), null, false);
            CreateRoomPart("PlantLeaf1",   new Vector3(3.72f, 0.60f, -2.8f),  new Vector3(0.42f, 0.44f, 0.42f),  new Color(0.18f, 0.42f, 0.16f), null, false);
            CreateRoomPart("PlantLeaf2",   new Vector3(3.58f, 0.52f, -2.72f), new Vector3(0.22f, 0.28f, 0.22f),  new Color(0.22f, 0.48f, 0.20f), null, false);

            // 장식장 (좌측 구석)
            CreateRoomPart("SideCabinet",     new Vector3(-3.35f, 0.48f, 2.72f), new Vector3(0.82f, 0.58f, 0.34f), new Color(0.40f, 0.24f, 0.09f), woodMat, false);
            CreateRoomPart("CabinetTopCloth", new Vector3(-3.35f, 0.79f, 2.72f), new Vector3(0.72f, 0.025f, 0.28f), new Color(0.60f, 0.12f, 0.10f), null, false);
            CreateRoomPart("OldRadioBody",    new Vector3(-3.35f, 0.92f, 2.70f), new Vector3(0.36f, 0.22f, 0.18f), new Color(0.50f, 0.35f, 0.15f), null, false);
            CreateRoomPart("OldRadioDial",    new Vector3(-3.18f, 0.93f, 2.58f), new Vector3(0.06f, 0.10f, 0.02f), new Color(0.08f, 0.08f, 0.07f), null, false);

            // 미닫이문 구분선
            CreateRoomPart("SlidingDoorLine", new Vector3(0f, 1.28f, 3.205f), new Vector3(0.035f, 2.06f, 0.03f), new Color(0.58f, 0.44f, 0.24f), woodMat, false);
        }

        void CreateFloorSeatingAndProps()
        {
            CreateFloorCushion(new Vector3(-1.38f, 0.065f, -2.22f), new Vector3(1.08f, 0.075f, 0.70f), new Color(0.62f, 0.18f, 0.14f), 4f);
            CreateFloorCushion(new Vector3(1.38f, 0.065f, -2.20f), new Vector3(1.08f, 0.075f, 0.70f), new Color(0.20f, 0.38f, 0.56f), -5f);
            CreateFloorCushion(new Vector3(-1.62f, 0.065f, 2.28f), new Vector3(1.05f, 0.075f, 0.68f), new Color(0.46f, 0.46f, 0.22f), -8f);
            CreateFloorCushion(new Vector3(1.55f, 0.065f, 2.30f), new Vector3(1.05f, 0.075f, 0.68f), new Color(0.54f, 0.30f, 0.15f), 7f);

            CreateRoomPart("SideSnackTray", new Vector3(3.08f, 0.10f, -0.82f), new Vector3(0.54f, 0.035f, 0.36f), new Color(0.40f, 0.20f, 0.08f), CreateWoodMaterial(new Color(0.34f, 0.18f, 0.07f), new Color(0.62f, 0.35f, 0.14f)), false);
            CreateRoomPart("SnackBowl", new Vector3(3.02f, 0.16f, -0.82f), new Vector3(0.20f, 0.06f, 0.20f), new Color(0.88f, 0.80f, 0.66f), null, false);
            CreateRoomPart("WarmTeaCup", new Vector3(3.24f, 0.16f, -0.66f), new Vector3(0.09f, 0.08f, 0.09f), new Color(0.93f, 0.89f, 0.78f), null, false);

            if (PlaceRoomPrefab("GoStopRoom/CoffeeTable", "ImportedSideCoffeeTable",
                    new Vector3(-3.15f, 0.18f, -2.42f), Quaternion.Euler(0f, 18f, 0f), Vector3.one * 0.60f))
            {
                CreateRoomPart("SideCoffeeTableCloth", new Vector3(-3.15f, 0.43f, -2.42f), new Vector3(0.70f, 0.025f, 0.48f), new Color(0.74f, 0.16f, 0.12f), null, false);
            }
        }

        void CreateFloorCushion(Vector3 localPos, Vector3 localScale, Color color, float yaw)
        {
            var cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cushion.name = "FloorCushion";
            cushion.transform.SetParent(_environmentRoot, false);
            cushion.transform.localPosition = localPos;
            cushion.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            cushion.transform.localScale = localScale;
            var col = cushion.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.mainTexture = CreateCushionTexture(color);
            mat.SetFloat("_Glossiness", 0.04f);
            cushion.GetComponent<MeshRenderer>().material = mat;
        }

        Texture2D CreateCushionTexture(Color baseColor)
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Color seam = Color.Lerp(baseColor, Color.black, 0.28f);
            Color highlight = Color.Lerp(baseColor, Color.white, 0.16f);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color color = baseColor;
                    if (x < 6 || x > tex.width - 7 || y < 6 || y > tex.height - 7)
                        color = seam;
                    else if ((x / 18 + y / 18) % 2 == 0)
                        color = Color.Lerp(color, highlight, 0.10f);
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        bool PlaceRoomPrefab(string resourcePath, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
                return false;

            var instance = Instantiate(prefab, _environmentRoot);
            instance.name = name;
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = localScale;
            RemoveColliders(instance);
            TintPrefabMaterials(instance);
            return true;
        }

        static void RemoveColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Destroy(col);
        }

        static void TintPrefabMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null)
                    continue;
                var mat = new Material(renderer.sharedMaterial);
                if (mat.HasProperty("_Color"))
                    mat.color = Color.Lerp(mat.color, new Color(0.92f, 0.82f, 0.65f), 0.12f);
                renderer.material = mat;
            }
        }

        Texture2D CreateGoStopFeltTexture()
        {
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color baseColor = new Color(0.05f, 0.52f, 0.15f);
            Color fiberColor = new Color(0.09f, 0.64f, 0.20f);
            Color darkFiber = new Color(0.03f, 0.36f, 0.11f);

            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.055f, y * 0.055f);
                    Color color = Color.Lerp(darkFiber, fiberColor, n);
                    color = Color.Lerp(baseColor, color, 0.35f);
                    if (x < 5 || x > tex.width - 6 || y < 5 || y > tex.height - 6)
                        color = new Color(0.76f, 0.58f, 0.28f);
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return tex;
        }

        Material CreateWoodMaterial(Color dark, Color light)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            mat.mainTexture = CreateWoodTexture(dark, light);
            mat.SetFloat("_Glossiness", 0.16f);
            return mat;
        }

        Texture2D CreateWoodTexture(Color dark, Color light)
        {
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float u = x / (float)tex.width;
                    float n = Mathf.PerlinNoise(u * 14f, y * 0.04f);
                    float grain = Mathf.Abs(Mathf.Sin((u * 18f + n * 1.4f) * Mathf.PI));
                    tex.SetPixel(x, y, Color.Lerp(dark, light, Mathf.Clamp01(n * 0.45f + grain * 0.55f)));
                }
            }

            tex.Apply();
            return tex;
        }

        void CreateBlanketMat(Vector3 localPos, Vector3 localScale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "BlanketMat";
            obj.transform.SetParent(_environmentRoot, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = CreateBlanketTexture();
            obj.GetComponent<MeshRenderer>().material = mat;
        }

        Texture2D CreateBlanketTexture()
        {
            var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var baseColor = new Color(0.34f, 0.46f, 0.28f);
            var stripeColor = new Color(0.26f, 0.31f, 0.18f);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color color = baseColor;
                    if ((x / 24) % 2 == 0)
                        color = Color.Lerp(color, stripeColor, 0.16f);
                    if ((y / 28) % 2 == 0)
                        color = Color.Lerp(color, stripeColor, 0.10f);
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return tex;
        }

        void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > r2)
                        continue;

                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        tex.SetPixel(px, py, color);
                }
            }
        }

        void CreateThermos(Vector3 localPos)
        {
            CreateRoomPart("ThermosBody", localPos, new Vector3(0.14f, 0.44f, 0.14f), new Color(0.84f, 0.32f, 0.26f));
            CreateRoomPart("ThermosCap", localPos + new Vector3(0f, 0.26f, 0f), new Vector3(0.11f, 0.08f, 0.11f), new Color(0.94f, 0.94f, 0.92f));
        }

        void CreatePaperCup(Vector3 localPos)
        {
            CreateRoomPart("PaperCup", localPos, new Vector3(0.08f, 0.12f, 0.08f), new Color(0.96f, 0.94f, 0.89f));
        }

        void CreateMixCoffeeBox(Vector3 localPos)
        {
            CreateRoomPart("MixCoffeeBox", localPos, new Vector3(0.24f, 0.10f, 0.16f), new Color(0.82f, 0.67f, 0.24f));
        }

        void CreateTissueBox(Vector3 localPos)
        {
            CreateRoomPart("TissueBox", localPos, new Vector3(0.20f, 0.08f, 0.12f), new Color(0.94f, 0.91f, 0.86f));
        }

        void CreateFanProp(Vector3 localPos)
        {
            CreateRoomPart("FanHandle", localPos + new Vector3(0f, 0.01f, 0.1f), new Vector3(0.04f, 0.02f, 0.22f), new Color(0.55f, 0.42f, 0.22f));
            CreateRoomPart("FanHead", localPos, new Vector3(0.26f, 0.02f, 0.18f), new Color(0.89f, 0.86f, 0.74f));
        }

        void GoToLobbySafe()
        {
            _isPlaying = false;
            _waitingForCard = false;
            StopAllCoroutines();

            if (_gameCanvas != null)   { Destroy(_gameCanvas.gameObject);   _gameCanvas = null; }
            if (_goStopCanvas != null) { Destroy(_goStopCanvas.gameObject); _goStopCanvas = null; }
            if (_cardRoot != null)     { Destroy(_cardRoot.gameObject);     _cardRoot = null; }
            if (_environmentRoot != null) { Destroy(_environmentRoot.gameObject); _environmentRoot = null; }

            if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadScene(lobbySceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }

    public class HandCardMarker : MonoBehaviour
    {
        public HwatooCard Card;
    }
}
