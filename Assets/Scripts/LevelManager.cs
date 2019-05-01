using Assets.Scripts.UI;
using Assets.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.Utils
{
    public class LevelManager : Singleton<LevelManager>
    {
        public UI.UICanvasGroupFader ScreenFader;
        public Sound.Sound Music;
        private string _nextLevelRequest;

        void Start()
        {
            if (ScreenFader == null)
                ScreenFader = GetComponent<UI.UICanvasGroupFader>();

            ScreenFader.StateChanged += StateChanged;
            ScreenFader.FadeOut();

            if (Music != null)
                Sound.SoundManager.Instance.PlayMusic(Music);
        }

        void OnLevelWasLoaded(int level)
        {
            // Unpause
            Time.timeScale = 1f;
        }


        private void StateChanged()
        {
            if (ScreenFader.State == UI.UICanvasGroupFader.FaderState.FadedIn)
            {
                if (!string.IsNullOrEmpty(_nextLevelRequest))
                {
                    SceneManager.LoadScene(_nextLevelRequest);
                    _nextLevelRequest = null;
                }
            }
        }

        public void LoadLevel(string levelName)
        {
            _nextLevelRequest = levelName;
            ScreenFader.FadeIn();
        }

        public void Restart()
        {
            LoadLevel(SceneManager.GetActiveScene().name);
        }

        public void MainMenu()
        {
            LoadLevel(Common.BaseLevelNames.MainMenu);
        }

        /*
        public void IntroLevel()
        {
            LoadLevel(Common.BaseLevelNames.Intro);
        }
        */

        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
