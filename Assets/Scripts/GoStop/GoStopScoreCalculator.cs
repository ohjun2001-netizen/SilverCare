// Assets/Scripts/GoStop/GoStopScoreCalculator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverCare.GoStop
{
    public sealed class GoStopScoreResult
    {
        public int gwangScore;
        public int yulScore;
        public int ttiScore;
        public int piScore;
        public int meldScore;
        public int goBonus;
        public int multiplier = 1;
        public int finalScore;
        public string multiplierLabel = "";
    }

    public class GoStopScoreCalculator : MonoBehaviour
    {
        public int Calculate(List<HwatooCard> captured, int goCount)
            => CalculateDetailed(captured, null, goCount, 0, 0, 0, false).finalScore;

        public GoStopScoreResult CalculateDetailed(
            List<HwatooCard> captured,
            List<HwatooCard> opponentCaptured,
            int goCount,
            int shakeCount,
            int bombCount,
            int opponentGoCount,
            bool applyFinalMultipliers)
        {
            var result = new GoStopScoreResult
            {
                gwangScore = CalcGwang(captured),
                yulScore   = CalcYul(captured),
                ttiScore   = CalcTti(captured),
                piScore    = CalcPi(captured)
            };

            result.meldScore = result.gwangScore + result.yulScore + result.ttiScore + result.piScore;
            result.goBonus = goCount <= 2 ? goCount : 0;

            int scoreBeforeMultiplier = result.meldScore + result.goBonus;
            int multiplier = goCount >= 3 ? 1 << (goCount - 2) : 1;
            var labels = new List<string>();
            if (multiplier > 1) labels.Add($"{goCount}고 x{multiplier}");

            if (applyFinalMultipliers && opponentCaptured != null)
            {
                if (result.piScore > 0 && CountPi(opponentCaptured) <= 5)
                {
                    multiplier *= 2;
                    labels.Add("피박 x2");
                }

                if (result.gwangScore > 0 && opponentCaptured.Count(c => c.cardType == CardType.Gwang) == 0)
                {
                    multiplier *= 2;
                    labels.Add("광박 x2");
                }

                if (captured.Count(c => c.cardType == CardType.Yul) >= 7)
                {
                    multiplier *= 2;
                    labels.Add("멍박 x2");
                }

                int powerBonusCount = Mathf.Max(0, shakeCount) + Mathf.Max(0, bombCount);
                if (powerBonusCount > 0)
                {
                    int powerMultiplier = 1 << powerBonusCount;
                    multiplier *= powerMultiplier;
                    labels.Add($"흔들기/폭탄 x{powerMultiplier}");
                }

                if (opponentGoCount > 0)
                {
                    multiplier *= 2;
                    labels.Add("고박 x2");
                }
            }

            result.multiplier = multiplier;
            result.finalScore = scoreBeforeMultiplier * multiplier;
            result.multiplierLabel = labels.Count == 0 ? "없음" : string.Join(", ", labels);
            return result;
        }

        static int CalcGwang(List<HwatooCard> cards)
        {
            var gwang = cards.Where(c => c.cardType == CardType.Gwang).ToList();
            int count = gwang.Count;
            if (count < 3) return 0;
            if (count == 5) return 15;
            if (count == 4) return 4;
            return gwang.Any(c => c.month == 12) ? 2 : 3;
        }

        static int CalcYul(List<HwatooCard> cards)
        {
            var yul = cards.Where(c => c.cardType == CardType.Yul).ToList();
            int baseScore = yul.Count >= 5 ? yul.Count - 4 : 0;
            var months = yul.Select(c => c.month).ToHashSet();
            int godori = months.Contains(2) && months.Contains(4) && months.Contains(8) ? 5 : 0;
            return baseScore + godori;
        }

        static int CalcTti(List<HwatooCard> cards)
        {
            var tti = cards.Where(c => c.cardType == CardType.Tti).ToList();
            int baseScore = tti.Count >= 5 ? tti.Count - 4 : 0;
            var months = tti.Select(c => c.month).ToHashSet();

            int bonus = 0;
            if (months.Contains(1) && months.Contains(2) && months.Contains(3)) bonus += 3;
            if (months.Contains(6) && months.Contains(9) && months.Contains(10)) bonus += 3;
            if (months.Contains(4) && months.Contains(5) && months.Contains(7)) bonus += 3;
            return baseScore + bonus;
        }

        static int CalcPi(List<HwatooCard> cards)
        {
            int count = CountPi(cards);
            return count >= 10 ? count - 9 : 0;
        }

        static int CountPi(List<HwatooCard> cards)
            => cards.Where(c => c.cardType == CardType.Pi).Sum(c => c.isDoublePi ? 2 : 1);

        public string ScoreBreakdown(List<HwatooCard> captured, int goCount)
        {
            var score = CalculateDetailed(captured, null, goCount, 0, 0, 0, false);
            return $"광 {score.gwangScore}, 열끗 {score.yulScore}, 띠 {score.ttiScore}, 피 {score.piScore}, 고 +{score.goBonus} = {score.finalScore}";
        }
    }
}
