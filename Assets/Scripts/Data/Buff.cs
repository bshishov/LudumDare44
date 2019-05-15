using System;
using Data;
using UnityEngine;

namespace Assets.Scripts.Data
{
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
        public bool ApplyInitialAffectsOnReapply;

        [Header("Temporary Modifiers")]
        // Changes that will be applied once the buff is applied
        // and will be REVERTED when buff removed
        public Modifier[] Modifiers;
        
        [Header("Affects on buff applied")]
        public Affect[] OnApplyBuff;

        [Header("Affects on buff refresh (reapplied while there was same buff already)")]
        public Affect[] OnRefreshBuff;

        [Header("Affects each tick")]
        public Affect[] OnTickBuff;

        [Header("Affects after buff remove")]
        public Affect[] OnRemove;
    }
}