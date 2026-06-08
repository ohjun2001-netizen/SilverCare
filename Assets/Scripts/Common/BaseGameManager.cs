using System.Collections;
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
            StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            // Quest XR 트래킹이 BeforeRender 단계에서 카메라를 갱신하므로
            // Start()가 그보다 먼저 실행될 경우 카메라 방향이 identity다.
            // 2프레임 대기 후 앵커를 강제 갱신해 콘텐츠를 실제 시점 기준으로 배치한다.
            yield return null;
            yield return null;
            XRUIUtility.RefreshSceneViewAnchor();
            TTSManager.Instance?.Speak($"{gameTitle} 시작합니다.");
            StartGame();
            _isPlaying = true;
            XRUIUtility.StepPlayerBack(1.5f);
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
