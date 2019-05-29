using System.Collections.Generic;

namespace Spells
{
    public interface ISpellEffectHandler
    {
        void OnEvent(SubSpellEventArgs args);
    }

    public struct SubSpellEventArgs
    {
        public readonly ISubSpellHandler Handler;
        public readonly SubSpellEvent Event;
        public readonly Query Query;
        public readonly IEnumerable<Target> QueriedTargets;

        public SubSpellEventArgs(ISubSpellHandler handler, SubSpellEvent e, Query q, IEnumerable<Target> targets)
        {
            Handler = handler;
            Event = e;
            Query = q;
            QueriedTargets = targets;
        }

        public SubSpellEventArgs(ISubSpellHandler handler, SubSpellEvent e)
            : this(handler, e, default, default)
        {
        }
    }
}