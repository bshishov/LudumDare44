using Actors;
using Assets.Scripts.Utils;
using UnityEngine;

namespace UI
{
    public class UIHealthBarOverlay : Singleton<UIHealthBarOverlay>
    {
        public GameObject HealthBar;

        public void Add(CharacterState character)
        {
            var healthBar = Instantiate(HealthBar, transform, false).GetComponent<UIHealthBar>();
            if(healthBar != null)
                healthBar.Setup(character);
        }
    }
}
