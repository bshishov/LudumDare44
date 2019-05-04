using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
public class SubSpellContext
{
    public object          customData;
    public ISubSpellEffect effect;
    public bool            failedToFindTargets;
    public bool            projectileSpawned;

    public ContextState     state;
    public float            stateActiveTime = 0.0f;
    public List<SpellTargets> newTargets = new List<SpellTargets>();

    public bool             Casting => failedToFindTargets == false && projectileSpawned == false;

    public static SubSpellContext Create(SpellContext context)
    {
        return new SubSpellContext {effect = context.CurrentSubSpell.GetEffect()};
    }
}

public class SpellContext : ISpellContext
{
    private SubSpell _currentSubSpell;

    public SpellCaster caster;

    public ISpellEffect effect;

    public List<CharacterState> filteredTargets;

    public float frameTime;

    private int startSubspellIndex;

        public List<SubSpellTargets> subSpellTargets;

        public int             CurrentSubSpellIndex { get; set; }
    public SubSpellContext SubContext           { get; set; }

    public SubSpellTargets  CurrentSubSpellTargets => subSpellTargets[CurrentSubSpellIndex - startSubspellIndex];
    public Spell.SpellFlags SpellFlags             => Spell.Flags;
    public bool             Aborted                { get; set; }
    public float            ActiveTime             { get; set; }
    public CharacterState   InitialSource          { get; private set; }

    public Spell Spell { get; private set; }

    public int          Stacks          { get; set; } = 1;
    public float        StartTime       { get; set; }
    public ContextState State           { get; set; }
    public float        StateActiveTime { get; set; }

    public bool IsLastSubSpell => CurrentSubSpellIndex == Spell.SubSpells.Length - 1;

    public SubSpell CurrentSubSpell => Spell.SubSpells[CurrentSubSpellIndex];

    public static SpellContext Create(SpellCaster caster, Spell spell, int stacks, SpellTargets targets, int subSpellStartIndex)
    {
        Debug.Log(targets);

        var context = new SpellContext
                      {
                          InitialSource        = targets.Source.Character,
                          caster               = caster,
                          State                = subSpellStartIndex == 0 ? ContextState.JustQueued : ContextState.FindTargets,
                          Spell                = spell,
                          Stacks               = stacks,
                          startSubspellIndex   = subSpellStartIndex,
                          CurrentSubSpellIndex = subSpellStartIndex,
                          SubContext           = null,
                          StartTime            = Time.fixedTime,
                          StateActiveTime      = 0.0f,
                          filteredTargets =
                              (spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce
                                  ? new List<CharacterState>()
                                  : null,
                          subSpellTargets = new List<SubSpellTargets> {new SubSpellTargets {TargetData = new List<SpellTargets> {targets}}},
                          effect          = spell.GetEffect()
                      };

        return context;
    }
}

public class SpellCaster : MonoBehaviour
{
    private SpellContext       _context;
    private List<SpellContext> _nestedContexts = new List<SpellContext>();

    private CharacterState _owner;
    public  float          MaxSpellDistance = 100.0f;

    [CanBeNull]
    public ISpellContext ActiveSpellContext => _context;

    // Start is called before the first frame update
    private void Start() { _owner = GetComponent<CharacterState>(); }

    public static bool IsValidTarget(SubSpell subSpell, TargetInfo target)
    {
        switch (subSpell.Targeting)
        {
            case SubSpell.SpellTargeting.Target when target.Transform == null && target.Character == null:
                return false;

            case SubSpell.SpellTargeting.Target:
            {
                if (target.Transform == null)
                    target.Transform = target.Character.GetNodeTransform(CharacterState.NodeRole.Chest);
                if (target.Character == null)
                {
                    target.Character = target.Transform.GetComponent<CharacterState>();
                    Assert.IsNotNull(target.Character);
                }

                break;
            }
            case SubSpell.SpellTargeting.Location:
                if (target.Position.HasValue == false)
                {
                    Debug.LogError("No valid target to cast!");
                    return false;
                }

                break;
            case SubSpell.SpellTargeting.None:
            default:
                Assert.IsTrue(false, "Unknown targeting option");
                break;
        }

        return true;
    }

