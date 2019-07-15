using System;
using Actors;
using UnityEngine;
using Utils.Debugger;

namespace Spells
{
    public static class TargetUtility
    {
        private static readonly Vector3 RaycastOffset = new Vector3(0, 10f, 0);
        private const float RaycastMaxDistance = 20f;
        
        public static Vector3 AboveGround(Vector3 position, float hoverHeight=0f)
        {
            if (Physics.Raycast(
                position + RaycastOffset, 
                Vector3.down, 
                out var hit, 
                RaycastMaxDistance, Common.LayerMasks.Ground))
                return hit.point + new Vector3(0, hoverHeight, 0);
            return position;
        }
        
        public static bool IsNearGround(Vector3 position, float testHeight = 0.5f)
        {
            return Physics.Raycast(
                position + Vector3.up, 
                Vector3.down, 
                testHeight, 
                Common.LayerMasks.Ground);
        }

        public static void DebugDraw(Target target, Color color)
        {
#if DEBUG
            if (target.HasPosition)
            {
                var tPos = target.OffsettedPosition;
                Debugger.Default.DrawCircle(tPos, Vector3.up, 0.5f, color, 1f);
                Debugger.Default.DrawRay(tPos, target.Forward, color, 0.5f, 1f);
            }
#endif
        }
        
        public static void DebugDrawSourceAndTarget(Target source, Target target)
        {
#if DEBUG
            if (source.HasPosition)
            {
                var sPos = source.OffsettedPosition;
                Debugger.Default.DrawCircle(sPos, Vector3.up, 0.5f, Color.red, 1f);
                Debugger.Default.DrawRay(sPos, source.Forward, Color.red, 0.5f, 1f);

                if (target.HasPosition)
                {
                    var tPos = target.OffsettedPosition;
                    Debugger.Default.DrawCircle(tPos, Vector3.up, 0.5f, Color.blue, 1f);
                    Debugger.Default.DrawRay(tPos, target.Forward, Color.blue, 0.5f, 1f);
                    
                    Debugger.Default.DrawLine(sPos, tPos, Color.magenta, 1f);
                }
            }
#endif
        }
        
        public static bool IsValidTeam(CharacterState origin, CharacterState other, Query.QueryTeam query)
        {
            switch (query)
            {
                case Query.QueryTeam.Self:
                    return origin.Equals(other);
                case Query.QueryTeam.EveryoneExceptSelf:
                    return !origin.Equals(other);
                case Query.QueryTeam.Everyone:
                    return true;
                case Query.QueryTeam.Ally:
                    if (other.CurrentTeam.HasFlag(CharacterState.Team.AgainstTheWorld))
                        return false;
                    return origin.CurrentTeam == other.CurrentTeam;
                case Query.QueryTeam.Enemy:
                    if (other.CurrentTeam.HasFlag(CharacterState.Team.AgainstTheWorld))
                        return true;
                    return origin.CurrentTeam != other.CurrentTeam;
                default:
                    throw new InvalidOperationException($"Invalid query team type: {query}");
            }
        }

        public static bool IsInRange(Target source, Target target, float minRange, float maxRange)
        {
            if (target.Type == TargetType.None)
                return true;

            var dir = target.Position - source.Position;
            dir.y = 0;
            var range = dir.magnitude;
            return range >= minRange && range <= maxRange;
        }

        public static float XZDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}