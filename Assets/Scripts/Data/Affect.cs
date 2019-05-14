using System;
using Actors;
using Assets.Scripts.Data;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class Affect
    {
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
        public enum StacksBehaviour
        {
            Inherit,
            Override,
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

        public Modifier ApplyModifier;
        public SpellCastInfo CastSpell;
        public ApplyBuffInfo ApplyBuff;
        public SpawnObjectInfo SpawnObject;
    }
}