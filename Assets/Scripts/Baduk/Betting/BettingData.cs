// Assets/Scripts/Baduk/Betting/BettingData.cs
using System;
using UnityEngine;

namespace Baduk.Data
{
    public enum BettingChoice { None, Black, White }

    [Serializable]
    public class BettingTicket
    {
        public BettingChoice choice;
        public int amount;          // 베팅 코인
        public int potentialPayout; // 예상 정산액 (이기면 받음)

        public bool IsValid => choice != BettingChoice.None && amount > 0;
    }

    public enum BettingResult { Pending, Win, Lose, Draw }

    public static class BettingPayout
    {
        // 단순 1.8배 정산 (운영 마진 0.2)
        public const float WinMultiplier = 1.8f;

        public static int CalculatePayout(int bet)
            => Mathf.RoundToInt(bet * WinMultiplier);
    }
}
