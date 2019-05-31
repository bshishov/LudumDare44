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
        SubSpellState State { get; }
        int Stacks { get; }
        void Abort();
        Target ResolveTarget(TargetResolution originType, Target defaultTarget);
    }
}