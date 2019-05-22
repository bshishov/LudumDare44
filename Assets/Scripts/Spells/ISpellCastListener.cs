using Assets.Scripts.Data;
using Data;

namespace Spells
{
    public interface ISpellCastListener
    {
        void OnAbortedFiring(Spell spell);
        void OnStartFiring(Spell    spell, SubSpell subspell);
        void OnEndFiring(Spell      spell);
        void OnEndCasting(Spell    spell);
    }
}