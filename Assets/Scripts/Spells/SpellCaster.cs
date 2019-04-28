using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using static Assets.Scripts.Data.SubSpell;

public struct SpellEmitterData
{
    public CharacterState owner;
    public SpellEmitter emitter;
    public Ray ray;
    public Vector3 floorIntercection;
}

public class SpellCaster : MonoBehaviour
{
    public enum ContextState
    {
        JistQueued = 0,
        PreDelays,
        Executing,
        PostDelay,
        Finishing,
    }

    private class SubSpellContext
    {
        public ContextState state;

        public float startTime;
        public float activeTime;
        public float stateActiveTime;

        public SubSpell subSpell;
        public bool aborted;

        public object customData;
    }

    private class SubSpellTargets
    {
        public List<SpellTargets> spellTargets;
    }

    private class CastContext
    {
        public ContextState state;

        public Spell spell;
        public SpellEmitterData emitterData;

        public int currentSubspell;
        public SubSpellContext subContext;

        public float startTime;
        public float activeTime;
        public float stateActiveTime;

        public float frameTime;
        public bool aborted;

        public SubSpellTargets[] spellTargets;
    }

    private CharacterState _owner;
    public float MaxSpellDistance = 100.0f;
    private CastContext _context = null;

    // Start is called before the first frame update
    private void Start() => _owner = GetComponent<CharacterState>();

    public void CastSpell(Spell spell, SpellEmitterData data)
    {
        if (_context != null)
        {
            Debug.LogError($"spell cast aready casting, {_context.spell.Name}");
            return;
        }
        _context = CreateContext(spell, data);
    }

    private static CastContext CreateContext(Spell spell, SpellEmitterData data) => new CastContext
    {
        spell = spell,
        emitterData = data,

        currentSubspell = -1,
        subContext = null,

        startTime = Time.fixedTime,
        stateActiveTime = 0.0f
    };

    private static SubSpellContext CreateSubContext(CastContext conext, SubSpell subSpell) => new SubSpellContext
    {
        startTime = Time.fixedTime,
        activeTime = 0.0f,
        subSpell = subSpell
    };

    private void Update()
    {
        if (_context == null)
            return;

        _context.frameTime = Time.deltaTime;
        _context.activeTime += _context.frameTime;
        _context.stateActiveTime += _context.frameTime;

        while (ManageContext(_context)) ;

        if (_context.aborted == true)
        {
            Debug.Log($"{_context.spell.Name} aborted");
        }

        if (_context.state != ContextState.Finishing)
            return;

        _context = null;
    }

