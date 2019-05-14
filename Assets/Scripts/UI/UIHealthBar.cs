using Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIHealthBar : MonoBehaviour
    {
        public Image BarImage;
        public TextMeshProUGUI HpText;
        public float SmoothTime = 0.1f;

        private float _velocity;
        private float _fillWhite;
        private float _fillExact;
        private CharacterState _character;
        private Transform _charTransform;
        private Camera _mainCamera;
        private float _offset;

        void Awake()
        {
            _mainCamera = Camera.main;
        }

        void Update()
        {
            if (_charTransform != null)
            {
                _fillWhite = Mathf.SmoothDamp(_fillWhite, _fillExact, ref _velocity, SmoothTime);
                BarImage.color = new Color(_fillExact, _fillWhite, 1, 1);

                var scr = _mainCamera.WorldToScreenPoint(_charTransform.position + Vector3.up * _offset);
                transform.position = new Vector3(scr.x, scr.y, transform.position.z);
            }
        }

        public void Setup(CharacterState character)
        {
            _character = character;
            _charTransform = _character.GetNodeTransform(CharacterState.NodeRole.Head);

            //_offset = _charTransform.transform.position.y - character.transform.position.y;
            _offset = 0.5f;


            _fillExact = character.Health / character.MaxHealth;
            _character.Died += () =>
            {
                 Destroy(gameObject, 0.5f);
            };

            _character.HealthChanged += CharacterOnHealthChanged;
        }

        private void CharacterOnHealthChanged(float newHealth)
        {
            _fillExact = newHealth / _character.MaxHealth;
        }
    }
}
