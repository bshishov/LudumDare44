using Assets.Scripts.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Serializable]
    public class Difficulty
    {
        public string DifficultyName;
        public float NextDifficultyStamp;
        public List<Buff> DifficultyBuffs = new List<Buff>();        
    }
    
    public List<Difficulty> Difficulties = new List<Difficulty>();
    private float _timeChecker;

    // Start is called before the first frame update
    void Start()
    {
        _timeChecker = 0;
    }

    // Update is called once per frame
    void Update()
    {
        _timeChecker += Time.deltaTime;           
    }

    public Difficulty ReturnDiff()
    {
        foreach (Difficulty Diff in Difficulties)
        {
            if (_timeChecker < Diff.NextDifficultyStamp)
            {
                return Diff;
            }
        }
        return null;
    }
}
