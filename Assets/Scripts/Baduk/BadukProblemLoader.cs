// Unity 복사 경로: Assets/Scripts/Baduk/BadukProblemLoader.cs
// JSON 파일 위치: Assets/Resources/Data/baduk_problems.json
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukProblemLoader : MonoBehaviour
    {
        private const string JSON_PATH = "Data/baduk_problems";

        private BadukProblemDatabase _database;

        public int TotalProblems => _database?.total ?? 0;

        void Awake()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
            if (jsonFile == null)
            {
                Debug.LogError($"[BadukProblemLoader] JSON 파일을 찾을 수 없습니다: Resources/{JSON_PATH}");
                return;
            }

            _database = JsonUtility.FromJson<BadukProblemDatabase>(jsonFile.text);
            Debug.Log($"[BadukProblemLoader] 총 {_database.total}개 문제 로드 완료 (ver {_database.version})");
        }

        /// <summary>ID로 문제 가져오기 (1-based)</summary>
        public BadukProblem GetProblemById(int id)
        {
            return _database?.problems.FirstOrDefault(p => p.id == id);
        }

        /// <summary>난이도별 문제 목록 반환</summary>
        public List<BadukProblem> GetProblemsByDifficulty(int difficulty)
        {
            return _database?.problems
                .Where(p => p.difficulty == difficulty)
                .ToList() ?? new List<BadukProblem>();
        }

        /// <summary>랜덤 문제 반환 (난이도 필터 선택)</summary>
        public BadukProblem GetRandomProblem(int difficulty = 0)
        {
            List<BadukProblem> pool = difficulty == 0
                ? _database.problems
                : GetProblemsByDifficulty(difficulty);

            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>순서대로 다음 문제 반환</summary>
        public BadukProblem GetNextProblem(int currentId)
        {
            int nextId = currentId + 1;
            if (nextId > TotalProblems) nextId = 1;
            return GetProblemById(nextId);
        }
    }
}
