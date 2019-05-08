using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Debugger = Assets.Scripts.Utils.Debugger.Debugger;

namespace Spells
{
public interface IChannelingInfo
{
    TargetInfo GetNewTarget();
}

[DebuggerStepThrough]
public class SubSpellContext
{
    public readonly List<SpellTargets> newTargets = new List<SpellTargets>();
    public          object             customData;
    public          ISubSpellEffect    effect;
    public          bool               failedToFindTargets;
    public          bool               projectileSpawned;

    public ContextState state;
    public float        stateActiveTime;

    public bool Casting => failedToFindTargets == false && projectileSpawned == false;

    public static SubSpellContext Create(SpellContext context) { return new SubSpellContext {effect = context.CurrentSubSpell.GetEffect()}; }
}

[DebuggerStepThrough]
public class SpellContext : ISpellContext
{
    private SubSpell _currentSubSpell;

    public SpellCaster caster;

    public ISpellEffect effect;

    public List<CharacterState> filteredTargets;

    public float frameTime;

    public ISpellCastListener listener;

    private int startSubspellIndex;

    public List<SubSpellTargets> subSpellTargets;

    public int             CurrentSubSpellIndex { get; set; }
    public SubSpellContext SubContext           { get; set; }

    public SubSpellTargets  CurrentSubSpellTargets => subSpellTargets[CurrentSubSpellIndex - startSubspellIndex];
    public Spell.SpellFlags SpellFlags             => Spell.Flags;
    public IChannelingInfo  ChannelingInfo         { get; private set; }
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

    public static SpellContext Create(SpellCaster        caster,
                                      Spell              spell,
                                      int                stacks,
                                      SpellTargets       targets,
                                      IChannelingInfo    channelingInfo,
                                      ISpellCastListener listener,
                                      int                subSpellStartIndex)
    {
        Debug.Log(targets);

        var context = new SpellContext
                      {
                          InitialSource        = targets.Source.Character,
                          caster               = caster,
                          ChannelingInfo       = channelingInfo,
                          listener             = listener,
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

    public static bool IsValidTarget(Spell spell, SpellTargets targets)
    {
        if ((spell.SubSpells[0].Flags & SubSpell.SpellFlags.SpecialEnd) != 0)
            return true;

        if (!targets.Destinations.Any(t => IsValidTarget(spell.SubSpells[0], t)))
            return false;

        return true;
    }

    private static bool IsValidTarget(SubSpell subSpell, TargetInfo target)
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

    public bool CastSpell(Spell spell, int stacks, SpellTargets targets, IChannelingInfo channelingInfo, ISpellCastListener listener)
    {
        if (_context != null)
        {
            Debug.LogError($"spell cast aready casting, {_context.Spell.Name}");
            return false;
        }

        if (!IsValidTarget(spell, targets))
            return false;

        _context = SpellContext.Create(this, spell, stacks, targets, channelingInfo, listener, 0);
        return true;
    }

    internal void ContinueCastSpell(Spell spell, SpellTargets targets, int subSpellStartIndex = 0, int stacks = 1)
    {
        lock (_nestedContexts)
        {
            _nestedContexts.Add(SpellContext.Create(this, spell, stacks, targets, null, null, subSpellStartIndex));
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
                var oldState = context.State;
                context.State = ContextState.Fire;
                context.effect?.OnStateChange(context, oldState);
                context.StateActiveTime = 0;
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

                        break;
                    }

                    if (context.SubContext.state != ContextState.Finishing)
                        continue;

                    if (context.SubContext.projectileSpawned)
                        break;

                    var effect = context.SubContext?.effect;
                    context.SubContext = null;

                    if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.Channeling) == 0)
                    {
                        effect?.OnEndSubSpell(context);
                        ++context.CurrentSubSpellIndex;
                    }
                    else
                        return false;
                }

                if (context.Aborted)
                {
                    context.SubContext?.effect?.OnEndSubSpell(context);
                    context.listener?.OnAbortedFiring(context.Spell);
                }
                else
                {

                    context.listener?.OnEndFiring(context.Spell);
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
                context.listener?.OnEndCasting(context.Spell);
                return false;
        }

        return false;

