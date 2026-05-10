// Assets/Scripts/GoStop/GoStopScoreCalculator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverCare.GoStop
{
    public class GoStopScoreCalculator : MonoBehaviour
    {
        public int Calculate(List<HwatooCard> captured, int goCount)
        {
            int score = CalcGwang(captured) + CalcYul(captured)
                      + CalcTti(captured)   + CalcPi(captured);
            for (int i = 0; i < goCount; i++) score = Mathf.RoundToInt(score * 1.5f);
            return score;
        }

        // 3광(비광제외)=3점 / 3광(비광포함)=2점 / 4광=4점 / 5광=15점
        static int CalcGwang(List<HwatooCard> cards)
        {
            var g = cards.Where(c => c.cardType == CardType.Gwang).ToList();
            int cnt = g.Count;
            if (cnt < 3) return 0;
            if (cnt == 5) return 15;
            if (cnt == 4) return 4;
            return g.Any(c => c.month == 12) ? 2 : 3;
        }

        // 5개=1점, 이후 1개당 +1점
        static int CalcYul(List<HwatooCard> cards)
        {
            int cnt = cards.Count(c => c.cardType == CardType.Yul);
            return cnt >= 5 ? cnt - 4 : 0;
        }

        // 5개=1점, 이후 +1점 / 홍단(1·2·3)+3 / 청단(6·9·10)+3 / 초단(4·5·7)+3
        static int CalcTti(List<HwatooCard> cards)
        {
            var tti    = cards.Where(c => c.cardType == CardType.Tti).ToList();
            int cnt    = tti.Count;
            int base_  = cnt >= 5 ? cnt - 4 : 0;
            var months = tti.Select(c => c.month).ToHashSet();

            int bonus = 0;
            if (months.Contains(1) && months.Contains(2) && months.Contains(3)) bonus += 3;
            if (months.Contains(6) && months.Contains(9) && months.Contains(10)) bonus += 3;
            if (months.Contains(4) && months.Contains(5) && months.Contains(7)) bonus += 3;
            return base_ + bonus;
        }

        // 10개=1점, 이후 1개당 +1점
        static int CalcPi(List<HwatooCard> cards)
        {
            int cnt = cards.Count(c => c.cardType == CardType.Pi);
            return cnt >= 10 ? cnt - 9 : 0;
        }

        public string ScoreBreakdown(List<HwatooCard> captured, int goCount)
        {
            int g = CalcGwang(captured), y = CalcYul(captured),
                t = CalcTti(captured),   p = CalcPi(captured);
            int total = Calculate(captured, goCount);
            return $"광:{g} 열끗:{y} 띠:{t} 피:{p} (고×{goCount}) = {total}점";
        }
    }
}
