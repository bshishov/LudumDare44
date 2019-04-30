using Assets.Scripts.Data;
using UnityEngine;

namespace Spells.Effects
{
    public class SpikeWaveEffect : MonoBehaviour, ISpellEffect
    {
        public GameObject WavePrefab;

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
                Destroy(Instantiate(WavePrefab, target.Source.Position.Value, target.Source.Transform.rotation), 2);
                return;
            }
        }
    }
}
