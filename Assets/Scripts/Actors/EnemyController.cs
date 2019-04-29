using System.Linq;
using Assets.Scripts;
using Spells;
using UnityEngine;
using UnityEngine.AI;
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
    private float _distance;

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

        _players = GameObject.FindGameObjectsWithTag(Tags.Player).Select(o => o.GetComponent<CharacterState>()).ToArray();
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
        // float minDistance = float.MaxValue;
        foreach (var player in _players)
        {
            var len = player.transform.position - transform.position;
            _distance = len.magnitude;
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
                        if (_characterState.CanDealMeleeDamage())
                        {
                            _navMeshAgent.isStopped = true;
                            player.ReceiveDamage(_characterState.character.Damage);
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
                            _spellbookState.TryFireSpellToTarget(Mathf.FloorToInt(Random.value * spellCount), player);
                            _navMeshAgent.isStopped = true;
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
