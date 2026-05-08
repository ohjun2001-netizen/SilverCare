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
        public string color;     // "black" | "white"
        public string comment;   // 비어있을 수 있음 (수별 특별 해설)
    }

    [Serializable]
    public class PredictionPoint
    {
        public int move_index;                 // 이 인덱스의 수를 두기 직전에 예측 발동 (0-based)
        public List<StonePosition> candidates; // 후보 위치들
        public int correct_index;              // candidates 중 실제 수와 일치하는 인덱스
        public string question;                // 질문 (없으면 기본 문구)
        public string explanation;             // 정답/오답 공개 후 해설
    }

    [Serializable]
    public class Kifu
    {
        public string id;
        public string title;          // 예: "이세돌 vs 알파고 4국"
        public string black_player;
        public string white_player;
        public string date;
        public int board_size;        // 9 | 13 | 19
        public int[] view_area;       // [row_start, col_start, row_end, col_end]
        public string description;    // 기보 소개 (재생 시작 전 TTS)
        public List<KifuMove> moves;
        public string result;         // 표시용 텍스트, 예: "백 불계승"
        public string winner;         // 판정용: "black" | "white" | "draw"
        public List<PredictionPoint> prediction_points;  // 다음 수 맞히기 모드 전용 (없으면 무시)
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
        public List<string> opening;       // 0~10수: 포석 단계
        public List<string> midgame;       // 11~50수: 중반
        public List<string> endgame;       // 51수~: 종반
        public List<string> good_move;     // 가끔 랜덤으로 칭찬
        public List<string> bad_move;      // 가끔 랜덤으로 비판
        public List<string> idle;          // 매 수마다 후보 (중립적 추임새)
    }
}
