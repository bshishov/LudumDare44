using Assets.Scripts.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpellCaster : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }


    public void CastSpell(Spell spell, Vector3 targetPosition)
    {
        switch (spell.SpellType)
        {
            case Spell.SpellTypes.Raycast:
            case Spell.SpellTypes.Projectile:
            case Spell.SpellTypes.Status:
                CastTargetableSpell(spell, GetTarget(targetPosition));
                break;
            case Spell.SpellTypes.Aoe:
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
            case Spell.SpellTypes.Raycast:
            case Spell.SpellTypes.Projectile:
            case Spell.SpellTypes.Status:
                CastTargetableSpell(spell, target);
                break;
            case Spell.SpellTypes.Aoe:
                CastAoeSpell(spell, target.transform.position);
                break;
            default:
                Debug.LogAssertion($"Unhandled SpellType {spell.SpellType}");
                break;
        }
    }


    private Transform GetTarget(Vector3 targetPosition)
    {
        return null;
    }

    private void CastTargetableSpell(Spell spell, Transform target)
    {
        switch (spell.SpellType)
        {
            case Spell.SpellTypes.Raycast:
            case Spell.SpellTypes.Projectile:
            case Spell.SpellTypes.Status:

                break;
            default:
                Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
                break;
        }
    }

    private void CastAoeSpell(Spell spell, Vector3 targetPosition)
    {
        switch (spell.SpellType)
        {
            case Spell.SpellTypes.Aoe:

                break;
            default:
                Debug.LogAssertion($"Invalid SpellType {spell.SpellType}");
                break;
        }

    }

    public CharacterParams[] GetAllCharacterInArea(CharacterParams[] characters, Vector3 position, AreaOfEffect area)
    {
        foreach (var character in characters)
        {
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Conus:
                    break;
                case AreaOfEffect.AreaType.Sphere:
                    break;
                case AreaOfEffect.AreaType.Cylinder:
                    break;
                default:
                    Debug.LogAssertion($"Unhandled AreaType {area.Area}");
                    break;
            }
        }
        return null;
    }

    private CharacterParams[] GetAllCharacters()
    {
        var actors = GameObject.FindGameObjectsWithTag("Actors");
        return actors.Select(a => a.GetComponent<CharacterParams>()).Where(a => a != null).ToArray();
    }
}
