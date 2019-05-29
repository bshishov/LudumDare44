
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using Attributes;

namespace Editor
{
    [CustomPropertyDrawer(typeof(PercentageAttribute))]
    public class PercentageDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var percentageAttr = attribute as PercentageAttribute;
            if (property.propertyType != SerializedPropertyType.Float)
            {
                base.OnGUI(rect, property, label);
                return;
            }
            
            EditorGUI.Slider(rect, property, 
                percentageAttr.Min, 
                percentageAttr.Max, 
                label);
        }
    }
}
#endif