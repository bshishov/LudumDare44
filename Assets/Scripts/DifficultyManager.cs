using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using System;
using UnityEngine;

public class DifficultyManager : Singleton<DifficultyManager>
{
    [Serializable]
    public class Difficulty
    {
        public string DifficultyName;
        public float NextDifficultyStamp;
        public Buff[] DifficultyBuffs;        
    }
    
    public Difficulty[] Difficulties;
    public int CurrentDifficultyIndex { get; private set; }
    private float _timeSinceStart;
    
    private void Start()
    {
        CurrentDifficultyIndex = 0;
        _timeSinceStart = 0;
    }

    private void Update()
    {
        _timeSinceStart += Time.deltaTime;
        if (CurrentDifficultyIndex + 1 < Difficulties.Length && 
            Difficulties[CurrentDifficultyIndex].NextDifficultyStamp < _timeSinceStart)
            CurrentDifficultyIndex += 1;
    }

    public Difficulty GetDifficulty(int difficultyIndex)
    {
        if (difficultyIndex >= Difficulties.Length || difficultyIndex < 0)
            return null;

        return Difficulties[difficultyIndex];
    }
    
    public float GetTimeSinceStart()
    {
        return _timeSinceStart;
    }
}
