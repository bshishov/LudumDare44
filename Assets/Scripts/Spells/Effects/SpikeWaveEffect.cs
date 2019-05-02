using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects
{
public class SpikeWaveEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject WavePrefab;

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        foreach (var target in targets.Destinations)
        {
            var orient          = Quaternion.LookRotation(target.Position.Value - targets.Source.Position.Value);
            orient.x = orient.z = 0;

            Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
            Destroy(Instantiate(WavePrefab, targets.Source.Position.Value, orient), 2);
        }
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets) { }
}
}