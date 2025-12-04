using AnimFlux.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnimFlux.Editor
{
    [CustomEditor(typeof(LocomotionBlendTreeAsset))]
    public sealed class LocomotionBlendTreeAssetInspector : UnityEditor.Editor
    {
        private const float RowSpacing = 2f;
        private const float ColumnSpacing = 6f;
        private const float DirectionColumnWidth = 60f;

        private SerializedProperty _nodesProp;
        private ReorderableList _nodeList;
        private int _draggingIndex = -1;

        private void OnEnable()
        {
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
                    element.FindPropertyRelative("direction").vector2Value = Vector2.up;
                    element.FindPropertyRelative("motion").FindPropertyRelative("_asset").objectReferenceValue = null;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4f);
            _nodeList.DoLayoutList();
            EditorGUILayout.Space(6f);
            DrawDirectionalCanvas();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            var motionWidth = rect.width - (DirectionColumnWidth * 2f) - ColumnSpacing * 2f;
            var nameWidth = 100f;
            var adjustedMotionWidth = Mathf.Max(motionWidth - nameWidth - ColumnSpacing, 100f);

            var motionRect = new Rect(rect.x, rect.y, adjustedMotionWidth, rect.height);
            var nameRect = new Rect(motionRect.xMax + ColumnSpacing, rect.y, nameWidth, rect.height);
            var posXRect = new Rect(nameRect.xMax + ColumnSpacing, rect.y, DirectionColumnWidth, rect.height);
            var posYRect = new Rect(posXRect.xMax + ColumnSpacing, rect.y, DirectionColumnWidth, rect.height);

            EditorGUI.LabelField(motionRect, "Motion");
            EditorGUI.LabelField(nameRect, "Label");
            EditorGUI.LabelField(posXRect, "Pos X");
            EditorGUI.LabelField(posYRect, "Pos Y");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _nodesProp.arraySize) return;

            var element = _nodesProp.GetArrayElementAtIndex(index);
            var motionProp = element.FindPropertyRelative("motion");
            var nameProp = element.FindPropertyRelative("name");
            var directionProp = element.FindPropertyRelative("direction");

            var lineRect = new Rect(rect.x, rect.y + RowSpacing, rect.width, EditorGUIUtility.singleLineHeight);
            var motionWidth = lineRect.width - (DirectionColumnWidth * 2f) - ColumnSpacing * 2f;
            var nameWidth = 100f;
            var adjustedMotionWidth = Mathf.Max(motionWidth - nameWidth - ColumnSpacing, 100f);

            var motionRect = new Rect(lineRect.x, lineRect.y, adjustedMotionWidth, lineRect.height);
            var nameRect = new Rect(motionRect.xMax + ColumnSpacing, lineRect.y, nameWidth, lineRect.height);
            var posXRect = new Rect(nameRect.xMax + ColumnSpacing, lineRect.y, DirectionColumnWidth, lineRect.height);
            var posYRect = new Rect(posXRect.xMax + ColumnSpacing, lineRect.y, DirectionColumnWidth, lineRect.height);

            EditorGUI.PropertyField(motionRect, motionProp, GUIContent.none);
            nameProp.stringValue = EditorGUI.TextField(nameRect, GUIContent.none, nameProp.stringValue);

            var dir = directionProp.vector2Value;
            dir.x = EditorGUI.FloatField(posXRect, dir.x);
            dir.y = EditorGUI.FloatField(posYRect, dir.y);
            directionProp.vector2Value = dir;

            var secondaryRect = new Rect(rect.x, lineRect.yMax + RowSpacing, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.LabelField(secondaryRect, $"Magnitude: {dir.magnitude:0.00}");
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDirectionalCanvas()
        {
            if (_nodesProp == null || _nodesProp.arraySize == 0) return;

            var aspectRect = GUILayoutUtility.GetAspectRect(1.2f, GUILayout.MaxHeight(260f));
            var canvasRect = new Rect(aspectRect.x + 16f, aspectRect.y + 16f, aspectRect.width - 32f, aspectRect.height - 32f);
            if (Event.current.type == EventType.Repaint)
            {
                var bgColor = EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.85f, 0.85f, 0.85f);
                EditorGUI.DrawRect(canvasRect, bgColor);
                Handles.BeginGUI();
                Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f);
                Handles.DrawSolidRectangleWithOutline(canvasRect, Color.clear, Handles.color);
                Handles.EndGUI();
            }

            var halfWidth = canvasRect.width * 0.5f;
            var halfHeight = canvasRect.height * 0.5f;
            var center = canvasRect.center;

            float maxAbsX = 1f;
            float maxAbsY = 1f;
            for (int i = 0; i < _nodesProp.arraySize; i++)
            {
                var dir = _nodesProp.GetArrayElementAtIndex(i).FindPropertyRelative("direction").vector2Value;
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(dir.x));
                maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(dir.y));
            }

            const float paddingMultiplier = 1.05f;
            var rangeX = Mathf.Max(1f, maxAbsX * paddingMultiplier);
            var rangeY = Mathf.Max(1f, maxAbsY * paddingMultiplier);
            var scaleX = halfWidth / rangeX;
            var scaleY = halfHeight / rangeY;

            DrawGrid(canvasRect, center, rangeX, rangeY);
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawLine(new Vector3(canvasRect.xMin, center.y, 0f), new Vector3(canvasRect.xMax, center.y, 0f));
            Handles.DrawLine(new Vector3(center.x, canvasRect.yMin, 0f), new Vector3(center.x, canvasRect.yMax, 0f));

            var guiEvent = Event.current;
            const float handleRadius = 6f;

            for (int i = 0; i < _nodesProp.arraySize; i++)
            {
                var element = _nodesProp.GetArrayElementAtIndex(i);
                var directionProp = element.FindPropertyRelative("direction");
                var nameProp = element.FindPropertyRelative("name");
                var dir = directionProp.vector2Value;
                var nodePos = new Vector2(center.x + dir.x * scaleX, center.y - dir.y * scaleY);

                var handleRect = new Rect(
                    nodePos.x - handleRadius,
                    nodePos.y - handleRadius,
                    handleRadius * 2f,
                    handleRadius * 2f);

                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = guiEvent.GetTypeForControl(controlId);

                switch (eventType)
                {
                    case EventType.MouseDown:
                        if (guiEvent.button == 0 && handleRect.Contains(guiEvent.mousePosition))
                        {
                            GUIUtility.hotControl = controlId;
                            _draggingIndex = i;
                            _nodeList.index = i;
                            guiEvent.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlId && _draggingIndex == i)
                        {
                            Undo.RecordObject(target, "Move Blend Node");
                            var canvasPos = guiEvent.mousePosition;
                            var newDir = new Vector2(
                                (canvasPos.x - center.x) / scaleX,
                                (center.y - canvasPos.y) / scaleY);
                            directionProp.vector2Value = newDir;
                            serializedObject.ApplyModifiedProperties();
                            guiEvent.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlId && _draggingIndex == i)
                        {
                            GUIUtility.hotControl = 0;
                            _draggingIndex = -1;
                            guiEvent.Use();
                        }
                        break;
                }

                var label = string.IsNullOrEmpty(nameProp.stringValue)
                    ? $"Node {i}"
                    : nameProp.stringValue;

                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.MoveArrow);

                if (Event.current.type == EventType.Repaint)
                {
                    var handleColor = i == _nodeList.index ? Color.yellow : new Color(1f, 1f, 1f, 0.9f);
                    Handles.color = handleColor;
                    Handles.DrawSolidDisc(new Vector3(nodePos.x, nodePos.y, 0f), Vector3.forward, handleRadius);
                    Handles.color = new Color(1f, 1f, 1f, 0.7f);

                    var labelPos = new Vector2(nodePos.x + 8f, nodePos.y - EditorGUIUtility.singleLineHeight * 0.5f);
                    Handles.Label(labelPos, label, EditorStyles.boldLabel);
                }
            }

            Handles.EndGUI();
        }

        private static void DrawGrid(Rect rect, Vector2 center, float rangeX, float rangeY)
        {
            var gridColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.08f);
            Handles.BeginGUI();
            Handles.color = gridColor;
            const int divisions = 8;
            for (int i = 1; i < divisions; i++)
            {
                var t = i / (float)divisions;
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                Handles.DrawLine(new Vector3(x, rect.yMin, 0f), new Vector3(x, rect.yMax, 0f));

                var y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                Handles.DrawLine(new Vector3(rect.xMin, y, 0f), new Vector3(rect.xMax, y, 0f));
            }

            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Handles.DrawLine(new Vector3(rect.xMin, center.y, 0f), new Vector3(rect.xMax, center.y, 0f));
            Handles.DrawLine(new Vector3(center.x, rect.yMin, 0f), new Vector3(center.x, rect.yMax, 0f));
            Handles.EndGUI();

            var labelStyle = EditorStyles.miniLabel;
            var axisLabelOffset = new Vector2(4f, -14f);
            GUI.Label(new Rect(rect.xMax - 50f, center.y + axisLabelOffset.y, 46f, 16f), $"X±{rangeX:0.##}", labelStyle);
            GUI.Label(new Rect(center.x + axisLabelOffset.x, rect.yMin - 4f, 46f, 16f), $"Y±{rangeY:0.##}", labelStyle);
        }
    }
}

