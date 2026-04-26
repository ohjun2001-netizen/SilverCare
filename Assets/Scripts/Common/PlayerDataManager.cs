// Assets/Scripts/Common/PlayerDataManager.cs
// 플레이어 데이터 저장/불러오기 (PlayerPrefs 기반, 나중에 파일 저장으로 교체 가능)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.Common
{
    [Serializable]
    public class PlayerProfile
    {
        public string playerName = "어르신";
        public int age = 65;
        public string gender = "";          // "male" | "female" | ""
        public string difficulty = "easy";  // "easy" | "normal" | "hard"
    }

    [Serializable]
    public class GameRecord
    {
        public string gameName;
        public int    highScore;
        public int    playCount;
        public string lastPlayedDate;
    }

    public class PlayerDataManager : MonoBehaviour
    {
        public static PlayerDataManager Instance { get; private set; }

        private const string KEY_PROFILE = "player_profile";
        private const string KEY_RECORDS = "game_records";
        private const string KEY_COINS   = "player_coins";
        private const int    DEFAULT_COINS = 1000;

        public PlayerProfile Profile { get; private set; }
        public int Coins { get; private set; }
        private Dictionary<string, GameRecord> _records = new();

        public event System.Action<int> OnCoinsChanged;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        // ── 프로필 ─────────────────────────────────────────────
        public void SaveProfile(PlayerProfile profile)
        {
            Profile = profile;
            PlayerPrefs.SetString(KEY_PROFILE, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }

        // ── 점수 ───────────────────────────────────────────────
        public void SaveScore(string gameName, int score)
        {
            if (!_records.TryGetValue(gameName, out GameRecord rec))
                rec = new GameRecord { gameName = gameName };

            rec.playCount++;
            rec.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (score > rec.highScore) rec.highScore = score;

            _records[gameName] = rec;
            SaveRecords();
        }

        public int GetHighScore(string gameName)
            => _records.TryGetValue(gameName, out var rec) ? rec.highScore : 0;

        public GameRecord GetRecord(string gameName)
            => _records.TryGetValue(gameName, out var rec) ? rec : null;

        // ── 코인 (내기 바둑 등) ──────────────────────────────
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            SaveCoins();
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            SaveCoins();
            OnCoinsChanged?.Invoke(Coins);
        }

        public void ResetCoins()
        {
            Coins = DEFAULT_COINS;
            SaveCoins();
            OnCoinsChanged?.Invoke(Coins);
        }

        private void SaveCoins()
        {
            PlayerPrefs.SetInt(KEY_COINS, Coins);
            PlayerPrefs.Save();
        }

        // ── 내부 ───────────────────────────────────────────────
        private void LoadAll()
        {
            string profileJson = PlayerPrefs.GetString(KEY_PROFILE, "");
            Profile = string.IsNullOrEmpty(profileJson)
                ? new PlayerProfile()
                : JsonUtility.FromJson<PlayerProfile>(profileJson);

            Coins = PlayerPrefs.GetInt(KEY_COINS, DEFAULT_COINS);

            // TODO: GameRecord 목록 로드 (직렬화 방식 확정 후 구현)
        }

        private void SaveRecords()
        {
            // TODO: GameRecord 목록 직렬화 저장
            PlayerPrefs.Save();
        }
    }
}
