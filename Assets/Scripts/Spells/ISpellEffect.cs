using Assets.Scripts.Data;
using UnityEngine;

public enum ContextState
{
    JistQueued = 0,
    PreDelays,
    Executing,
    AnimmationDelay,
    ApplayBuffs,
    PostDelay,
    Finishing,
}

public class SpellTargets
{
    public Vector3 origin;
    public Vector3 direction;

    public CharacterState source;
    public CharacterState[] destinations;
}

public interface ISpellEffect
{
    void OnSpellStateChange(Spell spell, ContextState newState);
    void OnSubSpellStateChange(Spell spell, SubSpell subspell, ContextState newSubState);

    void OnSubSpellStartCast(Spell spell, SubSpell subspell, SpellTargets data);
}