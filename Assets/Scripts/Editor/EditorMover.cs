using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class EditorMover : EditorWindow
    {
        [MenuItem("Window/Mover")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.GetWindow(typeof(EditorMover));
            wnd.titleContent = new GUIContent("Mover");
        }

        private bool _enabled = true;
        private GameObject _gameObject;
        private Vector3 _pivot = Vector3.zero;
        private Vector3 _size = Vector3.one;
        private Vector3 _frequency = Vector3.one;
        private Vector3 _offset = Vector3.zero;

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            _enabled = EditorGUILayout.Toggle("Enabled", _enabled);
            _gameObject = EditorGUILayout.ObjectField("Object", _gameObject, typeof(GameObject), true) as GameObject;
            _pivot = EditorGUILayout.Vector3Field("Pivot", _pivot);
            _size = EditorGUILayout.Vector3Field("Size", _size);
            _frequency = EditorGUILayout.Vector3Field("Frequency", _frequency);
            _offset = EditorGUILayout.Vector3Field("Offset", _offset);
            EditorGUILayout.EndVertical();
        }

        void Update()
        {
            if (_enabled && _gameObject != null)
            {
                var move = new Vector3(
                    Mathf.Sin(_frequency.x * Time.time + _offset.x), 
                    Mathf.Sin(_frequency.y * Time.time + _offset.y), 
                    Mathf.Sin(_frequency.z * Time.time + _offset.z));
                _gameObject.transform.position = _pivot + new Vector3(move.x * _size.x, move.y * _size.y, move.z * _size.z);
            }
        }
        
    }
}