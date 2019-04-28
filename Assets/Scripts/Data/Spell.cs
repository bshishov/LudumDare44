using UnityEngine;
using System;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Mechanics/Spell")]
    [Serializable]
    public class Spell : ScriptableObject
    {
        public string Name;

        public int BloodCost = 0;
        public int BloodBack = 0;
        public float Cooldown;

        public SubSpell SubSpells;
    }
}