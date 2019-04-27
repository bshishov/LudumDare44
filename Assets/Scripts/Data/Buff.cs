using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Mechanics/Buff")]
    [Serializable]
    public class Buff : ScriptableObject
    {
        [Header("Meta")]
        public string Name;        
        public enum ChangedProperties { Speed, Damage, Power, HealthRegen, HealthCap, Evasion, InventoryBuff }

        [Header("Effect")]
        public ChangedProperties ChangedProperty;
        public float Addition;
        public float Multiplier;
        public float Radius = 0f;
        public float Time;

        // TODO: set later
        [Header("Visual Effects")]
        public GameObject VisualEffect;
    }
}
