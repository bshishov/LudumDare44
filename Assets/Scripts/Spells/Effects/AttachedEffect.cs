using System.Collections.Generic;
using Actors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spells.Effects
{
public class AttachedEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject              Object;

    [FormerlySerializedAs("StartEffectOnPreSelected")]
    public bool                    StartEffectOnInputTargetValidated;
    public float                   LifeTime            = 2f;
    public bool                    RotateTowardsTarget = true;
    public CharacterState.NodeRole Node                = CharacterState.NodeRole.Chest;

    public void OnInputTargetsValidated(ISpellContext context, SpellTargets targets)
    {
        if (StartEffectOnInputTargetValidated)
            SpawnEffect(targets);
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        if (!StartEffectOnInputTargetValidated)
            SpawnEffect(targets);
    }

    public void OnEndSubSpell(ISpellContext     context)                        { }
    public void OnTargetsFinalized(SpellContext context, SpellTargets castData) { }


    private void SpawnEffect(SpellTargets targets)
    {
        // TODO : set rotation towards target or pass target transform to IAttachable
        var attachTo = targets.Source.Character.GetNodeTransform(Node);

        var origin   = attachTo.transform.position;
        var rotation = Quaternion.identity;

        if (RotateTowardsTarget && targets.Destinations.Length > 0)
        {
            rotation = Quaternion.LookRotation(targets.Destinations[0].Position.Value - origin);
        }

        var instance = Instantiate(Object, origin, rotation);
        instance.transform.SetParent(transform);

        var attachable = instance.GetComponent<IAttachable>();
        attachable?.Attach(attachTo);

        Destroy(instance, LifeTime);
    }
}
}