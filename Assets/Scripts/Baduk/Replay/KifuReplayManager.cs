// Assets/Scripts/Baduk/Replay/KifuReplayManager.cs
// 기보를 한 수씩 자동/수동 재생. BadukBoard의 PlaceStone/RemoveLastPlayerStone 재사용.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;

namespace Baduk.Replay
{
    public class KifuReplayManager : MonoBehaviour
    {
        [Header("재생 속도 (초/수)")]
        [SerializeField] float baseInterval = 2.0f;
        [Range(0.25f, 4f)] public float speedMultiplier = 1f;

        [Header("참조 (자동 탐색)")]
        BadukBoard       _board;
        NpcCommentator   _commentator;
        NpcAvatarSpawner _avatars;

        Kifu _currentKifu;
        int  _moveIndex;       // 다음에 둘 수의 인덱스 (0 = 아직 안 둠)
        bool _isPlaying;
        Coroutine _playLoop;

        // 콜백
        public System.Action<int, int> OnMoveAdvanced;   // (currentMoveIndex, totalMoves)
        public System.Action OnReplayEnded;
        public System.Action OnPlaybackStateChanged;

        public bool IsPlaying  => _isPlaying;
        public int  MoveIndex  => _moveIndex;
        public int  TotalMoves => _currentKifu?.moves?.Count ?? 0;
        public Kifu CurrentKifu => _currentKifu;

        void Awake()
        {
            _board       = GetComponent<BadukBoard>();
            _commentator = GetComponent<NpcCommentator>();
            _avatars     = GetComponent<NpcAvatarSpawner>();

            if (_board == null) Debug.LogError("[KifuReplayManager] BadukBoard 없음");
            if (_commentator == null) Debug.LogWarning("[KifuReplayManager] NpcCommentator 없음 — 코멘트 비활성");
        }

        // ── 공개 API ─────────────────────────────────────

        public void LoadKifu(Kifu kifu, NpcCommentPool commentPool)
        {
            StopPlayback();
            _currentKifu = kifu;
            _cachedPool = commentPool;
            _moveIndex = 0;

            var emptyProblem = new BadukProblem
            {
                view_area = kifu.view_area,
                stones = new BoardStones { black = new List<StonePosition>(), white = new List<StonePosition>() }
            };
            _board.SetupBoard(emptyProblem);

            _avatars?.Spawn(_board.transform);

            _commentator?.Initialize(commentPool, kifu.moves?.Count ?? 0);
            _commentator?.OnReplayStart(kifu);

            OnMoveAdvanced?.Invoke(_moveIndex, TotalMoves);
        }

        public void Play()
        {
            if (_currentKifu == null) return;
            if (_moveIndex >= TotalMoves) return;
            if (_isPlaying) return;

            _isPlaying = true;
            _playLoop = StartCoroutine(PlayLoop());
            OnPlaybackStateChanged?.Invoke();
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            StopPlayback();
            OnPlaybackStateChanged?.Invoke();
        }

        public void TogglePlayPause()
        {
            if (_isPlaying) Pause(); else Play();
        }

        public void Next()
        {
            Pause();
            StepForward();
        }

        public void Prev()
        {
            Pause();
            StepBackward();
        }

        public void SetSpeed(float multiplier)
            => speedMultiplier = Mathf.Clamp(multiplier, 0.25f, 4f);

        public void Restart()
        {
            if (_currentKifu == null) return;
            LoadKifu(_currentKifu, _cachedPool);
        }

        // ── 내부 ─────────────────────────────────────────

        NpcCommentPool _cachedPool;

        IEnumerator PlayLoop()
        {
            while (_isPlaying && _moveIndex < TotalMoves)
            {
                StepForward();
                if (!_isPlaying || _moveIndex >= TotalMoves) break;
                float wait = baseInterval / Mathf.Max(0.25f, speedMultiplier);
                yield return new WaitForSeconds(wait);
            }
            _isPlaying = false;
            OnPlaybackStateChanged?.Invoke();
        }

        void StepForward()
        {
            if (_currentKifu == null || _moveIndex >= TotalMoves) return;

            var move = _currentKifu.moves[_moveIndex];
            var type = move.color == "black" ? StoneType.Black : StoneType.White;
            _board.PlaceStone(move.row, move.col, type);

            _commentator?.OnMovePlayed(_moveIndex, move);

            _moveIndex++;
            OnMoveAdvanced?.Invoke(_moveIndex, TotalMoves);

            if (_moveIndex >= TotalMoves)
            {
                _commentator?.OnReplayEnd(_currentKifu);
                OnReplayEnded?.Invoke();
            }
        }

        void StepBackward()
        {
            if (_moveIndex <= 0) return;
            _board.RemoveLastPlayerStone();
            _moveIndex--;
            OnMoveAdvanced?.Invoke(_moveIndex, TotalMoves);
        }

        void StopPlayback()
        {
            if (_playLoop != null) StopCoroutine(_playLoop);
            _playLoop = null;
            _isPlaying = false;
        }
    }
}
