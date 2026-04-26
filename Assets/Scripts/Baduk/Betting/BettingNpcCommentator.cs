// Assets/Scripts/Baduk/Betting/BettingNpcCommentator.cs
// 베팅 모드 전용 - 더 떠들썩한 톤 + 베팅 결과 멘트
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk.Betting
{
    public class BettingNpcCommentator : MonoBehaviour
    {
        [Header("코멘트 빈도")]
        [Range(0f, 1f)] public float commentProbability = 0.55f; // 더 시끌벅적
        [Range(0f, 1f)] public float judgmentProbability = 0.25f;

        BettingCommentPool _pool;
        int _totalMoves;

        public System.Action<string> OnComment;

        public void Initialize(BettingCommentPool pool, int totalMoves)
        {
            _pool = pool;
            _totalMoves = totalMoves;
        }

        public void OnMovePlayed(int moveIndex, KifuMove move)
        {
            if (!string.IsNullOrEmpty(move.comment)) { Speak(move.comment); return; }

            if (Random.value > commentProbability) return;

            string line = PickLine(moveIndex);
            if (!string.IsNullOrEmpty(line)) Speak(line);
        }

        public void OnReplayStart(Kifu kifu)
        {
            // 시작 멘트 (포석 풀에서)
            string line = RandomFrom(_pool?.opening) ?? $"자, {kifu.title} 시작합니다.";
            Speak(line);
        }

        public void OnBetWin(int payout)
        {
            string line = RandomFrom(_pool?.win) ?? "축하합니다! 적중하셨네요.";
            Speak($"{line} (+{payout} 코인)");
        }

        public void OnBetLose(int amount)
        {
            string line = RandomFrom(_pool?.lose) ?? "아쉽네요, 다음에는 꼭 맞춰봅시다.";
            Speak($"{line} (-{amount} 코인)");
        }

        string PickLine(int moveIndex)
        {
            if (_pool == null) return null;

            if (Random.value < judgmentProbability)
            {
                var p = Random.value < 0.5f ? _pool.good_move : _pool.bad_move;
                var pick = RandomFrom(p);
                if (pick != null) return pick;
            }

            List<string> stage;
            if (moveIndex < 10)                    stage = _pool.opening;
            else if (moveIndex < _totalMoves - 10) stage = _pool.midgame;
            else                                   stage = _pool.endgame;

            var stageLine = RandomFrom(stage);
            if (stageLine != null) return stageLine;

            return RandomFrom(_pool.idle);
        }

        static string RandomFrom(List<string> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        void Speak(string text)
        {
            OnComment?.Invoke(text);
            if (TTSManager.Instance != null)
                TTSManager.Instance.Speak(text, interruptCurrent: true);
            else
                Debug.Log($"[Betting NPC] {text}");
        }
    }
}
