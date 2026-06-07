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

        protected abstract void InitGame();
        protected abstract void StartGame();
        protected abstract void EndGame();

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

            bool handledByStory = StoryProgressManager.Instance != null &&
                                  StoryProgressManager.Instance.TryHandleBaseGameClear(gameTitle, _score);

            if (!handledByStory)
                TTSManager.Instance?.Speak($"게임이 끝났습니다. 점수는 {_score}점입니다.");
        }

        public void GoToLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(lobbySceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }
}
