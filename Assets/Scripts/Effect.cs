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

    private Transform _target;
    
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
        if (_target != null)
            transform.position = _target.position;
    }

    public void Attach(Transform t)
    {
        _target = t;
    }
}
