using UnityEngine;
using UnityEditor;
using System;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();

    private Plane _ground;

    // Use this for initialization
    void Awake()
    {
    }
    

    void Start()
    {
        _ground = new Plane(Vector3.up, Vector3.zero);
    }

    void GetInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");

    }

    void Update()
    {
        GetInput();
        Move();
        LookAt();
    }

    void Move()
    {
        transform.position += moveDirection.normalized * maxSpeed * Time.deltaTime;
    }

    private void LookAt()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_ground.Raycast(ray, out var enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.LookAt(hitPoint);
        }
    }
}
