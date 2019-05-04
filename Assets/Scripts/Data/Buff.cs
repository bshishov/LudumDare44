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
            public enum SpellTarget
            {
                Self,
                Source,
                CurrentSpellEmitter
            }

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

    public enum ModificationParameter
    {
        None = 0,
        HpFlat = 1,  // Think twice while using it
        HpMult = 2,  // Think twice while using it
        MaxHpFlat = 3,
        MaxHpMult = 4,
        DmgFlat,
        DmgMult,
        EvasionChanceFlat,
        SizeFlat,
        SizeMult,
        SpeedFlat,
        SpeedMult,
        SpellStacksFlat,
        CritChanceFlat,
        SpellDamageAmpFlat
    }
    
    public enum BuffStackBehaviour
    {
        MaxStacksOfTwo,
        SumStacks,
        AddNewAsSeparate,
        Discard
    }

    [Serializable]
    public class Modifier
    {
        public ModificationParameter Parameter;
        public float Value;

        // Indicating that the value will not scale too much after this amount of stacks
        public float EffectiveStacks = 10f;
    }

    [CreateAssetMenu(fileName = "Buff", menuName = "Mechanics/Buff")]
    [Serializable]
    public class Buff : ScriptableObject
    {
        public string Name;
        public BuffStackBehaviour Behaviour;
        public float TickCooldown = 1f;
        public float Duration = 1f;

        [Header("Modifiers")]
        // Changes that will be applied once the buff is applied
        // and will be REVERTED when buff removed
        public Modifier[] Modifiers;

        [Header("Affects")]
        public Affect[] OnApplyBuff;
        public Affect[] OnTickBuff;
        public Affect[] OnRemove;
    }
}