using System;
using System.Collections.Generic;
using Spells;
using UnityEngine;

namespace Assets.Scripts.Data
{
[CreateAssetMenu(fileName = "SubSpell", menuName = "Spells/SubSpell")]
[Serializable]
public class SubSpell : ScriptableObject
{
    [Serializable]
    [Flags]
    public enum AffectedTargets
    {
        Self   = 1 << 1,
        Ally = 1 << 2,
        Enemy  = 1 << 3
    }

    [Serializable]
    public enum NewSourceType
    {
        AffectedTarget     = 1,
        OriginalTargetData = 2
    }

    [Serializable]
    [Flags]
    public enum ObstacleHandling
    {
        None                              = 0,
        ExecuteSpellSequence              = 1 << 1,
        Break                             = 1 << 2,
        ExecuteSpellSequenceOnMaxDistance = 1 << 3,
        IgnoreButTarget                   = 1 << 4,
        IgnoreWorldCollision              = 1 << 5
    }

    [Serializable]
    [Flags]
    public enum SpellFlags
    {
        Channeling = 1 << 1,
        Raycast    = 1 << 2,
        Projectile = 1 << 3,
        SelfTarget = 1 << 4,
        ClosestTarget = 1 << 5,

        Special = SelfTarget | ClosestTarget
    }

    [Serializable]
    public enum SpellOrigin
    {
        None   = 0,
        Self   = 1,
        Cursor = 2
    }


    [Serializable]
    public enum SpellTargeting
    {
        None = 0,
        Target,
        Location
    }

    [Header("Targeting")]
    [EnumFlag]
    public AffectedTargets AffectedTarget;
    [Expandable]
    public AreaOfEffect Area;
    public SpellTargeting Targeting;
    public SpellOrigin Origin;


    [Header("Buffs")]
    [Expandable]
    public List<Buff> Buffs;
    public float BloodCost = 0f;

    [Header("FX")]
    public GameObject Effect;


    [Header("Behaviour")]
    [EnumFlag]
    public SpellFlags Flags;

    [EnumFlag]
    public ObstacleHandling Obstacles;

    public float PreCastDelay;
    public float PreDamageDelay;
    public float PostCastDelay;

    public ProjectileData Projectile;
    public NewSourceType  NewSource;

    private GameObject _effect = null;

    public ISubSpellEffect GetEffect()
    {
        if (Effect == null)
            return null;

        if (_effect == null)
            _effect = Instantiate(Effect);

        return _effect.GetComponent<ISubSpellEffect>();
    }
}
}