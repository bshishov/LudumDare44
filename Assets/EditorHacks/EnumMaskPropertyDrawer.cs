using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
public class EnumMaskPropertyDrawer : PropertyDrawer
{
    private bool foldoutOpen = false;
    private object theEnum;
    private Array enumValues;
    private Type enumUnderlyingType;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (foldoutOpen)
            return EditorGUIUtility.singleLineHeight * (Enum.GetValues(fieldInfo.FieldType).Length + 2);
        else
            return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        theEnum = fieldInfo.GetValue(property.serializedObject.targetObject);
        enumValues = Enum.GetValues(theEnum.GetType());
        enumUnderlyingType = Enum.GetUnderlyingType(theEnum.GetType());

        //We need to convert the enum to its underlying type, if we don't it will be boxed
        //into an object later and then we would need to unbox it like (UnderlyingType)(EnumType)theEnum.
        //If we do this here we can just do (UnderlyingType)theEnum later (plus we can visualize the value of theEnum in VS when debugging)
        theEnum = Convert.ChangeType(theEnum, enumUnderlyingType);

        var enumNames = Enum.GetNames(fieldInfo.FieldType);

        int selectedIndex = -1;
        for (int i = 0; i < enumValues.Length; ++i)
        {
            if (IsSet(i))
                selectedIndex = i;
        }

        Rect labelPos = new Rect(position.x, position.y, position.width * 0.35f, position.height);
        Rect popupPos = new Rect(position.x + position.width * 0.35f, position.y, position.width * 0.65f, position.height);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.LabelField(labelPos, property.displayName);
        selectedIndex = EditorGUI.Popup(popupPos, selectedIndex, enumNames);
        EditorGUI.EndProperty();

        theEnum = GetEnumValue(selectedIndex);            

        fieldInfo.SetValue(property.serializedObject.targetObject, theEnum);
        property.serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Get the value of an enum element at the specified index (i.e. at the index of the name of the element in the names array)
    /// </summary>
    private object GetEnumValue(int _index) => Convert.ChangeType(enumValues.GetValue(_index), enumUnderlyingType);

    /// <summary>
    /// Sets or unsets a bit in theEnum based on the index of the enum element (i.e. the index of the element in the names array)
    /// </summary>
    /// <param name="_set">If true the flag will be set, if false the flag will be unset.</param>
    private void ToggleIndex(int _index, bool _set)
    {
        if (_set)
        {
            if (IsNoneElement(_index))
            {
                theEnum = Convert.ChangeType(0, enumUnderlyingType);
            }

            //enum = enum | val
            theEnum = DoOrOperator(theEnum, GetEnumValue(_index), enumUnderlyingType);
        }
        else
        {
            if (IsNoneElement(_index) || IsAllElement(_index))
            {
                return;
            }

            object val = GetEnumValue(_index);
            object notVal = DoNotOperator(val, enumUnderlyingType);

            //enum = enum & ~val
            theEnum = DoAndOperator(theEnum, notVal, enumUnderlyingType);
        }

    }

    /// <summary>
    /// Checks if a bit flag is set at the provided index of the enum element (i.e. the index of the element in the names array)
    /// </summary>
    private bool IsSet(int _index)
    {
        return DoEqOperator(theEnum, GetEnumValue(_index), enumUnderlyingType);    
    }

