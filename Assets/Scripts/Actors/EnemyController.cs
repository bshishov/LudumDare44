using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private CharacterParams _characterParams;
    private NavMeshAgent _navMeshAgent;

    public Transform Target;

    // Start is called before the first frame update
    void Start()
    {
        _characterParams = GetComponent<CharacterParams>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        CharacterUtils.ApplaySettings(_characterParams, _navMeshAgent, true);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTarget();
        if (Target)
            _navMeshAgent.SetDestination(Target.position);
    }

    private void UpdateTarget()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null)
            return;

        GameObject selectedPlayer = null;
        float minDistance = float.MaxValue;
        foreach(var player in players)
        {
            var len = (player.transform.position - transform.position);
            var distance = len.magnitude;

            if(distance < minDistance)
            {
                minDistance = distance;
                selectedPlayer = player;
            }
        }

        Target = selectedPlayer.transform;
    }
}
