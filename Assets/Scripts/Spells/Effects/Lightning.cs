﻿using UnityEngine;

namespace Spells.Effects
{
    [RequireComponent(typeof(LineRenderer))]
    public class Lightning : MonoBehaviour, IRayEffect
    {
        public int Points = 10;
        public float Lifetime = 0.2f;
        private LineRenderer _renderer;

        public void Awake()
        {
            _renderer = GetComponent<LineRenderer>();
            _renderer.material.SetFloat("_TargetTime", Time.time + Lifetime);
        }

        public void SetupLine(Vector3 from, Vector3 to)
        {
            var positions = new Vector3[Points];
            for (var i = 0; i < Points; i++)
            {
                positions[i] = Vector3.Lerp(from, to, i / (Points - 1f));
            }

            _renderer.positionCount = Points;
            _renderer.SetPositions(positions);
        }

        public void SetupDefaultDebug()
        {
            SetupLine(transform.position, transform.position + Vector3.forward * 5f);
        }

        public void RayStarted(Vector3 source, Vector3 destination)
        {
            SetupLine(source, destination);
        }

        public void RayUpdated(Vector3 source, Vector3 destination)
        {
            SetupLine(source, destination);
        }

        public void RayEnded()
        {
            Destroy(gameObject);
        }
    }
}