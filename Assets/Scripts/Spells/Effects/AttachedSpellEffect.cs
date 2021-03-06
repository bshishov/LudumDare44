﻿using System;
using System.Collections.Generic;
using Actors;
using Data;
using UnityEngine;

namespace Spells.Effects
{
    public class AttachedSpellEffect: MonoBehaviour, ISpellEffectHandler
    {
        private struct SubSpellEffectInstance
        {
            public GameObject InstanceObject;
            public Transform AttachedTo;
            public Target Relative;
            public ISubSpellHandler Handler;
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
        public SubSpellEvent SpawnEvent = SubSpellEvent.OnFire;
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

        public GameObject SpawnInstance(Vector3 position, Quaternion rotation, AreaOfEffect aoe, int stacks)
        {
            var go = Instantiate(Prefab, position, rotation, transform);
            if (DestroyAfterLifetime)
                Destroy(go, Lifetime);
            if (ScaleWithAoE && aoe != null)
                go.transform.localScale *= aoe.Size.GetValue(stacks);
            return go;
        }

        public void OnEndSubSpell(ISubSpellHandler handler)
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

                if (!instance.Handler.Equals(handler))
                    continue;

                if (DestroyAfterSpell)
                    Destroy(instance.InstanceObject);

                _instances.RemoveAt(i);
            }
        }

        private SubSpellEffectInstance Create(ISubSpellHandler handler, Target source, Target target)
        {
            Transform attachTo;
            Target relativeTo;
            switch (SpawnOrigin)
            {
                case EffectSpawnOrigin.Target:
                    attachTo = GetTransform(target);
                    relativeTo = source;
                    break;
                default:
                case EffectSpawnOrigin.Source:
                    attachTo = GetTransform(source);
                    relativeTo = target;
                    break;
            }

            CalculateTransform(attachTo, relativeTo, out var position, out var rotation);
            return new SubSpellEffectInstance
            {
                AttachedTo = attachTo,
                Relative = relativeTo,
                Handler = handler,
                InstanceObject = SpawnInstance(position, rotation, null, handler.Stacks)
            };
        }

        private Transform GetTransform(Target target)
        {
            if (target.Type == TargetType.Character)
                return target.Character.GetNodeTransform(PreferredNode);

            return target.Transform;
        }

        private void CalculateTransform(
            Transform attachedTo, 
            Target relative, 
            out Vector3 position, 
            out Quaternion rotation)
        {
            position = attachedTo.position;

            var direction = Vector3.forward;
            if (relative.HasPosition)
            {
                direction = relative.Position - attachedTo.position;
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

        public void OnEvent(SubSpellEventArgs args)
        {
            if (args.Event == SubSpellEvent.Ended)
            {
                OnEndSubSpell(args.Handler);
                return;
            }

            if (args.Event == SpawnEvent)
            {
                if (InstancingMode == EffectInstancingMode.OnePerSubSpell)
                {
                    var existingInstanceIdx = _instances.FindIndex(i => i.Handler.Equals(args.Handler));
                    if (existingInstanceIdx >= 0)
                    {
                        // Skip existing ???
                    }
                    else
                    {
                        _instances.Add(Create(args.Handler, args.Handler.Source, args.Handler.Target));
                    }
                }
                else if (InstancingMode == EffectInstancingMode.OnePerEventTarget)
                {
                    foreach (var target in args.QueriedTargets)
                        _instances.Add(Create(args.Handler, args.Handler.Source, target));
                }
            }
        }
    }
}
