// Assets/Scripts/Baduk/Betting/BettingGameManager.cs
// 내기 바둑 모드 전체 조율
// 흐름: 기보 선택 → 베팅 → 자동 재생 (스킵 불가) → 결과/정산
using UnityEngine;
using Baduk.Data;
using Baduk.Replay;
using SilverCare.Common;

namespace Baduk.Betting
{
    public class BettingGameManager : MonoBehaviour
    {
        [SerializeField] string lobbySceneName = "MainLobby";

        // 컴포넌트 (자동 탐색)
        KifuLoader               _loader;
        KifuReplayManager        _replay;
        NpcAvatarSpawner         _playerAvatars;
        BettingSpectatorSpawner  _spectators;
        BettingNpcCommentator    _commentator;
        BettingVRUI              _ui;

        // 상태
        Kifu          _currentKifu;
        BettingTicket _currentBet;
        BettingCommentPool _commentPool;

        void Awake()
        {
            _loader        = GetComponent<KifuLoader>();
            _replay        = GetComponent<KifuReplayManager>();
            _playerAvatars = GetComponent<NpcAvatarSpawner>();
            _spectators    = GetComponent<BettingSpectatorSpawner>();
            _commentator   = GetComponent<BettingNpcCommentator>();
            _ui            = GetComponent<BettingVRUI>();

            if (_loader == null) Debug.LogError("[BettingGameManager] KifuLoader 없음");
            if (_replay == null) Debug.LogError("[BettingGameManager] KifuReplayManager 없음");
            if (_ui == null)     Debug.LogError("[BettingGameManager] BettingVRUI 없음");

            LoadCommentPool();
        }

        void Start()
        {
            // UI 콜백
            _ui.OnKifuSelected = HandleKifuSelected;
            _ui.OnBetPlaced    = HandleBetPlaced;
            _ui.OnPlayPause    = () => _replay.TogglePlayPause();
            _ui.OnRestart      = HandleRestart;
            _ui.OnBack         = HandleBack;
            _ui.OnSpeedChanged = (s) => _replay.SetSpeed(s);

            // 재생 매니저 콜백
            _replay.OnMoveAdvanced         = HandleMoveAdvanced;
            _replay.OnPlaybackStateChanged = () => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);
            _replay.OnReplayEnded          = HandleReplayEnded;

            // NPC 자막
            if (_commentator != null) _commentator.OnComment = (text) => _ui.ShowComment(text);

            // 코인 변경 시 UI 갱신
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnCoinsChanged += OnCoinsChanged;

            ShowKifuSelect();
        }

        void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }

        void OnCoinsChanged(int coins) => _ui.UpdateCoins(coins);

        void LoadCommentPool()
        {
            var json = Resources.Load<TextAsset>("Data/betting_comments");
            if (json == null)
            {
                Debug.LogWarning("[BettingGameManager] betting_comments.json 없음 → 빈 풀");
                _commentPool = new BettingCommentPool();
                return;
            }
            _commentPool = JsonUtility.FromJson<BettingCommentPool>(json.text);
        }

        // ── 화면 흐름 ────────────────────────────────────

        void ShowKifuSelect()
        {
            _currentKifu = null;
            _currentBet = null;
            _ui.ShowKifuSelect(_loader.AllKifus, GetCoins());
        }

        void HandleKifuSelected(Kifu kifu)
        {
            _currentKifu = kifu;
            _ui.ShowBetting(kifu, GetCoins());
        }

        void HandleBetPlaced(BettingChoice choice, int amount)
        {
            if (_currentKifu == null) return;
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogWarning("[BettingGameManager] PlayerDataManager 없음 → 베팅 차감 스킵");
            }
            else if (!PlayerDataManager.Instance.TrySpendCoins(amount))
            {
                Debug.Log("[BettingGameManager] 코인 부족");
                return;
            }

            _currentBet = new BettingTicket
            {
                choice = choice,
                amount = amount,
                potentialPayout = BettingPayout.CalculatePayout(amount)
            };

            // 재생 시작
            _replay.LoadKifu(_currentKifu, null);  // 일반 NpcCommentator 풀은 사용 안 함
            _commentator?.Initialize(_commentPool, _currentKifu.moves?.Count ?? 0);
            _commentator?.OnReplayStart(_currentKifu);
            _spectators?.Spawn(transform);
            // _playerAvatars는 KifuReplayManager.LoadKifu 내부에서 이미 spawn됨

            _ui.ShowReplay(_currentKifu, GetCoins());
            _replay.Play();
        }

        void HandleMoveAdvanced(int cur, int total)
        {
            _ui.UpdateProgress(cur, total);
            // 막 진행된 수에 대해 NPC 코멘트
            if (cur > 0 && _currentKifu != null && cur <= _currentKifu.moves.Count)
            {
                var move = _currentKifu.moves[cur - 1];
                _commentator?.OnMovePlayed(cur - 1, move);
            }
        }

        void HandleReplayEnded()
        {
            if (_currentKifu == null || _currentBet == null) { ShowKifuSelect(); return; }

            BettingResult result = JudgeResult(_currentKifu.winner, _currentBet.choice);
            int delta = 0;

            switch (result)
            {
                case BettingResult.Win:
                    delta = _currentBet.potentialPayout;
                    PlayerDataManager.Instance?.AddCoins(delta);
                    _commentator?.OnBetWin(delta);
                    break;

                case BettingResult.Lose:
                    delta = _currentBet.amount;  // 이미 차감됨, 표시용
                    _commentator?.OnBetLose(delta);
                    break;

                case BettingResult.Draw:
                    // 환급
                    PlayerDataManager.Instance?.AddCoins(_currentBet.amount);
                    delta = 0;
                    break;
            }

            _ui.ShowResult(result, delta, GetCoins());
        }

        void HandleRestart()
        {
            if (_currentKifu == null) { ShowKifuSelect(); return; }
            _ui.ShowBetting(_currentKifu, GetCoins());  // 같은 기보로 베팅부터 다시
        }

        void HandleBack()
        {
            // 어느 화면이든 안전하게 처리
            _replay.Pause();
            _spectators?.Despawn();
            if (_currentKifu == null)
                LoadLobby();
            else
                ShowKifuSelect();
        }

        // ── 헬퍼 ─────────────────────────────────────────

        static BettingResult JudgeResult(string winnerCode, BettingChoice choice)
        {
            if (string.IsNullOrEmpty(winnerCode)) return BettingResult.Draw;
            string w = winnerCode.ToLowerInvariant();
            if (w == "draw") return BettingResult.Draw;

            bool blackWon = w == "black";
            bool whiteWon = w == "white";
            if (!blackWon && !whiteWon) return BettingResult.Draw;

            bool userPickedBlack = choice == BettingChoice.Black;
            return ((userPickedBlack && blackWon) || (!userPickedBlack && whiteWon))
                ? BettingResult.Win : BettingResult.Lose;
        }

        static int GetCoins() => PlayerDataManager.Instance?.Coins ?? 0;

        void LoadLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }
}
