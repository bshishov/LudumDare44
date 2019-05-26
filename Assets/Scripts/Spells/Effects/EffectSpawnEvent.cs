using System;

namespace Spells.Effects
{
    [Serializable]
    public enum EffectSpawnEvent
    {
        OnInputTargetsValidated,
        OnTargetsFinalized,
        OnTargetsAffected
    }
}