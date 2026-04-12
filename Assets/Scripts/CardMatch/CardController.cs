// Assets/Scripts/CardMatch/CardController.cs
// 담당: 박건영
// TODO: VR 컨트롤러 집기/놓기 연동, 플립 애니메이션
using UnityEngine;

namespace SilverCare.CardMatch
{
    public class CardController : MonoBehaviour
    {
        [SerializeField] private int cardId;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Renderer cardRenderer;

        public int  CardId    => cardId;
        public bool IsFaceUp  { get; private set; }
        public bool IsMatched { get; private set; }

        private CardMatchGameManager _manager;

        public void Init(int id, Sprite front, CardMatchGameManager manager)
        {
            cardId       = id;
            frontSprite  = front;
            _manager     = manager;
            IsFaceUp     = false;
            IsMatched    = false;
        }

        public void FlipUp()
        {
            IsFaceUp = true;
            // TODO: 박건영 - 뒤집기 애니메이션 (iTween or DOTween)
            // cardRenderer.material.mainTexture = frontSprite.texture;
        }

        public void FlipDown()
        {
            IsFaceUp = false;
            // TODO: 박건영 - 뒤집기 애니메이션
        }

        public void SetMatched()
        {
            IsMatched = true;
            // TODO: 박건영 - 매칭 이펙트 (빛나기, 사라지기 등)
        }

        // VR 컨트롤러 또는 마우스 클릭
        private void OnMouseDown() => _manager?.OnCardFlipped(this);
    }
}
