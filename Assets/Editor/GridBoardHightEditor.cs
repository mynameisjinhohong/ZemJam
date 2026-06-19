#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridBoard))]
public class GridBoardHeightEditor : Editor
{
    private SerializedProperty _cellHeightsProperty;

    private bool _editMode;
    private int _brushHeight = 1;
    private bool _showHeightLabels = true;

    private void OnEnable()
    {
        _cellHeightsProperty = serializedObject.FindProperty("_cellHeights");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Height Cell Editor", EditorStyles.boldLabel);

        _editMode = EditorGUILayout.Toggle("Edit Mode", _editMode);

        using (new EditorGUI.DisabledScope(!_editMode))
        {
            _brushHeight = EditorGUILayout.IntField("Brush Height", _brushHeight);
            _showHeightLabels = EditorGUILayout.Toggle("Show Height Labels", _showHeightLabels);
        }

        if (_editMode)
        {
            EditorGUILayout.HelpBox(
                "Scene View에서 칸을 클릭하거나 드래그하면 해당 칸의 Height가 Brush Height로 설정됩니다.\n\n" +
                "Height 0: 기본 바닥. _cellHeights에서 해당 칸 데이터 제거.\n" +
                "Height 1: 점프키로 올라갈 수 있는 낮은 칸.\n" +
                "Height 2: Height 1에서 다시 점프해서 올라갈 수 있는 칸.\n" +
                $"Height {GridBoard.BlockedHeight} 이상: 이동 불가능 칸.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Clear All Height Data"))
        {
            if (EditorUtility.DisplayDialog(
                    "Clear All Height Data",
                    "모든 Height 데이터를 삭제하시겠습니까?\n모든 칸이 기본 높이 0으로 취급됩니다.",
                    "삭제",
                    "취소"))
            {
                Undo.RecordObject(target, "Clear All Height Data");

                _cellHeightsProperty.ClearArray();

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        GridBoard board = (GridBoard)target;

        DrawBoardOverlay(board);

        if (!_editMode)
            return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        HandleSceneInput(board);
    }

    private void HandleSceneInput(GridBoard board)
    {
        Event e = Event.current;

        if (e == null)
            return;

        if (e.alt)
            return;

        if (e.button != 0)
            return;

        if (e.type != EventType.MouseDown &&
            e.type != EventType.MouseDrag)
        {
            return;
        }

        if (!TryGetMouseGridPosition(board, e.mousePosition, out Vector2Int gridPos))
            return;

        if (!board.IsInsideBoard(gridPos))
            return;

        serializedObject.Update();

        Undo.RecordObject(board, "Paint Cell Height");

        bool changed = SetCellHeight(gridPos, _brushHeight);

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(board);
            SceneView.RepaintAll();
        }

        e.Use();
    }

    private bool TryGetMouseGridPosition(
        GridBoard board,
        Vector2 mousePosition,
        out Vector2Int gridPos
    )
    {
        gridPos = default;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        // 현재 GridBoard는 XY 평면 기준
        Plane boardPlane = new Plane(Vector3.forward, board.transform.position);

        if (!boardPlane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        gridPos = board.WorldToGrid(worldPoint);

        return true;
    }

    private void DrawBoardOverlay(GridBoard board)
    {
        DrawGrid(board);
        DrawHeightCells(board);

        if (_editMode)
            DrawMousePreview(board);
    }

    private void DrawGrid(GridBoard board)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.25f);

        Vector2Int boardSize = board.BoardSize;
        float cellSize = board.CellSize;

        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 center = board.GridToWorld(cell);

                Handles.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
            }
        }
    }

    private void DrawHeightCells(GridBoard board)
    {
        serializedObject.Update();

        float cellSize = board.CellSize;

        for (int i = 0; i < _cellHeightsProperty.arraySize; i++)
        {
            SerializedProperty element = _cellHeightsProperty.GetArrayElementAtIndex(i);

            SerializedProperty gridPosProperty = element.FindPropertyRelative("GridPos");
            SerializedProperty heightProperty = element.FindPropertyRelative("Height");

            Vector2Int gridPos = gridPosProperty.vector2IntValue;
            int height = heightProperty.intValue;

            if (!board.IsInsideBoard(gridPos))
                continue;

            Vector3 center = board.GridToWorld(gridPos);

            if (height >= GridBoard.BlockedHeight)
            {
                Handles.color = new Color(1f, 0f, 0f, 0.35f);
                DrawSolidCell(center, cellSize);

                Handles.color = Color.red;
                Handles.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
            }
            else if (height > GridBoard.DefaultHeight)
            {
                Handles.color = GetHeightColor(height, 0.3f);
                DrawSolidCell(center, cellSize);

                Handles.color = GetHeightColor(height, 1f);
                Handles.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
            }

            if (_showHeightLabels)
            {
                DrawHeightLabel(center, height);
            }
        }
    }

    private void DrawMousePreview(GridBoard board)
    {
        Event e = Event.current;

        if (e == null)
            return;

        if (!TryGetMouseGridPosition(board, e.mousePosition, out Vector2Int gridPos))
            return;

        if (!board.IsInsideBoard(gridPos))
            return;

        Vector3 center = board.GridToWorld(gridPos);

        Handles.color = GetPreviewColor(_brushHeight);
        Handles.DrawWireCube(center, new Vector3(board.CellSize, board.CellSize, 0f));

        if (_showHeightLabels)
        {
            DrawHeightLabel(
                center + Vector3.up * board.CellSize * 0.25f,
                _brushHeight
            );
        }

        SceneView.RepaintAll();
    }

    private void DrawSolidCell(Vector3 center, float cellSize)
    {
        float half = cellSize * 0.5f;

        Vector3[] verts =
        {
            center + new Vector3(-half, -half, 0f),
            center + new Vector3(-half, half, 0f),
            center + new Vector3(half, half, 0f),
            center + new Vector3(half, -half, 0f)
        };

        Handles.DrawAAConvexPolygon(verts);
    }

    private void DrawHeightLabel(Vector3 center, int height)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                textColor = height >= GridBoard.BlockedHeight
                    ? Color.red
                    : Color.yellow
            }
        };

        string label = height >= GridBoard.BlockedHeight
            ? "X"
            : height.ToString();

        Handles.Label(center, label, style);
    }

    private Color GetHeightColor(int height, float alpha)
    {
        if (height >= GridBoard.BlockedHeight)
            return new Color(1f, 0f, 0f, alpha);

        switch (height)
        {
            case 1:
                return new Color(1f, 0.85f, 0f, alpha);

            case 2:
                return new Color(1f, 0.45f, 0f, alpha);

            default:
                return new Color(0.2f, 0.8f, 1f, alpha);
        }
    }

    private Color GetPreviewColor(int height)
    {
        if (height <= GridBoard.DefaultHeight)
            return new Color(0f, 1f, 1f, 0.75f);

        if (height >= GridBoard.BlockedHeight)
            return new Color(1f, 0f, 0f, 0.75f);

        return GetHeightColor(height, 0.75f);
    }

    private bool SetCellHeight(Vector2Int gridPos, int height)
    {
        int existingIndex = FindCellHeightIndex(gridPos);

        // Height 0은 기본값이므로 리스트에 저장하지 않는다.
        if (height <= GridBoard.DefaultHeight)
        {
            if (existingIndex < 0)
                return false;

            _cellHeightsProperty.DeleteArrayElementAtIndex(existingIndex);
            return true;
        }

        if (existingIndex >= 0)
        {
            SerializedProperty existingElement =
                _cellHeightsProperty.GetArrayElementAtIndex(existingIndex);

            SerializedProperty heightProperty =
                existingElement.FindPropertyRelative("Height");

            if (heightProperty.intValue == height)
                return false;

            heightProperty.intValue = height;
            return true;
        }

        int newIndex = _cellHeightsProperty.arraySize;
        _cellHeightsProperty.InsertArrayElementAtIndex(newIndex);

        SerializedProperty newElement =
            _cellHeightsProperty.GetArrayElementAtIndex(newIndex);

        newElement.FindPropertyRelative("GridPos").vector2IntValue = gridPos;
        newElement.FindPropertyRelative("Height").intValue = height;

        return true;
    }

    private int FindCellHeightIndex(Vector2Int gridPos)
    {
        for (int i = 0; i < _cellHeightsProperty.arraySize; i++)
        {
            SerializedProperty element = _cellHeightsProperty.GetArrayElementAtIndex(i);
            SerializedProperty gridPosProperty = element.FindPropertyRelative("GridPos");

            if (gridPosProperty.vector2IntValue == gridPos)
                return i;
        }

        return -1;
    }
}

#endif