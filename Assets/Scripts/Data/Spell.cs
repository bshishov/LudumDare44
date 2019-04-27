using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Mechanics/Spell")]
    [Serializable]
    public class Spell : ScriptableObject
    {
        public string Name;
        public enum SpellTypes { Raycast, Projectile, Aoe, Status };
        [Header("Characterisics")]        
        public SpellTypes SpellType;
        public float Damage = 1;
        public float AoeRadius = 0f;
        public int BloodCost = 0;
        public int BloodBack = 0;
        public float Cooldown;

        public AreaOfEffect Area;

        [Header("Buffs")]
        [SerializeField]
        public Buff[] Buffs;

        // TODO: set later
        [Header("Visual Effects")]
        public GameObject VisualEffect;
    }
}
