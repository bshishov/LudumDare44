using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using Spells;
using UnityEngine;
using UnityEngine.AI;
using Logger = Assets.Scripts.Utils.Debugger.Logger;

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
    private CharacterState _player;
    private float            _spellRange;
    private float            _timeCount;
    private Buff             _useBuff;
    private float           _doubleMeleeCheck;
    private int             _spellCount;

    private Logger _logger;

    private void Start()
    {
        _logger = Debugger.Default.GetLogger(gameObject.name + "/AI Log", unityLog: false);              

        _animationController = GetComponent<AnimationController>();
        _characterState      = GetComponent<CharacterState>();
        _movement            = GetComponent<MovementController>();
        _spellbookState      = GetComponent<SpellbookState>();
        
        _indifferenceDistance = _characterState.character.IndifferenceDistance;
        _spellRange           = _characterState.character.SpellRange;
        _fearRange            = _characterState.character.FearRange;
        _meleeRange           = _characterState.character.MeleeRange;

        var _players = GameObject.FindGameObjectsWithTag(Common.Tags.Player).Select(o => o.GetComponent<CharacterState>()).ToArray();
        _player = _players[0];

        _useBuff = _characterState.character.UseBuff;
        _spellCount = _characterState.character.UseSpells.Count;
        _doubleMeleeCheck = 0;

        switch (_characterState.character.Class)
        {
            case CharacterClass.Buffer:
                {

                    gameObject.tag = "Buffer";
                    break;
                }
            case CharacterClass.Melee:
                {
                    StartCoroutine(MeleeWanderState());
                    gameObject.tag = "Enemy";
                    break;
                }
            case CharacterClass.Caster:
                {
                    StartCoroutine(CasterWanderState());
                    gameObject.tag = "Enemy";
                    break;
                }
        }         
    }


    public IEnumerator MeleeWanderState()
    {
        yield return null;
        _logger.Log("In wander");
        while (_characterState.IsAlive && _player.IsAlive)
        {            
            _logger.Log("In wander while");
            var len = _player.transform.position - transform.position;
            var distance = len.magnitude;

            if (distance > _indifferenceDistance)
            {
                yield return null;
                continue;
            }

            if (distance > _meleeRange)
            {
                _movement.SetDestination(_player.transform.position);
                _movement.LookAt(_player.transform.position);               
            }
            else
            {
                StartCoroutine(MeleeAttackState(MeleeWanderState()));
                break;
            }
            yield return null;
        }        
    }

    public IEnumerator CasterWanderState()
    {
        yield return null;
        while (_characterState.IsAlive && _player.IsAlive)
        {

            var len = _player.transform.position - transform.position;
            var distance = len.magnitude;

            if (distance > _indifferenceDistance)
            {
                yield return null;
                continue;
            }

            if (distance > _spellRange)
            {
                _movement.SetDestination(_player.transform.position);
                _movement.LookAt(_player.transform.position);
            }
            else
            {
                if (distance < _meleeRange)
                    StartCoroutine(MeleeAttackState(CasterWanderState()));
                else
                    StartCoroutine(FearOrSpellState(len, distance));
                break;
            }
            yield return null;
        }
    }

    public IEnumerator MeleeAttackState(IEnumerator fromState)
    {
        yield return null;
        _logger.Log("In attack");
        if (_characterState.CanDealDamage())
        {
            _logger.Log("In can deal damage");
            _movement.LookAt(_player.transform.position);
            _animationController.PlayAttackAnimation();
            _movement.Stop();
            yield return new WaitForSeconds(_characterState.character.AnimationDelay);

            if ((_player.transform.position - transform.position).magnitude < _meleeRange)
            {
                _player.ReceiveDamage(_characterState, _characterState.Damage, null);
                if (_characterState.character.ApplyBuffOnAttack != null)
                    _player.ApplyBuff(_characterState.character.ApplyBuffOnAttack, _characterState, null, 1 + _characterState.AdditionSpellStacks);
                StartCoroutine(MeleeAttackState(fromState));
                yield break;
            }
            
        }        
        //yield return null;
        StartCoroutine(fromState);
    }

    public IEnumerator FearOrSpellState(Vector3 len, float distance)
    {
        yield return null;
        if (distance>_fearRange)
        {
            _movement.LookAt(_player.transform.position);
            _movement.Stop();
            _spellbookState.TryFireSpellToTarget(Mathf.FloorToInt(Random.value * _spellCount), _player, null);
        }
        else
        {
            var tgt = transform.position -  len.normalized;
            _movement.SetDestination(tgt);
            _movement.LookAt(tgt);
        }

        yield return null;
        StartCoroutine(CasterWanderState());        
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


    
}