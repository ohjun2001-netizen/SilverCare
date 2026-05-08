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
        TextMesh   _label;

        public void SetFaces(GameObject front, GameObject back)
        {
            _front         = front;
            _back          = back;
            _frontRenderer = front?.GetComponent<Renderer>();
        }

        public void Init(int id, Texture frontTex)
        {
            CardId    = id;
            IsFaceUp  = false;
            IsMatched = false;

            if (_frontRenderer != null)
            {
                var shader = Shader.Find("Unlit/Texture")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Standard");
                var mat = new Material(shader);
                mat.mainTexture = frontTex;
                if (mat.HasProperty("_MainTex"))   mat.SetTexture("_MainTex", frontTex);
                if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap", frontTex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", Color.white);
                _frontRenderer.material = mat;
            }

            EnsureLabel();
            if (_label != null)
            {
                _label.text = (id + 1).ToString();
                _label.color = Color.black;
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
            IsMatched = IsFaceUp = true;
            Refresh();
            if (_frontRenderer != null)
            {
                var c = new Color(0.65f, 1f, 0.65f);
                var source = _frontRenderer.material;
                var mat = source != null ? new Material(source) : new Material(Shader.Find("Standard"));
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                _frontRenderer.material = mat;
            }

            if (_label != null) _label.color = new Color(0.05f, 0.35f, 0.05f);
        }

        void Refresh()
        {
            if (_front != null) _front.SetActive(IsFaceUp);
            if (_back  != null) _back.SetActive(!IsFaceUp);
        }

        void EnsureLabel()
        {
            if (_front == null || _label != null) return;

            var labelGo = new GameObject("CardLabel");
            labelGo.transform.SetParent(_front.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            labelGo.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 64;
            _label.characterSize = 0.35f;
            _label.fontStyle = FontStyle.Bold;
            _label.text = "";
        }

    }
}
