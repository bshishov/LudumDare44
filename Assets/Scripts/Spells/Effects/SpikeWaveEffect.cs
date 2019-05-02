using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = System.Diagnostics.Debug;

namespace Spells.Effects
{
    public class SpikeWaveEffect : MonoBehaviour, ISubSpellEffect
    {
        public GameObject WavePrefab;
        
        public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
        {
            var orient = Quaternion.LookRotation(targets.Directions[0]);
            orient.x = orient.z = 0;

            Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
            Destroy(Instantiate(WavePrefab, targets.Source.Position.Value, orient), 2);
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets) {  }
    }
}
