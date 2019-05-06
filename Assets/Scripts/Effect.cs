using Assets.Scripts.Utils.Sound;
using UnityEngine;

public class Effect : MonoBehaviour, IAttachable
{
    [Header("Camera shake")]
    [Range(0f, 1f)]
    public float CameraImpact = 0f;

    [Header("Sound")]
    public Sound Sound;

    [Header("Lifecycle")]
    public bool AutoDestroy = false;
    public float LifeTime = 1f;

    [Header("IAttachable")]
    public bool AttachPosition;
    public bool AttachRotation;

    private Transform _target;
    private Quaternion _initialRotation;

    void Start()
    {
        if(AutoDestroy)
            Destroy(gameObject, LifeTime);

        if (CameraImpact > 0)   
            CameraController.Instance.Shake(CameraImpact);

        SoundManager.Instance.Play(Sound, transform);
    }

    void Update()
    {
        if (_target == null)
            return;
        if (AttachPosition)
            transform.position = _target.position;
        if (AttachRotation)
        {
            var rotation                    = _target.rotation * _initialRotation;
            rotation.x         = rotation.z = 0;
            transform.rotation = rotation;
        }
    }

    public void Attach(Transform t)
    {
        _target = t;

        _initialRotation = transform.rotation;
        _initialRotation = Quaternion.Inverse(_initialRotation);
    }
}