    private static bool ManageContext(CastContext context)
    {
        switch (context.state)
        {
            case ContextState.JistQueued:
                Debug.Log($"{context.spell.Name} start spell cast");
                Advance();
                return true;

            case ContextState.PreDelays:
                if (context.stateActiveTime < context.spell.PreCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} pre cast wait ended");
                Advance();
                return true;

            case ContextState.Executing:

                context.subContext = CreateSubContext(context, null);

                Debug.Log($"{context.spell.Name} cast sub spells");
                do
                {
                    ++context.currentSubspell;
                    if (context.currentSubspell >= context.spell.SubSpells.Length)
                        break;

                    context.subContext.subSpell = context.spell.SubSpells[context.currentSubspell];
                }
                while (ManageSubContext(context, context.subContext)) ;

                if (context.subContext.aborted == true)
                {
                    if ((context.spell.Flags & Spell.SpellFlags.BreakOnFailedTargeting)
                        == Spell.SpellFlags.BreakOnFailedTargeting)
                    {
                        context.aborted = true;
                        context.state = ContextState.PostDelay;

                        return true;
                    }
                }

                if (context.subContext.state != ContextState.Finishing)
                    break;

                context.subContext = null;
                Advance();
                return true;

            case ContextState.PostDelay:
                if (context.stateActiveTime < context.spell.PreCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} pre cast wait ended");
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

    private static bool ManageSubContext(CastContext context, SubSpellContext subContext)
    {
        void Advance()
        {
            ++subContext.state;
            subContext.stateActiveTime = 0;
        }

        switch (subContext.state)
        {
            case ContextState.JistQueued:
                Debug.Log($"{context.spell.Name} start subspell cast {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.PreDelays:
                if (subContext.activeTime < subContext.subSpell.PostCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} subspell PreDelays ended {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.Executing:
                if (!Execute(context, subContext))
                {
                    Debug.LogError($"{context.spell.Name} Failed to execute subspell {context.currentSubspell}");
                    subContext.state = ContextState.Finishing;
                    subContext.aborted = true;
                }
                else
                {
                    Debug.Log($"{context.spell.Name} Executed subspell {context.currentSubspell}");
                    Advance();
                }
                return true;

            case ContextState.PostDelay:
                if (subContext.activeTime < subContext.subSpell.PostCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} subspell PostDelay ended {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.Finishing:
                Debug.Log($"{context.spell.Name} subspell finished {context.currentSubspell}");
                return false;
        }

        return false;
    }

    private static bool Execute(CastContext context, SubSpellContext subContext)
    {
        var currentTargets = context.spellTargets[context.currentSubspell];

        foreach (var pair in currentTargets.spellTargets)
        {
            var owner = pair.source;

            var origin = GetOrigin(owner, context, subContext);
            var direction = GetDirection(owner, context, subContext);

            if ((subContext.subSpell.Flags & SubSpell.SpellFlags.Projectile) == SubSpell.SpellFlags.Projectile)
            {
                SpawnProjectile(owner, context, subContext);
                return true;
            }

            CharacterState[] targets = null;

            if ((subContext.subSpell.Flags & SubSpell.SpellFlags.Raycast) == SubSpell.SpellFlags.Raycast)
            {
                if ((subContext.subSpell.Flags & SubSpell.SpellFlags.SelfTarget) == SubSpell.SpellFlags.SelfTarget)
                    targets = new[] { owner };
                else if ((subContext.subSpell.Flags & SubSpell.SpellFlags.SelfTarget) == SubSpell.SpellFlags.ClosestTarget)
                {
                    targets = GetFilteredCharacters(owner, subContext.subSpell.AffectedTarget);

                    if (targets.Length != 0)
                        targets = new[] { targets.OrderBy(t => (t.transform.position - origin).magnitude).First() };
                }
            }

            if ((subContext.subSpell.Flags & SubSpell.SpellFlags.Raycast) == SubSpell.SpellFlags.Raycast)
            {
                targets = GetFilteredCharacters(owner, subContext.subSpell.AffectedTarget);
                targets = GetAllCharacterInArea(targets, subContext.subSpell.Area, new Ray(origin, direction), subContext.subSpell.Obstacles);
            }

            if (targets != null && targets.Length != 0)
            {
                pair.destinations = targets;
            }
        }

        return true;
    }

    private static Vector3 GetOrigin(CharacterState owner, CastContext context, SubSpellContext subContext)
    {
        switch (subContext.subSpell.Origin)
        {
            case SubSpell.SpellOrigin.Self:
                return owner.transform.position;
            case SubSpell.SpellOrigin.Cursor:
                Assert.IsTrue(context.currentSubspell == 0);
                return context.emitterData.floorIntercection;
        }

        throw new InvalidOperationException("GetOrigin unhandled!");
    }
    private static Vector3 GetDirection(CharacterState owner, CastContext context, SubSpellContext subContext)
    {
        if ((subContext.subSpell.Flags & SubSpell.SpellFlags.HaveDirection) == SubSpell.SpellFlags.HaveDirection)
        {
            if (context.currentSubspell == 0)
                return context.emitterData.ray.direction;
            return owner.transform.forward;
        }

        return Vector3.one;
    }

    private static void SpawnProjectile(CharacterState owner, CastContext context, SubSpellContext subContext)
    {

    }

    private static CharacterState[] GetAllCharacters() => FindObjectsOfType<CharacterState>().ToArray();

    private static CharacterState[] GetFilteredCharacters(CharacterState owner, SubSpell.AffectedTargets target) =>
        FilterCharacters(owner, GetAllCharacters(), target);

    private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters, SubSpell.AffectedTargets target) =>
        characters.Where(c =>
        {
            bool sameTeam = c.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
            var mask = sameTeam ? SubSpell.AffectedTargets.Friend : SubSpell.AffectedTargets.Enemy;
            if (c == owner)
                mask |= SubSpell.AffectedTargets.Self;

            return (mask & target) == target;
        }).ToArray();

    private static CharacterState[] GetAllCharacterInArea(CharacterState[] characters, AreaOfEffect area, Ray ray, ObstacleHandling obstacles)
    {
        float maxSpellDistance = 100;

        foreach (var character in characters)
        {
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Ray:
                {
                    CharacterState closest = null;
                    float minDist = float.MaxValue;
                    var hitedTargets = new List<CharacterState>(characters.Length / 5);

                    foreach (var target in characters)
                    {
                        var collider = target.GetComponent<Collider>();
                        if (collider == null)
                            continue;

                        if (collider.Raycast(ray, out var hit, maxSpellDistance))
                        {
                            if (obstacles == ObstacleHandling.Break)
                            {
                                if (hit.distance < minDist)
                                {
                                    minDist = hit.distance;
                                    closest = target;
                                }
                            }
                            else
                            {
                                hitedTargets.Add(target);
                            }
                        }
                    }

                    if (obstacles == ObstacleHandling.Break)
                    {
                        return closest == null ? null : new[] { closest };
                    }
                    return hitedTargets.ToArray();
                }

                case AreaOfEffect.AreaType.Conus:
                    return characters.Where(t => Vector3.Angle(ray.direction, (t.transform.position - ray.origin)) < area.Size).ToArray();

                case AreaOfEffect.AreaType.Sphere:
                    return characters.Where(t => ((t.transform.position - ray.origin).magnitude < area.Size)).ToArray();

                case AreaOfEffect.AreaType.Cylinder:
                    return characters.Where(t => Vector3.Cross(ray.direction, t.transform.position - ray.origin)
                        .magnitude < area.Size).ToArray();

                default:
                    Debug.LogAssertion($"Unhandled AreaType {area.Area}");
                    break;
            }
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
