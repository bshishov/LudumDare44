using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Item", menuName = "Mechanics/Item")]
    public class Item : ScriptableObject
    {
        [Header("Meta")]
        public string Name;
        [TextArea]
        public string Description;

        public int Cost;
        public Sprite Icon;

        [SerializeField]
        public Buff[] Buffs;

        public GameObject Prefab;
    }
}
