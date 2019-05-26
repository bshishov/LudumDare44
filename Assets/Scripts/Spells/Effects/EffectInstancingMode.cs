using System;

namespace Spells.Effects
{
    [Serializable]
    public enum EffectInstancingMode
    {
        OnePerEventTarget,
        OnePerSubSpell
    }
}