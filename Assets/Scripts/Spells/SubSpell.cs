using System;
using Attributes;
using Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spells
{
    [CreateAssetMenu(fileName = "SubSpell_", menuName = "Spells/Sub Spell")]
    public class SubSpell : ScriptableObject
    {
        [Header("Meta")]
        public StackableFloat BloodCost;
        public bool AbortSpellIfAborted;
        public bool SpellShouldWaitUntilEnd;
        public bool CanBeAborted = true;
        public StackableFloat MaxInstances = new StackableFloat(10);

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
            public bool TrackAffectedCharacters;

            [Header("SubSpell targeting")]
            public TargetResolution SubSpellSource;
            public TargetResolution SubSpellTarget;
            
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
        
        public ISpellEffectHandler EffectHandler {
            get
            {
                if (_effectHandler == null && Effect != null)
                    _effectHandler = Instantiate(Effect).GetComponent<ISpellEffectHandler>();

                return _effectHandler;
            }
        }

        private ISpellEffectHandler _effectHandler;

        public bool IsProjectileSubSpell => Projectile != null;
    }
}
