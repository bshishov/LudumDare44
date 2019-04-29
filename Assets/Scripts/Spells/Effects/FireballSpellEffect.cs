using Assets.Scripts.Data;
using UnityEngine;

namespace Spells.Effects
{
    public class FireballSpellEffect : MonoBehaviour, ISpellEffect
    {
        public GameObject LightningPrefab;

        void Start()
        {
        }

        public void OnSpellStateChange(Spell spell, ContextState newState)
        {
            Debug.Log("Spell state change");
        }

        public void OnSubSpellStateChange(Spell spell, SubSpell subSpell, ContextState newSubState)
        {
            Debug.Log("SubSpell state change");
        }

        public void OnSubSpellStartCast(Spell spell, SubSpell subSpell, SubSpellTargets data)
        {
            Debug.Log("SubSpell Start Cast");
        }
    }
}
