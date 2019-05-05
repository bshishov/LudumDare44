using UnityEngine;

namespace Spells.Effects
{
public class RageRayEffect : MonoBehaviour, ISpellEffect
{
    private BloodTube _tube;

    public BloodTube Prefab;

    public void OnStateChange(ISpellContext context, ContextState oldState)
    {
        if (context.State == ContextState.Fire)
            StartFiring(context);
        if (context.State > ContextState.Fire)
            StopFiring(context);
    }

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        if (_tube == null)
            return;
        _tube.SetupLine(targets.Source.Transform.position, targets.Destinations[0].Position.Value);
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets) { }

    private void StartFiring(ISpellContext context)
    {
        if (_tube != null)
            return;

        Debug.Log($"Create new Rage ray, current entity {gameObject.GetInstanceID()}");
        _tube = Instantiate(Prefab);
    }


    private void StopFiring(ISpellContext context) { Destroy(_tube.gameObject); }
}
}