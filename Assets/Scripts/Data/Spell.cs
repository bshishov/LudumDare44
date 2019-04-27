using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Mechanics/Spell")]
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

        [Header("Buffs")]
        public Buff[] buffs;

        // TODO: set later
        [Header("Visual Effects")]
        public GameObject VisualEffect;
    }
}
