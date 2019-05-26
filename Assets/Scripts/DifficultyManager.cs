using System;
using Data;
using UnityEngine;
using Utils;

public class DifficultyManager : Singleton<DifficultyManager>
{
    [Serializable]
    public class Difficulty
    {
        public string DifficultyName;
        public float Duration;
        public Buff[] DifficultyBuffs;        
    }
    
    public Difficulty[] Difficulties;
    public int CurrentDifficultyIndex { get; private set; }
    public Difficulty CurrentDifficulty => GetDifficulty(CurrentDifficultyIndex);
    public Difficulty NextDifficulty => GetDifficulty(CurrentDifficultyIndex + 1);

    public float Progress
    {
        get
        {
            var current = CurrentDifficulty;
            if (current == null)
                return 1f;
            return Mathf.Clamp01(_elapsedSinceDifficultyStart / CurrentDifficulty.Duration);
        }
    }

    private float _elapsedSinceDifficultyStart;
    
    private void Start()
    {
        CurrentDifficultyIndex = 0;
        _elapsedSinceDifficultyStart = 0;
    }

    private void Update()
    {
        _elapsedSinceDifficultyStart += Time.deltaTime;
        if (NextDifficulty != null && _elapsedSinceDifficultyStart > CurrentDifficulty?.Duration)
        {
            CurrentDifficultyIndex += 1;
            _elapsedSinceDifficultyStart = 0f;
        }
    }

    public Difficulty GetDifficulty(int difficultyIndex)
    {
        if (difficultyIndex >= Difficulties.Length || difficultyIndex < 0)
            return null;

        return Difficulties[difficultyIndex];
    }
}
