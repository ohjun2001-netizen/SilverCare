// Assets/Scripts/Lobby/LobbyManager.cs
// 담당: 양석원
// TODO: 프로필 선택, 게임 추천 로직, 플레이 기록 표시
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Lobby
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private GameSelectUI gameSelectUI;
        [SerializeField] private ProfilePanel profilePanel;

        void Start()
        {
            AudioManager.Instance?.PlayLobbyBGM();
            LoadPlayerData();
        }

        private void LoadPlayerData()
        {
            var profile = PlayerDataManager.Instance?.Profile;
            
            if (profile == null)
            {
                // 프로필이 없으면 프로필 입력창을 활성화
                if (profilePanel != null) profilePanel.gameObject.SetActive(true);
                return;
            }

            gameSelectUI?.RefreshHighScores();
            gameSelectUI?.ApplyRecommendation(profile.gender, profile.age);
            TTSManager.Instance?.Speak($"어서 오세요, {profile.playerName}님. 오늘은 어떤 게임을 하시겠어요?");
        }

        // ── 게임 선택 버튼 핸들러 ──────────────────────────────
        public void OnCardMatchClicked()   => LoadGame(GameSceneManager.SCENE_CARD_MATCH);
        public void OnQuizClicked()        => LoadGame(GameSceneManager.SCENE_QUIZ);
        public void OnSongGuessClicked()   => LoadGame(GameSceneManager.SCENE_SONG_GUESS);
        public void OnGoStopClicked()      => LoadGame(GameSceneManager.SCENE_GO_STOP);
        public void OnBadukClicked()       => LoadGame(GameSceneManager.SCENE_BADUK);
        public void OnGolfClicked()        => LoadGame(GameSceneManager.SCENE_GOLF);

        private void LoadGame(string sceneName)
        {
            AudioManager.Instance?.PlayButtonClick();
            GameSceneManager.Instance?.LoadScene(sceneName);
        }

        public void OnProfileButtonClicked()
        {
            profilePanel?.gameObject.SetActive(true);
        }
    }
}
