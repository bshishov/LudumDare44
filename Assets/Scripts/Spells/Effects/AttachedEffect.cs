using UnityEngine;

namespace Spells.Effects
{
public class AttachedEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject Object;
    public bool       StartEffectOnPreSelected;

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        if (StartEffectOnPreSelected)
            SpawnEffect(targets);
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        if (!StartEffectOnPreSelected)
            SpawnEffect(targets);
    }


    private void SpawnEffect(SpellTargets targets)
    {
        var instance = Instantiate(Object,
                                   targets.Source.Transform.position,
                                   Quaternion.Euler(targets.Destinations[0].Position.Value - targets.Source.Position.Value));

        Destroy(instance, 2.0f);
    }
}
}