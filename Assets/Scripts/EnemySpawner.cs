using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform playerObject;

    void Start()
    {
        InvokeRepeating("SpawnEnemmy", 1f, 1f);  //1s delay, repeat every 1s

    }

    public void SpawnEnemmy()
    {
        Vector3 randomPos = playerObject.position + new Vector3(Random.Range(-10.0f, 10.0f), 0, Random.Range(-10.0f, 10.0f));

        var enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
        enemy.GetComponent<EnemyController>().Target = playerObject;
    }
}
