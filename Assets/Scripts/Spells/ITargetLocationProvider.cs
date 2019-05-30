using UnityEngine;

namespace Spells
{
    public interface ITargetLocationProvider
    {
        Vector3 Location { get; }
        bool IsValid { get; }
    }

    public class TargetLocationProvider : ITargetLocationProvider
    {
        public Vector3 Location { get; set; }
        public bool IsValid { get; set; }
    }
}