using Assets.Scripts.Data;

namespace Spells
{
public interface ISpellCastListener
{
    void OnAbortedFiring(Spell spell);
    void OnStartFiring(Spell    spell);
    void OnEndFiring(Spell      spell);
    void OnEndCasting(Spell    spell);
}
}