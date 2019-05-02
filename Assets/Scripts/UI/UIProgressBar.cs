using Assets.Scripts.Utils.Debugger;
using Assets.Scripts.Utils.UI;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class UIProgressBar : MonoBehaviour
    {
        public RectTransform FillTransform;
        public UIShaker Shaker;

        public float Initial = 1f;
        public float ChangeTime = 0.5f;

        private float _value;
        private float _target;
        private Vector2 _initialSize;
        private float _velocity;

        void Start()
        {
            _initialSize = FillTransform.sizeDelta;
            SetValue(Initial);
        }

        void Update()
        {
            SetValue(Mathf.SmoothDamp(_value, _target, ref _velocity, ChangeTime));

            if (Shaker != null)
                Shaker.Shake(Mathf.Abs(_velocity));
        }

        void SetValue(float val)
        {
            if (FillTransform != null)
            {
                _value = Mathf.Clamp01(val);
                FillTransform.sizeDelta = new Vector2(_initialSize.x * _value, _initialSize.y);
            }
        }

        public void SetTarget(float value)
        {
            _target = value;
        }
    }
}
