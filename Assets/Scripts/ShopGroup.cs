using Assets.Scripts;
using UnityEngine;

public class ShopGroup : MonoBehaviour
{
    public Shop[] Shops;

    [ContextMenu("Close all")]
    public void CloseAll()
    {
        foreach (var shop in Shops)
        {
            shop.RemoveItemAndClose();
        }
    }
}
