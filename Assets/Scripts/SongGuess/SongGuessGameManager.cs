// Assets/Scripts/SongGuess/SongGuessGameManager.cs
// 담당: 조성우
// TODO: 트로트 DB 30곡 연동, 음악 재생, 음성 인식 정답 판정
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.SongGuess
{
    [System.Serializable]
    public class SongData
    {
        public int    id;
        public string title;
        public string artist;
        public string audioFile;    // Resources/Audio/Songs/ 경로
        public string[] keywords;  // 정답으로 인정할 키워드들
        public int    difficulty;
    }

    [System.Serializable]
    public class SongDatabase { public List<SongData> songs; }

    public class SongGuessGameManager : BaseGameManager
    {
        [Header("Song Guess Settings")]
        [SerializeField] private int totalRounds = 5;
        [SerializeField] private float playDuration = 10f;  // 미리듣기 시간(초)
        [SerializeField] private SongPlayer songPlayer;
        [SerializeField] private SongGuessUIManager songUI;

        private SongDatabase _db;
        private List<SongData> _pool;
        private SongData _current;
        private int _round = 0;

        protected override void InitGame()
        {
            gameTitle = "노래 맞추기";
            LoadSongDB();
        }

        protected override void StartGame()
        {
            _round = 0;
            _score = 0;
            ShufflePool();
            StartCoroutine(PlayRound());
        }

        protected override void EndGame()
        {
            songUI?.ShowFinalResult(_score);
        }

        /// <summary>정답 버튼 또는 음성 인식 결과로 호출</summary>
        public void OnAnswerSubmitted(string answer)
        {
            if (_current == null) return;

            bool correct = IsCorrectAnswer(answer);
            if (correct)
            {
                _score += 100;
                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.Speak($"정답! {_current.title} - {_current.artist}");
            }
            else
            {
                AudioManager.Instance?.PlayWrong();
                TTSManager.Instance?.Speak($"아쉽네요. 정답은 {_current.title}입니다.");
            }

            songUI?.ShowResult(correct, _current);
            _round++;

            if (_round >= totalRounds)
                Invoke(nameof(OnGameClear), 2f);
            else
                Invoke(nameof(StartNextRound), 2f);
        }

        private IEnumerator PlayRound()
        {
            _current = _pool[_round];
            songUI?.ShowRoundStart(_round + 1, totalRounds);
            TTSManager.Instance?.Speak("노래를 잘 들어보세요.");

            songPlayer?.PlaySong(_current.audioFile, playDuration);
            yield return new WaitForSeconds(playDuration);

            songUI?.ShowAnswerInput();
            TTSManager.Instance?.Speak("어떤 노래인지 맞춰보세요.");
            // TODO: 조성우 - 음성 인식(STT) 자동 시작
        }

        private void StartNextRound() => StartCoroutine(PlayRound());

        private bool IsCorrectAnswer(string answer)
        {
            if (_current == null) return false;
            string lower = answer.ToLower().Replace(" ", "");
            if (_current.title.ToLower().Replace(" ", "").Contains(lower)) return true;
            foreach (var kw in _current.keywords)
                if (lower.Contains(kw.ToLower().Replace(" ", ""))) return true;
            return false;
        }

        private void LoadSongDB()
        {
            TextAsset json = Resources.Load<TextAsset>("Data/song_list");
            if (json != null)
                _db = JsonUtility.FromJson<SongDatabase>(json.text);
        }

        private void ShufflePool()
        {
            _pool = new List<SongData>(_db?.songs ?? new List<SongData>());
            for (int i = _pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_pool[i], _pool[j]) = (_pool[j], _pool[i]);
            }
        }
    }
}
