using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects
{
public class FireballSpellEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject ExplosionPrefab;

        public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets){}

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {
            Assert.IsTrue(targets.Source.Position.HasValue, "targets.Source.Position != null");
            Destroy(Instantiate(ExplosionPrefab, targets.Source.Position.Value, Quaternion.identity), 2);
    }
}
}