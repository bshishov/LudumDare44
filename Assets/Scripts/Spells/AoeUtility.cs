using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Actors;
using UnityEngine;
using Utils.Debugger;
using Random = UnityEngine.Random;

namespace Spells
{
    public static class AoeUtility
    {
#if DEBUG
        private static Color Color = Color.cyan;
        private static float LifeTime = 1f;
#endif
        private static readonly Collider[] CollidersBuffer = new Collider[100];
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[50];
        
        public static int InLineNonAlloc(RaycastHit[] results, Vector3 from, Vector3 to, LayerMask mask)
        {
            #if DEBUG
            Debugger.Default.DrawLine(from, to, Color.cyan, 1f);
            #endif
            var dir = to - from;
            return Physics.RaycastNonAlloc(from, dir.normalized, results, dir.magnitude, mask);
        }

        public static IEnumerable<CharacterState> CharactersInsideSphere(Vector3 origin, float radius)
        {
            return Physics.OverlapSphere(origin, radius, Common.LayerMasks.Actors)
                .Select(o => o.GetComponent<CharacterState>());
        }

        public static IEnumerable<CharacterState> CharactersInsideSphere(Vector3 origin, float radius, float minDistance)
        {
            #if DEBUG
            Debugger.Default.DrawCircle(origin, Vector3.up, radius, Color, LifeTime);
            #endif
            
            return CharactersInsideSphere(origin, radius)
                .Where(collider =>
                {
                    if (collider == null)
                        return false;
                    var position = collider.transform.position;
                    var dir = position - origin;
                    var distance = dir.magnitude;

                    return distance > minDistance;
                });
        }
        
        public static void CharactersInsideSphereNonAlloc(
            List<CharacterState> results, 
            Vector3 origin, 
            float radius, 
            float minDistance=0f)
        {
            results.Clear();
            
            #if DEBUG
            Debugger.Default.DrawCircle(origin, Vector3.up, radius, Color, LifeTime);
            #endif
            
            var cols = Physics.OverlapSphereNonAlloc(origin, radius, CollidersBuffer, Common.LayerMasks.Actors);
            for (var i = 0; i < cols; i++)
            {
                var collider = CollidersBuffer[i];
                if(collider == null)
                    break;
                
                var state = collider.GetComponent<CharacterState>();
                if (state != null && state.IsAlive)
                {
                    if (Vector3.Distance(collider.transform.position, origin) >= minDistance)
                        results.Add(state);
                }
            }
        }

        public static IEnumerable<CharacterState> CharactersInsideCone(
            Vector3 origin, 
            Vector3 direction, 
            float maxAngle, 
            float maxDistance,
            float minDistance = 0f)
        {
            direction.y = 0;
            direction.Normalize();
            #if DEBUG
            Debugger.Default.DrawCone(origin, direction, maxDistance, maxAngle, Color, LifeTime);
            #endif       
            
            var insideSphere = Physics.OverlapSphere(origin, maxDistance, Common.LayerMasks.Actors);
            var filtered = insideSphere.Where(collider =>
            {
                var position = collider.transform.position;
                var dir = position - origin;
                var distance = dir.magnitude;
                dir.Normalize();
                
                // Distance check
                if (distance > maxDistance || distance <= minDistance)
                    return false;

                // Angle check
                var angle = Vector3.Angle(direction, dir);
                return angle < maxAngle * 0.5f;
            });
            return filtered.Select(f => f.GetComponent<CharacterState>());
        }

        public static void CharactersInsideConeNonAlloc(
            List<CharacterState> results,
            Vector3 origin,
            Vector3 direction,
            float maxAngle,
            float maxDistance,
            float minDistance = 0f)
        {
            results.Clear();
            direction.y = 0;
            direction.Normalize();
            #if DEBUG
            Debugger.Default.DrawCone(origin, direction, maxDistance, maxAngle, Color.cyan, 1f);
            #endif  
            
            var cols = Physics.OverlapSphereNonAlloc(origin, maxDistance, CollidersBuffer, Common.LayerMasks.Actors);
            for (var i = 0; i < cols; i++)
            {
                var collider = CollidersBuffer[i];
                if(collider == null)
                    break;

                var state = collider.GetComponent<CharacterState>();
                if(state == null || !state.IsAlive)
                    continue;
                
                var position = collider.transform.position;
                var dir = position - origin;
                var distance = dir.magnitude;
                dir.Normalize();
                
                // Distance check
                if (distance > maxDistance || distance <= minDistance)
                    continue;

                // Angle check
                var angle = Vector3.Angle(direction, dir);
                if(angle < maxAngle * 0.5f)
                    results.Add(state);
            }
        }

        public static IEnumerable<CharacterState> CharactersAlongRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            #if DEBUG
            Debugger.Default.DrawRay(origin, direction, Color, maxDistance, LifeTime);
            #endif
            
            return Physics.RaycastAll(origin, direction, maxDistance, Common.LayerMasks.Actors)
                .Select(hit => hit.collider.GetComponent<CharacterState>());
        }

        public static IEnumerable<CharacterState> CharactersLine(Vector3 from, Vector3 to)
        {
            #if DEBUG
            Debugger.Default.DrawLine(from, to, Color, LifeTime);
            #endif
            var dir = to - from;
            return CharactersAlongRay(from, dir.normalized, dir.magnitude);
        }
        
