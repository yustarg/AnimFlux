using AnimFlux.Runtime;
using UnityEditor;
using UnityEngine;

namespace AnimFlux.Editor
{
    [CustomPropertyDrawer(typeof(AnimationBlendMotion))]
    public sealed class AnimationBlendMotionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var assetProp = property.FindPropertyRelative("_asset");

            EditorGUI.BeginChangeCheck();
            var reference = EditorGUI.ObjectField(position, label, assetProp.objectReferenceValue, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (reference == null || reference is AnimationClip || reference is AnimationBlendTreeAsset || reference is IAnimationBlendSource)
                {
                    assetProp.objectReferenceValue = reference;
                }
                else
                {
                    Debug.LogWarning("[AnimFlux] Blend motions only accept AnimationClip or assets implementing IAnimationBlendSource.");
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