    /// <summary>
    /// Call the bitwise OR operator (|) on _lhs and _rhs given their types.
    /// Will basically return _lhs | _rhs
    /// </summary>
    /// <param name="_lhs">Left-hand side of the operation.</param>
    /// <param name="_rhs">Right-hand side of the operation.</param>
    /// <param name="_type">Type of the objects.</param>
    /// <returns>Result of the operation</returns>
    private static object DoOrOperator(object _lhs, object _rhs, Type _type)
    {
        if (_type == typeof(int))
        {
            return ((int)_lhs) | ((int)_rhs);
        }
        else if (_type == typeof(uint))
        {
            return ((uint)_lhs) | ((uint)_rhs);
        }
        else if (_type == typeof(short))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((short)((short)_lhs | (short)_rhs));
        }
        else if (_type == typeof(ushort))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((ushort)((ushort)_lhs | (ushort)_rhs));
        }
        else if (_type == typeof(long))
        {
            return ((long)_lhs) | ((long)_rhs);
        }
        else if (_type == typeof(ulong))
        {
            return ((ulong)_lhs) | ((ulong)_rhs);
        }
        else if (_type == typeof(byte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((byte)((byte)_lhs | (byte)_rhs));
        }
        else if (_type == typeof(sbyte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((sbyte)((sbyte)_lhs | (sbyte)_rhs));
        }
        else
        {
            throw new ArgumentException("Type " + _type.FullName + " not supported.");
        }
    }

    /// <summary>
    /// Call the bitwise AND operator (&) on _lhs and _rhs given their types.
    /// Will basically return _lhs & _rhs
    /// </summary>
    /// <param name="_lhs">Left-hand side of the operation.</param>
    /// <param name="_rhs">Right-hand side of the operation.</param>
    /// <param name="_type">Type of the objects.</param>
    /// <returns>Result of the operation</returns>
    private static object DoAndOperator(object _lhs, object _rhs, Type _type)
    {
        if (_type == typeof(int))
        {
            return ((int)_lhs) & ((int)_rhs);
        }
        else if (_type == typeof(uint))
        {
            return ((uint)_lhs) & ((uint)_rhs);
        }
        else if (_type == typeof(short))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((short)((short)_lhs & (short)_rhs));
        }
        else if (_type == typeof(ushort))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((ushort)((ushort)_lhs & (ushort)_rhs));
        }
        else if (_type == typeof(long))
        {
            return ((long)_lhs) & ((long)_rhs);
        }
        else if (_type == typeof(ulong))
        {
            return ((ulong)_lhs) & ((ulong)_rhs);
        }
        else if (_type == typeof(byte))
        {
            return unchecked((byte)((byte)_lhs & (byte)_rhs));
        }
        else if (_type == typeof(sbyte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((sbyte)((sbyte)_lhs & (sbyte)_rhs));
        }
        else
        {
            throw new System.ArgumentException("Type " + _type.FullName + " not supported.");
        }
    }

    private static bool DoEqOperator(object _lhs, object _rhs, Type _type)
    {
        if (_type == typeof(int))
        {
            return ((int)_lhs) == ((int)_rhs);
        }
        else if (_type == typeof(uint))
        {
            return ((uint)_lhs) == ((uint)_rhs);
        }
        else if (_type == typeof(short))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return ((short)_lhs == (short)_rhs);
        }
        else if (_type == typeof(ushort))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return ((ushort)_lhs == (ushort)_rhs);
        }
        else if (_type == typeof(long))
        {
            return ((long)_lhs) == ((long)_rhs);
        }
        else if (_type == typeof(ulong))
        {
            return ((ulong)_lhs) == ((ulong)_rhs);
        }
        else if (_type == typeof(byte))
        {
            return (((byte)_lhs == (byte)_rhs));
        }
        else if (_type == typeof(sbyte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return (((sbyte)_lhs == (sbyte)_rhs));
        }
        else
        {
            throw new System.ArgumentException("Type " + _type.FullName + " not supported.");
        }
    }

    /// <summary>
    /// Call the bitwise NOT operator (~) on _lhs given its type.
    /// Will basically return ~_lhs
    /// </summary>
    /// <param name="_lhs">Left-hand side of the operation.</param>
    /// <param name="_type">Type of the object.</param>
    /// <returns>Result of the operation</returns>
    private static object DoNotOperator(object _lhs, Type _type)
    {
        if (_type == typeof(int))
        {
            return ~(int)_lhs;
        }
        else if (_type == typeof(uint))
        {
            return ~(uint)_lhs;
        }
        else if (_type == typeof(short))
        {
            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((short)~(short)_lhs);
        }
        else if (_type == typeof(ushort))
        {

            //ushort and short don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((ushort)~(ushort)_lhs);
        }
        else if (_type == typeof(long))
        {
            return ~(long)_lhs;
        }
        else if (_type == typeof(ulong))
        {
            return ~(ulong)_lhs;
        }
        else if (_type == typeof(byte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return (byte)~(byte)_lhs;
        }
        else if (_type == typeof(sbyte))
        {
            //byte and sbyte don't have bitwise operators, it is automatically converted to an int, so we convert it back
            return unchecked((sbyte)~(sbyte)_lhs);
        }
        else
        {
            throw new System.ArgumentException("Type " + _type.FullName + " not supported.");
        }
    }

    /// <summary>
    /// Check if the element of specified index is a "None" element (all bits unset, value = 0).
    /// </summary>
    /// <param name="_index">Index of the element.</param>
    /// <returns>If the element has all bits unset or not.</returns>
    private bool IsNoneElement(int _index) => GetEnumValue(_index).Equals(Convert.ChangeType(0, enumUnderlyingType));

    /// <summary>
    /// Check if the element of specified index is an "All" element (all bits set, value = ~0).
    /// </summary>
    /// <param name="_index">Index of the element.</param>
    /// <returns>If the element has all bits set or not.</returns>
    private bool IsAllElement(int _index)
    {
        object elemVal = GetEnumValue(_index);
        return elemVal.Equals(DoNotOperator(Convert.ChangeType(0, enumUnderlyingType), enumUnderlyingType));
    }
}
#endif

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EnumMaskAttribute : PropertyAttribute
{

}
