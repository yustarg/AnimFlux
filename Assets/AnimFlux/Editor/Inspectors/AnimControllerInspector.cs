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

            _showLocomotion = EditorGUILayout.BeginFoldoutHeaderGroup(_showLocomotion, "Locomotion Settings");
            if (_showLocomotion)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("rootTree"), new GUIContent("Root Blend Tree"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("fallbackClip"), new GUIContent("Fallback Clip"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Parameter Normalization", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("maxMoveSpeed"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("maxForwardStrafe"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("maxStrafeDirection"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("maxInclineAngle"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("speedDampTime"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("parameterDampTime"));
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("crossFadeDuration"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_locomotionObject.FindProperty("enableRootMotion"));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _locomotionObject.ApplyModifiedProperties();
        }
    }
}

