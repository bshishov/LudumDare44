using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class UIBloodScreen : MonoBehaviour
{
    public float MaxAlpha = 0.5f;
    public Image Image;
    public float SmoothTime = 0.05f;

    private CharacterState _playerState;
    private float _hpFraction;
    private float _velocity;

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
        if (_playerState != null)
        {
            _hpFraction = Mathf.SmoothDamp(_hpFraction, _playerState.Health / _playerState.MaxHealth, ref _velocity,
                SmoothTime);
            Image.color = new Color(1, 1, 1, Mathf.Clamp01((1 - 5 * _hpFraction)) * MaxAlpha);
        }
    }

}
