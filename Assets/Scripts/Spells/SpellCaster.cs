﻿using Assets.Scripts.Data;
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
    public RaycastHit hitInfo;
}

public class SpellCaster : MonoBehaviour
{

    private class SubSpellContext
    {
        public ContextState state;

        public float startTime;
        public float activeTime;
        public float stateActiveTime;

        public bool aborted;

        public object customData;
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

        public List<SubSpellTargets> subSpellTargets;
        public ISpellEffect effect;

        public SubSpell GetCurrentSubSpell() => spell.SubSpells[currentSubspell];
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

    private static CastContext CreateContext(Spell spell, SpellEmitterData data)
    {
        var context = new CastContext
        {
            spell = spell,
            emitterData = data,

            currentSubspell = 0,
            subContext = null,

            startTime = Time.fixedTime,
            stateActiveTime = 0.0f,

            subSpellTargets = new List<SubSpellTargets>(spell.SubSpells.Length),
            effect = spell.GetEffect()
        };

        return context;
    }

    private static SubSpellContext CreateSubContext(CastContext conext) => new SubSpellContext
    {
        startTime = Time.fixedTime,
        activeTime = 0.0f
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
            case ContextState.JustQueued:
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

                context.subSpellTargets.Add(new SubSpellTargets {
                    targetData = new List<PerSourceTargets> {
                        new PerSourceTargets {
                            source = context.emitterData.owner
                        }
                    }
                });
                context.subContext = CreateSubContext(context);

                Debug.Log($"{context.spell.Name} cast sub spells");

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
            case ContextState.JustQueued:
                Debug.Log($"{context.spell.Name} start subspell cast {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.PreDelays:
                if (subContext.activeTime < context.GetCurrentSubSpell().PostCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} subspell PreDelays ended {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.Executing:
                if (!Execute(context, subContext))
                {
                    Debug.LogWarning($"{context.spell.Name} Failed to execute subspell {context.currentSubspell}");
                    subContext.state = ContextState.Finishing;
                    subContext.aborted = true;
                }
                else
                {
                    ApplySubSpell(context, subContext);
                    Debug.Log($"{context.spell.Name} Executed subspell {context.currentSubspell}");
                    Advance();
                }
                return true;

            case ContextState.PostDelay:
                if (subContext.activeTime < context.GetCurrentSubSpell().PostCastDelay)
                    break;

                Debug.Log($"{context.spell.Name} subspell PostDelay ended {context.currentSubspell}");
                Advance();
                return true;

            case ContextState.Finishing:
                Debug.Log($"{context.spell.Name} subspell finished {context.currentSubspell}");

                ++context.currentSubspell;
                return context.currentSubspell < context.spell.SubSpells.Length 
                    && subContext.aborted == false;
        }

        return false;
    }

    private static void ApplySubSpell(CastContext context, SubSpellContext subContext)
    {
        var currentTargets = context.subSpellTargets[context.currentSubspell];
        var newTargets = new SubSpellTargets { targetData = new List<PerSourceTargets>()};

        context.effect.OnSubSpellStartCast(context.spell, context.GetCurrentSubSpell(), currentTargets);

        foreach (var data in currentTargets.targetData)
        {
            if (data.destinations == null)
                continue;

            foreach(var src in data.destinations)
            {
                src.ApplySpell(context.emitterData.owner, context.GetCurrentSubSpell());

                newTargets.targetData.Add(new PerSourceTargets { source = src });
            }
        }

        context.subSpellTargets.Add(newTargets);
    }

    struct TargetingData
    {
        public CharacterState owner;
        public Vector3 origin;
        public Vector3? targetLocation;
        public CharacterState targetCharacter;
    }

    private static bool Execute(CastContext context, SubSpellContext subContext)
    {
        var anyTargetFound = false;
        var currentTargets = context.subSpellTargets[context.currentSubspell];

        foreach (var pair in currentTargets.targetData)
        {
            TargetingData targeting = new TargetingData
            {
                owner = pair.source,
                origin = GetOrigin(pair.source, context, subContext),
            };

            if ((context.GetCurrentSubSpell().Targeting & SpellTargeting.Location) == SpellTargeting.Location)
                targeting.targetLocation = context.emitterData.floorIntercection;

            CharacterState[] targets = GetFilteredCharacters(targeting.owner, targeting.owner, context.GetCurrentSubSpell().AffectedTarget);

            if ((context.GetCurrentSubSpell().Targeting & SpellTargeting.Target) == SpellTargeting.Target)
            {
                if ((context.GetCurrentSubSpell().Flags & SpellFlags.SelfTarget) == SpellFlags.SelfTarget)
                    targeting.targetCharacter = targeting.owner;
                else if ((context.GetCurrentSubSpell().Flags & SpellFlags.ClosestTarget) == SpellFlags.ClosestTarget)
                {
                    targeting.targetCharacter = targets.OrderBy(t => (t.transform.position - targeting.origin).magnitude).FirstOrDefault();
                }
                else if (context.emitterData.hitInfo.collider != null)
                    targeting.targetCharacter = context.emitterData.hitInfo.collider.GetComponent<CharacterState>();
            }

            if(targeting.targetCharacter == null && targeting.targetLocation == null)
            {
                Debug.LogError("No targets for spell!");
            }

            if ((context.GetCurrentSubSpell().Flags & SpellFlags.Projectile) == SpellFlags.Projectile)
            {
                SpawnProjectile(targeting, context, subContext);
                return true;
            }

            if ((context.GetCurrentSubSpell().Flags & SpellFlags.Raycast) == SpellFlags.Raycast)
            {
                targets = GetAllCharacterInArea(targets, targeting, context.GetCurrentSubSpell().Area, context.GetCurrentSubSpell().Obstacles);
            }

            if (targets != null && targets.Length != 0)
            {
                anyTargetFound = true;
                pair.destinations = targets;
            }
        }

        return anyTargetFound;
    }

    private static void SpawnProjectile(TargetingData targeting, CastContext context, SubSpellContext subContext)
    {

    }

    private static Vector3 GetOrigin(CharacterState owner, CastContext context, SubSpellContext subContext)
    {
        switch (context.GetCurrentSubSpell().Origin)
        {
            case SpellOrigin.Self:
                return owner.transform.position;
            case SpellOrigin.Cursor:
                Assert.IsTrue(context.currentSubspell == 0);
                return context.emitterData.floorIntercection;
        }

        throw new InvalidOperationException("GetOrigin unhandled!");
    }

    private static Vector3 GetDirection(CharacterState owner, CastContext context, SubSpellContext subContext)
    {
        if ((context.GetCurrentSubSpell().Flags & SpellFlags.HaveDirection) == SpellFlags.HaveDirection)
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

    private static CharacterState[] GetFilteredCharacters(CharacterState owner, CharacterState source, AffectedTargets target)
    {
        var characters = FilterCharacters(owner, GetAllCharacters(), target);
        if((target & AffectedTargets.Self) == 0)
        {
            characters = characters.Where(t => t != source).ToArray();
        }

        return characters;
    }
    private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters, SubSpell.AffectedTargets target) =>
        characters.Where(c =>
        {
            bool sameTeam = c.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
            var mask = sameTeam ? SubSpell.AffectedTargets.Friend : SubSpell.AffectedTargets.Enemy;
            if (c == owner)
                mask |= SubSpell.AffectedTargets.Self;

            return (mask & target) == target;
        }).ToArray();

    private static CharacterState[] GetAllCharacterInArea(CharacterState[] characters, TargetingData targeting, AreaOfEffect area, ObstacleHandling obstacles)
    {
        float maxSpellDistance = 100;

        foreach (var character in characters)
        {
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Ray:
                {
                    if(targeting.targetCharacter != null)
                    {
                        if (obstacles == ObstacleHandling.Break)
                        {
                            return new[] { targeting.targetCharacter };
                        }
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

                    return null;
                }

                //case AreaOfEffect.AreaType.Cone:
                //    return characters.Where(t => Vector3.Angle(ray.direction, (t.transform.position - ray.origin)) < area.Size).ToArray();

                //case AreaOfEffect.AreaType.Sphere:
                //    return characters.Where(t => ((t.transform.position - ray.origin).magnitude < area.Size)).ToArray();

                //case AreaOfEffect.AreaType.Cylinder:
                //    return characters.Where(t => Vector3.Cross(ray.direction, t.transform.position - ray.origin)
                //        .magnitude < area.Size).ToArray();

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
