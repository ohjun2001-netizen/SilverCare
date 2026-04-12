// Assets/Scripts/Quiz/QuizGameManager.cs
// 담당: 박건영
// TODO: 퀴즈 JSON 로드, GPT-4o 연동 문제 생성, 난이도 조절
using System.Collections.Generic;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Quiz
{
    [System.Serializable]
    public class QuizQuestion
    {
        public int    id;
        public string question;
        public string[] choices;    // 4지선다
        public int    answerIndex;  // 0~3
        public string explanation;
        public string category;     // "역사", "문화", "생활" 등
        public int    difficulty;   // 1~3
    }

    [System.Serializable]
    public class QuizDatabase { public List<QuizQuestion> questions; }

    public class QuizGameManager : BaseGameManager
    {
        [Header("Quiz Settings")]
        [SerializeField] private int totalQuestions = 10;
        [SerializeField] private QuizUIManager quizUI;

        private QuizDatabase _db;
        private List<QuizQuestion> _pool;
        private QuizQuestion _current;
        private int _questionIndex = 0;

        protected override void InitGame()
        {
            gameTitle = "세대 공감 퀴즈";
            LoadQuizDB();
        }

        protected override void StartGame()
        {
            _questionIndex = 0;
            _score = 0;
            ShufflePool();
            ShowNextQuestion();
        }

        protected override void EndGame()
        {
            quizUI?.ShowFinalResult(_score, totalQuestions);
        }

        public void OnAnswerSelected(int choiceIndex)
        {
            if (_current == null) return;

            bool correct = choiceIndex == _current.answerIndex;
            if (correct)
            {
                _score += 100;
                AudioManager.Instance?.PlayCorrect();
                TTSManager.Instance?.Speak("정답입니다! " + _current.explanation);
            }
            else
            {
                AudioManager.Instance?.PlayWrong();
                TTSManager.Instance?.Speak("틀렸습니다. 정답은 " + _current.choices[_current.answerIndex] + "입니다.");
            }

            quizUI?.ShowAnswerResult(correct, _current.answerIndex);

            _questionIndex++;
            if (_questionIndex >= totalQuestions)
                OnGameClear();
            else
                Invoke(nameof(ShowNextQuestion), 2f);
        }

        private void ShowNextQuestion()
        {
            if (_questionIndex >= _pool.Count) return;
            _current = _pool[_questionIndex];
            quizUI?.ShowQuestion(_current, _questionIndex + 1, totalQuestions);
            TTSManager.Instance?.Speak(_current.question);
        }

        private void LoadQuizDB()
        {
            TextAsset json = Resources.Load<TextAsset>("Data/quiz_questions");
            if (json != null)
                _db = JsonUtility.FromJson<QuizDatabase>(json.text);
            // TODO: 박건영 - GPT-4o로 동적 문제 생성 옵션 추가
        }

        private void ShufflePool()
        {
            _pool = new List<QuizQuestion>(_db?.questions ?? new List<QuizQuestion>());
            for (int i = _pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_pool[i], _pool[j]) = (_pool[j], _pool[i]);
            }
        }
    }
}
