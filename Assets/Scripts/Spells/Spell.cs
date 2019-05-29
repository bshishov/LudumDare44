using System;
using Attributes;
using Data;
using UnityEngine;

namespace Spells
{
    [CreateAssetMenu(fileName = "Spell_", menuName = "Spells/Spell")]
    [Serializable]
    public class Spell : ScriptableObject
    {
        [Header("Meta")]
        public string Name;
        public Sprite Icon;
        [TextArea]
        public string Description;
        public Buff ApplyBuffOnDismantle;
        public StackableFloat LifeSteal;
        public StackableFloat Cooldown = new StackableFloat(1f);
        public StackableFloat BloodCost = new StackableFloat(1f);
        public StackableFloat MaxRange = new StackableFloat(10);
        public StackableFloat MinRange = new StackableFloat(0);
        public SpellSlot DefaultSlot;
        public bool ManualAbort;
        public bool CheckRangeOnCast;

        public enum TargetRangeBehaviour
        {
            AbortWhenOutOfRange,
            ClampToRange,
            SetMaxRange
        }
        
        public TargetType TargetType = TargetType.Location;
        public TargetRangeBehaviour RangeBehaviour = TargetRangeBehaviour.ClampToRange;

        [Expandable]
        public SubSpell[] MainSubSpells;
        public GameObject DropItem;
    }
}