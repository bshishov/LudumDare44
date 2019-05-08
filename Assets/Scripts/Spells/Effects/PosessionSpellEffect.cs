using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects {
public class PosessionSpellEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject LightningPrefab;

    void Start() { Destroy(gameObject, 1f); }

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets) { }

        public void OnTargetsFinalized(SpellContext context, SpellTargets castData) { }
        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
        foreach (var dst in targets.Destinations)
        {
            Assert.IsTrue(dst.Position.HasValue, "dst.Position.Position != null");

            var lObj = Instantiate(LightningPrefab, transform);
            lObj.GetComponent<Lightning>().SetupLine(targets.Source.Position.Value, dst.Position.Value);
        }
    }

    public void OnEndSubSpell(ISpellContext context) {  }
}
}