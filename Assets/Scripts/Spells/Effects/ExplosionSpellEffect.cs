using System;
using Spells;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class ExplosionSpellEffect : MonoBehaviour, ISubSpellEffect
{
    public enum ExplosionOrigin
    {
        InSource,
        InDestination
    }

    public GameObject      ExplosionPrefab;
    public ExplosionOrigin Origin;

    public void OnTargetsPreSelected(ISpellContext context, SpellTargets targets) { }

    public void OnTargetsAffected(ISpellContext context, SpellTargets targets)
    {
        switch (Origin)
        {
            case ExplosionOrigin.InSource:
                SpawnEffect(targets.Source);
                break;
            case ExplosionOrigin.InDestination:
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
