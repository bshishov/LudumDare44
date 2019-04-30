using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
    public class SubSpellContext
    {
        public bool aborted;

        public float activeTime;

        public object customData;
        public bool projectileSpawned;

        public float startTime;
        public ContextState state;
        public float stateActiveTime;

        public static SubSpellContext Create(SpellContext conext)
        {
            return new SubSpellContext
            {
                startTime = Time.fixedTime,
                activeTime = 0.0f
            };
        }
    }

    public class SpellContext
    {
        public bool aborted;
        public float activeTime;

        public SpellCaster caster;

        public int currentSubspell;

        public ISpellEffect effect;

        public CharacterState[] filteredTargets;

        public float frameTime;
        public CharacterState initialSource;

        public Spell spell;

        private int startSubspellIndex;

        public float startTime;
        public ContextState state;
        public float stateActiveTime;
        public SubSpellContext subContext;

        public List<SubSpellTargets> subSpellTargets;
        public bool IsLastSubSpell => currentSubspell == spell.SubSpells.Length - 1;

        public SubSpell GetCurrentSubSpell()
        {
            return spell.SubSpells[currentSubspell];
        }

        public static SpellContext Create(SpellCaster caster, Spell spell, SpellTargets targets,
            int subSpellStartIndex)
        {
            Debug.Log(targets);

            var context = new SpellContext
            {
                initialSource = targets.Source.Character,
                caster = caster,
                state = subSpellStartIndex == 0 ? ContextState.JustQueued : ContextState.Executing,

                spell = spell,

                startSubspellIndex = subSpellStartIndex,
                currentSubspell = subSpellStartIndex,
                subContext = null,

                startTime = Time.fixedTime,
                stateActiveTime = 0.0f,

                filteredTargets =
                    (spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce
                        ? new CharacterState[] { }
                        : null,

                subSpellTargets = new List<SubSpellTargets>
                {
                    new SubSpellTargets
                    {
                        targetData = new List<SpellTargets> {targets}
                    }
                },
                effect = spell.GetEffect()
            };

            return context;
        }

        public SubSpellTargets GetCurrentSubSpellTargets()
        {
            return subSpellTargets[currentSubspell - startSubspellIndex];
        }
    }

    public struct TargetingData
    {
        public CharacterState owner;
        public Vector3 origin;
        public Vector3? targetLocation;
        public CharacterState targetCharacter;
    }

    public class SpellCaster : MonoBehaviour
    {
        private SpellContext _context;
        private List<SpellContext> _nestedContexts = new List<SpellContext>();

        private CharacterState _owner;
        public float MaxSpellDistance = 100.0f;

        // Start is called before the first frame update
        private void Start()
        {
            _owner = GetComponent<CharacterState>();
        }

        public void CastSpell(Spell spell, SpellTargets targets)
        {
            if (_context != null)
            {
                Debug.LogError($"spell cast aready casting, {_context.spell.Name}");
                return;
            }

            _context = SpellContext.Create(this, spell, targets, 0);
        }

        internal void ContinueCastSpell(Spell spell, SpellTargets targets, int subSpellStartIndex = 0)
        {
            lock (_nestedContexts)
            {
                _nestedContexts.Add(SpellContext.Create(this, spell, targets, subSpellStartIndex));
            }
        }

        private static bool ExecuteContext(SpellContext context)
        {
            context.frameTime = Time.deltaTime;
            context.activeTime += context.frameTime;
            context.stateActiveTime += context.frameTime;

            try
            {
                while (ManageContext(context))
                {
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return true;
            }

            if (context.aborted) Debug.Log($"{context.spell.Name} aborted");

            return context.state == ContextState.Finishing;
        }

        private void Update()
        {
            if (_context != null && ExecuteContext(_context)) _context = null;

            lock (_nestedContexts)
            {
                var newContexts = new List<SpellContext>(_nestedContexts.Count);
                foreach (var context in _nestedContexts)
                    if (!ExecuteContext(context))
                        newContexts.Add(context);

                _nestedContexts = newContexts;
            }
        }

        private static bool ManageContext(SpellContext context)
        {
            switch (context.state)
            {
                case ContextState.JustQueued:
                    Debug.Log($"{context.spell.Name} start spell cast");
                    Advance();
                    return true;

                case ContextState.PreDelays:
                    if (context.stateActiveTime < context.spell.PreCastDelay)
                        break;

                    Advance();
                    return true;

                case ContextState.Executing:
                    context.subContext = SubSpellContext.Create(context);

                    while (ManageSubContext(context, context.subContext)) ;

                    if (context.subContext.aborted)
                        if ((context.spell.Flags & Spell.SpellFlags.BreakOnFailedTargeting)
                            == Spell.SpellFlags.BreakOnFailedTargeting)
                        {
                            context.aborted = true;
                            context.state = ContextState.PostDelay;

                            return true;
                        }

                    context.subContext = null;
                    Advance();
                    return true;

                case ContextState.PostDelay:
                    if (context.stateActiveTime < context.spell.PreCastDelay)
                        break;

                    Advance();
                    return true;

                case ContextState.Finishing:
                    Debug.Log($"{context.spell.Name} finishing");
                    return false;
            }

            return false;

            void Advance()
            {
                ++context.state;
                context.stateActiveTime = 0;
            }
        }

        private static bool ManageSubContext(SpellContext context, SubSpellContext subContext)
        {
            switch (subContext.state)
            {
                case ContextState.JustQueued:
                    Advance();
                    return true;

                case ContextState.PreDelays:
                    if (subContext.activeTime < context.GetCurrentSubSpell().PostCastDelay)
                        break;

                    Advance();
                    return true;

                case ContextState.Executing:
                    if (Execute(context, subContext))
                    {
                        if (!context.subContext.projectileSpawned)
                        {
                            ApplySubSpell(context, subContext);
                            Debug.Log($"{context.spell.Name} Executed subspell {context.currentSubspell}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{context.spell.Name} Failed to execute subspell {context.currentSubspell}");
                        subContext.aborted = true;
                    }

                    Advance();
                    return true;

                case ContextState.PostDelay:
                    if (subContext.activeTime < context.GetCurrentSubSpell().PostCastDelay)
                        break;

                    Advance();
                    return true;

                case ContextState.Finishing:
                    ++context.currentSubspell;
                    subContext.state = ContextState.PreDelays;

                    var casting = context.currentSubspell < context.spell.SubSpells.Length
                                  && subContext.aborted == false
                                  && subContext.projectileSpawned == false;

                    if (casting)
                    {
                        subContext.state = ContextState.PreDelays;
                        subContext.stateActiveTime = 0;
                    }

                    return casting;
            }

            return false;

            void Advance()
            {
                ++subContext.state;
                subContext.stateActiveTime = 0;
            }
        }

        private static void ApplySubSpell(SpellContext context, SubSpellContext subContext)
        {
            var currentTargets = context.GetCurrentSubSpellTargets();
            var newTargets = new SubSpellTargets {targetData = new List<SpellTargets>()};

            if (context.effect != null)
                context.effect.OnSubSpellStartCast(context.spell, context.GetCurrentSubSpell(), currentTargets);

            foreach (var targets in currentTargets.targetData)
            {
                if (targets.Destinations == null)
                    continue;

                foreach (var destination in targets.Destinations)
                {
                    destination.Character.ApplySpell(context.initialSource, context.GetCurrentSubSpell());

                    if (!context.IsLastSubSpell)
                        newTargets.targetData.Add(new SpellTargets(destination));
                }
            }

            context.subSpellTargets.Add(newTargets);
        }

        private static bool Execute(SpellContext context, SubSpellContext subContext)
        {
            var anyTargetFound = false;
            var currentTargets = context.GetCurrentSubSpellTargets();

            var targets = new List<TargetInfo>();

            foreach (var castData in currentTargets.targetData)
            {
                var source = castData.Source;

                if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.SelfTarget) ==
                    SubSpell.SpellFlags.SelfTarget)
                    castData.Destinations = new[] {source};
                else if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.ClosestTarget) ==
                         SubSpell.SpellFlags.ClosestTarget)
                    castData.Destinations = new[]
                    {
                        TargetInfo.Create(context.filteredTargets
                            .OrderBy(t => (t.transform.position - source.Position.Value).magnitude)
                            .FirstOrDefault())
                    };

                foreach (var target in castData.Destinations)
                {
                    if ((context.GetCurrentSubSpell().Targeting & SubSpell.SpellTargeting.Target) ==
                        SubSpell.SpellTargeting.Target)
                    {
                    }

                    if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.Projectile) ==
                        SubSpell.SpellFlags.Projectile)
                    {
                        SpawnProjectile(source, target, context);
                        return true;
                    }

                    var availableTargets = GetFilteredCharacters(
                        context.initialSource,
                        source.Character,
                        context.GetCurrentSubSpell().AffectedTarget);

                    if ((context.spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce)
                        availableTargets = availableTargets.Except(context.filteredTargets).ToArray();

                    if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.Raycast) ==
                        SubSpell.SpellFlags.Raycast)
                    {
                        var dst = GetAllCharacterInArea(context, availableTargets, source, target);
                        if (dst != null && dst.Length > 0)
                            targets.AddRange(dst);
                    }

                    if (source.Position.HasValue && target.Position.HasValue)
                        castData.Directions.Add(target.Position.Value - source.Position.Value);
                }

                if (targets.Count == 0)
                    continue;

                anyTargetFound = true;
                castData.Destinations = targets.ToArray();

                if ((context.spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) ==
                    Spell.SpellFlags.AffectsOnlyOnce)
                    context.filteredTargets =
                        context.filteredTargets.Where(f => !targets.Any(t => t.Character == f)).ToArray();
            }

            return anyTargetFound;
        }


        private static void SpawnProjectile(TargetInfo source, TargetInfo target, SpellContext context)
        {
            var projectileContext = new ProjectileContext
            {
                owner = context.initialSource,
                projectileData = context.GetCurrentSubSpell().Projectile,

                spell = context.spell,
                startSubContext = context.currentSubspell,

                target = target,
                origin = source
            };

            var projectilePrefab = Instantiate(new GameObject(), source.Position.Value, Quaternion.identity);

            var projectileData = projectilePrefab.AddComponent<ProjectileBehaviour>();
            Instantiate(context.GetCurrentSubSpell().Projectile.ProjectilePrefab, projectilePrefab.transform);

            projectileData.Initialize(projectileContext, context.caster);

            context.subContext.projectileSpawned = true;
        }

        private static CharacterState[] GetAllCharacters()
        {
            return FindObjectsOfType<CharacterState>().ToArray();
        }

        private static CharacterState[] GetFilteredCharacters(CharacterState owner, CharacterState source,
            SubSpell.AffectedTargets target)
        {
            var characters = FilterCharacters(owner, GetAllCharacters(), target);
            if ((target & SubSpell.AffectedTargets.Self) == 0)
                characters = characters.Where(t => t != source).ToArray();

            return characters;
        }

        public static bool IsEnemy(CharacterState owner, CharacterState otherTharacter, SubSpell.AffectedTargets target)
        {
            Assert.IsNotNull(owner);
            Assert.IsNotNull(otherTharacter);

            var sameTeam = otherTharacter.CurrentTeam == owner.CurrentTeam &&
                           owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
            var mask = sameTeam ? SubSpell.AffectedTargets.Friend : SubSpell.AffectedTargets.Enemy;
            if (otherTharacter == owner)
                mask |= SubSpell.AffectedTargets.Self;

            return (mask & target) != 0;
        }

        private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters,
            SubSpell.AffectedTargets target)
        {
            return characters.Where(c => IsEnemy(c, owner, target)).ToArray();
        }

        private static TargetInfo[] GetAllCharacterInArea(
            SpellContext context,
            CharacterState[] avalibleTargets,
            TargetInfo source,
            TargetInfo target)

        {
            switch (context.GetCurrentSubSpell().Area.Area)
            {
                case AreaOfEffect.AreaType.Ray:
                {
                    if (target.Character != null)
                        if (context.GetCurrentSubSpell().Obstacles == SubSpell.ObstacleHandling.Break)
                        {
                            Debugger.Default.DrawLine(source.Transform.position, target.Transform.position, Color.blue, 1);
                            return new[] {target};
                        }

                    //Debug.DrawLine(ray.origin, ray.origin + ray.direction * 10, Color.green, 2);

                    //CharacterState closest = null;
                    //float minDist = float.MaxValue;
                    //var hitedTargets = new List<CharacterState>(characters.Length / 5);

                    //foreach (var target in characters)
                    //{
                    //    var collider = target.GetComponent<Collider>();
                    //    if (collider == null)
                    //        continue;

                    //    if (collider.Raycast(ray, out var hit, maxSpellDistance))
                    //    {
                    //        if (obstacles == ObstacleHandling.Break)
                    //        {
                    //            if (hit.distance < minDist)
                    //            {
                    //                minDist = hit.distance;
                    //                closest = target;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            hitedTargets.Add(target);
                    //        }
                    //    }
                    //}

                    Debug.LogWarning("Not Implemented Ray Option Combo");
                    return null;
                }

                case AreaOfEffect.AreaType.Cone:
                {
                    Assert.IsTrue(target.Position.HasValue);
                    Assert.IsTrue(source.Position.HasValue);

                    var direction = target.Position.Value - source.Position.Value;
                    direction.y = 0;

                    var pos = target.Position.Value;
                    var origin = (context.GetCurrentSubSpell().Origin & SubSpell.SpellOrigin.Self) == SubSpell.SpellOrigin.Self
                        ? source.Position.Value 
                        : pos;

                    var sphereSize = context.GetCurrentSubSpell().Area.Size;
                    var maxAngle = context.GetCurrentSubSpell().Area.Angle;

                    Debugger.Default.DrawCone(origin, direction, sphereSize, maxAngle, Color.blue, 1.0f);

                    return avalibleTargets
                        .Where(t =>
                        {
                            var position = t.transform.position;
                            Debugger.Default.DrawLine(origin, position, Color.green, 1.0f);

                            var directionTo = position - origin;
                            directionTo.y = 0;

                            var inSphere = directionTo.magnitude < sphereSize;
                            if (!inSphere)
                                return false;

                            var angle = Vector3.Angle(direction, directionTo);
                            if (angle > 0)
                                return angle < maxAngle;
                            else
                                return -angle < maxAngle;
                        })
                        .Select(TargetInfo.Create).ToArray();
                }

                case AreaOfEffect.AreaType.Sphere:
                {
                    Assert.IsTrue(target.Position.HasValue);

                    var pos = target.Position.Value;
                    Debugger.Default.DrawCircleSphere(pos, context.GetCurrentSubSpell().Area.Size, Color.blue, 1);

                    return avalibleTargets.Where(t =>
                            (t.transform.position - pos).magnitude < context.GetCurrentSubSpell().Area.Size)
                        .Select(TargetInfo.Create).ToArray();
                }
                //case AreaOfEffect.AreaType.Cylinder:
                //    return characters.Where(t => Vector3.Cross(ray.direction, t.transform.position - ray.origin)
                //        .magnitude < area.Size).ToArray();

                default:
                    Debug.LogWarning("Not Implemented Area");
                    break;
            }

            return null;
        }

        //internal void DrawSpellGizmos(SubSpell spell, Vector3 target)
        //{
        //    Gizmos.DrawSphere(target, 0.2f);

        //    var targetObject = GetTarget(spell, target);
        //    if (targetObject == null)
        //        return;
        //    Gizmos.DrawWireCube(targetObject.transform.position, Vector3.one);
        //}
    }
}