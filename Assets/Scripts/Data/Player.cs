using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Player", menuName = "Mechanics/Player")]
    public class Player : ScriptableObject
    {
        public float Health;        
        public float Speed;
    }
}