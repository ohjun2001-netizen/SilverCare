// Assets/Scripts/Baduk/Betting/BettingCommentPool.cs
using System;
using System.Collections.Generic;

namespace Baduk.Data
{
    [Serializable]
    public class BettingCommentPool
    {
        public List<string> opening;
        public List<string> midgame;
        public List<string> endgame;
        public List<string> good_move;
        public List<string> bad_move;
        public List<string> idle;
        public List<string> win;     // 사용자 베팅 적중 시
        public List<string> lose;    // 사용자 베팅 실패 시
    }
}
