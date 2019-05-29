using System.Collections.Generic;
using System.Linq;
using Actors;
using UnityEngine;
using Utils.Debugger;

namespace Spells
{
    public static class AoeUtility
    {
        public static IEnumerable<Collider> CollidersInsideSphere(Vector3 origin, float radius, LayerMask mask)
        {
#if DEBUG
            Debugger.Default.DrawCircle(origin, Vector3.up, radius, Color.cyan, 1f);
#endif
            return Physics.OverlapSphere(origin, radius, mask);
        }

        public static IEnumerable<CharacterState> CharactersInsideSphere(Vector3 origin, float radius)
        {
            return CollidersInsideSphere(origin, radius, Common.LayerMasks.Actors)
                .Select(o => o.GetComponent<CharacterState>());
        }

        public static IEnumerable<CharacterState> CharactersInsideSphere(Vector3 origin, float radius, float minDistance)
        {
            return CharactersInsideSphere(origin, radius)
                .Where(collider =>
                {
                    var position = collider.transform.position;
                    var dir = position - origin;
                    var distance = dir.magnitude;

                    return distance > minDistance;
                });
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
            Debugger.Default.DrawCone(origin, direction, maxDistance, maxAngle, Color.cyan, 1f);
#endif       

            var insideSphere = Physics.OverlapSphere(origin, maxDistance, Common.LayerMasks.Actors);
            var filtered = insideSphere.Where(collider =>
            {
                var position = collider.transform.position;
                var dir = position - origin;
                var distance = dir.magnitude;
                dir.Normalize();
                
                // Distance check
                if (distance > maxDistance || distance < minDistance)
                    return false;

                Debugger.Default.DrawLine(origin, position, Color.green);

                // Angle check
                var angle = Vector3.Angle(direction, dir);
                return angle < maxAngle * 0.5f;
            });
            return filtered.Select(f => f.GetComponent<CharacterState>());
        }

        public static IEnumerable<CharacterState> CharactersAlongRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
#if DEBUG
            Debugger.Default.DrawRay(new Ray(origin, direction), Color.cyan, maxDistance, 1f);
#endif
            return Physics.RaycastAll(origin, direction, maxDistance, Common.LayerMasks.Actors)
                .Select(hit => hit.collider.GetComponent<CharacterState>());
        }

        public static IEnumerable<CharacterState> CharactersLine(Vector3 from, Vector3 to)
        {
#if DEBUG
            Debugger.Default.DrawLine(from, to, Color.cyan, 1f);
#endif
            var dir = to - from;
            return Physics.RaycastAll(from, dir.normalized, dir.magnitude, Common.LayerMasks.Actors)
                .Select(hit => hit.collider.GetComponent<CharacterState>());
        }

        public static Vector3 RandomInLine(Vector3 from, Vector3 to)
        {
            return Vector3.Lerp(from, to, Random.value);
        }

        public static Vector3 RandomInSphere(Vector3 origin, float radius, float minRadius)
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
    }
}
