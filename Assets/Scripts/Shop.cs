using System;
using Actors;
using Assets.Scripts;
using Data;
using TMPro;
using UnityEngine;
using Utils;

public class Shop : MonoBehaviour, IInteractable
{
    public enum ShopState
    {
        None = 0,
        WithItem = 1,
        Sold = 2
    }

    [Serializable]
    public class ItemEntry
    {
        public Item Item;
        public float Weight = 1f;
    }

    public ShopState State = ShopState.None;
    public Item ActiveItem;
        
    public float CurrentPrice { get; private set; }
    public Action<Shop, Item> ItemBought;

    [Header("For manual (without shopgroup)")]
    public ItemEntry[] ItemPool;
    public bool RestockOnStart = true;

    [Header("Visuals")]
    public TextMeshPro Text;
    public Transform PlaceTransform;
    public GameObject BuyEffect;
    public GameObject RestockEffect;

    private GameObject _itemObject;

    void Start()
    {
        if(ItemPool == null || ItemPool.Length == 0)
            Debug.LogWarning("ItemPool for Shop is empty");

        if (PlaceTransform == null)
            PlaceTransform = transform;

        Text.text = string.Empty;

        if (ActiveItem != null && ActiveItem.Prefab != null)
            Restock(ActiveItem);
            
        if (RestockOnStart)
        {
            var entry = RandomUtils.Choice(ItemPool, e => e.Weight);
            Restock(entry.Item);
        }
    }
    
    void Update()
    {
    }

    public void Restock(Item item)
    {
        if(State == ShopState.WithItem)
            RemoveItemAndClose();

        ActiveItem = item;
        CurrentPrice = item.Cost;
        State = ShopState.WithItem;
        Text.text = CurrentPrice.ToString();

        if (ActiveItem != null && ActiveItem.Prefab != null)
        {
            _itemObject = GameObject.Instantiate(
                ActiveItem.Prefab,
                PlaceTransform,
                false);
        }

        if(RestockEffect != null)
            GameObject.Instantiate(RestockEffect, PlaceTransform.position, Quaternion.identity);
    }

    public void RemoveItemAndClose()
    {
        ActiveItem = null;
        State = ShopState.Sold;
        if(_itemObject != null)
            Destroy(_itemObject);
        Text.text = string.Empty;
    }

    public void Buy(CharacterState character)
    {
        if (State!= ShopState.WithItem)
        {
            Debug.Log("Buying from inactive shop");
            return;
        }

        if (character.SpendCurrency(CurrentPrice))
        {
            // TODO: Add stacks from shop
            character.Pickup(ActiveItem, 1);
            ItemBought?.Invoke(this, ActiveItem);
            RemoveItemAndClose();

            if (BuyEffect != null)
                GameObject.Instantiate(BuyEffect, PlaceTransform.position, Quaternion.identity);

            CameraController.Instance.Shake(0.4f);
        }
    }

    public bool IsActive => ActiveItem != null;
    public InteractableType Type => InteractableType.Shop;

    public void Interact(CharacterState character, Interaction interaction)
    {
        if (interaction == Interaction.Buy)
        {
            Buy(character);
        }
    }

    public string GetInteractionText(Interaction interaction)
    {
        if (ActiveItem == null)
            return string.Empty;
        switch (interaction)
        {
            case Interaction.Buy:
                return $"Buy {ActiveItem.Name}";
            default:
                return String.Empty;
        }
    }
}