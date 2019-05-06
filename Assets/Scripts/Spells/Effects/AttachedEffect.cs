using System.Collections.Generic;
using UnityEngine;

namespace Spells.Effects
{
    public class AttachedEffect : MonoBehaviour, ISubSpellEffect
    {
        public GameObject Object;
        public bool       StartEffectOnPreSelected;
        public float LifeTime = 2f;
        public CharacterState.NodeRole Node = CharacterState.NodeRole.Chest;

        public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
        {
            if (StartEffectOnPreSelected)
                SpawnEffect(targets);
        }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
        {
            if (!StartEffectOnPreSelected)
                SpawnEffect(targets);
        }

        public void OnEndSubSpell(SpellContext context) {  }


        private void SpawnEffect(SpellTargets targets)
        {
            // TODO : set rotation towards target or pass target transform to IAttachable
            var attachTo = targets.Source.Character.GetNodeTransform(Node);

            var origin = attachTo.transform.position;
            var rotation = Quaternion.identity;

            if (targets.Destinations.Length > 0)
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