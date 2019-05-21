using Data;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(StackableProperty))]
    public class StackablePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

            var baseProp = property.FindPropertyRelative("BaseValue");
            var typeProp = property.FindPropertyRelative("Type");
            var modProp = property.FindPropertyRelative("ModifierPerStack");
            var eSProp = property.FindPropertyRelative("EffectiveStacks");

            var type = (StackableProperty.ModifierType) typeProp.intValue;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            EditorGUI.PropertyField(Column(rect, 0f, 0.3f), baseProp, GUIContent.none);
            EditorGUI.PropertyField(Column(rect, 0.3f, 0.45f), typeProp, GUIContent.none);

            if (type != StackableProperty.ModifierType.Exact)
            {
                if (type == StackableProperty.ModifierType.Mult)
                    EditorGUI.LabelField(Column(rect, 0.45f, 0.55f), "*(1+");
                else
                    EditorGUI.LabelField(Column(rect, 0.45f, 0.55f), "+(");
                EditorGUI.PropertyField(Column(rect, 0.55f, 0.7f), modProp, GUIContent.none);
                EditorGUI.LabelField(Column(rect, 0.7f, 0.8f), "*s)");

                EditorGUI.LabelField(Column(rect, 0.8f, 0.85f), "Eff:");
                EditorGUI.PropertyField(Column(rect, 0.85f, 1f), eSProp, GUIContent.none);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public static Rect Column(Rect container, float start, float end)
        {
            return new Rect(
                container.x + container.width * start, 
                container.y, 
                container.width * (end - start), 
                container.height);
        }
    }
}
