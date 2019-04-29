using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells
{
    public enum ContextState
    {
        JustQueued = 0,
        PreDelays,
        Executing,
        PostDelay,
        Finishing,

        Projectile,
    }

    public class PerSourceTargets
    {
        public CharacterState source;
        public CharacterState[] destinations;
    }

    public class SubSpellTargets
    {
        public Vector3 origin;
        public Vector3 direction;

        public List<PerSourceTargets> targetData;
    }

    public interface ISpellEffect
    {
        void OnSpellStateChange(Spell spell, ContextState newState);
        void OnSubSpellStateChange(Spell spell, SubSpell subspell, ContextState newSubState);
        void OnSubSpellStartCast(Spell spell, SubSpell subspell, SubSpellTargets data);
    }
}