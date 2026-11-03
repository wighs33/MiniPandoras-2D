using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// 컴포넌트·에셋의 private [SerializeField]를 이름으로 채운다. 런타임 코드에 쓰기용 공개 setter를 두지 않으려고 쓴다.
    /// 장면 조립기와 들여오기 도구가 같이 쓴다.
    /// </summary>
    public static class SerializedFields
    {
        public static void Set(Object target, params (string field, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                Assign(Find(serialized, target, field), value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>구조체 칸(예: shape.radius) 안의 값을 채울 때 쓴다.</summary>
        public static void SetNested(Object target, string structField, params (string field, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty parent = Find(serialized, target, structField);
            foreach ((string field, object value) in values)
            {
                SerializedProperty property = parent.FindPropertyRelative(field);
                if (property == null)
                {
                    throw new ArgumentException($"{target.GetType().Name}.{structField}에 {field}가 없다");
                }

                Assign(property, value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(SerializedObject serialized, Object target, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new ArgumentException($"{target.GetType().Name}에 {field}가 없다");
            }

            return property;
        }

        private static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case bool flag:
                    property.boolValue = flag;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case Color color:
                    property.colorValue = color;
                    break;
                case Vector2 vector:
                    property.vector2Value = vector;
                    break;
                case Rect rect:
                    property.rectValue = rect;
                    break;
                case Enum choice:
                    property.enumValueIndex = Convert.ToInt32(choice);
                    break;
                case IList list when property.isArray:
                    property.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Assign(property.GetArrayElementAtIndex(i), list[i]);
                    }

                    break;
                case ValueType record when property.propertyType == SerializedPropertyType.Generic:
                    // 직렬화 구조체(예: TileAnimator.Flame)는 칸마다 같은 이름의 private 필드를 읽어 채운다.
                    foreach (FieldInfo field in record.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        SerializedProperty child = property.FindPropertyRelative(field.Name);
                        if (child != null)
                        {
                            Assign(child, field.GetValue(record));
                        }
                    }

                    break;
                default:
                    property.objectReferenceValue = (Object)value;
                    break;
            }
        }
    }
}
