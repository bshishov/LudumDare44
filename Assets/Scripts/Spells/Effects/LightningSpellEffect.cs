
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Spells.Effects
{
    public class LightningSpellEffect : MonoBehaviour, ISubSpellEffect
    {
        public GameObject LightningPrefab;
        
        public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
        {
            Assert.IsTrue(targets.Source.Transform);
            foreach (var dst in targets.Destinations)
            {
                Assert.IsTrue(dst.Position.HasValue, "dst.Position.Position != null");

                var lObj = Instantiate(LightningPrefab, transform);
                lObj.GetComponent<Lightning>().SetupLine(targets.Source.Position.Value, dst.Position.Value);
            }
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets) { }
        public void OnEndSubSpell(SpellContext context) {  }
    }
}
