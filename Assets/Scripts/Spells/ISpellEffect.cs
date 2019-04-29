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

    public class TargetInfo
    {
        public CharacterState Character;
        public Transform Transform;
        public Vector3? Position;

        public static TargetInfo Create(CharacterState character, Transform transform, Vector3 position)
            => new TargetInfo
            {
                Character = character,
                Transform = transform,
                Position = position
            };

        public static TargetInfo Create(CharacterState character, Transform transform)
            => Create(character, transform, transform.position);

        public static TargetInfo Create(CharacterState character)
            => Create(character, character.GetNodeTransform());
    }

    public class SpellTargets
    {
        public TargetInfo Source;
        public TargetInfo[] Destinations;

        public SpellTargets(TargetInfo source)
        {
            Source = source;
            Destinations = new TargetInfo[0];
        }

        public  SpellTargets(TargetInfo source, TargetInfo target)
        {
            Source = source;
            Destinations = new []{ target };
        }
    }

    public class SubSpellTargets
    {
        public Vector3 origin;
        public Vector3 direction;

        public List<SpellTargets> targetData;
    }

    public interface ISpellEffect
    {
        void OnSpellStateChange(Spell spell, ContextState newState);
        void OnSubSpellStateChange(Spell spell, SubSpell subspell, ContextState newSubState);
        void OnSubSpellStartCast(Spell spell, SubSpell subspell, SubSpellTargets data);
    }
}