// Assets/Scripts/CardMatch/CardController.cs
using UnityEngine;

namespace SilverCare.CardMatch
{
    public class CardController : MonoBehaviour
    {
        public int  CardId    { get; private set; }
        public bool IsFaceUp  { get; private set; }
        public bool IsMatched { get; private set; }

        GameObject _front, _back;
        Renderer   _frontRenderer;
        Vector3 _basePosition;
        Vector3 _baseScale;
        bool _hasBaseTransform;
        bool _isHovered;

        public void SetFaces(GameObject front, GameObject back)
        {
            _front         = front;
            _back          = back;
            _frontRenderer = front?.GetComponent<Renderer>();
            CacheBaseTransform();
        }

        public void Init(int id, Texture frontTex)
        {
            CardId    = id;
            IsFaceUp  = false;
            IsMatched = false;

            if (_frontRenderer != null)
            {
                // Sprites/Default: 내장 셰이더(항상 포함), 양면, 라이팅 무관
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = frontTex;
                mat.color       = Color.white;
                _frontRenderer.material = mat;
            }

            Refresh();
        }

        public void FlipUp()
        {
            if (IsMatched || IsFaceUp) return;
            IsFaceUp = true;
            Refresh();
        }

        public void FlipDown()
        {
            if (IsMatched) return;
            IsFaceUp = false;
            Refresh();
        }

        public void SetMatched()
        {
            SetHover(false);
            IsMatched = IsFaceUp = true;
            Refresh();
            if (_frontRenderer != null)
            {
                var c   = new Color(0.65f, 1f, 0.65f);
                var mat = _frontRenderer.material;
                mat.color = c;
            }

        }

        public void SetHover(bool hovered)
        {
            CacheBaseTransform();

            if (IsMatched)
                hovered = false;

            if (_isHovered == hovered)
                return;

            _isHovered = hovered;
            transform.position = hovered ? _basePosition + Vector3.up * 0.08f : _basePosition;
            transform.localScale = hovered ? _baseScale * 1.12f : _baseScale;
        }

        void CacheBaseTransform()
        {
            if (_hasBaseTransform)
                return;

            _basePosition = transform.position;
            _baseScale = transform.localScale;
            _hasBaseTransform = true;
        }

        void Refresh()
        {
            if (_front != null) _front.SetActive(IsFaceUp);
            if (_back  != null) _back.SetActive(!IsFaceUp);
        }

    }
}
