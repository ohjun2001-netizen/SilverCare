// Unity 복사 경로: Assets/Scripts/Baduk/Data/BadukProblemData.cs
using System;
using System.Collections.Generic;

namespace Baduk.Data
{
    [Serializable]
    public class StonePosition
    {
        public int row;
        public int col;
    }

    [Serializable]
    public class BoardStones
    {
        public List<StonePosition> black;
        public List<StonePosition> white;
    }

    [Serializable]
    public class BadukProblem
    {
        public int id;
        public string title;
        public int difficulty;           // 1=초급, 2=중급, 3=고급
        public string difficulty_name;
        public string description;
        public string hint;
        public string tts_hint;
        public string tts_correct;
        public string tts_wrong;
        public int board_size;
        public int[] view_area;          // [row_start, col_start, row_end, col_end]
        public BoardStones stones;
        public string player;            // "black" | "white"
        public string goal;              // "survive" | "kill" | "ko"
        public List<StonePosition> answer;
        public string explanation;
    }

    [Serializable]
    public class BadukProblemDatabase
    {
        public string version;
        public int total;
        public List<BadukProblem> problems;
    }

    public enum StoneType { Empty, Black, White }
    public enum GameGoal { Survive, Kill, Ko }
    public enum Difficulty { Easy = 1, Medium = 2, Hard = 3 }

    public enum ProblemResult
    {
        Correct,
        Wrong,
        PartialCorrect  // 연속 정답 중 첫 번째만 맞춘 경우
    }
}
