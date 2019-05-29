using System;

namespace Spells
{
    [Serializable]
    public enum TargetType
    {
        None,
        Character,
        Location,
        Transform,
        LocationProvider
    }
}