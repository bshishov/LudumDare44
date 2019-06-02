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
    public bool IsActive = false;
    public bool IsVisited = false;
    public SpawnPoint[] SpawnPoints;
    public int TriggerEnter = 0;

    private int _killed = 0;
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
        if (!IsVisited && other.tag == "Player")
        {
            IsVisited = true;
            BuildWall();
            Spawn();
        }
    }

    private void BuildWall()
    {

    }
    private void Spawn()
    {
        foreach (var spawnPoint in SpawnPoints)
            foreach (var enemy in spawnPoint.Enemies)
            {
                Instantiate(enemy, spawnPoint.Point.transform);
                var _characterState = enemy.GetComponent<CharacterState>();
                _characterState.Died += OnEnemyDeath;
            }
    }
    private void OnEnemyDeath()
    {
        _needToKill -= 1;
    }
}
