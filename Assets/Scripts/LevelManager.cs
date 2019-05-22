using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Sound;


public class LevelManager : Singleton<LevelManager>
{
    public Assets.Scripts.Utils.UI.UICanvasGroupFader ScreenFader;
    public Sound Music;
    private string _nextLevelRequest;

    void Start()
    {
        if (ScreenFader == null)
            ScreenFader = GetComponent<Assets.Scripts.Utils.UI.UICanvasGroupFader>();

        ScreenFader.StateChanged += StateChanged;
        ScreenFader.FadeOut();

        if (Music != null)
            Assets.Scripts.Utils.Sound.SoundManager.Instance.PlayMusic(Music);

        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
    }

    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        throw new System.NotImplementedException();
    }

    private void StateChanged()
    {
        if (ScreenFader.State == Assets.Scripts.Utils.UI.UICanvasGroupFader.FaderState.FadedIn)
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

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}