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
        private SerializedProperty _dimensionProp;
        private SerializedProperty _parameterLibraryProp;
        private SerializedProperty _floatParamProp;
        private SerializedProperty _vectorXParamProp;
        private SerializedProperty _vectorYParamProp;
        private ReorderableList _nodeList;

        private void OnEnable()
        {
            _dimensionProp = serializedObject.FindProperty("_dimension");
            _parameterLibraryProp = serializedObject.FindProperty("_parameterLibrary");
            _floatParamProp = serializedObject.FindProperty("_floatParameterOverride");
            _vectorXParamProp = serializedObject.FindProperty("_vectorXParameterOverride");
            _vectorYParamProp = serializedObject.FindProperty("_vectorYParameterOverride");
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
                    var asset = target as AnimationBlendTreeAsset;
                    var blendSpace = asset != null ? asset.BlendSpace : null;
                    var metaProp = element.FindPropertyRelative("_metadata");
                    if (blendSpace != null && metaProp != null)
                    {
                        metaProp.managedReferenceValue = blendSpace.CreateDefaultMetadata();
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_dimensionProp, new GUIContent("Dimension"));
            EditorGUILayout.PropertyField(_parameterLibraryProp, new GUIContent("Parameter Library"));
            var dim = (AnimationBlendTreeAsset.BlendTreeDimension)_dimensionProp.enumValueIndex;
            if (dim == AnimationBlendTreeAsset.BlendTreeDimension.OneD)
            {
                DrawParameterPopup("Float Parameter (1D)", _floatParamProp);
            }
            else
            {
                DrawParameterPopup("Vector X Parameter", _vectorXParamProp);
                DrawParameterPopup("Vector Y Parameter", _vectorYParamProp);
            }

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

            TryInheritParameterLibrary(motionProp);
        }

        private void DrawMetadata(Rect rect, SerializedProperty metadataProp)
        {
            var dim = (AnimationBlendTreeAsset.BlendTreeDimension)_dimensionProp.enumValueIndex;

            // Auto-fix metadata type when dimension changes.
            if (dim == AnimationBlendTreeAsset.BlendTreeDimension.TwoD && IsFloatThreshold(metadataProp, out _))
            {
                metadataProp.managedReferenceValue = new Directional2DNodeMetadata();
                metadataProp.serializedObject.ApplyModifiedProperties();
            }
            else if (dim == AnimationBlendTreeAsset.BlendTreeDimension.OneD && IsDirectional(metadataProp, out _))
            {
                metadataProp.managedReferenceValue = new FloatThresholdNodeMetadata();
                metadataProp.serializedObject.ApplyModifiedProperties();
            }

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

        private void DrawParameterPopup(string label, SerializedProperty prop)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            var library = _parameterLibraryProp.objectReferenceValue as AnimationParameterLibrary;
            var options = library != null && library.floatParameters != null ? library.floatParameters : null;
            if (options != null && options.Count > 0)
            {
                var current = prop.stringValue;
                var index = Mathf.Max(0, options.IndexOf(current));
                index = EditorGUILayout.Popup(index, options.ToArray());
                prop.stringValue = options[index];
            }
            else
            {
                prop.stringValue = EditorGUILayout.TextField(prop.stringValue);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void TryInheritParameterLibrary(SerializedProperty motionProp)
        {
            if (motionProp == null) return;
            var assetProp = motionProp.FindPropertyRelative("_asset");
            if (assetProp == null) return;
            var childObj = assetProp.objectReferenceValue as AnimationBlendTreeAsset;
            if (childObj == null) return;

            var parent = target as AnimationBlendTreeAsset;
            if (parent == null || parent.ParameterLibrary == null) return;
            if (childObj.ParameterLibrary != null) return;

            Undo.RecordObject(childObj, "Inherit Parameter Library");
            childObj.SetParameterLibrary(parent.ParameterLibrary);
            EditorUtility.SetDirty(childObj);
        }
    }
}

