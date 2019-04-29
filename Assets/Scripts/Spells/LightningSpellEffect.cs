using Assets.Scripts.Data;
using UnityEngine;

namespace Assets.Scripts.Spells
{
    [RequireComponent(typeof(LineRenderer))]
    public class LightningSpellEffect : MonoBehaviour, ISpellEffect
    {
        public int Points = 10;
        private LineRenderer _renderer;

        public void Start()
        {
            _renderer = GetComponent<LineRenderer>();
            //SetupLine(transform.position, transform.position + Vector3.forward * 5f);
            Destroy(gameObject, 10f);
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
                    SetupLine(tgt.source.transform.position, dst.transform.position);
                }
            }
            
        }

        public void SetupLine(Vector3 from, Vector3 to)
        {
            var positions = new Vector3[Points];
            for (var i = 0; i < Points; i++)
            {
                positions[i] = Vector3.Lerp(from, to, i / (Points - 1f));
            }

            _renderer.positionCount = Points;
            _renderer.SetPositions(positions);
        }

        public void SetupLine(Transform from, Transform to)
        {
            SetupLine(from.position, to.position);
        }
    }
}
