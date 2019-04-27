using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Item", menuName = "Mechanics/Item")]
    public class Item : ScriptableObject
    {
        public string Name;
        public int Cost;

        [SerializeField]
        public Buff[] Buffs;
    }
}
