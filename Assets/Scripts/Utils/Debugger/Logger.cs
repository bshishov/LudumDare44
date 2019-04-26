using Assets.Scripts.Utils.Debugger.Widgets;
using UnityEngine;

namespace Assets.Scripts.Utils.Debugger
{
    public class Logger : IWidget
    {
        private readonly FixedSizeStack<string> _messages;
        private readonly bool _unityLog;

        public Logger(int size = 20, bool unityLog = true)
        {
            _messages = new FixedSizeStack<string>(size);
            _unityLog = unityLog;
        }

        public void Log(string message)
        {
            _messages.Push(message);
            if (_unityLog)
                Debug.Log(message);
        }

        public void LogFormat(string message, params object[] args)
        {
            _messages.Push(string.Format(message, args));
            if (_unityLog)
                Debug.LogFormat(message, args);
        }

        public Vector2 GetSize(Style style)
        {
            return new Vector2(400f, style.LineHeight * _messages.Size); ;
        }

        public void Draw(Rect rect, Style style)
        {
            var currentY = rect.y;
            foreach (var message in _messages)
            {
                GUI.Label(new Rect(rect.x, currentY, rect.width, style.LineHeight), message);
                currentY += style.LineHeight;
            }
        }
    }
}