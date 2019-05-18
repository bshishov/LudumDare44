using Actors;
using Assets.Scripts.Data;
using Data;
using Spells;
using UnityEngine;

[RequireComponent(typeof(SpellbookState))]
public class SpellsPlaygroundCharacter : MonoBehaviour, IChannelingInfo
{
    private SpellbookState _spellbookState;

    private TargetInfo _targetInfo;
    public  float      Interval = 2.0f;
    public  Spell      SpellToCast;

    public CharacterState Target;

    public TargetInfo GetNewTarget() { return _targetInfo; }

    private void Start()
    {
        _spellbookState = GetComponent<SpellbookState>();

        _spellbookState.PlaceSpell(SpellToCast, 1);

        _targetInfo = new TargetInfo
                      {
                          Character = Target,
                          Position  = Target.transform.position,
                          Transform = Target.GetNodeTransform(CharacterState.NodeRole.Chest)
                      };

        InvokeRepeating(nameof(CastSpell), 2.0f, Interval);
    }

    private void CastSpell() { _spellbookState.TryFireSpellToTarget((int) SpellToCast.DefaultSlot, _targetInfo, this); }
}