using UnityEngine;

namespace Assets.Scripts.Utils.Debugger.Widgets
{
    public interface IWidget
    {
        Vector2 GetSize(Style style);
        void Draw(Rect rect, Style style);
    }
}