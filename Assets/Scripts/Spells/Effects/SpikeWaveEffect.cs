using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects
{
public class SpikeWaveEffect : MonoBehaviour, ISubSpellEffect
{
    private List<GameObject> _spawnedParticles = new List<GameObject>(1);
    public  ParticleSystem   SpikePrefab;
    public  float            SpikesPerDistance = 7.5f;

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        Assert.AreEqual(context.CurrentSubSpell.Area.Area, AreaOfEffect.AreaType.Cone);

        foreach (var target in targets.Destinations)
        {
            var orient          = Quaternion.LookRotation(target.Position.Value - targets.Source.Position.Value);
            orient.x = orient.z = 0;

            Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
            SpawnParticle(targets.Source.Position.Value,
                          orient,
                          (context.CurrentSubSpell.Area.Size + context.CurrentSubSpell.Area.MinSize) / 2,
                          context.CurrentSubSpell.Area.Angle);
        }
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets) { }

    private void Awake() { }

    [ContextMenu("DO!")]
    public void TestSpikes()
    {
        if (SpikePrefab == null)
            return;
        for (var i = 1; i < 6; ++i)
            SpawnParticle(gameObject.transform.position, Quaternion.identity, i, 60, gameObject.transform);
    }

    private void SpawnParticle(Vector3 origin, Quaternion rotation, float distance, float angle, Transform root = null)
    {
        var instance = Instantiate(SpikePrefab, gameObject.transform, false);
        instance.transform.position = origin;
        instance.transform.rotation = rotation;

        var particles = instance.GetComponent<ParticleSystem>();
        _spawnedParticles.Add(gameObject);

        var shape = particles.shape;
        shape.arc      = angle;
        shape.radius   = distance;
        shape.rotation = new Vector3(shape.rotation.x, shape.rotation.y, angle / 2);

        var emission = particles.emission;
        var burst    = emission.GetBurst(0);

        burst.count = new ParticleSystem.MinMaxCurve(distance * SpikesPerDistance);
        emission.SetBurst(0, burst);

        particles.Play();
    }

    private void OnDestroy()
    {
        foreach (var particle in _spawnedParticles)
            Destroy(particle);
    }
}
}