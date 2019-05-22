using Actors;
using Assets.Scripts;
using Assets.Scripts.Utils.UI;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(UICanvasGroupFader))]
    public class UIDeathScreen : MonoBehaviour
    {
        private CharacterState _playerState;
        private UICanvasGroupFader _canvasGroupFader;

        void Start()
        {
            var player = GameObject.FindGameObjectWithTag(Common.Tags.Player);
            if (player != null)
            {
                _playerState = player.GetComponent<CharacterState>();
                if (_playerState == null)
                {
                    Debug.LogWarning("PlayerState not found");
                }
            }

            _canvasGroupFader = GetComponent<UICanvasGroupFader>();
            if(_playerState != null)
                _playerState.Died += PlayerStateOnDeath;
        }

        private void PlayerStateOnDeath()
        {
            _canvasGroupFader.FadeIn();
        }
    }
}
