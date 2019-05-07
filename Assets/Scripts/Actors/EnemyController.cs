﻿using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Spells;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AnimationController))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(MovementController))]
public class EnemyController : MonoBehaviour
{
    private AnimationController _animationController;
    private CharacterState      _buffTarget;

    private CharacterState _characterState;
    private MovementController _movement;
    private SpellbookState _spellbookState;
    private float          _fearRange;
    private float          _indifferenceDistance;
    private float          _meleeRange;
    private CharacterState[] _players;
    private float            _spellRange;
    private float            _timeCount;
    private Buff             _useBuff;
    private float           _doubleMeleeCheck;

    private void Start()
    {
        _animationController = GetComponent<AnimationController>();
        _characterState      = GetComponent<CharacterState>();
        _movement            = GetComponent<MovementController>();
        _spellbookState      = GetComponent<SpellbookState>();

        _indifferenceDistance = _characterState.character.IndifferenceDistance;
        _spellRange           = _characterState.character.SpellRange;
        _fearRange            = _characterState.character.FearRange;
        _meleeRange           = _characterState.character.MeleeRange;
        _players = GameObject.FindGameObjectsWithTag(Common.Tags.Player).Select(o => o.GetComponent<CharacterState>()).ToArray();
        _useBuff = _characterState.character.UseBuff;
        _doubleMeleeCheck = 0;

        if (_useBuff == null)
            gameObject.tag = "Enemy";
        else
            gameObject.tag = "Buffer";
    }

    private void Update()
    {
        if (_characterState.IsAlive)
        {
            UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        if (!_characterState.IsAlive)
            return;

        // float minDistance = float.MaxValue;
        foreach (var player in _players)
        {
            if (!player.IsAlive)
                continue;

            if (_useBuff != null)
            {
                UseBuff(player);
            }
            else
            {                
                var len      = player.transform.position - transform.position;
                var distance = len.magnitude;

                if (!(distance < _indifferenceDistance))
                    continue;

                var spellCount = _characterState.character.UseSpells.Count;
                if (spellCount <= 0)
                {
                    MeleeAtack(player, len, distance);
                }
                else
                {
                    CastAttack(player, len, distance, spellCount);
                }
            }
        }
    }

    private void UseBuff(CharacterState player)
    {
        var len      = player.transform.position - transform.position;
        var distance = len.magnitude;

        if (_buffTarget == null || !_buffTarget.IsAlive)
        {
            var allies = GameObject.FindGameObjectsWithTag(Common.Tags.Enemy).Select(o => o.GetComponent<CharacterState>()).ToArray();
            if (allies.Length > 0)
                _buffTarget = RandomUtils.Choice(allies);
        }

        if (_buffTarget == null || distance < _fearRange)
        {
            _movement.SetDestination(transform.position - _fearRange * len.normalized);
        }
        else
        {
            var lenBuffed    = _buffTarget.transform.position - transform.position;
            var buffDistance = lenBuffed.magnitude;
            if (_spellRange < buffDistance)
            {
                _movement.SetDestination(_buffTarget.transform.position);
            }
            else
            {
                _movement.Stop();
                var buffTarget = _buffTarget.GetComponent<CharacterState>();

                buffTarget.ApplyBuff(_useBuff, buffTarget, null, 1);
                _animationController.PlayCastAnimation();
            }
        }
    }


    private void MeleeAtack(CharacterState player, Vector3 len, float distance)
    {
        if (distance > _meleeRange)
        {
            _movement.SetDestination(player.transform.position);
            _movement.LookAt(player.transform.position);            
        }
        else
        {
            if (_characterState.CanDealDamage())
            {
                _doubleMeleeCheck = Time.time;
                _movement.Stop();
               
                _movement.LookAt(player.transform.position);
                _animationController.PlayAttackAnimation();
            }
            if (Time.time - _doubleMeleeCheck > _characterState.character.AnimationDelay
                && Time.time - _doubleMeleeCheck<=_characterState.character.AttackCooldown)
            {
                _doubleMeleeCheck = 0;
                player.ReceiveDamage(_characterState, _characterState.Damage, null);
                if (_characterState.character.ApplyBuffOnAttack != null)
                    player.ApplyBuff(_characterState.character.ApplyBuffOnAttack, _characterState, null, 1 + _characterState.AdditionSpellStacks);
            }
        }
    }

    private void CastAttack(CharacterState player, Vector3 len, float distance, int spellCount)
    {
        

        if (distance > _spellRange)
        {
            _movement.LookAt(player.transform.position);
            _movement.SetDestination(player.transform.position);
        }
        else
        {
            if (_characterState.CanDealDamage())
            {
                _movement.LookAt(player.transform.position);
                _movement.Stop();
                _spellbookState.TryFireSpellToTarget(Mathf.FloorToInt(Random.value * spellCount), player, null);                

            }
            else if (distance < _fearRange)
            {
                var tgt = transform.position - _fearRange * len.normalized;
                _movement.SetDestination(tgt);
                _movement.LookAt(tgt);
            }
        }
    }
}