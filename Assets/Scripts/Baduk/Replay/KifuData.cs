// Assets/Scripts/Baduk/Replay/KifuData.cs
using System;
using System.Collections.Generic;

namespace Baduk.Data
{
    [Serializable]
    public class KifuMove
    {
        public int row;
        public int col;
        public string color;   // "black" | "white"
        public string comment; // Optional move comment.
    }

    [Serializable]
    public class PredictionPoint
    {
        public int move_index;                   // Pause after this many moves have been played.
        public List<StonePosition> candidates;   // Candidate intersections.
        public int correct_index;                // Index matching the real next move.
        public string question;                  // Prompt shown to the player.
        public string explanation;               // Overall explanation after answering.
        public List<string> candidate_labels;    // Optional intent labels for each candidate.
        public List<string> candidate_feedback;  // Optional per-candidate feedback after selection.
    }

    [Serializable]
    public class Kifu
    {
        public string id;
        public string title;
        public string black_player;
        public string white_player;
        public string date;
        public int board_size;       // 9 | 13 | 19
        public int[] view_area;      // [row_start, col_start, row_end, col_end]
        public string description;
        public List<KifuMove> moves;
        public string result;
        public string winner;        // "black" | "white" | "draw"
        public List<PredictionPoint> prediction_points;
    }

    [Serializable]
    public class KifuDatabase
    {
        public string version;
        public int total;
        public List<Kifu> kifus;
    }

    [Serializable]
    public class NpcCommentPool
    {
        public List<string> opening;
        public List<string> midgame;
        public List<string> endgame;
        public List<string> good_move;
        public List<string> bad_move;
        public List<string> idle;
    }
}
