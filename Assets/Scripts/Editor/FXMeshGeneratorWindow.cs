using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Editor
{
    public class FXMeshGeneratorWindow : EditorWindow
    {
        [MenuItem("Window/FX Mesh Generator")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.GetWindow(typeof(FXMeshGeneratorWindow));
        }

        private int _count = 10;
        private float _widthStart = 1f;
        private float _widthEnd = 1f;
        private float _length = 2f;
        private float _distanceFromCenter = 0.1f;
        private float _distanceFromCenterRandomSpread = 0.1f;
        private bool _optimize = true;
        private bool _hemisphere = false;
        private Mesh _mesh;

        void OnGUI()
        {
            _count = EditorGUILayout.IntField("Count", _count);
            _widthStart = EditorGUILayout.FloatField("Width Start", _widthStart);
            _widthEnd = EditorGUILayout.FloatField("Width End", _widthEnd);
            _length = EditorGUILayout.FloatField("Length", _length);
            _distanceFromCenter = EditorGUILayout.FloatField("DistanceFromCenter", _distanceFromCenter);
            _distanceFromCenterRandomSpread = EditorGUILayout.FloatField("Distance From Center Random Spread", _distanceFromCenterRandomSpread);
            _hemisphere = EditorGUILayout.Toggle("Hemisphere", _hemisphere);
            _optimize = EditorGUILayout.Toggle("Optimize", _optimize);
            _mesh = EditorGUILayout.ObjectField(new GUIContent("Mesh"), _mesh, typeof(Mesh), false) as Mesh;

            if (GUILayout.Button("Create"))
            {
                _mesh = Create();
            }

            if (GUILayout.Button("Save"))
            {
                if (_mesh != null)
                {
                    SaveMesh(_mesh, _mesh.name, false, _optimize);
                }
            }
        }

        public Mesh Create()
        {
            var mesh = new Mesh { name = "FX Mesh"};
            var vertices = new Vector3[_count * 4];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var indices = new int[_count * 6];

            // Generate quads
            for (var i = 0; i < _count; i++)
            {
                var dir = Random.insideUnitSphere.normalized;
                if (_hemisphere)
                {
                    dir.y = Mathf.Abs(dir.y);
                }
                var normal = Vector3.Cross(dir, Vector3.up).normalized;
                var right = Vector3.Cross(dir, normal).normalized;
                var dstFromCenter = _distanceFromCenter + _distanceFromCenterRandomSpread * Random.value;


                vertices[i * 4 + 0] = dir * dstFromCenter - right * _widthStart;
                vertices[i * 4 + 1] = dir * dstFromCenter + right * _widthStart;
                vertices[i * 4 + 2] = dir * (dstFromCenter + _length) - right * _widthEnd;
                vertices[i * 4 + 3] = dir * (dstFromCenter + _length) + right * _widthEnd;

                normals[i * 4 + 0] = normal;
                normals[i * 4 + 1] = normal;
                normals[i * 4 + 2] = normal;
                normals[i * 4 + 3] = normal;

                uvs[i * 4 + 0] = new Vector2(0, 0);
                uvs[i * 4 + 1] = new Vector2(1, 0);
                uvs[i * 4 + 2] = new Vector2(0, 1);
                uvs[i * 4 + 3] = new Vector2(1, 1);

                indices[i * 6 + 0] = i * 4 + 0;
                indices[i * 6 + 1] = i * 4 + 2;
                indices[i * 6 + 2] = i * 4 + 1;

                indices[i * 6 + 3] = i * 4 + 1;
                indices[i * 6 + 4] = i * 4 + 2;
                indices[i * 6 + 5] = i * 4 + 3;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }

        public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
        {
            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }
    }
}