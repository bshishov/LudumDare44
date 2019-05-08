﻿using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Spells.Effects
{
public class ExplosionSpellEffect : MonoBehaviour, ISubSpellEffect
{
    public GameObject   ExplosionPrefab;
    public EffectOrigin Origin;
    public bool         SpawnInGround;

    [FormerlySerializedAs("StartEffectOnPreSelected")]
    public bool         StartEffectOnInputTargetValidated;

    public void OnInputTargetsValidated(ISpellContext context, SpellTargets targets)
    {
        if (StartEffectOnInputTargetValidated)
            SpawnEffect(targets);
        }

        public void OnTargetsFinalized(SpellContext context, SpellTargets castData) { }

        public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        if (!StartEffectOnInputTargetValidated)
            SpawnEffect(targets);
    }

    public void OnEndSubSpell(ISpellContext context) { }

    private void SpawnEffect(SpellTargets targets)
    {
        switch (Origin)
        {
            case EffectOrigin.InSource:
                SpawnEffect(targets.Source);
                break;
            case EffectOrigin.InDestination:
                foreach (var destination in targets.Destinations)
                    SpawnEffect(destination);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SpawnEffect(TargetInfo target)
    {
        Assert.IsTrue(target.Position.HasValue, "targets.Source.Position != null");
        var position = target.Position.Value;
        if (SpawnInGround)
            if (Physics.Raycast(position, Vector3.down, out var hitInfo, 2.0f, Common.LayerMasks.ActorsOrGround))
                position = hitInfo.point;
        Destroy(Instantiate(ExplosionPrefab, position, Quaternion.identity), 2);
    }

    [MenuItem("Assets/Create/Effect Wrappers/Explosion", false, 1)]
    public static void ExplosionSpellEffect1() { ScriptableObjectUtility.CreateAsset<ExplosionSpellEffect>(); }
}
}