using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AreaOfEffect", menuName = "Spells/AreaOfEffect")]
    public class AreaOfEffect : ScriptableObject
    {
        [Serializable]
        public enum AreaType : int
        {
            Ray,
            Sphere,
            Cylinder,
            Cone
        }

        public float Size = 0.0f;
        public float Angle = 45.0f;
        public AreaType Area;
    }
}