        public static void CharactersAlongRayNonAlloc(
            List<CharacterState> results, 
            Vector3 origin, 
            Vector3 direction, 
            float maxDistance)
        {
            results.Clear();
    
            #if DEBUG
            Debugger.Default.DrawRay(origin, direction, Color, maxDistance, LifeTime);
            #endif

            var hits = Physics.RaycastNonAlloc(origin, direction, RaycastHits, maxDistance, Common.LayerMasks.Actors);

            for (var i = 0; i < hits; i++)
            {
                var hit = RaycastHits[i];
                if(hit.collider == null)
                    break;

                var state = hit.collider.GetComponent<CharacterState>();
                if(state != null && state.IsAlive)
                    results.Add(state);
            }
        }
        
        public static void CharactersInLineNonAlloc(List<CharacterState> results, Vector3 from, Vector3 to)
        {
            var dir = to - from;
            CharactersAlongRayNonAlloc(results, from, dir.normalized, dir.magnitude);
        }

        public static Vector3 RandomInLine(Vector3 from, Vector3 to)
        {
            return Vector3.Lerp(from, to, Random.value);
        }

        public static Vector3 RandomInsideSphere(Vector3 origin, float radius, float minRadius)
        {
            // Y plane omitted
            var distance = Mathf.Lerp(minRadius, radius, Random.value);
            var angle = Mathf.PI * 2 * Random.value;
            var dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            return origin + dir * distance;
        }

        public static Vector3 RandomInCone(
            Vector3 origin,
            Vector3 direction,
            float maxAngle,
            float radius, 
            float minRadius)
        {
            direction.y = 0;
            var distance = Mathf.Lerp(minRadius, radius, Random.value);
            var angle = Mathf.Lerp(-maxAngle * 0.5f,maxAngle * 0.5f, Random.value);
            return origin + distance * (Quaternion.Euler(0, angle, 0) * direction.normalized);
        }

        public static IEnumerable<Vector3> EnumerateLocationsInCircle(
            Vector3 origin,
            float radius,
            float minRadius = 0f,
            float spacing = 1f)
        {
            // Check Spacing
            spacing = Mathf.Max(0.2f, spacing);
            
            // Total "ring" band thickness 
            var rRange = Mathf.Max(0, radius - minRadius);
            const float arcSize = 360f;
            const float arc2Len = 2 * Mathf.PI * arcSize / 360f;

            // Orbital-Subdivision (number of rings)
            // Slight correction for circle-packing like feel
            var nRings = Subdiv(rRange, spacing * 0.8f);
            for (var ringIdx = 0; ringIdx < nRings; ringIdx ++)
            {
                var ringR = minRadius + SubdividedPos(ringIdx, nRings, rRange);
                
                // Radial subdivision
                // Subdivide ring length
                var nSegments = Subdiv(arc2Len * ringR, spacing);
                for (var segmentIdx = 0; segmentIdx < nSegments; segmentIdx++)
                {
                    // Angle is in RADIANS
                    var segmentAngle = SubdividedPos(segmentIdx, nSegments, Mathf.PI * 2);
                    yield return origin + FromRadialXz(ringR, segmentAngle); 
                }
            }
        }
        
        public static IEnumerable<Vector3> EnumerateLocationsInCone(
            Vector3 origin,
            Vector3 direction,
            float angle,
            float radius,
            float minRadius = 0f,
            float spacing = 1f)
        {
            // Check Spacing
            spacing = Mathf.Max(0.2f, spacing);
            
            // Total "ring" band thickness 
            var rRange = Mathf.Max(0, radius - minRadius);
            var arc2Len = 2 * Mathf.PI * angle / 360f;
            var arcLen = Mathf.Deg2Rad * angle;
            var baseAngle = Mathf.Atan2(direction.z, direction.x) - arcLen * 0.5f;

            // Orbital-Subdivision (number of rings)
            // Slight correction for circle-packing like feel
            var nRings = Subdiv(rRange, spacing * 0.8f);
            for (var ringIdx = 0; ringIdx < nRings; ringIdx ++)
            {
                var ringR = minRadius + SubdividedPos(ringIdx, nRings, rRange);
                var ringLength = arc2Len * ringR;
                
                // Radial subdivision
                var nSegments = Subdiv(ringLength, spacing);
                for (var segmentIdx = 0; segmentIdx < nSegments; segmentIdx++)
                {
                    // Angle is in RADIANS
                    var segmentAngle = baseAngle + SubdividedPos(segmentIdx, nSegments, arcLen);
                    yield return origin + FromRadialXz(ringR, segmentAngle); 
                }
            }
        }

        public static IEnumerable<Vector3> EnumerateLocationsInLine(
            Vector3 start,
            Vector3 end,
            float spacing = 1f
        )
        {
            spacing = Mathf.Max(0.2f, spacing);
            var nSegments = Subdiv((end - start).magnitude, spacing);
            for (var segmentIdx = 0; segmentIdx < nSegments; segmentIdx++)
            {
                yield return
                    Vector3.LerpUnclamped(
                        start, 
                        end, 
                        SubdividedPos(segmentIdx, nSegments, 1f)
                        );
            }
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SubdividedPos(int index, int subDivs, float length = 1f)
        {
            return (0.5f + index) * length / subDivs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Subdiv(float length, float spacing)
        {
            return Mathf.Max(1, Mathf.RoundToInt(length / spacing));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 FromRadialXz(float r, float angle)
        {
            return new Vector3(Mathf.Cos(angle) * r, 0 ,Mathf.Sin(angle) * r);
        }
    }
}
