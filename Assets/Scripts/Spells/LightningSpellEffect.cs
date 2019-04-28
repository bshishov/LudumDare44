using Assets.Scripts.Data;
using UnityEngine;

namespace Assets.Scripts.Spells
{
    public class LightningSpellEffect : MonoBehaviour, ISpellEffect
    {
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