        void Advance()
        {
            var oldState = context.State;
            ++context.State;

            context.effect?.OnStateChange(context, oldState);
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

                subContext.failedToFindTargets = !FinalizeTargets(context, subContext);
                context.listener?.OnStartFiring(context.Spell, context.CurrentSubSpell);

                Advance();
                return true;

            case ContextState.PreDamageDelay:
                if (subContext.stateActiveTime < context.CurrentSubSpell.PreDamageDelay)
                    break;

                Advance();
                return true;

            case ContextState.Fire:
                Execute(context, subContext);
                Debug.Log($"{context.Spell.Name} Executed subspell {context.CurrentSubSpellIndex}");

                if (!context.IsLastSubSpell)
                    switch (context.CurrentSubSpell.NewSource)
                    {
                        case SubSpell.NewSourceType.OriginalTargetData:
                            context.subSpellTargets.Add(context.CurrentSubSpellTargets);
                            break;
                        case SubSpell.NewSourceType.AffectedTarget:
                            context.subSpellTargets.Add(new SubSpellTargets
                                                        {
                                                            TargetData = subContext
                                                                         .newTargets.SelectMany(t => t.Destinations)
                                                                         .Select(d => new SpellTargets(new TargetInfo(d)))
                                                                         .ToList()
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

    private static bool PullChannelingTargetInfo(SpellContext context, SubSpellContext subContext, IReadOnlyList<SpellTargets> currentTargets)
    {
        Assert.IsTrue(currentTargets.Count == 1);
        Assert.IsTrue((context.SpellFlags & Spell.SpellFlags.BreakOnFailedTargeting) == 0);
        Assert.IsNotNull(context.ChannelingInfo);

        var newTarget = context.ChannelingInfo.GetNewTarget();
        if (newTarget == null)
            return false;

        if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.SelfTarget) == SubSpell.SpellFlags.SelfTarget)
        {
            newTarget = new TargetInfo(currentTargets[0].Source);
        }

        if (!IsValidTarget(context.CurrentSubSpell, newTarget))
        {
            Debug.LogWarning("Channeling target is invalid!");
            return false;
        }

        var newSpellTargets = new SpellTargets(currentTargets[0].Source, newTarget);

        subContext.newTargets.Add(newSpellTargets);
        subContext.effect?.OnInputTargetsValidated(context, newSpellTargets);
        context.effect?.OnInputTargetsValidated(context, newSpellTargets);
        return true;
    }

    private static bool LockTargets(SpellContext context, SubSpellContext subContext)
    {
        var anyTargetFound = false;
        var currentTargets = context.CurrentSubSpellTargets;

        if ((context.CurrentSubSpell.Flags & SubSpell.SpellFlags.Channeling) == SubSpell.SpellFlags.Channeling)
        {
            context.Aborted = !PullChannelingTargetInfo(context, subContext, currentTargets.TargetData);
            return !context.Aborted;
        }

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

                var availableTargets = GetFilteredCharacters(context.InitialSource,
                                                             source.Character,
                                                             context.CurrentSubSpell.AffectedTarget,
                                                             context.filteredTargets);
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

            subContext.effect?.OnInputTargetsValidated(context, newSpellTargets);
            context.effect?.OnInputTargetsValidated(context, newSpellTargets);
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
                if (FinalizeTarget(context, target, source, targets, castData, ref availableTargets))
                    return true;

            if ((context.SpellFlags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce)
                context.filteredTargets.AddRange(targets.Select(t => t.Character));

            anyTargetFound        = targets.Count != 0;
            castData.Destinations = targets.ToArray();

            context.SubContext.effect?.OnTargetsFinalized(context, castData);
            context.effect?.OnTargetsFinalized(context, castData);
        }

        return anyTargetFound;
    }

    private static bool FinalizeTarget(SpellContext         context,
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
            context.effect?.OnTargetsAffected(context, targets);

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

        var projectilePrefab = new GameObject("Projectile_root", typeof(ProjectileBehaviour));
        projectilePrefab.transform.position = source.Position.Value;
        projectilePrefab.transform.rotation = Quaternion.identity;

        Instantiate(context.CurrentSubSpell.Projectile.ProjectilePrefab, projectilePrefab.transform);

        projectilePrefab.GetComponent<ProjectileBehaviour>().Initialize(projectileContext, context.caster);

        context.SubContext.projectileSpawned = true;
    }

    private static CharacterState[] GetAllCharacters() { return FindObjectsOfType<CharacterState>().ToArray(); }

    private static CharacterState[] GetFilteredCharacters(CharacterState              owner,
                                                          CharacterState              source,
                                                          SubSpell.AffectedTargets    target,
                                                          IEnumerable<CharacterState> filteredCharacters)
    {
        var characters = FilterCharacters(owner, GetAllCharacters(), target, filteredCharacters);
        if ((target & SubSpell.AffectedTargets.Self) == 0)
            characters = characters.Where(t => t != source).ToArray();

        return characters;
    }

    public static bool IsEnemy(CharacterState owner, CharacterState otherCharacter, SubSpell.AffectedTargets target)
    {
        Assert.IsNotNull(owner);
        Assert.IsNotNull(otherCharacter);

        if (!otherCharacter.IsAlive)
            return false;

        var sameTeam = otherCharacter.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
        var mask     = sameTeam ? SubSpell.AffectedTargets.Ally : SubSpell.AffectedTargets.Enemy;
        if (otherCharacter == owner)
            mask |= SubSpell.AffectedTargets.Self;

        return (mask & target) != 0;
    }

    private static CharacterState[] FilterCharacters(CharacterState              owner,
                                                     CharacterState[]            characters,
                                                     SubSpell.AffectedTargets    target,
                                                     IEnumerable<CharacterState> filteredCharacters)
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
                // Special for single-targeted abilities
                if (target.Character != null)
                    if (context.CurrentSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.Break))
                        return new[] {target};

                if (context.CurrentSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.ExecuteSpellSequence))
                {
                    var direction = (target.Position.Value - source.Position.Value).normalized;
                    return Physics.RaycastAll(source.Position.Value, direction,
                            context.CurrentSubSpell.Area.Size, Common.LayerMasks.ActorsOrGround)
                        .Select(hitInfo => hitInfo.transform.GetComponent<CharacterState>())
                        .Where(characterState => characterState != null && avalibleTargets.Contains(characterState))
                        .Select(characterState =>
                        {
                            var transform = characterState.GetNodeTransform(CharacterState.NodeRole.Chest);
                            return new TargetInfo
                            {
                                Character = characterState,
                                Transform = transform,
                                Position = transform.position
                            };
                        })
                        .ToArray();
                }

                Debug.LogWarning("Not Implemented Ray-type SubSpell flags combination");
                return null;
            }

            case AreaOfEffect.AreaType.Cone:
            {
                Assert.IsTrue(target.Position.HasValue);
                Assert.IsTrue(source.Position.HasValue);

                var direction = target.Position.Value - source.Position.Value;
                direction.y = 0;

                var pos    = target.Position.Value;
                var origin = context.CurrentSubSpell.Origin.HasFlag(SubSpell.SpellOrigin.Self) ? source.Position.Value : pos;

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