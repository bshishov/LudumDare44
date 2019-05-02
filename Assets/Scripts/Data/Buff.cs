using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [Serializable]
    public class Affect
    {
        public Modifier ApplyModifier;
        public Spell CastSpell;
        public GameObject SpawnObject;
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
        public float PerStackMultiplier = 1f;
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