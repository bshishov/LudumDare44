using System;
using UnityEngine;

[ExecuteInEditMode]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 lookAtOffset = Vector3.zero;

    public Vector3 offset = new Vector3(0, 10, - 20);

    private PlayerController playerController;

    void Start()
    {

    }

    void SetCameraTarget(Transform t)
    {
        if (t != null)
            return;

        playerController = target.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        Move();
    }

    private void Move()
    {
        var dest = target.position + offset;
        transform.position = dest;

        transform.LookAt(target.position + lookAtOffset);
    }
}