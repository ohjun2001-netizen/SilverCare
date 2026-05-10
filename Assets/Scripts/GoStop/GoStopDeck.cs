// Assets/Scripts/GoStop/GoStopDeck.cs
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.GoStop
{
    public enum CardType { Gwang, Yul, Tti, Pi }
    public enum TtiType  { None, Red, Blue, Grass }

    public class HwatooCard
    {
        public int      index;      // 0~47 (BuildDeck 순서)
        public int      month;
        public CardType cardType;
        public TtiType  ttiType;
        public string   cardName;
    }

    public class GoStopDeck : MonoBehaviour
    {
        readonly List<HwatooCard> _deck = new();
        public int RemainingCount => _deck.Count;

        public void Initialize()
        {
            _deck.Clear();
            BuildDeck();
        }

        public void Shuffle()
        {
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }

        public HwatooCard Draw()
        {
            if (_deck.Count == 0) return null;
            var c = _deck[0]; _deck.RemoveAt(0); return c;
        }

        void BuildDeck()
        {
            // 광: 1·3·6·8·12월 / 홍단: 1·2·3월 / 청단: 6·9·10월 / 초단: 4·5·7월
            var defs = new (int m, CardType t, TtiType tt, string n)[]
            {
                (1,  CardType.Gwang, TtiType.None,  "솔광"),
                (1,  CardType.Yul,   TtiType.None,  "솔학"),
                (1,  CardType.Tti,   TtiType.Red,   "솔홍띠"),
                (1,  CardType.Pi,    TtiType.None,  "솔피"),

                (2,  CardType.Yul,   TtiType.None,  "매조"),
                (2,  CardType.Yul,   TtiType.None,  "매열끗"),
                (2,  CardType.Tti,   TtiType.Red,   "매홍띠"),
                (2,  CardType.Pi,    TtiType.None,  "매피"),

                (3,  CardType.Gwang, TtiType.None,  "벚광"),
                (3,  CardType.Yul,   TtiType.None,  "벚조"),
                (3,  CardType.Tti,   TtiType.Red,   "벚홍띠"),
                (3,  CardType.Pi,    TtiType.None,  "벚피"),

                (4,  CardType.Yul,   TtiType.None,  "등두견"),
                (4,  CardType.Yul,   TtiType.None,  "등열끗"),
                (4,  CardType.Tti,   TtiType.Grass, "등초단"),
                (4,  CardType.Pi,    TtiType.None,  "등피"),

                (5,  CardType.Yul,   TtiType.None,  "난조"),
                (5,  CardType.Yul,   TtiType.None,  "난열끗"),
                (5,  CardType.Tti,   TtiType.Grass, "난초단"),
                (5,  CardType.Pi,    TtiType.None,  "난피"),

                (6,  CardType.Gwang, TtiType.None,  "모란광"),
                (6,  CardType.Yul,   TtiType.None,  "모란나비"),
                (6,  CardType.Tti,   TtiType.Blue,  "모란청단"),
                (6,  CardType.Pi,    TtiType.None,  "모란피"),

                (7,  CardType.Yul,   TtiType.None,  "홍돼지"),
                (7,  CardType.Yul,   TtiType.None,  "홍열끗"),
                (7,  CardType.Tti,   TtiType.Grass, "홍초단"),
                (7,  CardType.Pi,    TtiType.None,  "홍피"),

                (8,  CardType.Gwang, TtiType.None,  "공산광"),
                (8,  CardType.Yul,   TtiType.None,  "공산기러기"),
                (8,  CardType.Tti,   TtiType.None,  "공산띠"),
                (8,  CardType.Pi,    TtiType.None,  "공산피"),

                (9,  CardType.Yul,   TtiType.None,  "국화술"),
                (9,  CardType.Yul,   TtiType.None,  "국화열끗"),
                (9,  CardType.Tti,   TtiType.Blue,  "국화청단"),
                (9,  CardType.Pi,    TtiType.None,  "국화피"),

                (10, CardType.Yul,   TtiType.None,  "단풍사슴"),
                (10, CardType.Yul,   TtiType.None,  "단풍열끗"),
                (10, CardType.Tti,   TtiType.Blue,  "단풍청단"),
                (10, CardType.Pi,    TtiType.None,  "단풍피"),

                (11, CardType.Pi,    TtiType.None,  "오동피1"),
                (11, CardType.Pi,    TtiType.None,  "오동피2"),
                (11, CardType.Pi,    TtiType.None,  "오동피3"),
                (11, CardType.Pi,    TtiType.None,  "오동쌍피"),

                (12, CardType.Gwang, TtiType.None,  "비광"),
                (12, CardType.Yul,   TtiType.None,  "비열끗"),
                (12, CardType.Pi,    TtiType.None,  "비피1"),
                (12, CardType.Pi,    TtiType.None,  "비피2"),
            };

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                _deck.Add(new HwatooCard { index = i, month = d.m, cardType = d.t, ttiType = d.tt, cardName = d.n });
            }
        }
    }
}
