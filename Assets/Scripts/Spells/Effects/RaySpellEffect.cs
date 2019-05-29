using System.Collections.Generic;
using Actors;
using UnityEngine;

namespace Spells.Effects
{
    public class RaySpellEffect : MonoBehaviour, ISpellEffectHandler
    {
        private struct SubSpellEffectInstance
        {
            public GameObject InstanceObject;
            public IRayEffect Ray;
            public Target Source;
            public Target Target;
            public ISubSpellHandler Handler;
        }

        public GameObject RayPrefab;
        public SubSpellEvent SpawnEvent = SubSpellEvent.OnFire;
        public EffectInstancingMode InstanceMode;
        public bool AutoDestroyAfterSpell = true;
        public CharacterState.NodeRole PreferredSourceNode = CharacterState.NodeRole.SpellEmitter;
        public CharacterState.NodeRole PreferredTargetNode = CharacterState.NodeRole.Chest;

        private readonly List<SubSpellEffectInstance> _instances = new List<SubSpellEffectInstance>(1);
        
        void Update()
        {
            foreach (var instance in _instances)
            {
                GetRayStartAndEnd(instance, out var src, out var dst);
                instance.Ray?.RayUpdated(src, dst);
            }
        }
        public void OnEvent(SubSpellEventArgs args)
        {
            if (args.Event == SubSpellEvent.Ended)
            {
                OnEndSubSpell(args.Handler);
                return;
            }

            if(args.Event != SpawnEvent)
                return;

            if (InstanceMode == EffectInstancingMode.OnePerSubSpell)
            {
                // If the mode is one per subspell than we need to find existing one
                // and update its target information. And also raise its UpdateEvent
                var existingInstanceIdx = _instances.FindIndex(i => i.Handler.Equals(args.Handler));
                if (existingInstanceIdx >= 0)
                {
                    var existing = _instances[existingInstanceIdx];
                    var instance = new SubSpellEffectInstance
                    {
                        InstanceObject = existing.InstanceObject,
                        Target = args.Handler.Target,
                        Source = args.Handler.Source,
                        Handler = args.Handler,
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
                            Target = args.Handler.Target,
                            Source = args.Handler.Source,
                            Handler = args.Handler,
                            Ray = ray
                        };
                        _instances.Add(instance);
                        GetRayStartAndEnd(instance, out var src, out var dst);
                        instance.Ray?.RayStarted(src, dst);
                    }
                }
            }
            else if(InstanceMode == EffectInstancingMode.OnePerEventTarget)
            {
                if (args.QueriedTargets != null)
                {
                    foreach (var target in args.QueriedTargets)
                    {
                        CreateRayEffectInstance(out var gameObjectInstance, out var ray);
                        if (ray != null)
                        {
                            var instance = new SubSpellEffectInstance
                            {
                                Ray = ray,
                                InstanceObject = gameObjectInstance,
                                Source = args.Handler.Source,
                                Target = target,
                                Handler = args.Handler
                            };
                            _instances.Add(instance);
                            GetRayStartAndEnd(instance, out var src, out var dst);
                            ray.RayStarted(src, dst);
                        }
                    }
                }
            }
        }

        private void CreateRayEffectInstance(out GameObject gameObjectInstance, out IRayEffect rayInstance)
        {
            gameObjectInstance = Instantiate(RayPrefab, transform, true);
            rayInstance = gameObjectInstance.GetComponent<IRayEffect>();
        }

        public void OnEndSubSpell(ISubSpellHandler handler)
        {
            // If SubSpell has ended - remove and destroy all instances that are assigned to that SubSpell
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                var instance = _instances[i];
                if(!instance.Handler.Equals(handler))
                    continue;

                instance.Ray?.RayEnded();

                if (AutoDestroyAfterSpell && instance.InstanceObject != null)
                    Destroy(instance.InstanceObject);

                _instances.RemoveAt(i);
            }
        }

        private Vector3 GetPosition(Target target, CharacterState.NodeRole preferredRole = CharacterState.NodeRole.Default)
        {
            // Character target overrider
            if (target.Type == TargetType.Character)
                return target.Character.GetNodeTransform(preferredRole).position;

            return target.Position;
        }

        private void GetRayStartAndEnd(SubSpellEffectInstance instance, out Vector3 source, out Vector3 destination)
        {
            source = GetPosition(instance.Source, PreferredSourceNode);
            destination = GetPosition(instance.Target, PreferredTargetNode);
        }
    }
}
