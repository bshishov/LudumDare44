using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Spells.Effects
{
public class RageRayEffect : MonoBehaviour, ISubSpellEffect
{
    private struct Data
    {
        [NotNull]
        public BloodTube tube;

        [NotNull]
        public Transform source;
    }

    private Dictionary<ISpellContext, Data> _tubes = new Dictionary<ISpellContext, Data>(1);
    public  BloodTube                       Prefab;


    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        if (_tubes.ContainsKey(context))
            return;

        Debug.Log($"Create new Rage ray, current entity {gameObject.GetInstanceID()}");
        var data = new Data {tube = Instantiate(Prefab), source = targets.Source.Transform};
        _tubes.Add(context, data);
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets) { }

    public void OnEndSubSpell(ISpellContext context)
    {
        if (_tubes.TryGetValue(context, out var data))
        {
            Destroy(data.tube.gameObject);
            _tubes.Remove(context);
        }
    }

    private void Update()
    {
        foreach (var entry in _tubes)
        {
            var context = entry.Key;
            var data    = entry.Value;

            var target = context.ChannelingInfo.GetNewTarget();
            if (target == null)
                continue;

            var origin    = data.source.position;
            var direction = (target.Position.Value - origin);
            direction.Normalize();

            data.tube.SetupLine(origin, origin + direction * context.CurrentSubSpell.Area.Size);
        }
    }
}
}