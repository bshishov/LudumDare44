using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells
{
    public enum ContextState
    {
        JustQueued = 0,
        PreDelays,
        FindTargets,
        PreDamageDelay,
        Fire,
        PostDelay,
        Finishing,
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Character != null)
            {
                sb.Append($"Character = {Character}\r\n");
            }
            if (Transform != null)
            {
                sb.Append($"Transform = {Transform}\r\n");
            }
            if (Position.HasValue == true)
            {
                sb.Append($"Position = {Position.Value}\r\n");
            }

            return sb.ToString();
        }
    }

    public class SpellTargets
    {
        public List<Vector3> Directions = new List<Vector3>();

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

        public override string ToString()
        {
            string dst = "";
            if(Destinations != null && Destinations.Length > 0)
                dst = string.Join("; ", Destinations.Select(d => d.ToString()));
            return $"Source = {Source}, Destinations = {dst}";
        }
    }

    public class SubSpellTargets
    {
        public List<SpellTargets> targetData;
    }

    public interface ISpellEffect
    {
        void OnSpellStateChange(Spell spell, ContextState newState);
        void OnSubSpellStateChange(Spell spell, SubSpell subspell, ContextState newSubState);
        void OnSubSpellStartCast(Spell spell, SubSpell subspell, SubSpellTargets data);
    }
}