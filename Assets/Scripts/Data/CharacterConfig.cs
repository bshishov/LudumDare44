using System.Collections.Generic;
using Attributes;
using Spells;
using UnityEngine;

namespace Data
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
        public int AdditionalSpellStacks = 0;

        [Header("AI")]
        [Expandable]
        public AIConfig AI;

        [Header("Drop Info")] 
        [Range(0f, 1f)]
        public float DropRate;
        public List<Spell> DropSpells;

        [Header("Spell Info")]
        public List<Spell> UseSpells;
        public Buff UseBuff;
    }
}