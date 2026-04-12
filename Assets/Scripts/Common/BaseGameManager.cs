// Assets/Scripts/Common/BaseGameManager.cs
// 모든 게임 매니저가 상속받는 추상 기반 클래스
using UnityEngine;

namespace SilverCare.Common
{
    public abstract class BaseGameManager : MonoBehaviour
    {
        [Header("Common Settings")]
        [SerializeField] protected string gameTitle = "게임";
        [SerializeField] protected string lobbySceneName = "MainLobby";

        protected int _score = 0;
        protected bool _isPlaying = false;

        // ── 각 게임이 반드시 구현해야 하는 메서드 ──────────────
        protected abstract void InitGame();     // 문제/자원 로드
        protected abstract void StartGame();    // 게임 시작 처리
        protected abstract void EndGame();      // 점수 정산, 결과 표시

        // ── 공통 흐름 (변경 불필요) ────────────────────────────
        protected virtual void Awake()
        {
            InitGame();
        }

        protected virtual void Start()
        {
            TTSManager.Instance?.Speak($"{gameTitle} 시작합니다.");
            StartGame();
            _isPlaying = true;
        }

        protected virtual void OnGameClear()
        {
            _isPlaying = false;
            EndGame();
            PlayerDataManager.Instance?.SaveScore(gameTitle, _score);
            TTSManager.Instance?.Speak($"게임이 끝났습니다. 점수는 {_score}점입니다.");
        }

        public void GoToLobby()
        {
            GameSceneManager.Instance?.LoadScene(lobbySceneName);
        }
    }
}
