using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using UnityEngine;

public class ShopGroup : MonoBehaviour
{
    public Shop[] Shops;
    public bool AutoRestock = false;
    public float RestockTimer = 120f;
    public float TimerValueAtStart = 120f;
    public Shop.ItemEntry[] ItemPool;
    public bool RestockOnStart = false;
    [Range(0f, 1f)]
    public float PreviousStockWeightMod = 0.2f;
    public bool CanBuyOnlyOne = true;

    [Header("FX")]
    public GameObject RestockEffect;
    public Renderer Renderer;
    public string ValueParameter = "_Value";

    private float _currentTimer;
    private readonly List<Item> _previousStock = new List<Item>();

    void Start()
    {
        _currentTimer = Mathf.Clamp(TimerValueAtStart, 0, RestockTimer);
        foreach (var shop in Shops)
        {
            shop.ItemBought += OnItemBought;
        }
    }

    private void OnItemBought(Shop shop, Item item)
    {
        if (CanBuyOnlyOne)
        {
            foreach (var s in Shops)
            {
                if(!shop.Equals(s))
                    s.RemoveItemAndClose();
            }
        }
    }

    void Update()
    {
        if (AutoRestock)
        {
            _currentTimer -= Time.deltaTime;
            if (_currentTimer < 0)
            {
                RestockAll();
                _currentTimer = RestockTimer;
            }

            if (Renderer != null)
                Renderer.material.SetFloat(ValueParameter, 1 - Mathf.Clamp01(_currentTimer / RestockTimer));
        }
    }

    [ContextMenu("Close all")]
    public void CloseAll()
    {
        foreach (var shop in Shops)
            shop.RemoveItemAndClose();
    }

    [ContextMenu("Restock all")]
    public void RestockAll()
    {
        foreach (var shop in Shops)
        {
            var entry = RandomUtils.Choice(ItemPool, e =>
            {
                if (_previousStock.Contains(e.Item))
                    return e.Weight * PreviousStockWeightMod;
                return e.Weight;
            });
            shop.Restock(entry.Item);
        }

        _previousStock.Clear();

        foreach (var shop in Shops)
        {
            _previousStock.Add(shop.ActiveItem);
        }

        if (RestockEffect != null)
            GameObject.Instantiate(RestockEffect, transform.position, Quaternion.identity);
    }
}
