using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    public enum CharacterClass
    {
        Melee,
        Caster,
        Buffer
    }

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
        public CharacterClass Class;
        public float IndifferenceDistance = 10f;
        public float SpellRange = 0f;
        public float FearRange = 0f;

        [Header("Melee attack")]
        public float MeleeRange = 0f;
        public float MeleeCooldown = 1f;
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
        public float SpellCooldown = 1f;
        public List<Spell> UseSpells;
        public Buff UseBuff;
    }
}