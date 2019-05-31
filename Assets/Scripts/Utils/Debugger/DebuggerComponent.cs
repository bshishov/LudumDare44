#if DEBUG
#define USE_REFLECTION
#endif

using System;
using UnityEngine;
using Utils.Debugger.Widgets;

namespace Utils.Debugger
{
    public class DebuggerComponent : MonoBehaviour
    {
        public const char PathSeparator = '/';

        public KeyCode OpenKey = KeyCode.F3;
        public KeyCode CollapseKey = KeyCode.F4;
        public KeyCode ActionKey = KeyCode.F5;
        public KeyCode NavigateUp = KeyCode.PageUp;
        public KeyCode NavigateDown = KeyCode.PageDown;
        public KeyCode ToggleDebugDrawings = KeyCode.F6;

        private bool _isOpened;
        private bool _isDrawingDebugLines;

        private readonly DebugNode _root = new DebugNode("Debug")
        {
            IsExpanded = true,
            Widget = new StringWidget("F4 - Expand/Collapse, PageUp/PageDown - Navigation, F5 - Activate")
        };
        
        private readonly Cache<string, DebugNode> _pathCache = new Cache<string, DebugNode>();
        private readonly DrawingContext _context = new DrawingContext();
        private Logger _defaultLog;
        private Drawer _drawer;
        private float _scroll;

        void Awake()
        {
            _drawer = new Drawer();
            _defaultLog = GetLogger("Log");
            Display("Debug/Clear path cache", () => { _pathCache.Clear(); });
            Display("Debug/Toggle debug lines", () => { _isDrawingDebugLines = !_isDrawingDebugLines; });

#if USE_REFLECTION
            GetOrCreateNode("Debug/Context").Widget = new ObjectWidget(_context);
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

                if (Input.GetKeyDown(ActionKey))
                    _context.ActionRequested = true;

                _scroll = Mathf.Min(0, _scroll + Input.mouseScrollDelta.y * 20f);
            }

            if (Input.GetKeyDown(ToggleDebugDrawings))
            {
                _isDrawingDebugLines = !_isDrawingDebugLines;
            }
        }

        private void LateUpdate()
        {
            if (_isDrawingDebugLines)
                _drawer.DrawQueuedMeshes();
            _drawer.ProcessQueue();
        }

        public void Open()
        {
            _scroll = 0f;
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

        public void Display(string path, Action action)
        {
            var node = GetOrCreateNode(path);
            var payload = node.Widget as ActionWidget;
            if (payload != null)
            {
                payload.SetValue(action);
            }
            else
            {
                // New payload
                node.Widget = new ActionWidget(action);
            }

            node.Touch();
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

        public void DrawRay(Ray ray, Color col, float maxRange = 100f, float duration = 0.0f)
        {
            _drawer.DrawLine(ray.origin, ray.origin + ray.direction * maxRange, col, duration);
        }
        
        public void DrawRay(Vector3 origin, Vector3 direction, Color col, float maxRange = 100f, float duration = 0.0f)
        {
            _drawer.DrawLine(origin, origin + direction.normalized * maxRange, col, duration);
        }

        public void DrawLine(Vector3 from, Vector3 to, Color col, float duration = 0.0f)
        {
            _drawer.DrawLine(from, to, col, duration);
        }

        public void DrawCircle(Vector3 center, Vector3 normal, float radius, Color col, float duration = 0.0f)
        {
            _drawer.DrawCircle(center, normal, radius, col, duration);
        }

        public void DrawCircleSphere(Vector3 center, float radius, Color col, float duration = 0.0f)
        {
            _drawer.DrawCircleSphere(center, radius, col, duration);
        }

        public Logger GetLogger(string path, int historySize=100, bool unityLog=true)
        {
            var node = GetOrCreateNode(path);
            var payload = GetOrCreateNode(path).Widget as Logger;
            if (payload != null)
                return payload;

            // New payload
            var p = new Logger(historySize, unityLog);
            node.Widget = p;
            return p;
        }

        void OnGUI()
        {
            if (!_isOpened || _context == null)
                return;

            if(!_context.Style.IsInitialized)
                _context.Style.Initialize();

            // Start from 0
            _context.Y = _scroll;
            _context.Index = 0;
            _context.Depth = 0;

            // Draw
            _root.Draw(_context);

            // Reset context
            _context.CollapseRequested = false;
            _context.ActionRequested = false;
            _context.CursorIndex = Mathf.Clamp(_context.CursorIndex, 0, _context.Index - 1);
        }

        public void DrawCone(Vector3 origin, Vector3 direction, float sphereSize, float angle, Color color, float duration)
        {
            _drawer.DrawCone(origin, direction, sphereSize, angle, color, duration);
        }

        public void DrawAxis(Vector3 pos, Quaternion rot, float duration)
        {
            Debugger.Default.DrawLine(pos, pos + rot * Vector3.forward, Color.blue, duration);
            Debugger.Default.DrawLine(pos, pos + rot * Vector3.right, Color.red, duration);
            Debugger.Default.DrawLine(pos, pos + rot * Vector3.up, Color.green, duration);
        }
    }
}