using System;
using Actors;
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
        public SpellSlot DefaultSlot;
        public GameObject DropItem;
        
        [Header("Initial targeting")]
        public StackableFloat MaxRange = new StackableFloat(10);
        public StackableFloat MinRange = new StackableFloat(0);
        public bool CheckRangeOnCast;

        public enum TargetRangeBehaviour
        {
            Free = 3,
            AbortWhenOutOfRange = 0,
            RetargetClampToRange = 1,
            RetargetSetMaxRange = 2
        }
        public TargetType TargetType = TargetType.Location;
        public Query.QueryTeam AffectsTeam = Query.QueryTeam.Enemy;
        
        [Header("Logic")]
        public TargetRangeBehaviour RangeBehaviour = TargetRangeBehaviour.Free;
        public bool ManualAbort;
        [Expandable]
        public SubSpell[] MainSubSpells;
        
    }
}