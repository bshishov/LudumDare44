﻿using System;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

public class UIBloodBar : MonoBehaviour
{
    public UIProgressBar ProgressBar;
    public Text Text;

    private CharacterState _playerState;
    private const string HpBarFormat = "{0:#} / {1:#}";

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag(Tags.Player);
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
