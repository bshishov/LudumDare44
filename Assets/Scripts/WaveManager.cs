using System.Collections;
using System.Collections.Generic;
using Actors;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Sound;
using Data;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(DifficultyManager))]
public class WaveManager : Singleton<WaveManager>
{
    public List<Transform> SpawnPoints;
    public GameObject Target;
    [Expandable]
    public List<Wave> Waves;
    public InfiniteWave InfiniteWave;
    public float TimeBetweenWaves = 20f;
    public int StartDifficulty = 1;        

    [Header("Audio")]
    public Sound WaveStartedSound;

    public Wave CurrentWave { get; private set; }
    public bool WaveInProgress { get; private set; }
    public int WaveNumber { get { return _currentWaveIndex + 1; } }
    public int EnemiesOut { get; private set; }
    public int EnemiesInThisWave { get; private set; }
    public float TimeToNextWave { get; private set; }
    public int CurrentDifficulty { get; private set; }
    public bool IsRunningInfinite { get; private set; }
        
    public float Percentage
    {
        get
        {
            if (CurrentWave != null)
                return Mathf.Clamp01((float)EnemiesOut / EnemiesInThisWave);
            return 0f;
        }
    }

    private int _currentWaveIndex = -1;

    void Start()
    {
           
        CurrentDifficulty = StartDifficulty;
        NextWave();
    }

    void Update()
    {
        if (!WaveInProgress)
        {
            TimeToNextWave -= Time.deltaTime;
            if (TimeToNextWave < 0)
            {
                TimeToNextWave = 0;
                NextWave();
            }
        }
    }

    void NextWave()
    {
        if (WaveInProgress)
        {
            Debug.LogWarning("Can't start new wave, current one is in progress");
            return;
        }

        _currentWaveIndex++;
        if (_currentWaveIndex >= Waves.Count)
        {
            if (InfiniteWave != null)
                StartCoroutine(DoInfiniteWave(InfiniteWave));
            else
                Debug.LogWarning("No more waves");
        }
        else
        {
            CurrentWave = Waves[_currentWaveIndex];
            StartCoroutine(DoWave(CurrentWave));
        }

        SoundManager.Instance.Play(WaveStartedSound);
    }

    private void SpawnEnemy(Transform spawnPoint, GameObject prefab, ref Buff[] buffs)
    {
        var enemy = Instantiate(prefab);
        enemy.GetComponent<NavMeshAgent>().Warp(spawnPoint.position);
        var enemyState = enemy.GetComponent<CharacterState>();

        // Apply buffs
        foreach (var buff in buffs)
            enemyState.ApplyBuff(buff, enemyState, null, 1);

        var currDiff = DifficultyManager.Instance.GetDifficulty(DifficultyManager.Instance.CurrentDifficultyIndex);
        if (currDiff != null)
            foreach (var buff in currDiff.DifficultyBuffs)
            {
                enemyState.ApplyBuff(buff, enemyState, null, 1);
            }
        EnemiesOut++;
    }

    private void SpawnEnemy(int spawnerIndex, GameObject prefab, ref Buff[] buffs)
    {
        if (spawnerIndex < 0 || spawnerIndex > SpawnPoints.Count - 1)
        {
            foreach (var spawnPoint in SpawnPoints)
                SpawnEnemy(spawnPoint, prefab, ref buffs);
        }
        else
        {
            SpawnEnemy(SpawnPoints[spawnerIndex], prefab, ref buffs);
        }
    }

    private IEnumerator DoWave(Wave wave)
    {
        WaveInProgress = true;
        EnemiesOut = 0;
        EnemiesInThisWave = CurrentWave.TotalNumberOfEnemies(SpawnPoints.Count);
        Debug.LogFormat("Wave {0}/{1} started!", WaveNumber, Waves.Count);

        foreach (var spawnEntry in wave.Items)
        {
            if (spawnEntry.EnemyPrefab == null || spawnEntry.NumberOfThisType == 0)
                continue;

            for (var i = 0; i < spawnEntry.NumberOfThisType; i++)
            {
                SpawnEnemy(spawnEntry.SpawnIndex, spawnEntry.EnemyPrefab, ref spawnEntry.InitialBuffs);
                yield return new WaitForSeconds(spawnEntry.DelayBetween);
            }

            yield return new WaitForSeconds(spawnEntry.DelayBeforeNext);
        }
        WaveInProgress = false;
        Debug.LogFormat("Wave {0}/{1} ended!", WaveNumber, Waves.Count);
        TimeToNextWave = this.TimeBetweenWaves;
        EnemiesInThisWave = 0;
    }

    private IEnumerator DoInfiniteWave(InfiniteWave wave)
    {
        EnemiesOut = 0;
        EnemiesInThisWave = wave.TotalNumberOfEnemies(SpawnPoints.Count, CurrentDifficulty);
        IsRunningInfinite = true;
        WaveInProgress = true;

        Debug.LogFormat("Infinite wave: Difficulty {0}", CurrentDifficulty);

        foreach (var pack in wave.Packs)
        {
            if (pack.Difficulty >= CurrentDifficulty)
                continue;

            var spawnPackTimes = Mathf.Min(CurrentDifficulty - pack.Difficulty, pack.MaxNumber);

            for (var packI = 0; packI < spawnPackTimes; packI++)
            {
                foreach (var spawnEntry in pack.Items)
                {
                    if (spawnEntry.EnemyPrefab == null || spawnEntry.NumberOfThisType == 0)
                        continue;

                    for (var i = 0; i < spawnEntry.NumberOfThisType; i++)
                    {
                        SpawnEnemy(spawnEntry.SpawnIndex, spawnEntry.EnemyPrefab, ref spawnEntry.InitialBuffs);
                        yield return new WaitForSeconds(spawnEntry.DelayBetween);
                    }

                    yield return new WaitForSeconds(spawnEntry.DelayBeforeNext);
                }

                yield return new WaitForSeconds(pack.DelayBeforeNextPack);
            }
        }

        CurrentDifficulty += 1;
        WaveInProgress = false;
        Debug.LogFormat("Difficulty increased: {0}", CurrentDifficulty);
        TimeToNextWave = this.TimeBetweenWaves;
        EnemiesInThisWave = 0;
    }
}