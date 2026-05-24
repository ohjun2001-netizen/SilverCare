// Assets/Scripts/GoStop/GoStopDeck.cs
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.GoStop
{
    public enum CardType { Gwang, Yul, Tti, Pi }
    public enum TtiType  { None, Red, Blue, Grass }

    public class HwatooCard
    {
        public int      index;      // 0~47 (BuildDeck order)
        public int      month;
        public CardType cardType;
        public TtiType  ttiType;
        public string   cardName;
        public bool     isDoublePi; // Counts as 2 pi
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
            var c = _deck[0];
            _deck.RemoveAt(0);
            return c;
        }

        void BuildDeck()
        {
            // Keep this order aligned with Resources/GoStop/card_00..card_47.
            var defs = new (int month, CardType type, TtiType tti, string name, bool isDoublePi)[]
            {
                (1,  CardType.Gwang, TtiType.None,  "Jan Gwang",       false),
                (1,  CardType.Tti,   TtiType.Red,   "Jan Hongdan",     false),
                (1,  CardType.Pi,    TtiType.None,  "Jan Pi 1",        false),
                (1,  CardType.Pi,    TtiType.None,  "Jan Pi 2",        false),

                (2,  CardType.Yul,   TtiType.None,  "Feb Bird",        false),
                (2,  CardType.Tti,   TtiType.Red,   "Feb Hongdan",     false),
                (2,  CardType.Pi,    TtiType.None,  "Feb Pi 1",        false),
                (2,  CardType.Pi,    TtiType.None,  "Feb Pi 2",        false),

                (3,  CardType.Gwang, TtiType.None,  "Mar Gwang",       false),
                (3,  CardType.Tti,   TtiType.Red,   "Mar Hongdan",     false),
                (3,  CardType.Pi,    TtiType.None,  "Mar Pi 1",        false),
                (3,  CardType.Pi,    TtiType.None,  "Mar Pi 2",        false),

                (4,  CardType.Yul,   TtiType.None,  "Apr Cuckoo",      false),
                (4,  CardType.Tti,   TtiType.Grass, "Apr Chodan",      false),
                (4,  CardType.Pi,    TtiType.None,  "Apr Pi 1",        false),
                (4,  CardType.Pi,    TtiType.None,  "Apr Pi 2",        false),

                (5,  CardType.Yul,   TtiType.None,  "May Bridge",      false),
                (5,  CardType.Tti,   TtiType.Grass, "May Chodan",      false),
                (5,  CardType.Pi,    TtiType.None,  "May Pi 1",        false),
                (5,  CardType.Pi,    TtiType.None,  "May Pi 2",        false),

                (6,  CardType.Yul,   TtiType.None,  "Jun Butterfly",   false),
                (6,  CardType.Tti,   TtiType.Blue,  "Jun Cheongdan",   false),
                (6,  CardType.Pi,    TtiType.None,  "Jun Pi 1",        false),
                (6,  CardType.Pi,    TtiType.None,  "Jun Pi 2",        false),

                (7,  CardType.Yul,   TtiType.None,  "Jul Boar",        false),
                (7,  CardType.Tti,   TtiType.Grass, "Jul Chodan",      false),
                (7,  CardType.Pi,    TtiType.None,  "Jul Pi 1",        false),
                (7,  CardType.Pi,    TtiType.None,  "Jul Pi 2",        false),

                (8,  CardType.Gwang, TtiType.None,  "Aug Gwang",       false),
                (8,  CardType.Yul,   TtiType.None,  "Aug Goose",       false),
                (8,  CardType.Pi,    TtiType.None,  "Aug Pi 1",        false),
                (8,  CardType.Pi,    TtiType.None,  "Aug Pi 2",        false),

                (9,  CardType.Yul,   TtiType.None,  "Sep Cup",         false),
                (9,  CardType.Tti,   TtiType.Blue,  "Sep Cheongdan",   false),
                (9,  CardType.Pi,    TtiType.None,  "Sep Pi 1",        false),
                (9,  CardType.Pi,    TtiType.None,  "Sep Pi 2",        false),

                (10, CardType.Yul,   TtiType.None,  "Oct Deer",        false),
                (10, CardType.Tti,   TtiType.Blue,  "Oct Cheongdan",   false),
                (10, CardType.Pi,    TtiType.None,  "Oct Pi 1",        false),
                (10, CardType.Pi,    TtiType.None,  "Oct Pi 2",        false),

                (11, CardType.Gwang, TtiType.None,  "Nov Gwang",       false),
                (11, CardType.Pi,    TtiType.None,  "Nov Double Pi",   true),
                (11, CardType.Pi,    TtiType.None,  "Nov Pi 1",        false),
                (11, CardType.Pi,    TtiType.None,  "Nov Pi 2",        false),

                (12, CardType.Gwang, TtiType.None,  "Dec Bi-Gwang",    false),
                (12, CardType.Yul,   TtiType.None,  "Dec Phoenix",     false),
                (12, CardType.Tti,   TtiType.None,  "Dec Ribbon",      false),
                (12, CardType.Pi,    TtiType.None,  "Dec Pi",          false),
            };

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                _deck.Add(new HwatooCard
                {
                    index      = i,
                    month      = d.month,
                    cardType   = d.type,
                    ttiType    = d.tti,
                    cardName   = d.name,
                    isDoublePi = d.isDoublePi
                });
            }
        }
    }
}
