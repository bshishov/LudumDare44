using System.Collections.Generic;
using Data;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(Affect))]
    public class AffectDrawer : PropertyDrawer
    {
        private readonly Dictionary<Affect.AffectType, string> _enumToProperty = 
            new Dictionary<Affect.AffectType, string>
            {
                {Affect.AffectType.ApplyModifier, "ApplyModifier"},
                {Affect.AffectType.ApplyBuff, "ApplyBuff"},
                {Affect.AffectType.CastSpell, "CastSpell"},
                {Affect.AffectType.SpawnObject, "SpawnObject"},
                {Affect.AffectType.Move, "Move"}
            };

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var type = Affect.AffectType.ApplyModifier;

            var typeProperty = property.FindPropertyRelative("Type");
            if (typeProperty != null)
                type = (Affect.AffectType) typeProperty.intValue;

            rect.height += 50f;
            label = EditorGUI.BeginProperty(rect, GUIContent.none, property);            

            // Prefix label
            //rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);
            var typeRect = new Rect(rect.position, new Vector2(rect.width, EditorGUI.GetPropertyHeight(typeProperty)));
            EditorGUI.PropertyField(typeRect, typeProperty, new GUIContent( typeProperty?.displayName ));

            if (_enumToProperty.TryGetValue(type, out var propertyName))
            {
                var prop = property.FindPropertyRelative(propertyName);
                prop.isExpanded = true;
                EditorGUI.PropertyField(rect, prop, GUIContent.none, true);
            }

            EditorGUI.EndProperty();
        }
        private Affect.AffectType GetType(SerializedProperty property)
        {
            var typeProperty = property.FindPropertyRelative("Type");
            if (typeProperty != null)
                return (Affect.AffectType)typeProperty.intValue;

            return Affect.AffectType.ApplyModifier;
        }      

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var type = GetType(property);
            if (_enumToProperty.TryGetValue(type, out var propertyName))
            {
                var prop = property.FindPropertyRelative(propertyName);
                prop.isExpanded = true;
                return EditorGUI.GetPropertyHeight(prop, true);
            }
            return 0;
        }
    }
}
