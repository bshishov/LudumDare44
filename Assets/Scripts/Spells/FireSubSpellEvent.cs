using System;
using UnityEngine;

namespace Spells
{
    [Serializable]
    public struct FireSubSpellEvent
    {
        public SubSpellEvent Type;
        public SubSpell SubSpell;
        [Range(0, 1)]
        public float Chance;
    }
}