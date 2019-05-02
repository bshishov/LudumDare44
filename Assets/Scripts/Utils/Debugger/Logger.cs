using System.Linq;
using Assets.Scripts.Utils.Debugger.Widgets;
using UnityEngine;

namespace Assets.Scripts.Utils.Debugger
{
    public class Logger : IWidget
    {
        private readonly FixedSizeStack<string> _messages;
        private readonly bool _unityLog;
        private Vector2 _scrollPosition = Vector2.zero;
        private int _rows;

        public Logger(int historySize = 100, bool unityLog = true, int rows=20)
        {
            _messages = new FixedSizeStack<string>(historySize);
            _unityLog = unityLog;
            _rows = rows;
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
            return new Vector2(700f, style.LineHeight * _rows); ;
        }

        public void Draw(Rect rect, Style style)
        {
            _scrollPosition = GUI.BeginScrollView(rect, _scrollPosition, new Rect(0, 0, 980, style.LineHeight * _messages.Size), false, true);
             
            var currentY = 0f;
            foreach (var message in _messages.Reverse())
            {
                GUI.Label(new Rect(10, currentY, rect.width, style.LineHeight), message);
                currentY += style.LineHeight;
            }

            GUI.EndScrollView();
        }
    }
}