using Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Serializable]
    public class SpawnPoint
    {
        public GameObject Point;
        public GameObject[] Enemies;
    }
    public GameObject Wall;
    public bool IsActive = false;
    public bool IsVisited = false;
    public SpawnPoint[] SpawnPoints;
    public int TriggerEnter = 0;

    private int _killed = 0;
    private List<CharacterState> _characterStates = new List<CharacterState>();

    public int _needToKill = 0;
    // Start is called before the first frame update
    void Start()
    {
        foreach (var spawnPoint in SpawnPoints)
            _needToKill = spawnPoint.Enemies.Length;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsVisited && other.CompareTag("Player"))
        {
            IsVisited = true;
            BuildWall();
            Spawn();
        }
    }

    private void BuildWall()
    {
        Wall.SetActive(true);
    }
    private void Spawn()
    {
        foreach (var spawnPoint in SpawnPoints)
            foreach (var enemy in spawnPoint.Enemies)
            {
                var enemyObject = Instantiate(enemy, spawnPoint.Point.transform);                
                var characterState = enemyObject.GetComponent<CharacterState>();
                characterState.Died += OnEnemyDeath;                
            }
    }
    private void OnEnemyDeath()
    {
        _needToKill -= 1;
        if (_needToKill <= 0)
            TurnOffWall();
    }

    private void TurnOffWall()
    {
        Wall.SetActive(false);
    }
}
