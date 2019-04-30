using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Utils.Debugger
{
    public class Drawer
    {
        private readonly Material _material;
        private readonly Mesh _lineMesh;
        private readonly Mesh _circleMesh;

        public Drawer()
        {
            _lineMesh = MeshUtils.LineMesh(Vector3.zero, Vector3.forward);
            _circleMesh = MeshUtils.CircleMesh(1f);
            _material = new Material(Shader.Find("Unlit/Color"));
        }

        public void DrawLine(Vector3 from, Vector3 to, Color col)
        {
            _material.color = col; 
            Graphics.DrawMesh(_lineMesh, GetLineTransform(from, to), _material, 0, null);
        }

        public void DrawCircle(Vector3 center, Vector3 normal, float radius, Color col)
        {
            _material.color = col;

            var transform = Matrix4x4.TRS(center, Quaternion.LookRotation(normal), Vector3.one * radius);
            Graphics.DrawMesh(_circleMesh, transform, _material, 0, null);
        }

        public void DrawCircleSphere(Vector3 center, float radius, Color col)
        {
            DrawCircle(center, Vector3.up, radius, col);
            DrawCircle(center, Vector3.right, radius, col);
            DrawCircle(center, Vector3.forward, radius, col);
        }

        public static Matrix4x4 GetLineTransform(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            return Matrix4x4.TRS(from, Quaternion.LookRotation(direction), Vector3.one * direction.magnitude);
        }
    }

    public static class MeshUtils
    {
        public static Mesh LineMesh(Vector3 from, Vector3 to)
        {
            var mesh = new Mesh();
            mesh.SetVertices(new List<Vector3>() { from, to });
            mesh.SetNormals(new List<Vector3>() {Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector3>() { Vector3.zero, Vector3.one });
            mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0, true);
            return mesh;
        }

        public static Mesh CircleMesh(float radius, int segments = 50)
        {
            var mesh = new Mesh();
            var vertices = new Vector3[segments];
            for (var i = 0; i < vertices.Length; i++)
            {
                var x = radius * Mathf.Sin((2 * Mathf.PI * i) / segments);
                var y = radius * Mathf.Cos((2 * Mathf.PI * i) / segments);
                vertices[i] = new Vector3(x, y, 0f);
            }
            var tris = new int[2 * segments];
            var idx = 0;
            for (var i = 0; i < segments; i++)
            {
                tris[idx++] = i;
                tris[idx++] = (i + 1) % segments;
            }
            
            var normals = new Vector3[vertices.Length];
            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = -Vector3.forward;
            }
            
            mesh.vertices = vertices;
            mesh.SetIndices(tris, MeshTopology.Lines, 0);
            mesh.normals = normals;

            return mesh;
        }
    }
}
