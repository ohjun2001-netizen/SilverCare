// Assets/Scripts/GoStop/GoStopGameManager.cs
// 담당: 조성우
// TODO: 화투 패 배분, 고스톱 규칙 구현, 점수 계산
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.GoStop
{
    public enum GoStopState { Dealing, PlayerTurn, CPUTurn, Scoring, GameOver }

    public class GoStopGameManager : BaseGameManager
    {
        [Header("GoStop Settings")]
        [SerializeField] private GoStopDeck deck;
        [SerializeField] private GoStopScoreCalculator scorer;
        [SerializeField] private GoStopUIManager goStopUI;

        public GoStopState CurrentState { get; private set; }

        // 플레이어/CPU/바닥 패
        private List<HwatooCard> _playerHand  = new();
        private List<HwatooCard> _cpuHand     = new();
        private List<HwatooCard> _floorCards  = new();

        // 획득한 패 (광, 띠, 피 분류)
        private List<HwatooCard> _playerCaptured = new();
        private List<HwatooCard> _cpuCaptured    = new();

        private int _goCount = 0;   // 고 선언 횟수

        protected override void InitGame()
        {
            gameTitle = "고스톱";
            deck?.Initialize();
        }

        protected override void StartGame()
        {
            DealCards();
            CurrentState = GoStopState.PlayerTurn;
            goStopUI?.RefreshAll(_playerHand, _floorCards, _playerCaptured);
            TTSManager.Instance?.Speak("고스톱을 시작합니다. 패를 내어 바닥 패와 맞춰보세요.");
        }

        protected override void EndGame()
        {
            int playerScore = scorer?.Calculate(_playerCaptured, _goCount) ?? 0;
            goStopUI?.ShowFinalResult(playerScore);
        }

        /// <summary>플레이어가 패를 낼 때 호출</summary>
        public void OnPlayerPlayCard(HwatooCard card)
        {
            if (CurrentState != GoStopState.PlayerTurn) return;
            // TODO: 조성우 - 같은 월 바닥 패와 매칭, 덱에서 1장 뒤집기
        }

        /// <summary>고/스톱 선택 시 호출</summary>
        public void OnGoStopDecision(bool isGo)
        {
            if (isGo)
            {
                _goCount++;
                TTSManager.Instance?.Speak("고!");
                CurrentState = GoStopState.PlayerTurn;
            }
            else
            {
                TTSManager.Instance?.Speak("스톱!");
                OnGameClear();
            }
        }

        private void DealCards()
        {
            // TODO: 조성우 - 화투 48장 배분 (플레이어 10장, CPU 10장, 바닥 8장)
            deck?.Shuffle();
        }
    }
}
