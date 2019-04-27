using UnityEngine;
using UnityEditor;
using System;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();
    private Transform transform;

    // Use this for initialization
    void Awake()
    {
    }
    

    void Start()
    {
        transform = GetComponent<Transform>();
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
    }


    void Move()
    {
        transform.position += moveDirection.normalized * maxSpeed * Time.deltaTime;
    }
}
