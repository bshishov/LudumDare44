using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Utils;

namespace Spells.Effects
{
    public class ExplosionSpellEffect : MonoBehaviour, ISubSpellEffect
    {
        public GameObject   ExplosionPrefab;
        public EffectOrigin Origin;
        public bool         SpawnInGround;
        public bool AlignRotation;
        public bool XZRotationOnly = true;

        [Header("LifeCycle")]
        public bool AutoDestroy;
        public float DestroyAfter = 2f;

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
                    SpawnEffect(targets.Source, Quaternion.identity);
                    break;
                case EffectOrigin.InDestination:
                    foreach (var destination in targets.Destinations)
                        SpawnEffect(destination, GetRotation(targets.Source, destination));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Quaternion GetRotation(TargetInfo source, TargetInfo target)
        {
            if(!AlignRotation)
                return Quaternion.identity;

            var dir = target.Position.Value - source.Position.Value;
            if (XZRotationOnly)
                dir.y = 0;

            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void SpawnEffect(TargetInfo target, Quaternion rotation)
        {
            Assert.IsTrue(target.Position.HasValue, "targets.Source.Position != null");
            var position = target.Position.Value;
            if (SpawnInGround)
                if (Physics.Raycast(position, Vector3.down, out var hitInfo, 2.0f, Common.LayerMasks.ActorsOrGround))
                    position = hitInfo.point;

            var instance = Instantiate(ExplosionPrefab, position, rotation);
            if(AutoDestroy)
                Destroy(instance, DestroyAfter);
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Effect Wrappers/Explosion", false, 1)]
        public static void ExplosionSpellEffect1() { AssetUtility.CreateAsset<ExplosionSpellEffect>(); }
#endif
    }
}