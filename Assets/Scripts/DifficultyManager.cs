using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DifficultyManager : Singleton<DifficultyManager>
{
    [Serializable]
    public class Difficulty
    {
        public string DifficultyName;
        public float NextDifficultyStamp;
        public List<Buff> DifficultyBuffs = new List<Buff>();        
    }
    
    public  List<Difficulty> Difficulties = new List<Difficulty>();
    public int CurrentDifficultyIndex { get; private set; }
    private float _timeChecker;

    // Start is called before the first frame update
    void Start()
    {
        CurrentDifficultyIndex = 0;
        _timeChecker = 0;
    }

    // Update is called once per frame
    void Update()
    {
        _timeChecker += Time.deltaTime;
        if (CurrentDifficultyIndex + 1 < Difficulties.Count && 
            Difficulties[CurrentDifficultyIndex].NextDifficultyStamp < _timeChecker)
            CurrentDifficultyIndex += 1;
    }

    public Difficulty GetDifficulty(int difficultyIndex)
    {
        if (difficultyIndex >= Difficulties.Count || difficultyIndex < 0)
            return null;
        else
            return Difficulties[difficultyIndex];
    }
    
    public float ReturnDiffTime()
    {
        return _timeChecker;
    }
}
