using System;
using System.Collections.Generic;
using System.Linq;
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

        [Serializable]
        public enum SpellEvent
        {
            OnInputTargetsValidated,
            OnTargetsFinalized,
            OnTargetsAffected
        }

        [Serializable]
        public enum InstancingMode
        {
            OnePerEventTarget,
            OnePerSubSpell
        }

        public GameObject RayPrefab;
        public SpellEvent SpawnEvent = SpellEvent.OnTargetsFinalized;
        public InstancingMode InstanceMode;
        public bool AutoDestroyAfterSpell = true;
        public bool UseChannelingInfoAsTarget = false;
        public CharacterState.NodeRole PreferredSourceNode = CharacterState.NodeRole.SpellEmitter;
        public CharacterState.NodeRole PreferredTargetNode = CharacterState.NodeRole.Chest;

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
            if (SpawnEvent == SpellEvent.OnInputTargetsValidated)
                HandleEvent(context, targets);
        }

        public void OnTargetsFinalized(SpellContext context, SpellTargets targets)
        {
            if (SpawnEvent == SpellEvent.OnTargetsFinalized)
                HandleEvent(context, targets);
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {
            if(SpawnEvent == SpellEvent.OnTargetsAffected)
                HandleEvent(context, targets);
        }

        private void HandleEvent(ISpellContext context, SpellTargets targets)
        {
            if (InstanceMode == InstancingMode.OnePerSubSpell)
            {
                // If the mode is one per subspell than we need to find existing one
                // and update its target information. And also raise its UpdateEvent
                var existingInstanceIdx = _instances.FindIndex(i => i.Context.Equals(context));
                if (existingInstanceIdx >= 0)
                {
                    var existing = _instances[existingInstanceIdx];
                    var instance = new SubSpellEffectInstance
                    {
                        InstanceObject = existing.InstanceObject,
                        Target = targets.Destinations[0],
                        Source = targets.Source,
                        UseChannelingInfoAsTarget = existing.UseChannelingInfoAsTarget,
                        Context = context,
                        Ray = existing.Ray
                    };
                    _instances[existingInstanceIdx] = instance;
                    
                    GetRayStartAndEnd(instance, out var src, out var dst);
                    instance.Ray?.RayUpdated(src, dst);
                }
                else
                {
                    // If no existing instance is found, create new
                    CreateRayEffectInstance(out var gameObjectInstance, out var ray);
                    if (ray != null)
                    {
                        var instance = new SubSpellEffectInstance
                        {
                            InstanceObject = gameObjectInstance,
                            Target = targets.Destinations[0],
                            Source = targets.Source,
                            UseChannelingInfoAsTarget = UseChannelingInfoAsTarget,
                            Context = context,
                            Ray = ray
                        };
                        _instances.Add(instance);
                        GetRayStartAndEnd(instance, out var src, out var dst);
                        instance.Ray?.RayStarted(src, dst);
                    }
                }
            }
            else if(InstanceMode == InstancingMode.OnePerEventTarget)
            {
                foreach (var spellTarget in targets.Destinations)
                {
                    CreateRayEffectInstance(out var gameObjectInstance, out var ray);
                    if (ray != null)
                    {
                        var instance = new SubSpellEffectInstance
                        {
                            Ray = ray,
                            InstanceObject = gameObjectInstance,
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
        }

        private void CreateRayEffectInstance(out GameObject gameObjectInstance, out IRayEffect rayInstance)
        {
            gameObjectInstance = Instantiate(RayPrefab, transform, true);
            rayInstance = gameObjectInstance.GetComponent<IRayEffect>();
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

        private Vector3 GetPosition(TargetInfo tgtInfo, CharacterState.NodeRole preferredRole = CharacterState.NodeRole.Default)
        {
            if (tgtInfo.Transform != null)
                return tgtInfo.Transform.position;

            if (tgtInfo.Character != null)
                return tgtInfo.Character.GetNodeTransform(preferredRole).position;

            if (tgtInfo.Position.HasValue)
                return tgtInfo.Position.Value;

            return Vector3.zero;
        }

        private void GetRayStartAndEnd(SubSpellEffectInstance instance, out Vector3 source, out Vector3 destination)
        {
            source = GetPosition(instance.Source);
            if (instance.UseChannelingInfoAsTarget)
            {
                destination = GetPosition(instance.Context.ChannelingInfo.GetNewTarget(), PreferredSourceNode);
            }
            else
            {
                destination = GetPosition(instance.Target, PreferredTargetNode);
            }
        }
    }
}
