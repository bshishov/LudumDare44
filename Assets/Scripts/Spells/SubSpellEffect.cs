using Actors;
using Spells.Effects;
using UnityEngine;

namespace Spells
{
    /// <summary>
    /// Singleton-like object that handles sub spell effects
    /// </summary>
    [CreateAssetMenu(fileName = "SubSpellEffect_", menuName = "Spells/Sub Spell Effect")]
    public class SubSpellEffect : ScriptableObject, ISpellEffectHandler
    {
        public enum EffectTarget
        {
            EventOrigin,
            SubSpellSource,
            SubSpellTarget,
            OriginalSpellSource,
            OriginalSpellTarget,
            QueriedTarget
        }
        
        public enum RotationControls
        {
            DontRotate,
            SameAsObjectAttachedTo,
            LookTowardsRelative,
            LookAwayFromRelative,
        }
        
        public SubSpellEvent SpawnEvent;
        public GameObject Prefab;
        
        [Header("Positioning")]
        public EffectTarget SpawnOrigin;
        public CharacterState.NodeRole PreferredSpawnNode;
        public bool IsAttached;
        public bool OnGround;

        [Header("Rotation")] 
        public RotationControls RotationMode; // ENUM
        public bool XzRotationOnly;

        [Header("Lifecycle")] 
        public bool AutoDestroyAfterLifeTime;
        public float LifeTime;
        public bool AutoDestroyAfterSubSpell;
        
        public void OnEvent(SubSpellEventArgs args)
        {
            // TODO: create ECS entity here
        }
    }
}