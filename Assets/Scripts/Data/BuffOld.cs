using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Mechanics/Buff")]
    [Serializable]
    public class BuffOld : ScriptableObject
    {
        [Header("Meta")]
        public string Name;        
        public enum ChangedProperties { Speed, Damage, Power, HealthRegen, HealthCap, Evasion, InventoryBuff, Size, Aura }

        [Header("Effect")]
        public ChangedProperties ChangedProperty;
        public float Addition;
        public float Multiplier = 1f;
        public float Radius = 0f;
        public bool PerSecond;
        public bool Permanent;
        public float Duration;
        
        [Header("Visual Effects")]
        public GameObject OnAppliedEffect;
    }


    
}
