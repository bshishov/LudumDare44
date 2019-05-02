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
        public readonly TargetInfo Source;
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
        public List<SpellTargets> TargetData;
    }

    public interface ISpellEffect
    {
        void OnStateChange(ISpellContext context, ContextState oldState);
    }

    public interface ISubSpellEffect
    {
        void OnTargetsPreSelected(ISpellContext context, SpellTargets targets);

        void OnTargetsAffected(ISpellContext context, SpellTargets targets);
    }
}