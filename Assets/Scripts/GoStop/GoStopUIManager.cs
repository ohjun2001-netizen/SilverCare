// Assets/Scripts/GoStop/GoStopUIManager.cs
// 담당: 조성우
// TODO: 플레이어 패·바닥 패·점수 UI 구현
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilverCare.GoStop
{
    public class GoStopUIManager : MonoBehaviour
    {
        [Header("패 영역")]
        [SerializeField] private Transform playerHandArea;
        [SerializeField] private Transform floorArea;
        [SerializeField] private Transform capturedArea;

        [Header("점수·상태")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;
        [SerializeField] private GameObject goStopPanel;   // 고/스톱 선택 UI
        [SerializeField] private Button goButton;
        [SerializeField] private Button stopButton;

        private GoStopGameManager _gm;

        void Awake()
        {
            _gm = FindObjectOfType<GoStopGameManager>();
            goButton?.onClick.AddListener(() => _gm?.OnGoStopDecision(true));
            stopButton?.onClick.AddListener(() => _gm?.OnGoStopDecision(false));
            goStopPanel?.SetActive(false);
        }

        /// <summary>패·바닥·획득 패 전체 갱신 (StartGame 후 호출)</summary>
        public void RefreshAll(List<HwatooCard> hand, List<HwatooCard> floor, List<HwatooCard> captured)
        {
            // TODO: 조성우 - 카드 스프라이트 배치
            Debug.Log($"[GoStopUI] 손패:{hand.Count} 바닥:{floor.Count} 획득:{captured.Count}");
        }

        /// <summary>고/스톱 선택 패널 표시</summary>
        public void ShowGoStopChoice()
        {
            goStopPanel?.SetActive(true);
        }

        /// <summary>최종 결과 표시</summary>
        public void ShowFinalResult(int score)
        {
            goStopPanel?.SetActive(false);
            if (scoreText != null) scoreText.text = $"최종 점수: {score}점";
            // TODO: 조성우 - 결과 화면 애니메이션
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = $"점수: {score}";
        }
    }
}
