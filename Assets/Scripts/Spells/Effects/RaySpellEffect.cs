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
            public TargetInfo Target;
            public bool UseChannelingInfoAsTarget;
            public ISpellContext Context;
        }

        public GameObject RayPrefab;
        public bool AutoDestroyAfterSpell = true;
        public bool UseChannelingInfoAsTarget = false;

        private readonly List<SubSpellEffectInstance> _instances = new List<SubSpellEffectInstance>(1);

        void Start()
        {
        }
        
        void Update()
        {
            foreach (var instance in _instances)
            {
                GetRayStartAndEnd(instance, out var src, out var dst);
                instance.Ray?.RayUpdated(src, dst);
            }
        }

        public void OnInputTargetsValidated(ISpellContext context, SpellTargets targets)
        {
            foreach (var spellTarget in targets.Destinations)
            {
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
                        Ray = ray,
                        InstanceObject = go,
                        Source = targets.Source,
                        Target = spellTarget,
                        Context = context,
                        UseChannelingInfoAsTarget = UseChannelingInfoAsTarget
                    };
                    _instances.Add(instance);
                    
                    GetRayStartAndEnd(instance, out var src, out var dst);
                    ray.RayStarted(src, dst);
                }
            }
        }

        public void OnTargetsFinalized(SpellContext context, SpellTargets castData) { }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {
        }

        public void OnEndSubSpell(ISpellContext context)
        {
            // If SubSpell has ended - remove and destroy all instances that are assigned to that SubSpell
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                var instance = _instances[i];
                if(!instance.Context.Equals(context))
                    continue;

                instance.Ray?.RayEnded();

                if (AutoDestroyAfterSpell && instance.InstanceObject != null)
                    Destroy(instance.InstanceObject);

                _instances.RemoveAt(i);
            }
        }

        private Vector3 GetPosition(TargetInfo tgtInfo)
        {
            if (tgtInfo.Transform != null)
                return tgtInfo.Transform.position;

            if (tgtInfo.Position.HasValue)
                return tgtInfo.Position.Value;

            if (tgtInfo.Character != null)
                return tgtInfo.Character.GetNodeTransform().position;

            return Vector3.zero;
        }

        private void GetRayStartAndEnd(SubSpellEffectInstance instance, out Vector3 source, out Vector3 destination)
        {
            source = GetPosition(instance.Source);
            if (instance.UseChannelingInfoAsTarget)
            {
                destination = GetPosition(instance.Context.ChannelingInfo.GetNewTarget());
            }
            else
            {
                destination = GetPosition(instance.Target);
            }
        }
    }
}
