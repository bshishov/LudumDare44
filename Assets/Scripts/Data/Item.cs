using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Item", menuName = "Mechanics/Item")]
    public class Item : ScriptableObject
    {
        public string Name;
        public int Cost;
        public Sprite Icon;

        [SerializeField]
        public Buff[] Buffs;

        public GameObject Prefab;
    }
}
