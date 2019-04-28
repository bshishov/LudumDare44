using UnityEngine;
using UnityEditor;
using Assets.Scripts.Data;

public class ItemPickuper : MonoBehaviour
{
    private CharacterState _character;

    void Start()
    {
        _character = GetComponent<CharacterState>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var drop = other.gameObject.GetComponent<DroppedItem>();
        if(drop == null)
        {
            Debug.LogWarning("Unknown collision with no DroppedItem component");
            return;
        }

        var spell = drop.ContainingObject as Spell;
        var item = drop.ContainingObject as Item;

        if ((spell != null && item != null) || (item == null && spell == null))
        {
            Debug.LogWarning("Invalid content");
            return;
        }

        if (spell != null)
        {
            _character.Pickup(spell);
        }
        else if (item != null)
        {
            _character.Pickup(item);
        }
    }
}