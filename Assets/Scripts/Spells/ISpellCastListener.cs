using Assets.Scripts.Data;

namespace Spells
{
public interface ISpellCastListener
{
    void OnAbortedtFiring(Spell spell);
    void OnStartFiring(Spell    spell);
    void OnEndFiring(Spell      spell);
    void OnEndCastinng(Spell    spell);
}
}