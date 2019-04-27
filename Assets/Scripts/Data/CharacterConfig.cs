using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Mechanics/CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        public float Health;
        public float Damage;
        public float Speed;
        public float Evasion = 0f;
        public float Size = 1f;

        [Header("Drop Info")]
        public float DropRate;
        [SerializeField]
        public List<Spell> DropSpells;

        [Header("Spell Info")]
        [SerializeField]
        public List<Spell> UseSpells;
    }
}