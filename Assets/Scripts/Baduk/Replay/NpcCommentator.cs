// Assets/Scripts/Baduk/Replay/NpcCommentator.cs
// 수 진행에 맞춰 NPC 훈수 코멘트 선택 + TTSManager로 발화
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk.Replay
{
    public class NpcCommentator : MonoBehaviour
    {
        [Header("코멘트 빈도")]
        [Tooltip("매 수마다 코멘트할 확률 (0~1)")]
        [Range(0f, 1f)] public float commentProbability = 0.4f;

        [Tooltip("good_move/bad_move 같은 평가성 멘트 확률 (0~1)")]
        [Range(0f, 1f)] public float judgmentProbability = 0.15f;

        NpcCommentPool _pool;
        int _totalMoves;

        // 콜백 - UI에서 자막 표시용
        public System.Action<string> OnComment;

        public void Initialize(NpcCommentPool pool, int totalMoves)
        {
            _pool = pool;
            _totalMoves = totalMoves;
        }

        /// <summary>한 수가 진행될 때마다 호출. 코멘트가 있으면 발화.</summary>
        public void OnMovePlayed(int moveIndex, KifuMove move)
        {
            // 1) 기보에 저장된 특별 코멘트가 있으면 무조건 발화
            if (!string.IsNullOrEmpty(move.comment))
            {
                Speak(move.comment);
                return;
            }

            // 2) 확률적으로 NPC 코멘트 발화
            if (Random.value > commentProbability) return;

            string line = PickLine(moveIndex);
            if (!string.IsNullOrEmpty(line)) Speak(line);
        }

        public void OnReplayStart(Kifu kifu)
        {
            if (!string.IsNullOrEmpty(kifu.description)) Speak(kifu.description);
        }

        public void OnReplayEnd(Kifu kifu)
        {
            string line = string.IsNullOrEmpty(kifu.result) ? "복기를 마쳤네." : $"복기 끝. {kifu.result}.";
            Speak(line);
        }

        string PickLine(int moveIndex)
        {
            if (_pool == null) return null;

            // 평가성 멘트 (good/bad) 우선 확률
            if (Random.value < judgmentProbability)
            {
                var pool = Random.value < 0.5f ? _pool.good_move : _pool.bad_move;
                var pick = RandomFrom(pool);
                if (pick != null) return pick;
            }

            // 단계별 멘트
            List<string> stage;
            if (moveIndex < 10)               stage = _pool.opening;
            else if (moveIndex < _totalMoves - 10) stage = _pool.midgame;
            else                                   stage = _pool.endgame;

            var stageLine = RandomFrom(stage);
            if (stageLine != null) return stageLine;

            // 폴백: idle 추임새
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
                Debug.Log($"[NPC] {text}");
        }
    }
}
