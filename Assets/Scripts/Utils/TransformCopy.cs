using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class TransformCopy :  MonoBehaviour
    {
        public Transform Target;
        public Vector3 PositionOffset;
        public Quaternion RotationOffset;

        void Update()
        {
            transform.position = Target.position + PositionOffset;
            transform.rotation = Target.rotation * RotationOffset;
        }

        [ContextMenu("Bake Offset")]
        public void BakeOffset()
        {
            PositionOffset = transform.position - Target.position;
            RotationOffset = transform.rotation * Quaternion.Inverse(Target.rotation);
        }

        [ContextMenu("Reset Offset")]
        public void ResetOffset()
        {
            PositionOffset = Vector3.zero;
            RotationOffset = Quaternion.identity;
        }
    }
}
