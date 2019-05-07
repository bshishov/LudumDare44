using UnityEngine;

namespace Spells.Effects
{
    public interface IRayEffect
    {
        void RayStarted(Vector3 source, Vector3 destination);
        void RayUpdated(Vector3 source, Vector3 destination);
        void RayEnded();
    }
}