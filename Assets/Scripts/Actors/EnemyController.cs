﻿using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Spells;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AnimationController))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private AnimationController _animationController;
    private CharacterState      _buffTarget;

    private CharacterState _characterState;
    private float          _fearRange;
    private float          _indifferenceDistance;
    private float          _meleeRange;
    private NavMeshAgent   _navMeshAgent;

    private CharacterState[] _players;
    private SpellbookState   _spellbookState;
    private float            _spellRange;
    private float            _timeCount;
    private Buff             _useBuff;

    private void Start()
    {
        _animationController = GetComponent<AnimationController>();
        _characterState      = GetComponent<CharacterState>();
        _navMeshAgent        = GetComponent<NavMeshAgent>();
        _spellbookState      = GetComponent<SpellbookState>();

        _indifferenceDistance = _characterState.character.IndifferenceDistance;
        _spellRange           = _characterState.character.SpellRange;
        _fearRange            = _characterState.character.FearRange;
        _meleeRange           = _characterState.character.MeleeRange;
        _navMeshAgent.speed   = _characterState.Speed;
        CharacterUtils.ApplySettings(_characterState, _navMeshAgent, true);
        _players = GameObject.FindGameObjectsWithTag(Common.Tags.Player).Select(o => o.GetComponent<CharacterState>()).ToArray();
        _useBuff = _characterState.character.UseBuff;
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
                    if (distance > _meleeRange)
                    {
                        _navMeshAgent.isStopped = false;
                        _navMeshAgent.SetDestination(player.transform.position);
                    }
                    else
                    {
                        if (_characterState.CanDealDamage())
                        {
                            _navMeshAgent.isStopped = true;
                            player.ReceiveDamage(_characterState, _characterState.Damage, null);

                            if (_characterState.character.ApplyBuffOnAttack != null)
                                player.ApplyBuff(_characterState.character.ApplyBuffOnAttack, _characterState, null, 1 + _characterState.AdditionSpellStacks);

                            transform.rotation = Quaternion.LookRotation(len);
                            _characterState.GetComponent<AnimationController>().PlayAttackAnimation();
                        }
                    }
                }
                else
                {
                    if (distance > _spellRange)
                    {
                        _navMeshAgent.speed     = _characterState.Speed;
                        _navMeshAgent.isStopped = false;
                        _navMeshAgent.SetDestination(player.transform.position);
                    }
                    else
                    {
                        if (distance > _fearRange)
                        {
                            if (_characterState.CanDealDamage())
                            {
                                transform.LookAt(player.transform);
                                _spellbookState.TryFireSpellToTarget(Mathf.FloorToInt(Random.value * spellCount), player);
                                _navMeshAgent.isStopped = true;
                            }
                        }
                        else
                        {
                            _navMeshAgent.speed     = 10 * _characterState.Speed;
                            _navMeshAgent.isStopped = false;
                            _navMeshAgent.SetDestination(transform.position - _fearRange * len.normalized);
                        }
                    }
                }
            }
        }
    }

    private void UseBuff(CharacterState player)
    {
        var len      = player.transform.position - transform.position;
        var distance = len.magnitude;

        if (_buffTarget == null || _buffTarget.Health <= 0)
        {
            var allies = GameObject.FindGameObjectsWithTag(Common.Tags.Enemy).Select(o => o.GetComponent<CharacterState>()).ToArray();
            if (allies.Length > 0)
                _buffTarget = RandomUtils.Choice(allies);
        }

        if (_buffTarget == null || distance < _fearRange)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(transform.position - _fearRange * len.normalized);
        }
        else
        {
            var lenBuffed    = _buffTarget.transform.position - transform.position;
            var buffDistance = lenBuffed.magnitude;
            if (_spellRange < buffDistance)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_buffTarget.transform.position);
            }
            else
            {
                _navMeshAgent.isStopped = true;
                var buffTarget = _buffTarget.GetComponent<CharacterState>();

                buffTarget.ApplyBuff(_useBuff, buffTarget, null, 1);
                GetComponent<AnimationController>().PlayCastAnimation();
            }
        }
    }
}