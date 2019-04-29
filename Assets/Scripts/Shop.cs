using System;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class Shop : MonoBehaviour
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
        public Transform PlaceTransform;

        public ItemEntry[] ItemPool;

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
            }

            if (PlaceTransform)
                PlaceTransform = transform;

            if (ActiveItem != null && ActiveItem.Prefab != null)
            {
                _itemObject = GameObject.Instantiate(ActiveItem.Prefab, PlaceTransform);
            }
        }
    
        void Update()
        {
        }

        void DropItem()
        {
            ActiveItem = null;
            State = ShopState.Sold;
            if(_itemObject != null)
                Destroy(_itemObject);
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
                character.Pickup(ActiveItem);
                DropItem();
            }
            else
            {
                Debug.Log("Insufficient funds");
            }
        }
    }
}
