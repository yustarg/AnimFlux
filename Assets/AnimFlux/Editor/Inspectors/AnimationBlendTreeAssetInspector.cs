using AnimFlux.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnimFlux.Editor
{
    [CustomEditor(typeof(AnimationBlendTreeAsset))]
    public sealed class AnimationBlendTreeAssetInspector : UnityEditor.Editor
    {
        private const float RowSpacing = 2f;
        private const float ColumnSpacing = 6f;
        private const float MetadataColumnWidth = 120f;

        private SerializedProperty _nodesProp;
        private SerializedProperty _blendSpaceProp;
        private ReorderableList _nodeList;

        private void OnEnable()
        {
            _blendSpaceProp = serializedObject.FindProperty("_blendSpace");
            _nodesProp = serializedObject.FindProperty("_nodes");
            _nodeList = new ReorderableList(serializedObject, _nodesProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight * 2f + RowSpacing * 3f,
                onAddCallback = list =>
                {
                    ReorderableList.defaultBehaviours.DoAddButton(list);
                    if (list.serializedProperty.arraySize <= 0) return;
                    var element = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                    element.FindPropertyRelative("name").stringValue = $"Node {list.serializedProperty.arraySize}";
                    element.FindPropertyRelative("motion").FindPropertyRelative("_asset").objectReferenceValue = null;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_blendSpaceProp);
            EditorGUILayout.Space(4f);
            _nodeList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            var motionWidth = rect.width - MetadataColumnWidth - ColumnSpacing - 120f;
            var nameWidth = 120f;
            var adjustedMotionWidth = Mathf.Max(motionWidth, 100f);

            var motionRect = new Rect(rect.x, rect.y, adjustedMotionWidth, rect.height);
            var nameRect = new Rect(motionRect.xMax + ColumnSpacing, rect.y, nameWidth, rect.height);
            var metadataRect = new Rect(nameRect.xMax + ColumnSpacing, rect.y, MetadataColumnWidth, rect.height);

            EditorGUI.LabelField(motionRect, "Motion");
            EditorGUI.LabelField(nameRect, "Label");
            EditorGUI.LabelField(metadataRect, "Metadata");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _nodesProp.arraySize) return;

            var element = _nodesProp.GetArrayElementAtIndex(index);
            var motionProp = element.FindPropertyRelative("motion");
            var nameProp = element.FindPropertyRelative("name");
            var metadataProp = element.FindPropertyRelative("_metadata");

            var lineRect = new Rect(rect.x, rect.y + RowSpacing, rect.width, EditorGUIUtility.singleLineHeight);
            var motionWidth = lineRect.width - MetadataColumnWidth - ColumnSpacing - 120f;
            var nameWidth = 120f;
            var adjustedMotionWidth = Mathf.Max(motionWidth, 100f);

            var motionRect = new Rect(lineRect.x, lineRect.y, adjustedMotionWidth, lineRect.height);
            var nameRect = new Rect(motionRect.xMax + ColumnSpacing, lineRect.y, nameWidth, lineRect.height);
            var metadataRect = new Rect(nameRect.xMax + ColumnSpacing, lineRect.y, MetadataColumnWidth, lineRect.height);

            EditorGUI.PropertyField(motionRect, motionProp, GUIContent.none);
            nameProp.stringValue = EditorGUI.TextField(nameRect, GUIContent.none, nameProp.stringValue);

            DrawMetadata(metadataRect, metadataProp);
        }

        private void DrawMetadata(Rect rect, SerializedProperty metadataProp)
        {
            if (IsDirectional(metadataProp, out var positionProp))
            {
                var halfWidth = rect.width * 0.5f - 2f;
                var posXRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
                var posYRect = new Rect(posXRect.xMax + 4f, rect.y, halfWidth, rect.height);

                var vector = positionProp.vector2Value;
                vector.x = EditorGUI.FloatField(posXRect, vector.x);
                vector.y = EditorGUI.FloatField(posYRect, vector.y);
                positionProp.vector2Value = vector;
            }
            else if (IsFloatThreshold(metadataProp, out var thresholdProp))
            {
                thresholdProp.floatValue = EditorGUI.FloatField(rect, thresholdProp.floatValue);
            }
            else
            {
                EditorGUI.PropertyField(rect, metadataProp, GUIContent.none, true);
            }
        }

        private static bool IsDirectional(SerializedProperty metadataProp, out SerializedProperty positionProp)
        {
            positionProp = null;
            if (metadataProp == null) return false;
            if (metadataProp.managedReferenceFullTypename.Contains(nameof(Directional2DNodeMetadata)))
            {
                positionProp = metadataProp.FindPropertyRelative("position");
                return positionProp != null;
            }

            return false;
        }

        private static bool IsFloatThreshold(SerializedProperty metadataProp, out SerializedProperty thresholdProp)
        {
            thresholdProp = null;
            if (metadataProp == null) return false;
            if (metadataProp.managedReferenceFullTypename.Contains(nameof(FloatThresholdNodeMetadata)))
            {
                thresholdProp = metadataProp.FindPropertyRelative("threshold");
                return thresholdProp != null;
            }

            return false;
        }
    }
}

