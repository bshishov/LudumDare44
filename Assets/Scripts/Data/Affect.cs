using System;
using Actors;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class Affect
    {
        public enum AffectType
        {
            ApplyModifier,
            CastSpell,
            ApplyBuff,
            SpawnObject,
            Move
        }
        
        public enum StacksBehaviour
        {
            Inherit,
            Override,
        }

        [Serializable]
        public class SpawnObjectInfo
        {
            public GameObject Prefab;

            [Header("Position")]
            public CharacterState.NodeRole CharacterNode;
            public bool AttachToTransform = false;

            [Header("Lifecycle")]
            public bool AutoDestroyAfterBuff = false;
        }

        [Serializable]
        public class SpellCastInfo
        {
            [Serializable]
            public enum SpellTarget
            { 
                Self,
                Source,
                CurrentSpellEmitter
            }

            [Tooltip("How many stacks casted spell will have")]
            public StacksBehaviour SpellStacks;
            public Spell Spell;

            [Tooltip("From current buff receiver point of view")]
            public SpellTarget Target;

            [Tooltip("Used only when StacksBehaviour is set to override. Use only if you want to explicitly specify stack amount.")]
            public int StacksOverride = 1;
        }

        [Serializable]
        public struct ApplyBuffInfo
        {
            [Serializable]
            public enum BuffTarget
            {
                Self,
                SpellSource
            }

            [Tooltip("From current buff receiver point of view")]
            public BuffTarget Target;

            [Tooltip("How many stacks new buff will have")]
            public StacksBehaviour StacksBehaviour;
            public Buff Buff;

            [Tooltip("Used only when StacksBehaviour is set to override. Use only if you want to explicitly specify stack amount.")]
            public int StacksOverride;
        }

        [Serializable]
        public struct MoveInfo
        {
            public enum MoveRelation
            {
                LookDirection,
                SourceCharacter,
                SpellTarget,
                SpellSource
            }

            public MoveRelation RelativeTo;
            public float Speed;
            public float MovementDuration; // also controlled by buff.Duration
            public bool BreakOnDestination;
            public bool BreakOnDuration;
        }

        public AffectType Type;
        public Modifier ApplyModifier;
        public SpellCastInfo CastSpell;
        public ApplyBuffInfo ApplyBuff;
        public SpawnObjectInfo SpawnObject;
        public MoveInfo Move;
    }
}