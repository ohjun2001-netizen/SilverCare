// Assets/Scripts/CardMatch/CardMatchGameManager.cs
// 담당: 박건영
// TODO: VR 집기/놓기 완성, 카드 짝맞추기 로직, 난이도별 카드 수
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;
using System.Diagnostics;
using System;

namespace SilverCare.CardMatch
{
    public class CardMatchGameManager : BaseGameManager
    {
        [Header("Card Match Settings")]
        [SerializeField] private CardController cardPrefab;
        [SerializeField] private Transform cardParent;
        [SerializeField] private Texture[] cardFrontTextures;
        [SerializeField] private int pairCount = 4;

        [Header("Layout Settings")]
        [SerializeField] private int columnCount = 4;
        [SerializeField] private float xSpacing = 2.0f;
        [SerializeField] private float zSpacing = 2.5f;
        [SerializeField] private Vector3 startOffset = Vector3.zero;

        private readonly List<CardController> _cards = new();
        private CardController _firstFlipped;
        private int _matchedPairs = 0;
        private bool _isChecking = false;

        protected override void InitGame()
        {
            gameTitle = "카드 짝맞추기";

            ClearExistingCards();
            CreateAndShuffleCards();
            SpawnCards();

            Debug.Log("[CardMatch] InitGame 완료");
        }

        protected override void StartGame()
        {
            _matchedPairs = 0;
            _score = 0;
            _firstFlipped = null;
            _isChecking = false;
        }

        protected override void EndGame()
        {
            Debug.Log($"[CardMatch] 게임 종료 - 점수: {_score}");
        }

        public void OnCardFlipped(CardController card)
        {
            if (_isChecking || card == null || card.IsMatched || card.IsFaceUp)
                return;

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

            yield return new WaitForSeconds(0.8f);

            if (_firstFlipped.CardId == second.CardId)
            {
                _firstFlipped.SetMatched();
                second.SetMatched();

                _matchedPairs++;
                _score += 100;

                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.Speak("짝이 맞았습니다!");

                if (_matchedPairs >= pairCount)
                {
                    OnGameClear();
                }
            }
            else
            {
                _firstFlipped.FlipDown();
                second.FlipDown();

                _score = Mathf.Max(0, _score - 10);

                AudioManager.Instance?.PlayWrong();
            }

            _firstFlipped = null;
            _isChecking = false;
        }

        private void ClearExistingCards()
        {
            _cards.Clear();

            if (cardParent == null) return;

            for (int i = cardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardParent.GetChild(i).gameObject);
            }
        }

        private void CreateAndShuffleCards()
        {
            if (cardFrontTextures == null || cardFrontTextures.Length < pairCount)
            {
                Debug.LogError("[CardMatch] cardFrontTextures 개수가 pairCount보다 적습니다.");
                return;
            }

            List<CardSpawnData> spawnList = new();

            for (int i = 0; i < pairCount; i++)
            {
                spawnList.Add(new CardSpawnData(i, cardFrontTextures[i]));
                spawnList.Add(new CardSpawnData(i, cardFrontTextures[i]));
            }

            Shuffle(spawnList);

            for (int i = 0; i < spawnList.Count; i++)
            {
                var data = spawnList[i];
                Vector3 spawnPos = CalculateCardPosition(i);

                CardController card = Instantiate(cardPrefab, spawnPos, Quaternion.identity, cardParent);
                card.name = $"Card_{data.CardId}_{i}";
                card.Init(data.CardId, data.FrontTexture, this);

                _cards.Add(card);
            }
        }

        private void SpawnCards()
        {
            // 현재는 CreateAndShuffleCards 안에서 생성까지 처리
        }

        private Vector3 CalculateCardPosition(int index)
        {
            int row = index / columnCount;
            int col = index % columnCount;

            float x = col * xSpacing;
            float z = row * zSpacing;

            return startOffset + new Vector3(x, 0f, z);
        }

        private void Shuffle(List<CardSpawnData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }

        private struct CardSpawnData
        {
            public int CardId { get; }
            public Texture FrontTexture { get; }

            public CardSpawnData(int cardId, Texture frontTexture)
            {
                CardId = cardId;
                FrontTexture = frontTexture;
            }
        }
    }
}