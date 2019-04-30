using System;
using Assets.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraController : MonoBehaviour
{
    [Header("Position & Movement")]
    public Vector3 TargetOffset;
    [Range(0f, 5f)]
    public float MoveTime = 0.3f;
    [Range(0f, 5f)]
    public float RotationTime = 0.3f;
    [Range(0f, 1f)]
    public float SecondaryTargetWeight = 0.3f;

    [Header("Camera Shake")]
    public Transform CameraTransform;
    public bool EnableCameraShake;

    [Range(0, 1f)]
    public float TraumaDecayStep = 0.02f;
    public Vector3 MaxPosOffset = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 MaxRotAngleOffset = new Vector3(5f, 5f, 5f);

    private Quaternion _targetRotation;
    private Vector3 _targetPosition;
    private Transform _target;
    private Transform _secondaryTarget;

    private Vector3 _velocity;
    private float _angularVelocity;
    private float _shakeTraumaLinear;

    void Start()
    {
        var t = GameObject.FindGameObjectWithTag(Tags.Player).transform;
        SetTarget(t);
        SetSecondaryTarget(t);
    }

    void Update()
    {
        if (_target != null)
        {
            var lookAt = _target.position;

            if (_secondaryTarget != null)
            {
                lookAt = Vector3.Lerp(_target.position, _secondaryTarget.position, SecondaryTargetWeight);
            }

            _targetPosition = _target.position + TargetOffset;
            _targetRotation = Quaternion.LookRotation(lookAt - transform.position);
        }

        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, MoveTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, RotationTime);

        if (EnableCameraShake)
            DoCameraShake();
    }

    private void DoCameraShake()
    {
        if (CameraTransform == null)
            return;

        // Camera shake
        _shakeTraumaLinear = Mathf.Clamp01(_shakeTraumaLinear - TraumaDecayStep);
        var shake = Mathf.Pow(_shakeTraumaLinear, 3);

        var rotAngles = new Vector3(
            MaxRotAngleOffset.x * Random.Range(-1f, 1f),
            MaxRotAngleOffset.y * shake * Random.Range(-1f, 1f),
            MaxRotAngleOffset.z * shake * Random.Range(-1f, 1f)) * shake;

        var posOffset = new Vector3(
                            MaxPosOffset.x * Random.Range(-1f, 1f),
                            MaxPosOffset.y * shake * Random.Range(-1f, 1f),
                            MaxPosOffset.z * shake * Random.Range(-1f, 1f)) * shake;

        CameraTransform.localPosition = posOffset;
        CameraTransform.localEulerAngles = rotAngles;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void SetSecondaryTarget(Transform target)
    {
        _secondaryTarget = target;
    }

    public void Shake(float trauma)
    {
        _shakeTraumaLinear = Mathf.Clamp01(_shakeTraumaLinear + trauma);
    }
}