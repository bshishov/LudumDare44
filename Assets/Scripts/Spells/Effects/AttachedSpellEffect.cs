using System;
using System.Collections.Generic;
using System.Linq;
using Actors;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells.Effects
{
    public class AttachedSpellEffect: MonoBehaviour, ISubSpellEffect
    {
        private struct SubSpellEffectInstance
        {
            public GameObject InstanceObject;
            public Transform AttachedTo;
            public Transform Relative;
            public ISpellContext Context;
        }

        [Serializable]
        public enum EffectSpawnOrigin
        {
            Source,
            Target
        }

        public enum RotationControls
        {
            NoControl,
            SameAsObjectAttachedTo,
            LookTowardsRelative,
            LookAwayFromRelative,
        }

        public GameObject Prefab;
        public EffectSpawnEvent SpawnEvent = EffectSpawnEvent.OnTargetsFinalized;
        public EffectSpawnOrigin SpawnOrigin;
        public EffectInstancingMode InstancingMode;

        [Header("Positioning")]
        public CharacterState.NodeRole PreferredNode = CharacterState.NodeRole.Chest;
        public bool ScaleWithAoE = true;
        public RotationControls RotationMode;
        public bool OnlyXzRotation = true;

        [Header("Lifecycle")]
        public bool DestroyAfterSpell = false;
        public bool DestroyAfterLifetime = false;
        public float Lifetime = 2f;

        private readonly List<SubSpellEffectInstance> _instances = new List<SubSpellEffectInstance>(1);

        private void Update()
        {
            foreach (var instance in _instances)
            {
                // Skip destroyed
                if(instance.InstanceObject == null)
                    continue;

                CalculateTransform(instance.AttachedTo, instance.Relative, out var position, out var rotation);
                instance.InstanceObject.transform.position = position;
                instance.InstanceObject.transform.rotation = rotation;
            }
        }

        public void OnInputTargetsValidated(ISpellContext context, SpellTargets targets)
        {
            if (SpawnEvent == EffectSpawnEvent.OnInputTargetsValidated)
                HandleEvent(context, targets);
        }

        public void OnTargetsFinalized(SpellContext context, SpellTargets targets)
        {
            if (SpawnEvent == EffectSpawnEvent.OnTargetsFinalized)
                HandleEvent(context, targets);
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {
            if (SpawnEvent == EffectSpawnEvent.OnTargetsAffected)
                HandleEvent(context, targets);
        }

        private void HandleEvent(ISpellContext context, SpellTargets targets)
        {
            if (InstancingMode == EffectInstancingMode.OnePerSubSpell)
            {
                var existingInstanceIdx = _instances.FindIndex(i => i.Context.Equals(context));
                if (existingInstanceIdx >= 0)
                {
                    // Skip existing ???
                }
                else
                {
                    var target = targets.Destinations.FirstOrDefault();
                    _instances.Add(Create(context, targets.Source, target));
                }
            }
            else if (InstancingMode == EffectInstancingMode.OnePerEventTarget)
            {
                foreach (var target in targets.Destinations)
                    _instances.Add(Create(context, targets.Source, target));
            }
        }

        public GameObject SpawnInstance(Vector3 position, Quaternion rotation, AreaOfEffect aoe)
        {
            var go = Instantiate(Prefab, position, rotation, transform);
            if (DestroyAfterLifetime)
                Destroy(go, Lifetime);
            if (ScaleWithAoE && aoe != null)
                go.transform.localScale *= aoe.Size;
            return go;
        }

        public void OnEndSubSpell(ISpellContext context)
        {
            // If SubSpell has ended - remove and destroy all instances that are assigned to that SubSpell
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                var instance = _instances[i];
                // If instance was destroyed
                if (instance.InstanceObject == null)
                {
                    _instances.RemoveAt(i);
                    continue;
                }

                if (!instance.Context.Equals(context))
                    continue;

                if (DestroyAfterSpell)
                    Destroy(instance.InstanceObject);

                _instances.RemoveAt(i);
            }
        }

        private SubSpellEffectInstance Create(ISpellContext context, TargetInfo source, TargetInfo target)
        {
            Transform attachTo;
            Transform relativeTo;
            switch (SpawnOrigin)
            {
                case EffectSpawnOrigin.Target:
                    attachTo = GetTransform(target);
                    relativeTo = GetTransform(source);
                    break;
                default:
                case EffectSpawnOrigin.Source:
                    attachTo = GetTransform(source);
                    relativeTo = GetTransform(target);
                    break;
            }

            CalculateTransform(attachTo, relativeTo, out var position, out var rotation);
            return new SubSpellEffectInstance
            {
                AttachedTo = attachTo,
                Relative = relativeTo,
                Context = context,
                InstanceObject = SpawnInstance(position, rotation, context.CurrentSubSpell?.Area)
            };
        }

        private Transform GetTransform(TargetInfo target)
        {
            if (target == null)
                return null;

            if (target.Character != null)
                return target.Character.GetNodeTransform(PreferredNode);

            if (target.Transform != null)
                return target.Transform;

            return null;
        }

        private void CalculateTransform(
            Transform attachedTo, 
            Transform relative, 
            out Vector3 position, 
            out Quaternion rotation)
        {
            position = attachedTo.position;

            var direction = Vector3.forward;
            if (relative != null)
            {
                direction = relative.position - attachedTo.position;
                if (OnlyXzRotation)
                    direction.y = 0;
                direction = direction.normalized;
            }

            switch (RotationMode)
            {
                case RotationControls.LookAwayFromRelative:
                    rotation = Quaternion.LookRotation(-direction, Vector3.up);
                    break;
                case RotationControls.LookTowardsRelative:
                    rotation = Quaternion.LookRotation(direction, Vector3.up);
                    break;
                case RotationControls.SameAsObjectAttachedTo:
                    rotation = attachedTo.rotation;
                    break;
                default:
                case RotationControls.NoControl:
                    rotation = Quaternion.identity;
                    break;
            }
        }
    }
}
