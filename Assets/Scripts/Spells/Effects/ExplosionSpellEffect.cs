using System;
using UnityEngine;

namespace Spells.Effects
{
    public class ExplosionSpellEffect : MonoBehaviour, ISpellEffectHandler
    {
        public SubSpellEvent SpawnEvent;
        public GameObject   ExplosionPrefab;
        public EffectOrigin Origin;
        public bool         SpawnInGround;
        public bool AlignRotation;
        public bool XZRotationOnly = true;

        [Header("LifeCycle")]
        public bool AutoDestroy;
        public float DestroyAfter = 2f;

        private Quaternion GetRotation(Target source, Target target)
        {
            if(!AlignRotation)
                return Quaternion.identity;

            var dir = target.Position - source.Position;
            if (XZRotationOnly)
                dir.y = 0;

            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void SpawnEffect(Target target, Quaternion rotation)
        {
            var position = target.Position;
            if (SpawnInGround)
                if (Physics.Raycast(position, Vector3.down, out var hitInfo, 2.0f, Common.LayerMasks.ActorsOrGround))
                    position = hitInfo.point;

            var instance = Instantiate(ExplosionPrefab, position, rotation);
            if(AutoDestroy)
                Destroy(instance, DestroyAfter);
        }

        public void OnEvent(SubSpellEventArgs args)
        {
            // If it is not a spawn event -> do nothing
            if(args.Event != SpawnEvent)
                return;

            switch (Origin)
            {
                case EffectOrigin.InSource:
                    SpawnEffect(args.Handler.Source, Quaternion.identity);
                    break;
                case EffectOrigin.InTarget:
                    SpawnEffect(args.Handler.Target, Quaternion.identity);
                    break;
                case EffectOrigin.InOriginalSpellSource:
                    SpawnEffect(args.Handler.SpellHandler.Source, Quaternion.identity);
                    break;
                case EffectOrigin.InOriginalSpellCastTarget:
                    SpawnEffect(args.Handler.SpellHandler.CastTarget, Quaternion.identity);
                    break;
                case EffectOrigin.InEachQueriedTargets:
                    if(args.QueriedTargets == null)
                        break;
                    foreach (var target in args.QueriedTargets)
                        SpawnEffect(target, GetRotation(args.Handler.Source, target));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}