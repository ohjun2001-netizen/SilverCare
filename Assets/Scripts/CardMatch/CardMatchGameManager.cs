// Assets/Scripts/CardMatch/CardMatchGameManager.cs
// 담당: 박건영
// TODO: VR 집기/놓기 완성, 카드 짝맞추기 로직, 난이도별 카드 수
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.CardMatch
{
    public class CardMatchGameManager : BaseGameManager
    {
        [Header("Card Match Settings")]
        [SerializeField] private CardController cardPrefab;
        [SerializeField] private Transform cardParent;
        [SerializeField] private int pairCount = 8;          // 기본 8쌍 = 카드 16장

        private List<CardController> _cards = new();
        private CardController _firstFlipped;
        private int _matchedPairs = 0;
        private bool _isChecking = false;

        protected override void InitGame()
        {
            gameTitle = "카드 짝맞추기";
            // TODO: 박건영 - 카드 스프라이트 로드, 섞기, 배치
            Debug.Log("[CardMatch] InitGame - 카드 배치 TODO");
        }

        protected override void StartGame()
        {
            _matchedPairs = 0;
            _score = 0;
            // TODO: 박건영 - 카드 뒤집기 애니메이션 시작
        }

        protected override void EndGame()
        {
            // TODO: 박건영 - 결과 UI 표시
            Debug.Log($"[CardMatch] 게임 종료 - 점수: {_score}");
        }

        /// <summary>CardController가 클릭됐을 때 호출</summary>
        public void OnCardFlipped(CardController card)
        {
            if (_isChecking || card.IsMatched || card.IsFaceUp) return;

            card.FlipUp();

            if (_firstFlipped == null)
            {
                _firstFlipped = card;
            }
            else
            {
                StartCoroutine(CheckMatch(card));
            }
        }

        private IEnumerator CheckMatch(CardController second)
        {
            _isChecking = true;
            yield return new WaitForSeconds(1.0f);

            if (_firstFlipped.CardId == second.CardId)
            {
                // 짝 맞춤
                _firstFlipped.SetMatched();
                second.SetMatched();
                _matchedPairs++;
                _score += 100;
                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.Speak("짝이 맞았습니다!");

                if (_matchedPairs >= pairCount) OnGameClear();
            }
            else
            {
                // 불일치
                _firstFlipped.FlipDown();
                second.FlipDown();
                _score = Mathf.Max(0, _score - 10);
                AudioManager.Instance?.PlayWrong();
            }

            _firstFlipped = null;
            _isChecking = false;
        }
    }
}
