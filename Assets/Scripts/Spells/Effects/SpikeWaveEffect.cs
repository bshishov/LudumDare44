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
                var orient = Quaternion.LookRotation(target.Directions[0]);
                orient.x = orient.z = 0;

                Destroy(Instantiate(WavePrefab, target.Source.Position.Value, orient), 2);
                return;
            }
        }
    }
}
