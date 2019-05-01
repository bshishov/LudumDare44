using UnityEngine;

namespace Assets.Scripts.Utils.UI
{
    [RequireComponent(typeof(UICanvasGroupFader))]
    public class UIPauseScreen : MonoBehaviour
    {
        private UICanvasGroupFader _fader;
        public GameObject CastPanel;

        void Start()
        {
            _fader = GetComponent<UICanvasGroupFader>();
            _fader.StateChanged += StateChanged;
        }

        private void StateChanged()
        {
            if (_fader.State == UICanvasGroupFader.FaderState.FadedIn)
            {
                Time.timeScale = 0f;
                CastPanel.SetActive(false);
            }

            if (_fader.State == UICanvasGroupFader.FaderState.FadedOut)
            {
                Time.timeScale = 1f;
                CastPanel.SetActive(true);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_fader.State == UICanvasGroupFader.FaderState.FadedIn)
                    _fader.FadeOut();

                if (_fader.State == UICanvasGroupFader.FaderState.FadedOut)
                    _fader.FadeIn();
            }
        }


    }
}
