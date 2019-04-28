using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AreaOfEffect", menuName = "Mechanics/AreaOfEffect")]
    public class AreaOfEffect : ScriptableObject
    {
        [Serializable]
        public enum AreaType : int
        {
            Ray,
            Sphere,
            Cylinder,
            Conus
        }

        public float Size = 0.0f;
        public AreaType Area;
    }
}
