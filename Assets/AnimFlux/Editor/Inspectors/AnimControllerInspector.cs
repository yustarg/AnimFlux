using AnimFlux.Runtime;
using UnityEditor;
using UnityEngine;

namespace AnimFlux.Editor
{
    [CustomEditor(typeof(AnimController))]
    public sealed class AnimControllerInspector : UnityEditor.Editor
    {
        private SerializedProperty _animatorProp;
        private SerializedProperty _configProp;
        private SerializedProperty _initializeProp;
        private SerializedObject _locomotionObject;
        private bool _showLocomotion;

        private void OnEnable()
        {
            _animatorProp = serializedObject.FindProperty("animator");
            _configProp = serializedObject.FindProperty("config");
            _initializeProp = serializedObject.FindProperty("initializeOnAwake");
        }

        private void OnDisable()
        {
            _locomotionObject?.Dispose();
            _locomotionObject = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_animatorProp);
            EditorGUILayout.PropertyField(_configProp);
            EditorGUILayout.PropertyField(_initializeProp);
            serializedObject.ApplyModifiedProperties();

            DrawLocomotionSection();
        }

        private void DrawLocomotionSection()
        {
            var config = _configProp.objectReferenceValue as AnimControllerConfig;
            if (!config)
            {
                EditorGUILayout.HelpBox("Assign an AnimControllerConfig to configure locomotion.", MessageType.Info);
                return;
            }

            var locomotionConfig = config.LocomotionConfig;
            if (!locomotionConfig)
            {
                EditorGUILayout.HelpBox("The assigned AnimControllerConfig is missing a LocomotionConfig asset.", MessageType.Warning);
                return;
            }

            if (_locomotionObject == null || _locomotionObject.targetObject != locomotionConfig)
            {
                _locomotionObject = new SerializedObject(locomotionConfig);
            }

            _locomotionObject.Update();

            _showLocomotion = EditorGUILayout.BeginFoldoutHeaderGroup(_showLocomotion, "Locomotion Trees");
            if (_showLocomotion)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawBlendTreeField("walkTree", "Walk Blend Tree");
                    DrawClipFallbackField("walkClip", "Walk Fallback Clip");
                    EditorGUILayout.Space();
                    DrawBlendTreeField("sprintTree", "Sprint Blend Tree");
                    DrawClipFallbackField("sprintClip", "Sprint Fallback Clip");
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("walkSpeed"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("sprintSpeed"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("sprintBlendRange"));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _locomotionObject.ApplyModifiedProperties();
        }

        private void DrawBlendTreeField(string propertyName, string label)
        {
            var property = _locomotionObject.FindProperty(propertyName);
            if (property == null) return;
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        private void DrawClipFallbackField(string propertyName, string label)
        {
            var property = _locomotionObject.FindProperty(propertyName);
            if (property == null) return;
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }
    }
}

