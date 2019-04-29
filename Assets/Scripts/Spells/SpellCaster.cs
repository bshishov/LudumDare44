using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
    public struct SpellEmitterData
    {
        public CharacterState owner;
        public Transform SourceTransform;
        public Ray ray;
        public Vector3 floorIntercection;
        public RaycastHit hitInfo;
    }

    public class SubSpellContext
    {
        public bool aborted;
        public bool projectileSpawned;

        public float activeTime;

        public object customData;

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

        public int currentSubspell;
        public ISpellEffect effect;
        public SpellEmitterData emitterData;

        public CharacterState[] filteredTargets;

        public float frameTime;

        public Spell spell;

        public float startTime;
        public ContextState state;
        public float stateActiveTime;
        public SubSpellContext subContext;

        public List<SubSpellTargets> subSpellTargets;

        public SubSpell GetCurrentSubSpell()
        {
            return spell.SubSpells[currentSubspell];
        }

        public static SpellContext Create(Spell spell, SpellEmitterData data, int subSpellStartIndex)
        {
            var context = new SpellContext
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

        private CharacterState _owner;
        public float MaxSpellDistance = 100.0f;

        // Start is called before the first frame update
        private void Start()
        {
            _owner = GetComponent<CharacterState>();
        }
        
        public void CastSpell(Spell spell, SpellEmitterData data, int subSpellStartIndex = 0, bool ignoreQueue = false)
        {
            if (_context != null)
            {
                Debug.LogError($"spell cast aready casting, {_context.spell.Name}");
                return;
            }

            _context = SpellContext.Create(spell, data, subSpellStartIndex);
        }

        private void Update()
        {
            if (_context == null)
                return;

            _context.frameTime = Time.deltaTime;
            _context.activeTime += _context.frameTime;
            _context.stateActiveTime += _context.frameTime;

            while (ManageContext(_context)) ;

            if (_context.aborted) Debug.Log($"{_context.spell.Name} aborted");

            if (_context.state != ContextState.Finishing)
                return;

            _context = null;
        }

        private bool ManageContext(SpellContext context)
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

                    context.subSpellTargets.Add(new SubSpellTargets
                    {
                        targetData = new List<PerSourceTargets>
                        {
                            new PerSourceTargets
                            {
                                source = context.emitterData.owner
                            }
                        }
                    });
                    context.subContext = SubSpellContext.Create(context);

                    Debug.Log($"{context.spell.Name} cast sub spells");

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

        private bool ManageSubContext(SpellContext context, SubSpellContext subContext)
        {
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
                        subContext.aborted = true;
                    }
                    else
                    {
                        ApplySubSpell(context, subContext);
                        Debug.Log($"{context.spell.Name} Executed subspell {context.currentSubspell}");
                    }

                    Advance();
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
                    subContext.state = ContextState.PreDelays;

                    var doneCasting = !(context.currentSubspell < context.spell.SubSpells.Length
                                        && subContext.aborted == false
                                        && subContext.projectileSpawned == false);

                    if (!doneCasting)
                    {
                        subContext.state = ContextState.JustQueued;
                        subContext.stateActiveTime = 0;
                    }

                    return !doneCasting;
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
            var currentTargets = context.subSpellTargets[context.currentSubspell];
            var newTargets = new SubSpellTargets {targetData = new List<PerSourceTargets>()};

            context.effect.OnSubSpellStartCast(context.spell, context.GetCurrentSubSpell(), currentTargets);

            foreach (var data in currentTargets.targetData)
            {
                if (data.destinations == null)
                    continue;

                foreach (var src in data.destinations)
                {
                    src.ApplySpell(context.emitterData.owner, context.GetCurrentSubSpell());

                    newTargets.targetData.Add(new PerSourceTargets {source = src});
                }
            }

            context.subSpellTargets.Add(newTargets);
        }

        private bool Execute(SpellContext context, SubSpellContext subContext)
        {
            var anyTargetFound = false;
            var currentTargets = context.subSpellTargets[context.currentSubspell];

            foreach (var pair in currentTargets.targetData)
            {
                var targeting = new TargetingData
                {
                    owner = pair.source,
                    origin = GetOrigin(pair.source, context, subContext)
                };

                if ((context.GetCurrentSubSpell().Targeting & SubSpell.SpellTargeting.Location) == SubSpell.SpellTargeting.Location)
                    targeting.targetLocation = context.emitterData.floorIntercection;

                if ((context.spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) == 0 || context.filteredTargets == null)
                    context.filteredTargets = GetFilteredCharacters(context.emitterData.owner, targeting.owner,
                        context.GetCurrentSubSpell().AffectedTarget);

                if ((context.GetCurrentSubSpell().Targeting & SubSpell.SpellTargeting.Target) == SubSpell.SpellTargeting.Target)
                {
                    if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.SelfTarget) == SubSpell.SpellFlags.SelfTarget)
                        targeting.targetCharacter = targeting.owner;
                    else if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.ClosestTarget) == SubSpell.SpellFlags.ClosestTarget)
                        targeting.targetCharacter = context.filteredTargets
                            .OrderBy(t => (t.transform.position - targeting.origin).magnitude).FirstOrDefault();
                    else if (context.emitterData.hitInfo.collider != null)
                        targeting.targetCharacter = context.emitterData.hitInfo.collider.GetComponent<CharacterState>();
                }

                if (targeting.targetCharacter == null && targeting.targetLocation == null)
                    Debug.LogError("No targets for spell!");

                if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.Projectile) == SubSpell.SpellFlags.Projectile)
                {
                    SpawnProjectile(targeting, context);
                    return true;
                }

                CharacterState[] targets = null;
                if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.Raycast) == SubSpell.SpellFlags.Raycast)
                    targets = GetAllCharacterInArea(context.filteredTargets, targeting, context);

                if (targets != null && targets.Length != 0)
                {
                    anyTargetFound = true;
                    pair.destinations = targets;

                    if ((context.spell.Flags & Spell.SpellFlags.AffectsOnlyOnce) == Spell.SpellFlags.AffectsOnlyOnce)
                        context.filteredTargets = context.filteredTargets.Except(targets).ToArray();
                }
            }

            return anyTargetFound;
        }

        private void SpawnProjectile(TargetingData targeting, SpellContext context)
        {
            Vector3 target = Vector3.one;
            if (targeting.targetCharacter != null)
            {
                target = targeting.targetCharacter.transform.position;
            }
            else if (targeting.targetLocation.HasValue)
            {
                target = targeting.targetLocation.Value;
            }
            else
            {
                Debug.Log("NO target for particle");
                return;
            }

            var projectileContext = new ProjectileContext
            {
                owner = context.emitterData.owner,
                projectileData = context.GetCurrentSubSpell().Projectile,

                spell = context.spell,
                startSubContext = context.currentSubspell,

                targetCharacter = targeting.targetCharacter,
                target = target,
                origin = targeting.origin
            };

            var projectilePrefab = Instantiate(new GameObject(), targeting.origin, Quaternion.identity);
            var projectileData = projectilePrefab.AddComponent<ProjectileBehaviour>();
            Instantiate(context.GetCurrentSubSpell().Projectile.ProjectilePrefab, projectilePrefab.transform);

            projectileData.Initialize(projectileContext, this);

            context.subContext.projectileSpawned = true;
        }

        private static Vector3 GetOrigin(CharacterState owner, SpellContext context, SubSpellContext subContext)
        {
            switch (context.GetCurrentSubSpell().Origin)
            {
                case SubSpell.SpellOrigin.Self:
                    return owner.transform.position;
                case SubSpell.SpellOrigin.Cursor:
                    Assert.IsTrue(context.currentSubspell == 0);
                    return context.emitterData.floorIntercection;
            }

            throw new InvalidOperationException("GetOrigin unhandled!");
        }

        private static Vector3 GetDirection(CharacterState owner, SpellContext context, SubSpellContext subContext)
        {
            if ((context.GetCurrentSubSpell().Flags & SubSpell.SpellFlags.HaveDirection) == SubSpell.SpellFlags.HaveDirection)
            {
                if (context.currentSubspell == 0)
                    return context.emitterData.ray.direction;
                return owner.transform.forward;
            }

            return Vector3.one;
        }

        private static CharacterState[] GetAllCharacters()
        {
            return FindObjectsOfType<CharacterState>().ToArray();
        }

        private static CharacterState[] GetFilteredCharacters(CharacterState owner, CharacterState source,
            SubSpell.AffectedTargets target)
        {
            var characters = FilterCharacters(owner, GetAllCharacters(), target);
            if ((target & SubSpell.AffectedTargets.Self) == 0) characters = characters.Where(t => t != source).ToArray();

            return characters;
        }

        private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters,
            SubSpell.AffectedTargets target)
        {
            return characters.Where(c =>
            {
                var sameTeam = c.CurrentTeam == owner.CurrentTeam &&
                               owner.CurrentTeam != CharacterState.Team.AgainstTheWorld;
                var mask = sameTeam ? SubSpell.AffectedTargets.Friend : SubSpell.AffectedTargets.Enemy;
                if (c == owner)
                    mask |= SubSpell.AffectedTargets.Self;

                return (mask & target) == target;
            }).ToArray();
        }

        private static CharacterState[] GetAllCharacterInArea(CharacterState[] characters, TargetingData targeting,
            SpellContext context)
        {
            foreach (var character in characters)
                switch (context.GetCurrentSubSpell().Area.Area)
                {
                    case AreaOfEffect.AreaType.Ray:
                    {
                        if (targeting.targetCharacter != null)
                            if (context.GetCurrentSubSpell().Obstacles == SubSpell.ObstacleHandling.Break)
                                return new[] {targeting.targetCharacter};

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
                        Debug.LogAssertion($"Unhandled AreaType {context.GetCurrentSubSpell().Area.Area}");
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