    public bool CastSpell(Spell spell, int stacks, SpellTargets targets)
    {
        if (_context != null)
        {
            Debug.LogError($"spell cast aready casting, {_context.Spell.Name}");
            return false;
        }

        if (!targets.Destinations.Any(t => IsValidTarget(spell.SubSpells[0], t)))
            return false;

        _context = SpellContext.Create(this, spell, stacks, targets, 0);
        return true;
    }

    internal void ContinueCastSpell(Spell spell, SpellTargets targets, int subSpellStartIndex = 0, int stacks = 1)
    {
        lock (_nestedContexts)
        {
            _nestedContexts.Add(SpellContext.Create(this, spell, stacks, targets, subSpellStartIndex));
        }
    }

    private static bool ExecuteContext(SpellContext context)
    {
        context.frameTime       =  Time.deltaTime;
        context.ActiveTime      += context.frameTime;
        context.StateActiveTime += context.frameTime;

        try
        {
            while (ManageContext(context)) { }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return true;
        }

        return context.State == ContextState.Finishing;
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
        switch (context.State)
        {
            case ContextState.JustQueued:
                Debug.Log($"{context.Spell.Name} start spell cast");
                Advance();
                return true;

            case ContextState.PreDelays:
                if (context.StateActiveTime < context.Spell.PreCastDelay)
                    break;

                Advance();
                return true;

            case ContextState.FindTargets:
            case ContextState.PreDamageDelay:
                context.State = ContextState.Fire;
                return true;

            case ContextState.Fire:

                if (context.SubContext != null)
                    context.SubContext.stateActiveTime += context.frameTime;

                while (context.CurrentSubSpellIndex < context.Spell.SubSpells.Length && context.Aborted == false)
                {
                    if (context.SubContext == null)
                        context.SubContext = SubSpellContext.Create(context);

                    if (!ManageSubContext(context, context.SubContext))
                        return false;

                    if (context.SubContext.failedToFindTargets
                        && (context.SpellFlags & Spell.SpellFlags.BreakOnFailedTargeting) == Spell.SpellFlags.BreakOnFailedTargeting)
                    {
                        context.Aborted = true;
                        if (context.SubContext.state < ContextState.PostDelay)
                            context.SubContext.state = ContextState.PostDelay;

                        context.SubContext.state = ContextState.JustQueued;
                        break;
                    }

                    if (context.SubContext.state == ContextState.Finishing)
                    {
                        if (context.CurrentSubSpell.Effect != null)
                            context.CurrentSubSpell.DestoryEffectInstance();
                        context.SubContext = null;
                        ++context.CurrentSubSpellIndex;
                    }
                }

                context.SubContext = null;
                Advance();
                return true;

            case ContextState.PostDelay:
                if (context.StateActiveTime < context.Spell.PostCastDelay)
                    break;

                Advance();
                return true;

            case ContextState.Finishing:
                Debug.Log($"{context.Spell.Name} finishing");
                if (context.effect != null)
                        context.Spell.DestoryEffectInstance();
                    return false;
        }

        return false;

        void Advance()
        {
            ++context.State;
            context.StateActiveTime = 0;
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
                if (subContext.stateActiveTime < context.CurrentSubSpell.PreCastDelay)
                    break;

                Advance();
                return true;

            case ContextState.FindTargets:
                subContext.failedToFindTargets = !LockTargets(context, subContext);
                context.effect?.OnStateChange(context, ContextState.FindTargets);

                Advance();
                return true;

            case ContextState.PreDamageDelay:
                if (subContext.stateActiveTime < context.CurrentSubSpell.PreDamageDelay)
                    break;

                Advance();
                return true;

            case ContextState.Fire:
                subContext.failedToFindTargets = !FinalizeTargets(context, subContext);

                Execute(context, subContext);
                Debug.Log($"{context.Spell.Name} Executed subspell {context.CurrentSubSpellIndex}");

                if(!context.IsLastSubSpell)
                switch (context.CurrentSubSpell.NewSource) {
                    case SubSpell.NewSourceType.OriginalTargetData: context.subSpellTargets.Add(context.CurrentSubSpellTargets);
                        break;
                    case SubSpell.NewSourceType.AffectedTarget:
                        context.subSpellTargets.Add(new SubSpellTargets
                                                    {
                                                        TargetData = subContext
                                                                     .newTargets.SelectMany(t => t.Destinations)
                                                                     .Select(d => new SpellTargets(new TargetInfo(d))).ToList()
                                                    });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }

                    Advance();
                return true;

            case ContextState.PostDelay:
                if (subContext.stateActiveTime < context.CurrentSubSpell.PostCastDelay)
                    break;

                Advance();
                return true;

            case ContextState.Finishing:
                return true;
        }

        return false;

        void Advance()
        {
            ++context.SubContext.state;
            context.SubContext.stateActiveTime = 0;
        }
    }

