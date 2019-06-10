using System;
using Attributes;
using Data;

namespace Spells
{
    [Serializable]
    public struct Query
    {
        public enum QueryType
        {
            None,
            OriginAsTarget,
            AllTargetsInAoe,
            ClosestToOriginInAoe,
            RandomTargetInAoe,
            RandomLocationInAoe,
            FillAoE
        }

        public enum QueryTeam
        {
            Self,
            Ally,
            Enemy,
            Everyone,
            EveryoneExceptSelf
        }

        public QueryType NewTargetsQueryType;
        public TargetResolution Origin;

        [Expandable]
        public AreaOfEffect Area;
        public QueryTeam AffectsTeam;
        
        public bool ExcludeAlreadyAffected;
    }
}