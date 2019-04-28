using Assets.Scripts.Data;
using System.Linq;
using UnityEngine;
using static Assets.Scripts.Data.Spell;
using static CharacterState;

public class SpellCaster : MonoBehaviour
{
    private CharacterState _owner;
    public float MaxSpellDistance = 100.0f;

    // Start is called before the first frame update
    private void Start() => _owner = GetComponent<CharacterState>();

    public void CastSpell(Spell spell, Vector3 targetPosition)
    {
        switch (spell.SpellType)
        {
            case SpellTypes.Raycast:
            case SpellTypes.Projectile:
            case SpellTypes.Status:
                CastTargetableSpell(spell, GetTarget(spell, targetPosition));
                break;
            case SpellTypes.Aoe:
                CastAoeSpell(spell, targetPosition);
                break;
            default:
                Debug.LogAssertion($"Unhandled SpellType {spell.SpellType}");
                break;
        }
    }

    public void CastSpell(Spell spell, Transform target)
    {
        switch (spell.SpellType)
        {
            case SpellTypes.Raycast:
            case SpellTypes.Projectile:
            case SpellTypes.Status:
                CastTargetableSpell(spell, target);
                break;
            case SpellTypes.Aoe:
                CastAoeSpell(spell, target.transform.position);
                break;
            default:
                Debug.LogAssertion($"Unhandled SpellType {spell.SpellType}");
                break;
        }
    }


    private Transform GetTarget(Spell spell, Vector3 targetPosition)
    {
        var availibleTargets = GetFilteredCharacters(_owner, spell.SpellTarget);
        if (availibleTargets.Length == 0)
            return null;

        return availibleTargets.OrderBy(t => (t.transform.position - targetPosition).magnitude).First().transform;
    }

    private void CastTargetableSpell(Spell spell, Transform target)
    {
        var availibleTargets = GetFilteredCharacters(_owner, spell.SpellTarget);

        switch (spell.SpellType)
        {
            case SpellTypes.Raycast:
            {
                var ray = new Ray(_owner.transform.position, target.transform.position);
                availibleTargets = availibleTargets.Where(t =>
                {
                    var collider = t.GetComponent<Collider>();
                    if (collider == null)
                        return false;

                    return collider.Raycast(ray, out var hit, MaxSpellDistance);
                }).ToArray();
            }
            break;

            case SpellTypes.Projectile:
            {
                float maxDist = MaxSpellDistance;
                var ray = new Ray(_owner.transform.position, target.transform.position);

                CharacterState hitTarget = null;
                foreach (var t in availibleTargets)
                {
                    var collider = t.GetComponent<Collider>();
                    if (collider == null)
                        continue;

                    if (!collider.Raycast(ray, out var hit, maxDist))
                        continue;

                    if (maxDist > hit.distance)
                    {
                        maxDist = hit.distance;
                        hitTarget = t;
                    }
                }

                availibleTargets = new[] { hitTarget };
            }
            break;

            case SpellTypes.Status:
                Debug.Assert(availibleTargets.Length <= 1);
                break;

            default:
                Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
                return;
        }

        ApplySpell(spell, availibleTargets);
    }

    private void CastAoeSpell(Spell spell, Vector3 targetPosition)
    {
        switch (spell.SpellType)
        {
            case SpellTypes.Aoe:
                var availibleTargets = GetFilteredCharacters(_owner, spell.SpellTarget);
                availibleTargets = GetAllCharacterInArea(availibleTargets, targetPosition, spell.Area);
                ApplySpell(spell, availibleTargets);
                break;
            default:
                Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
                break;
        }

    }

    public CharacterState[] GetAllCharacterInArea(CharacterState[] characters, Vector3 position, AreaOfEffect area)
    {
        foreach (var character in characters)
        {
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Conus:
                    var direction = position - _owner.transform.position;
                    return characters.Where(t => Vector3.Angle(direction, (t.transform.position - _owner.transform.position)) < area.Size).ToArray();

                case AreaOfEffect.AreaType.Sphere:
                    return characters.Where(t => ((t.transform.position - position).magnitude < area.Size)).ToArray();

                case AreaOfEffect.AreaType.Cylinder:
                    var ray = new Ray(_owner.transform.position, position);
                    return characters.Where(t => Vector3.Cross(ray.direction, t.transform.position - ray.origin)
                        .magnitude < area.Size).ToArray();

                default:
                    Debug.LogAssertion($"Unhandled AreaType {area.Area}");
                    break;
            }
        }
        return null;
    }

    private void ApplySpell(Spell spell, CharacterState[] availibleTargets)
    {
        foreach (var target in availibleTargets)
            target.ApplySpell(_owner, spell);
    }

    private static CharacterState[] GetAllCharacters()
    {
        var actors = GameObject.FindGameObjectsWithTag("Actors");
        return actors.Select(a => a.GetComponent<CharacterState>()).Where(a => a != null).ToArray();
    }

    private static CharacterState[] FilterCharacters(CharacterState owner, CharacterState[] characters, SpellTargets target) =>
        characters.Where(c =>
        {
            bool sameTeam = c.CurrentTeam == owner.CurrentTeam && owner.CurrentTeam != Team.AgainstTheWorld;
            var mask = sameTeam ? SpellTargets.Friend : SpellTargets.Enemy;
            if (c == owner)
                mask |= SpellTargets.Self;

            return (mask & target) == target;
        }).ToArray();


    private static CharacterState[] GetFilteredCharacters(CharacterState owner, SpellTargets target) =>
        FilterCharacters(owner, GetAllCharacters(), target);
}
