using System;
using UnityEngine;

namespace Assets.Scripts.Utils.Debugger.Widgets
{
    public class ActionWidget : IValueWidget<Action>, IActionWidget
    {
        private Action _action;

        public ActionWidget(Action action)
        {
            _action = action;
        }

        public void SetValue(Action value)
        {
            _action = value;
        }

        public Vector2 GetSize(Style style)
        {
            return Vector2.zero;
        }

        public void Draw(Rect rect, Style style)
        {
        }

        public void SetValue(object o)
        {
            _action = o as Action;
        }

        public void DoAction()
        {
            _action?.Invoke();
        }
    }
}