namespace Spells
{
    public interface ISubSpellHandler
    {
        ISpellHandler SpellHandler { get; }
        Spell Spell { get; }
        SubSpell SubSpell { get; }
        Target Source { get; }
        Target Target { get; }
        bool IsActive { get; }
        SpellState State { get; }
        int Stacks { get; }
        void Abort();
        Target ResolveOrigin(Query.QueryOrigin originType);
    }
}