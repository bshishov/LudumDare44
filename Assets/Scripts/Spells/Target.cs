using System;
using System.Text;
using Actors;
using UnityEngine;

namespace Spells
{
    public struct Target
    {
        public readonly TargetType Type;
        public readonly CharacterState Character;
        public readonly Transform Transform;
        private readonly Vector3 _location;
        private readonly Vector3 _forward;
        private readonly ITargetLocationProvider _locationProvider;
        private static readonly Vector3 Offset = new Vector3(0, 1f, 0);

        public static Target None = new Target();

        public Target(Vector3 location, Vector3 forward)
        {
            Type = TargetType.Location;
            Character = null;
            Transform = null;
            _location = location;
            _forward = forward;
            _locationProvider = null;
        }
        
        public Target(Vector3 location) : this(location, Vector3.forward)
        {
        }
        
        public Target(CharacterState character)
        {
            Type = TargetType.Character;
            Character = character;
            Transform = character.GetNodeTransform(CharacterState.NodeRole.Root);
            _location = default;
            _forward = default;
            _locationProvider = null;
        }

        public Target(Transform transform)
        {
            Type = TargetType.Transform;
            Character = null;
            Transform = transform;
            _location = default;
            _forward = default;
            _locationProvider = null;
        }

        public Target(ITargetLocationProvider provider)
        {
            Type = TargetType.LocationProvider;
            Character = null;
            Transform = null;
            _location = default;
            _forward = default;
            _locationProvider = provider;
        }

        public Vector3 Position
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Location:
                        return _location;
                    case TargetType.Transform:
                    case TargetType.Character:
                        return Transform.position + Offset;
                    case TargetType.LocationProvider:
                        return _locationProvider.GetTargetLocation();
                    default:
                    case TargetType.None:
                        throw new InvalidOperationException($"Target of type {Type} does not have position");
                }
            }
        }

        public Vector3 Forward
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Location:
                    case TargetType.LocationProvider:
                        return _forward;
                    case TargetType.Transform:
                    case TargetType.Character:
                        return Transform.forward;
                    default:
                    case TargetType.None:
                        throw new InvalidOperationException($"Invalid target. Type: {Type}");
                }
            }
        }

        public bool IsValid
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Location:
                        return true;
                    case TargetType.Character:
                        return Character != null && Transform != null;
                    case TargetType.Transform:
                        return Transform != null;
                    case TargetType.LocationProvider:
                        return _locationProvider != null;
                    case TargetType.None:
                        // NOTE! None is a valid target
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool HasPosition
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Location:
                        return true;
                    case TargetType.Character:
                        return Character != null && Transform != null;
                    case TargetType.Transform:
                        return Transform != null;
                    case TargetType.LocationProvider:
                        return _locationProvider != null;
                    case TargetType.None:
                        // NOTE: None target can't have a position
                        return false;
                    default:
                        return false;
                }
            }
        }

        public Target ToLocationTarget()
        {
            return new Target(this.Position, this.Forward);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[Target: type={Type}");
            if (Character != null) sb.Append($" character=<b>{Character}</b>");
            if (Transform != null) sb.Append($" transform={Transform}");
            if (_locationProvider != null) sb.Append($" provider={_locationProvider}");
            sb.Append($" position={Position}");
            sb.Append("]");
            return sb.ToString();
        }
    }
}