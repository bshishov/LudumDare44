using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform playerObject;

    void Start()
    {
        for (int i = 0; i < 5; ++i)
            SpawnEnemmy();

    }

    public void SpawnEnemmy()
    {
        Vector3 randomPos = playerObject.position + new Vector3(Random.Range(-10.0f, 10.0f), 0, Random.Range(-10.0f, 10.0f));

        var enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
        enemy.GetComponent<EnemyController>().Target = playerObject;
    }
}
