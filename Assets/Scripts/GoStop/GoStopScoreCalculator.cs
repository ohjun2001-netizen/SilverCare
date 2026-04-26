// Assets/Scripts/GoStop/GoStopScoreCalculator.cs
// 담당: 조성우
// TODO: 광·열끗·띠·피 점수 규칙 완성, 고 배수 적용
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverCare.GoStop
{
    public class GoStopScoreCalculator : MonoBehaviour
    {
        public int Calculate(List<HwatooCard> captured, int goCount)
        {
            int score = 0;
            score += CalcGwang(captured);
            score += CalcYul(captured);
            score += CalcTti(captured);
            score += CalcPi(captured);

            // 고 배수 적용
            for (int i = 0; i < goCount; i++)
                score = (int)(score * 1.5f);

            return score;
        }

        private int CalcGwang(List<HwatooCard> cards)
        {
            int gwangCount = cards.Count(c => c.cardType == CardType.Gwang);
            // TODO: 조성우 - 3광=3점, 4광=4점(비광없이), 5광=15점, 비광포함3광=2점
            return gwangCount >= 3 ? gwangCount * 3 : 0;
        }

        private int CalcYul(List<HwatooCard> cards)
        {
            int yulCount = cards.Count(c => c.cardType == CardType.Yul);
            // TODO: 조성우 - 5개=1점, 이후 1개당 1점 추가
            return Mathf.Max(0, yulCount - 4);
        }

        private int CalcTti(List<HwatooCard> cards)
        {
            int ttiCount = cards.Count(c => c.cardType == CardType.Tti);
            // TODO: 조성우 - 5개=1점, 이후 1개당 1점 추가, 청단/홍단/초단 보너스
            return Mathf.Max(0, ttiCount - 4);
        }

        private int CalcPi(List<HwatooCard> cards)
        {
            int piCount = cards.Count(c => c.cardType == CardType.Pi);
            // TODO: 조성우 - 10개=1점, 이후 1개당 1점 추가
            return Mathf.Max(0, piCount - 9);
        }
    }
}
