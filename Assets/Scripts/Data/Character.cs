using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Character", menuName = "Mechanics/Character")]
    public class Character : ScriptableObject
    {
        public float Health;
        public float Damage;
        public float Speed;    

        [Header("Drop Info")]
        public float DropRate;
        [SerializeField]
        public Spell[] DropSpells;

        [Header("Spell Info")]
        [SerializeField]
        public Spell[] UseSpells;
    }
}