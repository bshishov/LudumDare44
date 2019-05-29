using System;

namespace Spells
{
    public interface ISpellHandler
    {
        Spell Spell { get; }
        int Stacks { get; }
        Target Source { get; }
        Target CastTarget { get; }
        SpellState State { get; }
        bool IsActive { get; }
        void Abort();

        event Action<ISpellHandler, SpellEvent, ISubSpellHandler> Event;
    }
}