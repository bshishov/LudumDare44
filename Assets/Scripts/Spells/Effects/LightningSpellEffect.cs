using Assets.Scripts.Data;
using UnityEngine;

namespace Spells.Effects
{
    public class LightningSpellEffect : MonoBehaviour, ISpellEffect
    {
        public GameObject LightningPrefab;

        void Start()
        {
            Destroy(gameObject, 1f);
        } 

        public void OnSpellStateChange(Spell spell, ContextState newState)
        {
            Debug.Log("Spell state change");
        }

        public void OnSubSpellStateChange(Spell spell, SubSpell subSpell, ContextState newSubState)
        {
            Debug.Log("SubSpell state change");
        }

        public void OnSubSpellStartCast(Spell spell, SubSpell subSpell, SubSpellTargets data)
        {
            Debug.Log("SubSpell Start Cast");
            foreach (var tgt in data.targetData)
            {
                foreach (var dst in tgt.destinations)
                {
                    var lObj = GameObject.Instantiate(LightningPrefab, transform);
                    lObj.GetComponent<Lightning>().SetupLine(
                        tgt.source.GetNodeTransform(CharacterState.CharacterNode.NodeRole.SpellEmitter).position, 
                        dst.GetNodeTransform(CharacterState.CharacterNode.NodeRole.Chest).position);
                }
            }
        }
    }
}
