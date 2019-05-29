using Actors;
using Spells;
using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(SpellbookState))]
    public class SpellsPlaygroundCharacter : MonoBehaviour
    {
        private SpellbookState _spellbookState;

        private Target _target;
        public  float      Interval = 2.0f;
        public  Spell      SpellToCast;

        public CharacterState Target;

        public Target GetNewTarget() { return _target; }

        private void Start()
        {
            _spellbookState = GetComponent<SpellbookState>();
            _spellbookState.PlaceSpell(SpellToCast, 1);
            _target = new Target(Target);
            InvokeRepeating(nameof(CastSpell), 2.0f, Interval);
        }

        private void CastSpell()
        {
            _spellbookState.TryFireSpellToTarget((int) SpellToCast.DefaultSlot, _target);
        }
    }
}