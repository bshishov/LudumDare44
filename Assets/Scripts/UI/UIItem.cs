using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIItem : MonoBehaviour
    {
        public Image Icon;
        public TextMeshProUGUI StacksText;
        public Item Item;

        private Item _item;

        public void Setup(Item item, int stacks)
        {
            Item = item;
            Icon.sprite = item.Icon;
            StacksText.text = $"x{stacks:0}";
        }
    }
}
