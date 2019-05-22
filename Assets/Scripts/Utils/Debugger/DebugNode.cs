#if DEBUG
#define USE_REFLECTION
#endif

using System.Collections.Generic;
using UnityEngine;
using Utils.Debugger.Widgets;

#if USE_REFLECTION
#endif

namespace Utils.Debugger
{
    public class DebugNode
    {
        public string Name;
        public bool IsExpanded = false;
        public IWidget Widget;

        private float _lastUpdate;

        private readonly Dictionary<string, DebugNode> _children
            = new Dictionary<string, DebugNode>();

        public DebugNode(string name)
        {
            Name = name;
        }

        private static void Draw(DrawingContext context, string header, ref bool isExpanded, IWidget widget,
            bool isActualNode)
        {
            var x = context.Depth * context.Style.Padding;
            var style = context.Style.HeaderStyle;

            if (!isActualNode)
                style = context.Style.PropertyHeaderStyle;

            // If selected
            if (context.Index == context.CursorIndex)
            {
                style = context.Style.SelectedHeaderStyle;
                if (context.CollapseRequested)
                {
                    isExpanded = !isExpanded;
                    context.CollapseRequested = false;
                }
            }
            
            var headerRect = new Rect(x + 20, context.Y, context.Style.HeaderColumn - x - 20, context.Style.LineHeight);
            if (GUI.Button(new Rect(x, headerRect.y, 20, headerRect.height), isExpanded ? "-" : "+"))
            {
                isExpanded = !isExpanded;
            }
            
            GUI.Label(headerRect, header, style);

            if (widget != null)
            {
                if (context.Index == context.CursorIndex && 
                    context.ActionRequested && 
                    widget is IActionWidget )
                {
                    ((IActionWidget)widget).DoAction();
                }

                // Widget
                if (isExpanded && widget != null)
                {
                    var widgetSize = widget.GetSize(context.Style);
                    var payloadRect = new Rect(context.Style.HeaderColumn, context.Y, widgetSize.x, widgetSize.y);
                    GUI.Box(payloadRect, GUIContent.none, context.Style.ContentStyle);
                    widget.Draw(payloadRect, context.Style);
                    context.Y += Mathf.Max(widgetSize.y, context.Style.LineHeight) - context.Style.LineHeight;
                }
            }

            context.Y += context.Style.LineHeight;
            context.Index += 1;

            // Children
            if (isExpanded)
            {
                if (widget is INestedWidget nestedWidget)
                {
                    context.Depth += 1;
                    var localIdx = 0;
                    foreach (var kvp in nestedWidget)
                    {
                        var childExpanded = nestedWidget.GetExpanded(localIdx);
                        var refChildExpanded = childExpanded;
                        Draw(context, kvp.Key, ref refChildExpanded, kvp.Value, false);
                        if (refChildExpanded != childExpanded)
                            nestedWidget.SetExpanded(localIdx, refChildExpanded);

                        localIdx += 1;
                    }

                    context.Depth -= 1;
                }
            }
        }

        public void Draw(DrawingContext context)
        {
            Draw(context, Name, ref IsExpanded, Widget, true);

            // Child nodes
            if (IsExpanded)
            {
                context.Depth += 1;
                foreach (var node in _children.Values)
                {
                    node.Draw(context);
                }

                context.Depth -= 1;
            }
        }

        public DebugNode GetOrCreateChild(string name)
        {
            if (_children.ContainsKey(name))
                return _children[name];

            var node = new DebugNode(name);
            _children.Add(name, node);
            return node;
        }

        public void Touch()
        {
            _lastUpdate = Time.deltaTime;
        }
    }
}