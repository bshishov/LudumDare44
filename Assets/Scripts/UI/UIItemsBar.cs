using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Data;
using UnityEngine;

public class UIItemsBar : MonoBehaviour
{
    public GameObject UIItemPrefab;

    private CharacterState _playerState;
    private readonly List<UIItem> _items = new List<UIItem>();

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag(Common.Tags.Player);
        if (player != null)
        {
            _playerState = player.GetComponent<CharacterState>();
            if (_playerState == null)
            {
                Debug.LogWarning("PlayerState not found");
            }
        }

        if (_playerState != null)
        {

            _playerState.OnItemPickup += PlayerStateOnOnItemPickup;
            
        }
    }

    void UpdateItems()
    {
        foreach (var itemState in _playerState.Items)
        {
            var existing = _items.FirstOrDefault(i => i.Item.Equals(itemState.Item));
            if (existing != null)
            {
                existing.Setup(itemState.Item, itemState.Stacks);
            }
            else
            {
                var newItem = Instantiate(UIItemPrefab, transform, false).GetComponent<UIItem>();
                newItem.Setup(itemState.Item, itemState.Stacks);
                _items.Add(newItem);
            }
        }
    }

    private void PlayerStateOnOnItemPickup(Item item, int stacks)
    {
        UpdateItems();
    }
}
