using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshSimpleController : MonoBehaviour
{
    public Transform Target;
    private NavMeshAgent _agent;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Target != null)
        {
            _agent.SetDestination(Target.position);
        }
    }
}
