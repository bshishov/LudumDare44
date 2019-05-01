using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform playerObject;
    public int Count = 5;

    void Start()
    {
        for (var i = 0; i < Count; ++i)
            SpawnEnemy();

    }

    public void SpawnEnemy()
    {
        Vector3 randomPos = playerObject.position + new Vector3(Random.Range(-10.0f, 10.0f), 0, Random.Range(-10.0f, 10.0f));
        var enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
    }
}
