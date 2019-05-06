using System;
using Spells;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells.Effects {
[ExecuteInEditMode]
public class ExplosionSpellEffect : MonoBehaviour, ISubSpellEffect
{

    public GameObject   ExplosionPrefab;
    public EffectOrigin Origin;
    public bool         StartEffectOnPreSelected;

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets)
    {
        if (StartEffectOnPreSelected)
            SpawnEffect(targets);
    }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        if (!StartEffectOnPreSelected)
            SpawnEffect(targets);
    }

    public void OnEndSubSpell(SpellContext context) {  }

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
        Destroy(Instantiate(ExplosionPrefab, target.Position.Value, Quaternion.identity), 2);
    }

    [MenuItem("Assets/Create/Effect Wrappers/Explosion", false, 1)]
    public static void ExplosionSpellEffect1() { ScriptableObjectUtility.CreateAsset<ExplosionSpellEffect>(); }
}
}