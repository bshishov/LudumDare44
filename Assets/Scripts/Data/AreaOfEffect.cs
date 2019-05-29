using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AoE_", menuName = "Spells/AoE")]
    public class AreaOfEffect : ScriptableObject
    {
        [Serializable]
        public enum AreaType
        {
            Sphere,
            Cone,
            Line
        }

        public AreaType Area;
        public StackableFloat Size;
        public StackableFloat MinSize;
        public StackableFloat Angle;
    }
}
