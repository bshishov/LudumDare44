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
            AsOrigin,
            AllTargetsInAoe,
            ClosestToOriginInAoe,
            RandomTargetInAoe,
            RandomLocationInAoe
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