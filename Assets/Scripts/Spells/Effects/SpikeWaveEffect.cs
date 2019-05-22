using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects
{
    public class SpikeWaveEffect : MonoBehaviour, ISubSpellEffect
    {
        public ParticleSystem SpikePrefab;
        public float SpikesPerDistance = 7.5f;

        public void OnInputTargetsValidated(ISpellContext context, SpellTargets targets)
        {
            Assert.AreEqual(context.CurrentSubSpell.Area.Area, AreaOfEffect.AreaType.Cone);

            foreach (var target in targets.Destinations)
            {
                var orient = Quaternion.LookRotation(target.Position.Value - targets.Source.Position.Value);
                orient.x = orient.z = 0;

                Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
                SpawnParticle(targets.Source.Position.Value,
                    orient,
                    (context.CurrentSubSpell.Area.Size + context.CurrentSubSpell.Area.MinSize) / 2,
                    context.CurrentSubSpell.Area.Angle);
            }
        }

        public void OnTargetsFinalized(SpellContext context, SpellTargets castData) {}

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets) {}

        public void OnEndSubSpell(ISpellContext context) {}

        [ContextMenu("DO!")]
        public void TestSpikes()
        {
            if (SpikePrefab == null)
                return;
            for (var i = 1; i < 6; ++i)
                SpawnParticle(gameObject.transform.position, Quaternion.identity, i, 60);
        }

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
    }
}