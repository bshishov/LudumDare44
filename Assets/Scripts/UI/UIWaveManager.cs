using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIWaveManager : MonoBehaviour
    {
        public TextMeshProUGUI CurrentWave;
        public TextMeshProUGUI EnemyCounter;
        public Image ProgressBar;
        public Image DifficultyProgressBar;
        public TextMeshProUGUI DifficultyName;

        private WaveManager _waveManager;
        private DifficultyManager _difficultyManager;

        void Start()
        {
            _difficultyManager = DifficultyManager.Instance;
            _waveManager = WaveManager.Instance;
            if (_waveManager == null)
            {
                Debug.Log("Wave manager is missing in the scene, disabling WaveManager UI", this);
                this.gameObject.SetActive(false);
            }
            else
            {
                OnNewWaveStarted();
                _waveManager.NewWaveStarted += OnNewWaveStarted;
            }
        }

        private void OnNewWaveStarted()
        {
            CurrentWave.text = $"{_waveManager.WaveNumber:0}";
        }

        void Update()
        {
            if (_waveManager.WaveInProgress)
            {
                if (EnemyCounter != null)
                    EnemyCounter.text = $"{_waveManager.EnemiesKilled:0} / {_waveManager.EnemiesInThisWave:0}";

                if (ProgressBar != null)
                    ProgressBar.fillAmount = _waveManager.Progress;
            }
            else
            {
                if (EnemyCounter != null)
                    EnemyCounter.text = $"{Mathf.Round(_waveManager.TimeToNextWave):0} s";

                if (ProgressBar != null)
                    ProgressBar.fillAmount = 1f;
            }


            if (_difficultyManager != null)
            {
                if (DifficultyProgressBar != null)
                    DifficultyProgressBar.fillAmount = _difficultyManager.Progress;

                if (DifficultyName != null)
                    DifficultyName.text = _difficultyManager.CurrentDifficulty.DifficultyName;
            }
        }
    }
}
