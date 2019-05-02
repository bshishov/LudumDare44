using System;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
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
        public TextMeshPro Text;
        public float CurrentPrice { get; private set; }
        public Transform PlaceTransform;
        public ItemEntry[] ItemPool;
        public GameObject BuyEffect;

        private GameObject _itemObject;

        void Start()
        {
            if(ItemPool == null || ItemPool.Length == 0)
                Debug.LogWarning("ItemPool for Shop is empty");


            if (State == ShopState.None)
            {
                var entry = RandomUtils.Choice(ItemPool, e => e.Weight);
                ActiveItem = entry.Item;
                CurrentPrice = entry.Item.Cost;
                State = ShopState.WithItem;
                Text.text = CurrentPrice.ToString();
            }

            if (PlaceTransform == null)
                PlaceTransform = transform;

            if (ActiveItem != null && ActiveItem.Prefab != null)
            {
                _itemObject = GameObject.Instantiate(ActiveItem.Prefab, 
                    PlaceTransform.position, Quaternion.identity);
            }
        }
    
        void Update()
        {
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
                RemoveItemAndClose();

                if (BuyEffect != null)
                    GameObject.Instantiate(BuyEffect, PlaceTransform.position, Quaternion.identity);

                var shopGroup = GetComponentInParent<ShopGroup>();
                if(shopGroup != null)
                    shopGroup.CloseAll();

                CameraController.Instance.Shake(0.4f);
            }
            else
            {
                Debug.Log("Insufficient funds");
            }
        }

        public InteractableType Type => InteractableType.Shop;

        public void Interact(CharacterState character, Interaction interaction)
        {
            if (interaction == Interaction.Buy)
                Buy(character);
        }
    }
}
