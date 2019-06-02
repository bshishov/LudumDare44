using UnityEngine;

namespace Utils
{
    [ExecuteInEditMode]
    public class EditorProjectileMover : MonoBehaviour
    {
        public GameObject Target;
        
        [Range(0, 20)]
        public float SpeedX = 1f;
        
        [Range(0, 20)]
        public float SpeedZ = 1f;

        [Range(0, 20f)]
        public float Range = 1f;

        public bool Enabled = true;

        private void Update()
        {
            if(Target == null || !Enabled)
                return;
            
            var x = Mathf.Cos(SpeedX * Time.time);
            var z = Mathf.Cos(SpeedZ * Time.time);
            Target.transform.position  = transform.position + new Vector3(x, 0, z) * Range;
        }


        [ContextMenu("Enable")]
        public void Enable()
        {
            Enabled = true;
        }

        [ContextMenu("Disable")]
        public void Disable()
        {
            Enabled = false;
        }
        
    }
}