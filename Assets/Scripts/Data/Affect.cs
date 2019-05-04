using System;
using UnityEngine;

namespace Assets.Scripts.Data
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
        public class SpellCastInfo
        {
            [Serializable]
            public enum SpellTarget
            { 
                Self,
                Source,
                CurrentSpellEmitter
            }

            [Serializable]
            public enum StacksBehaviour
            {
                SameStacksAsBuff,
                Override,
            }

            public StacksBehaviour SpellStacks;
            public Spell Spell;
            public SpellTarget Target;
            public int StacksOverride = 1;
        }

        public Modifier ApplyModifier;
        public SpellCastInfo CastSpell;
        public SpawnObjectInfo SpawnObject;
    }
}