    private static bool LockTargets(SpellContext context, SubSpellContext subContext)
    {
        var anyTargetFound = false;
        var currentTargets = context.CurrentSubSpellTargets;

        foreach (var castData in currentTargets.TargetData)
        {
            var source          = castData.Source;
            var newSpellTargets = new SpellTargets(source);

            if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.SelfTarget) == SubSpell.SpellFlags.SelfTarget)
            {
                castData.Destinations = new[] {source};
            }
            else if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.ClosestTarget) == SubSpell.SpellFlags.ClosestTarget)
            {
                Assert.IsTrue(source.Position.HasValue, "source.Position != null");

                var availableTargets = GetFilteredCharacters(context.InitialSource, source.Character, context.CurrentSubSpell.AffectedTarget, context.filteredTargets);
                castData.Destinations = new[]
                                        {
                                            TargetInfo.Create(availableTargets
                                                              .OrderBy(t => (t.transform.position - source.Position.Value).magnitude)
                                                              .FirstOrDefault())
                                        };
            }

            var newTargets = new List<TargetInfo>(castData.Destinations.Length);
            foreach (var target in castData.Destinations)
                if (IsValidTarget(context.CurrentSubSpell, target))
                    newTargets.Add(target);

            anyTargetFound = newTargets.Count != 0;

            newSpellTargets.Destinations = newTargets.ToArray();
            subContext.newTargets.Add(newSpellTargets);

            subContext.effect?.OnTargetsPreSelected(context, newSpellTargets);
        }

        return anyTargetFound;
    }

    private static bool FinalizeTargets(SpellContext context, SubSpellContext subContext)
    {
        var anyTargetFound = false;
        var currentTargets = subContext.newTargets;

        var targets = new List<TargetInfo>();

        foreach (var castData in currentTargets)
        {
            var              source           = castData.Source;
            CharacterState[] availableTargets = null;

            foreach (var target in castData.Destinations)
                if (LockTarget(context, target, source, targets, castData, ref availableTargets))
                    return true;


            if ((context.SpellFlags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce)
            {
                context.filteredTargets.AddRange(targets.Select(t => t.Character));
            }

            anyTargetFound        = targets.Count != 0;
            castData.Destinations = targets.ToArray();
        }

        return anyTargetFound;
    }

    private static bool LockTarget(SpellContext         context,
                                   TargetInfo           target,
                                   TargetInfo           source,
                                   List<TargetInfo>     targets,
                                   SpellTargets         castData,
                                   ref CharacterState[] availableTargets)
    {
        Assert.IsTrue(IsValidTarget(context.CurrentSubSpell, target));

        if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.Projectile) == SubSpell.SpellFlags.Projectile)
        {
            SpawnProjectile(source, target, context);
            return true;
        }

        Assert.IsFalse(context.SubContext.projectileSpawned);

        if (availableTargets == null)
            availableTargets = GetFilteredCharacters(context.InitialSource, source.Character, context.CurrentSubSpell.AffectedTarget, context.filteredTargets);

        if ((context.SpellFlags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce)
            availableTargets = availableTargets.Except(context.filteredTargets).ToArray();

        if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.Raycast) == SubSpell.SpellFlags.Raycast)
        {
            var dst = GetAllCharacterInArea(context, availableTargets, source, target);
            if (dst != null && dst.Length > 0)
                targets.AddRange(dst);
        }

        return false;
    }

    private static void Execute(SpellContext context, SubSpellContext subContext)
    {
        var currentTargets = subContext.newTargets;

        foreach (var targets in currentTargets)
        {
            if (targets.Destinations == null)
                continue;

            context.SubContext.effect?.OnTargetsAffected(context, targets);

            if (context.SubContext.projectileSpawned)
                continue;

            foreach (var destination in targets.Destinations)
                if (destination.Character != null)
                    destination.Character.ApplySpell(context.InitialSource, context);
                else
                    Debug.LogWarning("Failed to apply spell");
        }
    }

    private static void SpawnProjectile(TargetInfo source, TargetInfo target, SpellContext context)
    {
        var projectileContext = new ProjectileContext
                                {
                                    owner           = context.InitialSource,
                                    projectileData  = context.CurrentSubSpell.Projectile,
                                    spell           = context.Spell,
                                    startSubContext = context.CurrentSubSpellIndex,
                                    target          = target,
                                    origin          = source,
                                    Stacks          = context.Stacks
                                };

        var projectilePrefab = Instantiate(new GameObject("Projectile_root"), source.Position.Value, Quaternion.identity);

        var projectileData = projectilePrefab.AddComponent<ProjectileBehaviour>();
        Instantiate(context.CurrentSubSpell.Projectile.ProjectilePrefab, projectilePrefab.transform);

        projectileData.Initialize(projectileContext, context.caster);

        context.SubContext.projectileSpawned = true;
    }

    private static CharacterState[] GetAllCharacters() { return FindObjectsOfType<CharacterState>().ToArray(); }

    private static CharacterState[] GetFilteredCharacters(CharacterState owner, CharacterState source, SubSpell.AffectedTargets target,
                                                          IEnumerable<CharacterState> filteredCharacters)
    {
        var characters = FilterCharacters(owner, GetAllCharacters(), target, filteredCharacters);
        if ((target & SubSpell.AffectedTargets.Self) == 0)
            characters = characters.Where(t => t != source).ToArray();

        return characters;
    }

    public static bool IsEnemy(CharacterState owner, CharacterState otherTharacter, SubSpell.AffectedTargets target)
    {
        Assert.IsNotNull(owner);
        Assert.IsNotNull(otherTharacter);

        var sameTeam = otherTharacter.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
        var mask     = sameTeam ? SubSpell.AffectedTargets.Friend : SubSpell.AffectedTargets.Enemy;
        if (otherTharacter == owner)
            mask |= SubSpell.AffectedTargets.Self;

        return (mask & target) != 0;
    }

    private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters, SubSpell.AffectedTargets target, IEnumerable<CharacterState> filteredCharacters)
    {
        var availibleCharacters = characters.Where(c => IsEnemy(c, owner, target));
        if (filteredCharacters != null)
            availibleCharacters = availibleCharacters.Except(filteredCharacters);
        return availibleCharacters.ToArray();
    }

    private static TargetInfo[] GetAllCharacterInArea(SpellContext context, CharacterState[] avalibleTargets, TargetInfo source, TargetInfo target)

    {
        switch (context.CurrentSubSpell.Area.Area)
        {
            case AreaOfEffect.AreaType.Ray:
            {
                if (target.Character != null)
                    if (context.CurrentSubSpell.Obstacles == SubSpell.ObstacleHandling.Break)
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

                var pos    = target.Position.Value;
                var origin = (context.CurrentSubSpell.Origin & SubSpell.SpellOrigin.Self) == SubSpell.SpellOrigin.Self ? source.Position.Value : pos;

                var sphereMinRadius = context.CurrentSubSpell.Area.MinSize;
                var sphereMaxRadius = context.CurrentSubSpell.Area.Size;
                var maxAngle        = context.CurrentSubSpell.Area.Angle;

                Debugger.Default.DrawCone(origin, direction, sphereMinRadius, maxAngle, Color.blue, 1.0f);

                return avalibleTargets.Where(t =>
                                             {
                                                 var position = t.transform.position;
                                                 Debugger.Default.DrawLine(origin, position, Color.green, 1.0f);

                                                 var directionTo = position - origin;
                                                 directionTo.y = 0;

                                                 var inSphere = directionTo.magnitude >= sphereMinRadius && directionTo.magnitude < sphereMaxRadius;
                                                 if (!inSphere)
                                                     return false;

                                                 var angle = Vector3.Angle(direction, directionTo);
                                                 if (angle > 0)
                                                     return angle < maxAngle;
                                                 return -angle < maxAngle;
                                             })
                                      .Select(TargetInfo.Create)
                                      .ToArray();
            }

            case AreaOfEffect.AreaType.Sphere:
            {
                Assert.IsTrue(target.Position.HasValue);

                var pos = target.Position.Value;
                Debugger.Default.DrawCircleSphere(pos, context.CurrentSubSpell.Area.Size, Color.blue, 1);

                return avalibleTargets.Where(t => (t.transform.position - pos).magnitude < context.CurrentSubSpell.Area.Size)
                                      .Select(TargetInfo.Create)
                                      .ToArray();
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