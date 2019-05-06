using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public class Dummy : MonoBehaviour
{
    public TextMeshPro Text;

    private CharacterState _characterState;
    private StringBuilder _stringBuilder = new StringBuilder();

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
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
