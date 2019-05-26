using Actors;
using UnityEngine;
using Utils;

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
