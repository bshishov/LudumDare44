using UnityEngine;

namespace Spells
{
    public interface ITargetLocationProvider
    {
        Vector3? GetTargetLocation();
    }
}