using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        Finishing
    }

    [DebuggerStepThrough]
    public class TargetInfo
    {
        public CharacterState Character;
        public Vector3? Position;
        public Transform Transform;

        public TargetInfo()
        {
            Character = null;
            Transform = null;
            Position = null;
        }

        public TargetInfo(TargetInfo source)
        {
            Character = source.Character;
            Transform = source.Transform;
            Position = source.Position;
        }

        public static TargetInfo Create(CharacterState character, Transform transform, Vector3 position)
        {
            return new TargetInfo
            {
                Character = character,
                Transform = transform,
                Position = position
            };
        }

        public static TargetInfo Create(CharacterState character, Transform transform)
        {
            return Create(character, transform, transform.position);
        }

        public static TargetInfo Create(CharacterState character)
        {
            return Create(character, character.GetNodeTransform());
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Character != null) sb.Append($"Character: <b>{Character}</b>\r\n");

            if (Transform != null) sb.Append($"Transform: {Transform}\r\n");

            if (Position.HasValue) sb.Append($"Position: {Position.Value}\r\n");

            return sb.ToString();
        }
    }

    [DebuggerStepThrough]
    public class SpellTargets
    {
        public TargetInfo[] Destinations;
        public TargetInfo Source;

        public SpellTargets(SpellTargets source)
        {
            Source = new TargetInfo(source.Source);
            Destinations = source.Destinations;
        }

        public SpellTargets(TargetInfo source)
        {
            Source = source;
            Destinations = new TargetInfo[0];
        }

        public SpellTargets(TargetInfo source, TargetInfo target)
        {
            Source = source;
            Destinations = new[] {target};
        }

        public SpellTargets(TargetInfo source, TargetInfo[] targets)
        {
            Source = source;
            Destinations = targets;
        }

        public override string ToString()
        {
            var dst = "";
            if (Destinations != null && Destinations.Length > 0)
                dst = string.Join("; ", Destinations.Select(d => d.ToString()));
            return $"Source = {Source}, Destinations = {dst}";
        }
    }

    [DebuggerStepThrough]
    public struct SubSpellTargets
    {
        public List<SpellTargets> TargetData;
    }


    public interface ISubSpellEffect
    {
        void OnTargetsPreSelected(ISpellContext context, SpellTargets targets);
        void OnTargetsAffected(ISpellContext context, SpellTargets targets);
        void OnEndSubSpell(ISpellContext context);
    }

    public interface ISpellEffect : ISubSpellEffect
    {
        void OnStateChange(ISpellContext context, ContextState oldState);
    }
}