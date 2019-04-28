using System;
using UnityEngine;

[ExecuteInEditMode]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 lookAtOffset = Vector3.zero;

    public Vector3 offset = new Vector3(0, 10, - 20);

    private PlayerController _playerController;

    void Start()
    {

    }

    void SetCameraTarget(Transform t)
    {
        if (t != null)
            return;

        _playerController = target.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        if(target != null)
            Move();
    }

    private void Move()
    {
        var dest = target.position + offset;
        transform.position = dest;

        transform.LookAt(target.position + lookAtOffset);
    }
}