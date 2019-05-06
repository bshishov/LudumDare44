using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Mechanics/CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Stats")]
        public float Health;
        public float Damage;
        public float Speed;
        [Range(0f, 1f)]
        public float Evasion = 0f;
        public float Size = 1f;
        public float HealthModifier = 1f;
        public float SpellDamageAmp = 0f;

        [Header("AI")]
        public float IndifferenceDistance = 10f;
        public float SpellRange = 0f;
        public float FearRange = 0f;

        [Header("Melee attack")]
        public float MeleeRange = 0f;
        public float AttackCooldown = 1f;
        public float AnimationDelay = 0.9f;
        public Buff ApplyBuffOnAttack;
        public int AdditionalSpellStacks = 0;

        [Header("Drop Info")]
        [Range(0f, 1f)]
        public float DropRate;
        [SerializeField]
        public List<Spell> DropSpells;

        [Header("Spell Info")]
        [SerializeField]
        public List<Spell> UseSpells;
        public Buff UseBuff;
    }
}