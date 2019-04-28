using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyController : MonoBehaviour
{
    private CharacterState _characterParams;
    private NavMeshAgent _navMeshAgent;

    private int indifferenceDistance;
    private int spellRange;
    private int fearRange;
    public float distance;

    public Transform Target;

    // Start is called before the first frame update
    void Start()
    {
        _characterParams = GetComponent<CharacterState>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        indifferenceDistance = _characterParams.character.IndifferenceDistance;
        spellRange = _characterParams.character.SpellRange;
        fearRange = _characterParams.character.FearRange;

        CharacterUtils.ApplySettings(_characterParams, _navMeshAgent, true);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTarget(); 
    }

    private void UpdateTarget()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null)
            return;

        GameObject selectedPlayer = null;
        // float minDistance = float.MaxValue;
        foreach (var player in players)
        {
            var len = (player.transform.position - transform.position);
            distance = len.magnitude;
            if (distance < indifferenceDistance) {
                int spellCount = _characterParams.character.UseSpells.Count;
                if (spellCount <= 0)
                {
                    selectedPlayer = player;
                }
                else
                {
                    if (distance > spellRange)
                    {
                        selectedPlayer = player;
                        _navMeshAgent.isStopped = false;
                        _navMeshAgent.SetDestination(selectedPlayer.transform.position);
                    }
                    else
                    {
                        if (distance > fearRange)
                        {
                            // TODO: Miktor fix firespell
                            // _characterParams.FireSpell(Mathf.FloorToInt(Random.value * spellCount), player);
                            selectedPlayer = null;
                            _navMeshAgent.isStopped = true;
                        }
                        else
                        {
                            selectedPlayer = null;
                            _navMeshAgent.isStopped = false;
                            _navMeshAgent.SetDestination(transform.position - 5* len.normalized);
                        }
                    }
                }
            }
        }
        
            
    }
}
