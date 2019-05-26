using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIDifficulty : MonoBehaviour
    {
        public TextMeshProUGUI CurrDiffText;
        public TextMeshProUGUI NextDiffText;
        public Slider DiffSlider;

        private DifficultyManager _difficultyManager;
    
        void Start()
        {
            _difficultyManager = DifficultyManager.Instance;
            if (_difficultyManager == null)
            {
                Debug.Log("Difficulty manager is missing in the scene, disabling difficulty UI", this);
                this.gameObject.SetActive(false);
            }

            if (DiffSlider != null)
            {
                DiffSlider.minValue = 0;
                DiffSlider.maxValue = 1;
            }
        }
    
        void Update()
        {
            if (CurrDiffText != null)
                CurrDiffText.text = _difficultyManager.CurrentDifficulty.DifficultyName;

            if (NextDiffText != null)
                NextDiffText.text = _difficultyManager.CurrentDifficulty.DifficultyName;

            if (DiffSlider != null)
                DiffSlider.value = _difficultyManager.Progress;
        }
    }
}
