using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public struct SpellEmitterData
{
    public GameObject owner;
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
    }

    private CharacterState _owner;
    public float MaxSpellDistance = 100.0f;
    private CastContext _context;

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

        currentSubspell = 1,
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

        while (ManageContext(_context));

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
        void Advance()
        {
            ++context.state;
            context.stateActiveTime = 0;
        }

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
                if(context.subContext == null)
                {
                    context.subContext = CreateSubContext(context, context.spell.SubSpells[context.currentSubspell]);
                }

                Debug.Log($"{context.spell.Name} cast sub spells");
                while (ManageSubContext(context, context.subContext)) ;

                if (context.subContext.aborted == true)
                {
                    if((context.spell.Flags & Spell.SpellFlags.BreakOnFailedTargeting)
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
        return true;
    }

    private bool FindTarget(SubSpellContext subContext) => throw new NotImplementedException();

    //public void CastSpell(Spell spell, Vector3 targetPosition)
    //{
    //    Assert.IsNotNull(spell);

    //    switch (spell.SpellType)
    //    {
    //        case SpellTypes.Raycast:
    //        case SpellTypes.Projectile:
    //        case SpellTypes.Status:
    //            CastTargetableSpell(spell, GetTarget(spell, targetPosition));
    //            break;
    //        case SpellTypes.Aoe:
    //            CastAoeSpell(spell, targetPosition);
    //            break;
    //        default:
    //            Debug.LogAssertion($"Unhandled SpellType {spell.SpellType}");
    //            break;
    //    }
    //}

    //public void CastSpell(SubSpell spell, Transform target)
    //{
    //    Assert.IsNotNull(spell);
    //    Assert.IsNotNull(target);

    //    switch (spell.SpellType)
    //    {
    //        case SpellTypes.Raycast:
    //        case SpellTypes.Projectile:
    //        case SpellTypes.Status:
    //            CastTargetableSpell(spell, target);
    //            break;
    //        case SpellTypes.Aoe:
    //            CastAoeSpell(spell, target.transform.position);
    //            break;
    //        default:
    //            Debug.LogAssertion($"Unhandled SpellType {spell.SpellType}");
    //            break;
    //    }
    //}

    //private Transform GetTarget(SubSpell spell, Vector3 targetPosition)
    //{
    //    var availibleTargets = GetFilteredCharacters(_owner, spell.AffectedTargets);
    //    if (availibleTargets.Length == 0)
    //        return null;

    //    return availibleTargets.OrderBy(t => (t.transform.position - targetPosition).magnitude).First().transform;
    //}

    //private void CastTargetableSpell(SubSpell spell, Transform target)
    //{
    //    var availibleTargets = GetFilteredCharacters(_owner, spell.AffectedTargets);

    //    switch (spell.SpellType)
    //    {
    //        case SpellTypes.Raycast:
    //        {
    //            var ray = new Ray(_owner.transform.position, target.transform.position);
    //            availibleTargets = availibleTargets.Where(t =>
    //            {
    //                var collider = t.GetComponent<Collider>();
    //                if (collider == null)
    //                    return false;

    //                return collider.Raycast(ray, out var hit, MaxSpellDistance);
    //            }).ToArray();
    //        }
    //        break;

    //        case SpellTypes.Projectile:
    //        {
    //            float maxDist = MaxSpellDistance;
    //            var ray = new Ray(_owner.transform.position, target.transform.position);

    //            CharacterState hitTarget = null;
    //            foreach (var t in availibleTargets)
    //            {
    //                var collider = t.GetComponent<Collider>();
    //                if (collider == null)
    //                    continue;

    //                if (!collider.Raycast(ray, out var hit, maxDist))
    //                    continue;

    //                if (maxDist > hit.distance)
    //                {
    //                    maxDist = hit.distance;
    //                    hitTarget = t;
    //                }
    //            }

    //            availibleTargets = new[] { hitTarget };
    //        }
    //        break;

    //        case SpellTypes.Status:
    //            Debug.Assert(availibleTargets.Length <= 1);
    //            break;

    //        default:
    //            Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
    //            return;
    //    }

    //    ApplySpell(spell, availibleTargets);
    //}

    //internal void DrawSpellGizmos(SubSpell spell, Vector3 target)
    //{
    //    Gizmos.DrawSphere(target, 0.2f);

    //    var targetObject = GetTarget(spell, target);
    //    if (targetObject == null)
    //        return;
    //    Gizmos.DrawWireCube(targetObject.transform.position, Vector3.one);
    //}

    //private void CastAoeSpell(SubSpell spell, Vector3 targetPosition)
    //{
    //    switch (spell.SpellType)
    //    {
    //        case SpellTypes.Aoe:
    //            var availibleTargets = GetFilteredCharacters(_owner, spell.AffectedTargets);
    //            availibleTargets = GetAllCharacterInArea(availibleTargets, targetPosition, spell.Area);
    //            ApplySpell(spell, availibleTargets);
    //            break;
    //        default:
    //            Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
    //            break;
    //    }

    //}

    //public CharacterState[] GetAllCharacterInArea(CharacterState[] characters, Vector3 position, AreaOfEffect area)
    //{
    //    foreach (var character in characters)
    //    {
    //        switch (area.Area)
    //        {
    //            case AreaOfEffect.AreaType.Conus:
    //                var direction = position - _owner.transform.position;
    //                return characters.Where(t => Vector3.Angle(direction, (t.transform.position - _owner.transform.position)) < area.Size).ToArray();

    //            case AreaOfEffect.AreaType.Sphere:
    //                return characters.Where(t => ((t.transform.position - position).magnitude < area.Size)).ToArray();

    //            case AreaOfEffect.AreaType.Cylinder:
    //                var ray = new Ray(_owner.transform.position, position);
    //                return characters.Where(t => Vector3.Cross(ray.direction, t.transform.position - ray.origin)
    //                    .magnitude < area.Size).ToArray();

    //            default:
    //                Debug.LogAssertion($"Unhandled AreaType {area.Area}");
    //                break;
    //        }
    //    }
    //    return null;
    //}

    //private void ApplySpell(SubSpell spell, CharacterState[] availibleTargets)
    //{
    //    foreach (var target in availibleTargets)
    //        target.ApplySpell(_owner, spell);
    //}

    //private static CharacterState[] GetAllCharacters()
    //{
    //    return FindObjectsOfType<CharacterState>().ToArray();
    //}

    //private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters, SpellTargets target) =>
    //    characters.Where(c =>
    //    {
    //        bool sameTeam = c.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != Team.AgainstTheWorld;
    //        var mask = sameTeam ? SpellTargets.Friend : SpellTargets.Enemy;
    //        if (c == owner)
    //            mask |= SpellTargets.Self;

    //        return (mask & target) == target;
    //    }).ToArray();


    //private static CharacterState[] GetFilteredCharacters(CharacterState owner, SpellTargets target) =>
    //    FilterCharacters(owner, GetAllCharacters(), target);
}
