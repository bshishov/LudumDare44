using System;
using Data;
using UnityEngine;

namespace Spells
{
    [Serializable]
    public struct ApplyBuffSpellEvent
    {
        public SubSpellEvent Type;
        public Buff Buff;

        [Range(0, 1)]
        public float Chance;
    }
}