using System.Collections.Generic;
using Actors;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells
{
    public interface ISubSpellContext
    {

    }

    public interface ISpellContext
    {
        Spell Spell { get; }
        IChannelingInfo ChannelingInfo { get;}
        CharacterState InitialSource { get; }

        bool Aborted { get; }

        int Stacks { get; }
        float StartTime { get; }
        float ActiveTime { get; }
        float StateActiveTime { get; }

        ContextState State { get; }

        //ISubSpellContext SubContext { get; }

        bool IsLastSubSpell { get; }

        SubSpell CurrentSubSpell { get; }
    }
}
