using Actors;
using Assets.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIHealthBar : MaskableGraphic
    {
        [SerializeField]
        Texture _texture;

        // make it such that unity will trigger our ui element to redraw whenever we change the texture in the inspector
        public Texture Texture
        {
            get => _texture;
            set
            {
                if (_texture == value)
                    return;

                _texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public TextMeshProUGUI HpText;
        public float SmoothTime = 0.5f;
        public float OneBarWidth = 10f;

        private float _velocity;
        private float _fillWhite;
        private float _fillExact;
        private CharacterState _character;
        private Transform _charTransform;
        private Camera _mainCamera;
        private float _offset;
        private float _divisor;

        void Update()
        {
            if (_charTransform != null)
            {
                _fillWhite = Mathf.SmoothDamp(_fillWhite, _fillExact, ref _velocity, SmoothTime);
                this.color = new Color(_fillExact, _fillWhite, _divisor, 1);

                var scr = _mainCamera.WorldToScreenPoint(_charTransform.position + Vector3.up * _offset);
                transform.position = new Vector3(scr.x, scr.y, transform.position.z);
            }
        }

        public void Setup(CharacterState character)
        {
            _character = character;
            _charTransform = _character.GetNodeTransform(CharacterState.NodeRole.Head);
            _mainCamera = Camera.main;

            //_offset = _charTransform.transform.position.y - character.transform.position.y;
            _offset = 0.5f;


            CharacterOnHealthChanged(character.Health);
            _character.Died += () =>
            {
                 Destroy(gameObject, 0.5f);
            };

            _character.HealthChanged += CharacterOnHealthChanged;
        }

        private void CharacterOnHealthChanged(float newHealth)
        {
            var max = _character.MaxHealth;
            _fillExact = newHealth / max;
            _divisor = OneBarWidth / max;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
            SetMaterialDirty();
        }

        public override Texture mainTexture => _texture == null ? s_WhiteTexture : _texture;

        void AddQuad(VertexHelper vh, Vector2 corner1, Vector2 corner2, Vector2 uvCorner1, Vector2 uvCorner2)
        {
            var i = vh.currentVertCount;

            UIVertex vert = new UIVertex();
            vert.color = this.color;  // Do not forget to set this, otherwise 

            vert.position = corner1;
            vert.uv0 = uvCorner1;
            vh.AddVert(vert);

            vert.position = new Vector2(corner2.x, corner1.y);
            vert.uv0 = new Vector2(uvCorner2.x, uvCorner1.y);
            vh.AddVert(vert);

            vert.position = corner2;
            vert.uv0 = uvCorner2;
            vh.AddVert(vert);

            vert.position = new Vector2(corner1.x, corner2.y);
            vert.uv0 = new Vector2(uvCorner1.x, uvCorner2.y);
            vh.AddVert(vert);

            vh.AddTriangle(i + 0, i + 2, i + 1);
            vh.AddTriangle(i + 3, i + 2, i + 0);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // Clear vertex helper to reset vertices, indices etc.
            vh.Clear();

            // Bottom left corner of the full RectTransform of our UI element
            var bottomLeftCorner = -rectTransform.pivot;
            bottomLeftCorner.x *= rectTransform.rect.width;
            bottomLeftCorner.y *= rectTransform.rect.height;

            var topRightCorner = rectTransform.pivot;
            topRightCorner.x *= rectTransform.rect.width;
            topRightCorner.y *= rectTransform.rect.height;

            AddQuad(vh,
                bottomLeftCorner,
                topRightCorner,
                Vector2.zero, Vector2.one); // UVs
        }
    }
}
