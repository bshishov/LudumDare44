using UnityEngine;

namespace Assets.Scripts.Utils.UI
{
    public class UIShaker : MonoBehaviour
    {
        public RectTransform TargetTransform;
        [Range(0.001f, 10f)] public float ShakeDecay = 1f;
        [Range(0, 50f)] public float ShakeAmplitude = 50f;

        private float _shakeValue = 0f;
        private Vector3 _initialLocalPos;

        void Start ()
        {
            if (TargetTransform == null)
                TargetTransform = GetComponent<RectTransform>();

            if (TargetTransform == null)
            {
                Debug.LogError("[UIShaker] Target transform is null. Turning off the shaker component");
                this.enabled = false;
                return;
            }

            _initialLocalPos = TargetTransform.localPosition;
        }
        
        void Update ()
        {
            _shakeValue = Mathf.Clamp01(_shakeValue - ShakeDecay * Time.deltaTime);
            var d = Mathf.Pow(_shakeValue, 3);
            if (d > 0f)
            {
                TargetTransform.localPosition = _initialLocalPos + 
                    new Vector3(
                        d * Random.Range(-ShakeAmplitude, ShakeAmplitude), 
                        d * Random.Range(-ShakeAmplitude, ShakeAmplitude), 0);
            }
        }

        public void Shake(float force = 1f)
        {
            _shakeValue += force;
        }
    }
}
