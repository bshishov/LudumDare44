using UnityEngine;

namespace Spells
{
    public interface ITargetLocationProvider
    {
        Vector3 Location { get; }
        Vector3 Forward { get; }
        bool IsValid { get; }
    }

    public class TargetLocationProvider : ITargetLocationProvider
    {
        public Vector3 Location { get; set; }
        public Vector3 Forward { get; set; } = Vector3.forward;
        public bool IsValid { get; set; }
    }
}