using System;
using Attributes;
using Data;
using UnityEngine;

namespace Spells
{
    [CreateAssetMenu(fileName = "SubSpell_", menuName = "Spells/Sub Spell")]
    [Serializable]
    public class SubSpell : ScriptableObject
    {
        public enum Targeting
        {
            Default,
            PreviousSource,
            PreviousTarget,
            OriginalSpellSource,
            OriginalSpellTarget,
            None
        }
        
        [Header("Targeting Inheritance")]
        public Targeting Source;
        public Targeting Target;

        [Header("Meta")]
        public StackableFloat BloodCost;
        public bool AbortSpellIfAborted;
        public bool SpellShouldWaitUntilEnd;
        public bool CanBeAborted = true;
        public StackableFloat MaxInstances = new StackableFloat(10);
        public bool AffectsCharactersOnlyOncePerSpell = false;

        [Header("Timings")]
        public StackableFloat PreCastDelay;
        public StackableFloat FireDelay;
        public StackableFloat PostCastDelay;
        public StackableFloat FireDuration;

        [Serializable]
        public struct SubSpellEvent
        {
            public Spells.SubSpellEvent Type;
            public Query Query;

            [Header("Payload")]
            public SubSpell FireSubSpell;
            public Buff ApplyBuffToTarget;
        }

        [Header("Events")]
        [Reorderable]
        public SubSpellEvent[] FireSubSpellEvents;

        [Header("Projectile")]
        [Expandable]
        public ProjectileData Projectile;

        [Header("Effect")]
        public GameObject Effect;

        private ISpellEffectHandler _effectHandler;

        public bool IsProjectileSubSpell => Projectile != null;
        

        public ISpellEffectHandler GetEffect()
        {
            if (Effect == null)
                return null;

            if (_effectHandler == null)
                _effectHandler = Instantiate(Effect).GetComponent<ISpellEffectHandler>();

            return _effectHandler;
        }
    }
}
