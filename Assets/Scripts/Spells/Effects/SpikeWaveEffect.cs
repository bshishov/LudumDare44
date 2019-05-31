using UnityEngine;

namespace Spells.Effects
{
    public class SpikeWaveEffect : MonoBehaviour, ISpellEffectHandler
    {
        public ParticleSystem SpikePrefab;
        public float SpikesPerDistance = 7.5f;

        private void SpawnParticle(Vector3 origin, Quaternion rotation, float distance, float angle)
        {
            // Try cast a ray to the ground
            if (Physics.Raycast(origin, Vector3.down, out var hit, 2f, Common.LayerMasks.Ground))
            {
                // If succeeded - replace the origin with the hit point, so we spawn particles from ground
                origin = hit.point;
            }

            var instance = Instantiate(SpikePrefab, origin, rotation);
            Destroy(instance.gameObject, 1.0f);

            var particles = instance.GetComponent<ParticleSystem>();
            var shape = particles.shape;
            shape.arc = angle;
            shape.radius = distance;
            shape.rotation = new Vector3(shape.rotation.x, shape.rotation.y, 90 - angle / 2);

            var emission = particles.emission;
            var burst = emission.GetBurst(0);

            burst.count = new ParticleSystem.MinMaxCurve(distance * SpikesPerDistance);
            emission.SetBurst(0, burst);

            particles.Play();
        }

        public void OnEvent(SubSpellEventArgs args)
        {
            if(args.Event != SubSpellEvent.OnFire)
                return;

            if(args.Query.NewTargetsQueryType == Query.QueryType.None)
                return;
            
            var source = args.Handler.Source;
            
            if (!(source.IsValid && source.HasPosition))
                return;
            
            var sourcePos = source.Position;
            var orientation = Quaternion.LookRotation(source.Forward);
            orientation.x = orientation.z = 0;

            SpawnParticle(sourcePos, 
                orientation,
                (args.Query.Area.Size.GetValue(args.Handler.Stacks) + args.Query.Area.MinSize.GetValue(args.Handler.Stacks)) * 0.5f,
                args.Query.Area.Angle.GetValue(args.Handler.Stacks));
        }
    }
}