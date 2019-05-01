using Assets.Scripts.Data;
using UnityEngine;

namespace Spells.Effects
{
    public class FireballSpellEffect : MonoBehaviour, ISpellEffect
    {
        public GameObject ExplosionPrefab;


        public void OnSpellStateChange(Spell spell, ContextState newState)
        {
        }

        public void OnSubSpellStateChange(Spell spell, SubSpell subSpell, ContextState newSubState)
        {
        }

        public void OnSubSpellStartCast(Spell spell, SubSpell subSpell, SubSpellTargets data)
        {
            foreach (var target in data.targetData)
            {
                Destroy(Instantiate(ExplosionPrefab, target.Source.Position.Value, Quaternion.identity), 2);
            }
        }
    }
}
