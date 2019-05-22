using System;
using Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UIBloodBar : MonoBehaviour
    {
        public UIProgressBar ProgressBar;
        public TextMeshProUGUI Text;

        private CharacterState _playerState;
        private const string HpBarFormat = "{0:0} / {1:0}";

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
        }
    
        void Update()
        {
            var f = _playerState.Health / _playerState.MaxHealth;
            ProgressBar.SetTarget(f);
            Text.text = String.Format(HpBarFormat, _playerState.Health, _playerState.MaxHealth);
        }
    }
}
