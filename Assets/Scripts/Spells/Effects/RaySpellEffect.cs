using System.Collections.Generic;
using UnityEngine;

namespace Spells.Effects
{
public class RaySpellEffect : MonoBehaviour, ISubSpellEffect
{
    private struct SubSpellEffectInstance
    {
        public GameObject InstanceObject;
        public IRayEffect Ray;
        public TargetInfo Source;
    }

    public GameObject RayPrefab;
    public bool       AutoDestroyAfterSpell = true;

    private readonly Dictionary<ISpellContext, SubSpellEffectInstance> _instances = new Dictionary<ISpellContext, SubSpellEffectInstance>(1);

    void Start() { }

    void Update()
    {
        foreach (var kvp in _instances)
        {
            var context  = kvp.Key;
            var instance = kvp.Value;

            var position = context.ChannelingInfo.GetNewTarget()?.Position;
            if (position != null)
                instance.Ray.RayUpdated(instance.Source.Transform.position, position.Value);
        }
    }

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        if (_instances.ContainsKey(context))
            return;


        var go = Instantiate(RayPrefab, transform, true);
        if (go != null)
        {
            var ray = go.GetComponent<IRayEffect>();

            if (ray == null)
            {
                Debug.LogWarning("Created a ray instance but it has no IRayEffect");
                Destroy(go);
                return;
            }

            var instance = new SubSpellEffectInstance
                           {
                               Ray            = ray,
                               InstanceObject = go,
                               Source         = targets.Source
                           };
            _instances.Add(context, instance);

            // Raise started event
            var position = context.ChannelingInfo?.GetNewTarget()?.Position;
            if (position != null)
                ray.RayStarted(targets.Source.Transform.position, position.Value);
        }
    }

    public void OnTargetsFinalized(SpellContext context, SpellTargets castData) { }
    public void OnTargetsAffected(ISpellContext context, SpellTargets targets)  { }

    public void OnEndSubSpell(ISpellContext context)
    {
        if (_instances.TryGetValue(context, out var instance))
        {
            instance.Ray.RayEnded();

            if (AutoDestroyAfterSpell)
                Destroy(instance.InstanceObject);

            _instances.Remove(context);
        }
    }
}
}
