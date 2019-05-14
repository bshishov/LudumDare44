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
        }
    
        void Update()
        {
            if(_difficultyManager == null)
                return;

            var prevDifficulty = _difficultyManager.GetDifficulty(_difficultyManager.CurrentDifficultyIndex - 1);
            var currentDifficulty = _difficultyManager.GetDifficulty(_difficultyManager.CurrentDifficultyIndex);
            var nextDifficulty = _difficultyManager.GetDifficulty(_difficultyManager.CurrentDifficultyIndex + 1);
            
            if (nextDifficulty == null)
                NextDiffText.text = "Infinity";
            else
                NextDiffText.text = nextDifficulty.DifficultyName;

            if (prevDifficulty == null)
                DiffSlider.minValue = 0;
            else
                DiffSlider.minValue = prevDifficulty.NextDifficultyStamp;

            if (currentDifficulty != null)
            {
                CurrDiffText.text = currentDifficulty.DifficultyName;
                DiffSlider.value = _difficultyManager.GetTimeSinceStart();
                DiffSlider.maxValue = currentDifficulty.NextDifficultyStamp;
            }
        }
    }
}
