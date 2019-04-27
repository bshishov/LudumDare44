#if DEBUG
#define USE_REFLECTION
#endif

using System;
using System.Collections.Generic;
using Assets.Scripts.Utils.Debugger.Widgets;
using UnityEngine;

namespace Assets.Scripts.Utils.Debugger
{
    public class DebuggerComponent : MonoBehaviour
    {
        public const char PathSeparator = '/';

        public KeyCode OpenKey = KeyCode.F3;
        public KeyCode CollapseKey = KeyCode.F5;
        public KeyCode NavigateUp = KeyCode.PageUp;
        public KeyCode NavigateDown = KeyCode.PageDown;

        private bool _isOpened;

        private readonly DebugNode _root = new DebugNode("Debug")
        {
            IsExpanded = true,
            Widget = new StringWidget("F5 - Expand/Collapse, PageUp/PageDown - Navigation")
        };

        private Logger _defaultLog;
        private readonly Cache<string, DebugNode> _pathCache = new Cache<string, DebugNode>();
        private readonly DrawingContext _context = new DrawingContext();

        void Awake()
        {
            _defaultLog = GetLogger("Log");
            Display("Debug/Path cache", new Vector2(200, 20), rect =>
            {
                GUILayout.BeginArea(rect);
                if (GUILayout.Button("Clear cache"))
                {
                    Log("Debug/Log", "Clearing cache");
                    _pathCache.Clear();
                }

                GUILayout.EndArea();
            });

#if USE_REFLECTION
            GetOrCreateNode("Debug/Dictionary").Widget = new DictionaryWidget<string, string>(new Dictionary<string, string>()
            {
                {"hello", "world"},
                {"1", "@"},
                {"foo", "bar"}
            });

            GetOrCreateNode("Debug/Context").Widget = new ObjectWidget(_context);
            GetOrCreateNode("Debug/Array").Widget = new EnumerableWidget<DebugNode>(new[] {_root, _root, _root, _root});
#endif
        }

        void Update()
        {
            if (Input.GetKeyDown(OpenKey))
            {
                if (!_isOpened)
                    Open();
                else
                    Close();
            }

            if (_isOpened)
            {
                if (Input.GetKeyDown(NavigateDown))
                    _context.CursorIndex += 1;

                if (Input.GetKeyDown(NavigateUp))
                    _context.CursorIndex -= 1;

                if (Input.GetKeyDown(CollapseKey))
                    _context.CollapseRequested = true;
            }
        }

        public void Open()
        {
            _isOpened = true;
        }

        public void Close()
        {
            _isOpened = false;
        }

        public void Log(string message)
        {
            _defaultLog.Log(message);
        }

        public void LogFormat(string message, params object[] args)
        {
            _defaultLog.Log(string.Format(message, args));
        }

        public void Log(string message, UnityEngine.Object context)
        {
            _defaultLog.Log(message);
        }

        public DebugNode GetOrCreateNode(string path)
        {
            return _pathCache.Get(path, () =>
            {
                var parts = path.Split(PathSeparator);
                return GetOrCreateNode(parts);
            });
        }

        public DebugNode GetOrCreateNode(params string[] path)
        {
            var node = _root;
            foreach (var nodeName in path)
            {
                node = node.GetOrCreateChild(nodeName);
            }

            return node;
        }

        public void Display(DebugNode node, string value)
        {
            var payload = node.Widget as StringWidget;
            if (payload != null)
            {
                payload.SetValue(value);
            }
            else
            {
                // New payload
                node.Widget = new StringWidget(value);
            }

            node.Touch();
        }

        public void Display(DebugNode node, Texture texture)
        {
            var payload = node.Widget as TextureWidget;
            if (payload != null)
            {
                payload.SetValue(texture);
            }
            else
            {
                node.Widget = new TextureWidget(texture);
            }

            node.Touch();
        }

        public void Display(DebugNode node, float value)
        {
            Display(node, value.ToString());
        }

        public void Display(string path, string value)
        {
            Display(GetOrCreateNode(path), value);
        }

        public void Log(DebugNode node, string message)
        {
            var payload = node.Widget as Logger;
            if (payload != null)
            {
                // Existing payload
                payload.Log(message);
            }
            else
            {
                // New payload
                var p = new Logger();
                p.Log(message);
                node.Widget = p;
            }

            node.Touch();
        }

        public void Display(DebugNode node, Vector2 size, Action<Rect> drawAction)
        {
            node.Widget = new CustomUIWidget(size, drawAction);
            node.Touch();
        }

        public void DisplayFullPath(string value, params string[] path)
        {
            Display(GetOrCreateNode(path), value);
        }

        public void Display(string path, float value)
        {
            Display(GetOrCreateNode(path), value);
        }

        public void Log(string path, string message)
        {
            Log(GetOrCreateNode(path), message);
        }

        public void LogFormat(string path, string message, params object[] args)
        {
            Log(GetOrCreateNode(path), string.Format(message, args));
        }

        public void Display(string path, Texture texture)
        {
            Display(GetOrCreateNode(path), texture);
        }

        public void Display(string path, Vector2 size, Action<Rect> drawAction)
        {
            Display(GetOrCreateNode(path), size, drawAction);
        }
        public void Display(string path, Action action, string buttonLabel = "Click!")
        {
            if(action == null)
                return;
            Display(GetOrCreateNode(path), new Vector2(200, 20f), rect =>
            {
                if (GUI.Button(rect, buttonLabel))
                {
                    action();
                }
            });
        }

        public Logger GetLogger(string path)
        {
            var node = GetOrCreateNode(path);
            var payload = GetOrCreateNode(path).Widget as Logger;
            if (payload != null)
                return payload;

            // New payload
            var p = new Logger();
            node.Widget = p;
            return p;
        }

        void OnGUI()
        {
            if (!_isOpened)
                return;

            // Start from 0
            _context.Y = 0;
            _context.Index = 0;
            _context.Depth = 0;

            // Draw
            _root.Draw(_context);

            // Reset context
            _context.CollapseRequested = false;
            _context.CursorIndex = Mathf.Clamp(_context.CursorIndex, 0, _context.Index - 1);
        }
    }
}