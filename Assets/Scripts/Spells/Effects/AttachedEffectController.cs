using System.Collections.Generic;
using UnityEngine;

namespace Spells.Effects
{
    public class AttachedEffectController : MonoBehaviour, ISpellEffect
    {
        public GameObject Prefab;
        public CharacterState.NodeRole Node = CharacterState.NodeRole.Chest;
        private readonly Dictionary<ISpellContext, GameObject> _instances 
            = new Dictionary<ISpellContext, GameObject>(1);

        public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
        {
            if (_instances.ContainsKey(context))
                return;

            _instances.Add(context, CreateInstance(targets));
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {

        }

        public void OnStateChange(ISpellContext context, ContextState oldState)
        {
            if (context.State == ContextState.Finishing)
            {
                if (_instances.TryGetValue(context, out var instance))
                {
                    Destroy(instance.gameObject);
                    _instances.Remove(context);
                }
            }
        }

        GameObject CreateInstance(SpellTargets targets)
        {
            var attachTo = targets.Source.Character.GetNodeTransform(Node);
            var instance = Instantiate(Prefab, attachTo.transform.position, Quaternion.identity);
            instance.transform.SetParent(transform);

            var attachable = instance.GetComponent<IAttachable>();
            attachable?.Attach(attachTo);

            return instance;
        }
    }
}
