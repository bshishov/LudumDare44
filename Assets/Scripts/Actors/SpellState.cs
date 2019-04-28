using UnityEngine;
using UnityEditor;
using System;

public class SpellState : MonoBehaviour
{
    private CharacterState _characterState;

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
    }

    internal void FireSpell(int index)
    {

    }
}