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

    public List<Difficulty> ReturnDiff()
    {
        foreach (var diff in Difficulties.Select((Value, Index) => new { Value, Index }))
        {
            if (_timeChecker < diff.Value.NextDifficultyStamp)
            {
                var difficultiesToReturn = new List<Difficulty>();

                if (diff.Index > 0)
                    difficultiesToReturn.Add(Difficulties[diff.Index - 1]);
                else
                    difficultiesToReturn.Add(null);

                difficultiesToReturn.Add(diff.Value);

                if (diff.Index < Difficulties.Count-1)
                    difficultiesToReturn.Add(Difficulties[diff.Index +1]);
                else
                    difficultiesToReturn.Add(null);

                return difficultiesToReturn;
            }
        }
        return null;
    }
    public float ReturnDiffTime()
    {
        return _timeChecker;
    }
}
