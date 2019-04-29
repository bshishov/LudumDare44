using UnityEngine;

namespace Spells
{
    public class ProjectileBehaviour : MonoBehaviour
    {
        public ProjectileContext Conext;
        

        void Update()
        {
            if(Conext == null)
                Destroy(this);

            transform.position += transform.forward * 2;
        }
    }
}
