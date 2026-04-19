// Assets/Scripts/CardMatch/CardController.cs
// 담당: 박건영
// TODO: VR 컨트롤러 집기/놓기 연동, 플립 애니메이션
using UnityEngine;

namespace SilverCare.CardMatch
{
    public class CardController : MonoBehaviour
    {
        [Header("Card Data")]
        [SerializeField] private int cardId;

        [Header("Card Visuals")]
        [SerializeField] private GameObject frontFaceObject;
        [SerializeField] private GameObject backFaceObject;
        [SerializeField] private MeshRenderer frontRenderer;

        public int CardId => cardId;
        public bool IsFaceUp { get; private set; }
        public bool IsMatched { get; private set; }

        private CardMatchGameManager _manager;

        public void Init(int id, Texture frontTexture, CardMatchGameManager manager)
        {
            cardId = id;
            _manager = manager;

            IsFaceUp = false;
            IsMatched = false;

            if (frontRenderer != null)
            {
                frontRenderer.material.mainTexture = frontTexture;
            }

            ShowBack();
        }

        public void FlipUp()
        {
            if (IsMatched || IsFaceUp) return;

            IsFaceUp = true;
            ShowFront();
        }

        public void FlipDown()
        {
            if (IsMatched) return;

            IsFaceUp = false;
            ShowBack();
        }

        public void SetMatched()
        {
            IsMatched = true;
            IsFaceUp = true;
            ShowFront();

            // 나중에 반짝임/사운드/비활성화 연출 추가
        }

        private void ShowFront()
        {
            if (frontFaceObject != null) frontFaceObject.SetActive(true);
            if (backFaceObject != null) backFaceObject.SetActive(false);
        }

        private void ShowBack()
        {
            if (frontFaceObject != null) frontFaceObject.SetActive(false);
            if (backFaceObject != null) backFaceObject.SetActive(true);
        }

        private void OnMouseDown()
        {
            _manager?.OnCardFlipped(this);
        }
    }
}