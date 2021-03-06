﻿using System;
using System.Linq;
using System.Text;
using Actors;
using Data;
using Spells;
using TMPro;
using UnityEngine;
using Utils.Debugger;

[RequireComponent(typeof(CharacterState))]
public class Dummy : MonoBehaviour
{
    public TextMeshPro Text;
    public TextMeshPro Text2;

    private CharacterState _characterState;
    private readonly StringBuilder _stringBuilder = new StringBuilder();
    private readonly StringBuilder _stringBuilder2 = new StringBuilder();
    private readonly FixedSizeStack<string> _modLog = new FixedSizeStack<string>(10);

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
#if DEBUG
        _characterState.ModifierApplied += CharacterStateOnOnModifierApplied;
#endif
    }

    private void CharacterStateOnOnModifierApplied(ModificationParameter parameter, ISubSpellHandler spell, float actualChange)
    {
        var spellName = String.Empty;
        if (spell != null)
            spellName = spell.Spell.name;
        _modLog.Push($"{spellName} <color=yellow>{parameter}</color>: {actualChange:0.##}");

        foreach (var line in _modLog.Reverse())
        {
            _stringBuilder2.AppendLine(line);
        }
         
        Text2.text = _stringBuilder2.ToString();
        _stringBuilder2.Clear();
    }

    void Update()
    {
        _stringBuilder.AppendFormat("HP: {0}/{1}\n", _characterState.Health, _characterState.MaxHealth);
        _stringBuilder.AppendFormat("SPD: {0:##}\n", _characterState.Speed);

        foreach (var b in _characterState.Buffs)
        {
            _stringBuilder.AppendFormat("{0} <color=red>x{1}</color>: <color=yellow>{2:0.#}s</color> \n", b.Buff.name, b.Stacks, b.TimeRemaining);
        }

        Text.text = _stringBuilder.ToString();
        _stringBuilder.Clear();
    }
}
