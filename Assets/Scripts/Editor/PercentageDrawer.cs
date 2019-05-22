#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Editor
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