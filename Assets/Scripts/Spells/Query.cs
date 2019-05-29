using System;
using Attributes;
using Data;

namespace Spells
{
    [Serializable]
    public struct Query
    {
        public enum QueryOrigin
        {
            CurrentSource,
            CurrentTarget,
            OriginalSpellSource,
            OriginalSpellTarget
        }

        public enum QueryType
        {
            None,
            CurrentSource,
            CurrentTarget,
            OriginalSpellSource,
            OriginalSpellTarget,
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
        public QueryOrigin Origin;

        [Expandable]
        public AreaOfEffect Area;
        public QueryTeam AffectsTeam;
    }
}