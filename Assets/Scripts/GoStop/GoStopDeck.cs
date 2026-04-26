// Assets/Scripts/GoStop/GoStopDeck.cs
// 담당: 조성우
// TODO: 화투 48장 에셋 연결, 월별 패 분류
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.GoStop
{
    public enum CardType { Gwang, Yul, Tti, Pi }  // 광·열끗·띠·피

    [System.Serializable]
    public class HwatooCard
    {
        public int      month;      // 1~12
        public CardType cardType;
        public Sprite   frontSprite;
        public string   cardName;   // "솔광", "매조" 등
    }

    public class GoStopDeck : MonoBehaviour
    {
        [SerializeField] private Sprite[] cardSprites;  // Inspector에서 48장 연결
        private List<HwatooCard> _deck = new();

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
            var card = _deck[0];
            _deck.RemoveAt(0);
            return card;
        }

        public int RemainingCount => _deck.Count;

        private void BuildDeck()
        {
            // TODO: 조성우 - 화투 48장 카드 타입 정의
            // 월 1~12, 각 월 4장 (광/열끗/띠/피 구성은 월마다 다름)
            // 예시: 1월 = 솔광(광), 솔조(열끗), 솔띠(띠), 솔피(피)
            Debug.Log("[GoStopDeck] BuildDeck TODO - 48장 카드 데이터 구성");
        }
    }
}
