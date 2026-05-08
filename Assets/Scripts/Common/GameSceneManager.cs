// Assets/Scripts/Common/GameSceneManager.cs
// 씬 전환 전담 싱글턴 - 모든 씬 이름을 여기서 관리
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilverCare.Common
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        // ── 씬 이름 상수 (빌드 세팅과 반드시 일치) ────────────
        public const string SCENE_LOBBY         = "MainLobby";
        public const string SCENE_CARD_MATCH    = "CardMatch";
        public const string SCENE_QUIZ          = "Quiz";
        public const string SCENE_SONG_GUESS    = "SongGuess";
        public const string SCENE_GO_STOP       = "GoStop";
        public const string SCENE_BADUK            = "BadukVR";
        public const string SCENE_BADUK_REPLAY     = "BadukReplay";
        public const string SCENE_BADUK_PREDICTION = "BadukPrediction";
        public const string SCENE_GOLF             = "Golf";

        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private float minLoadingTime = 0.5f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            yield return new WaitForSeconds(minLoadingTime);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[GameSceneManager] 씬 '{sceneName}'을 찾을 수 없음. Build Settings에 추가됐는지 확인.");
                if (loadingPanel != null) loadingPanel.SetActive(false);
                yield break;
            }
            while (!op.isDone) yield return null;

            if (loadingPanel != null) loadingPanel.SetActive(false);
        }
    }
}
