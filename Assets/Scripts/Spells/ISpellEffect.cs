using Assets.Scripts.Data;


public class SpellTargets
{
    public CharacterState source;
    public CharacterState[] destinations;
}

public interface ISpellEffect 
{
    void OnSubSpellStartCast(Spell spell, SubSpell subspell, SpellTargets data);
}