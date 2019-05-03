using System.Linq;
using Assets.Scripts;
using Spells;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Utils;
using Random = UnityEngine.Random;


[RequireComponent(typeof(AnimationController))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private float _indifferenceDistance;
    private float _spellRange;
    private float _fearRange;
    private float _meleeRange;

    private CharacterState _characterState;
    private NavMeshAgent _navMeshAgent;
    private AnimationController _animationController;
    private SpellbookState _spellbookState;

    private CharacterState[] _players;
    private CharacterState _buffTarget;
    private Assets.Scripts.Data.Buff _useBuff;
    private float _distance;
    private float _timeCount;

    void Start()
    {
        _animationController = GetComponent<AnimationController>();
        _characterState = GetComponent<CharacterState>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _spellbookState = GetComponent<SpellbookState>();

        _indifferenceDistance = _characterState.character.IndifferenceDistance;
        _spellRange = _characterState.character.SpellRange;
        _fearRange = _characterState.character.FearRange;
        _meleeRange = _characterState.character.MeleeRange;
        _navMeshAgent.speed = _characterState.Speed;
        CharacterUtils.ApplySettings(_characterState, _navMeshAgent, true);       
        _players = GameObject.FindGameObjectsWithTag(Common.Tags.Player).Select(o => o.GetComponent<CharacterState>()).ToArray();
        _useBuff = _characterState.character.UseBuff;
        if (_useBuff == null)
            gameObject.tag = "Enemy";
        else
            gameObject.tag = "Buffer";
    }
    
    void Update()
    {
        if (_characterState.IsAlive)
        {
            UpdateTarget();
        }
        else
        {
            /*
            if (_navMeshAgent.enabled && !_navMeshAgent.isStopped)
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.enabled = false;
            }*/
        }
    }

    private void UpdateTarget()
    {
        CharacterState selectedPlayer;
        if (_characterState.IsAlive)  {
            // float minDistance = float.MaxValue;
            foreach (var player in _players)
            {
                if (player.IsAlive)
                {
                    var len = player.transform.position - transform.position;
                    _distance = len.magnitude;
                    if (_useBuff != null)
                    {
                        if (_buffTarget == null || _buffTarget.Health<=0)
                        {
                            var _allies = GameObject.FindGameObjectsWithTag(Common.Tags.Enemy).Select(o => o.GetComponent<CharacterState>()).ToArray();
                            if (_allies.Count() > 0)
                                _buffTarget = RandomUtils.Choice(_allies);                            
                        }
                        if (_buffTarget == null || _distance < _fearRange)
                        {
                            _navMeshAgent.isStopped = false;
                            _navMeshAgent.SetDestination(transform.position - _fearRange * len.normalized);
                        }
                        else
                        {
                            var lenBuffed = _buffTarget.transform.position - transform.position;
                            var buffDistance = lenBuffed.magnitude;
                            if (_spellRange< buffDistance)
                            {
                                _navMeshAgent.isStopped = false;
                                _navMeshAgent.SetDestination(_buffTarget.transform.position);
                            }
                            else
                            {
                                _navMeshAgent.isStopped = true;
                                _buffTarget.GetComponent<CharacterState>().ApplyBuff(_useBuff);
                                GetComponent<AnimationController>().PlayCastAnimation();
                            }
                        }
                    }
                    else { 
                        if (_distance < _indifferenceDistance)
                        {
                            var spellCount = _characterState.character.UseSpells.Count;
                            if (spellCount <= 0)
                            {
                                if (_distance > _meleeRange)
                                {
                                    _navMeshAgent.isStopped = false;
                                    _navMeshAgent.SetDestination(player.transform.position);
                                }
                                else
                                {
                                    if (_characterState.CanDealDamage())
                                    {
                                        _navMeshAgent.isStopped = true;
                                        player.ReceiveDamage(_characterState.Damage);

                                        if (_characterState.character.ApplyBuffOnAttack != null)
                                        {
                                            player.ApplyBuff(
                                                _characterState.character.ApplyBuffOnAttack,
                                                1 + _characterState.AdditionSpellStacks);
                                        }

                                        transform.rotation = Quaternion.LookRotation(len);
                                        _characterState.GetComponent<AnimationController>().PlayAttackAnimation();
                                    }
                                }
                            }
                            else
                            {
                                if (_distance > _spellRange)
                                {
                                    _navMeshAgent.speed = _characterState.Speed;
                                    selectedPlayer = player;
                                    _navMeshAgent.isStopped = false;
                                    _navMeshAgent.SetDestination(selectedPlayer.transform.position);
                                }
                                else
                                {
                                    if (_distance > _fearRange)
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
                                        _navMeshAgent.speed = 10 * _characterState.Speed;
                                        _navMeshAgent.isStopped = false;
                                        _navMeshAgent.SetDestination(transform.position - _fearRange * len.normalized);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
