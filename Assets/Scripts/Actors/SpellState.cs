using UnityEngine;
using UnityEditor;
using System;
using Assets.Scripts.Data;

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

    internal void Pickup(Spell spell)
    {

    }
}
