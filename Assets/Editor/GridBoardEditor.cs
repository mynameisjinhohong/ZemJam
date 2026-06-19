#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridBoard))]
public class GridBoardEditor : Editor
{
    private SerializedProperty _blockedCellsProperty;

    private bool _editMode;
    private EditToolMode _toolMode = EditToolMode.PaintBlocked;

    private enum EditToolMode
    {
        PaintBlocked,
        EraseBlocked
    }

    private void OnEnable()
    {
        _blockedCellsProperty = serializedObject.FindProperty("_blockedCells");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Blocked Cell Editor", EditorStyles.boldLabel);

        _editMode = EditorGUILayout.Toggle("Edit Mode", _editMode);

        using (new EditorGUI.DisabledScope(!_editMode))
        {
            _toolMode = (EditToolMode)EditorGUILayout.EnumPopup("Tool Mode", _toolMode);
        }

        if (_editMode)
        {
            EditorGUILayout.HelpBox(
                "Scene View에서 칸을 클릭하거나 드래그하면 Blocked Cell을 편집합니다.\n" +
                "PaintBlocked: 막힌 칸 추가\n" +
                "EraseBlocked: 막힌 칸 제거",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Clear All Blocked Cells"))
        {
            if (EditorUtility.DisplayDialog(
                    "Clear Blocked Cells",
                    "모든 Blocked Cell을 삭제하시겠습니까?",
                    "삭제",
                    "취소"))
            {
                Undo.RecordObject(target, "Clear Blocked Cells");

                _blockedCellsProperty.ClearArray();

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

        HandleSceneInput(board);
    }

    private void HandleSceneInput(GridBoard board)
    {
        Event e = Event.current;

        if (e == null)
            return;

        if (e.type != EventType.MouseDown &&
            e.type != EventType.MouseDrag)
        {
            return;
        }

        if (e.button != 0)
            return;

        // Alt + 좌클릭은 Scene View 카메라 조작에 쓰이므로 무시
        if (e.alt)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Plane boardPlane = new Plane(Vector3.forward, board.transform.position);

        if (!boardPlane.Raycast(ray, out float distance))
            return;

        Vector3 worldPoint = ray.GetPoint(distance);
        Vector2Int gridPos = board.WorldToGrid(worldPoint);

        if (!board.IsInsideBoard(gridPos))
            return;

        Undo.RecordObject(board, "Edit Blocked Cell");

        serializedObject.Update();

        switch (_toolMode)
        {
            case EditToolMode.PaintBlocked:
                AddBlockedCell(gridPos);
                break;

            case EditToolMode.EraseBlocked:
                RemoveBlockedCell(gridPos);
                break;
        }

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(board);
        SceneView.RepaintAll();

        e.Use();
    }

    private void DrawBoardOverlay(GridBoard board)
    {
        Vector2Int boardSize = board.BoardSize;
        float cellSize = board.CellSize;

        Handles.color = new Color(1f, 1f, 1f, 0.25f);

        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 center = board.GridToWorld(cell);

                Vector3 size = new Vector3(cellSize, cellSize, 0f);

                Handles.DrawWireCube(center, size);
            }
        }

        serializedObject.Update();

        for (int i = 0; i < _blockedCellsProperty.arraySize; i++)
        {
            SerializedProperty element = _blockedCellsProperty.GetArrayElementAtIndex(i);
            Vector2Int cell = element.vector2IntValue;

            if (!board.IsInsideBoard(cell))
                continue;

            Vector3 center = board.GridToWorld(cell);

            Handles.color = new Color(1f, 0f, 0f, 0.25f);
            DrawSolidCell(center, cellSize);

            Handles.color = Color.red;
            Handles.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
        }

        if (_editMode)
        {
            DrawMousePreview(board);
        }
    }

    private void DrawMousePreview(GridBoard board)
    {
        Event e = Event.current;

        if (e == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane boardPlane = new Plane(Vector3.forward, board.transform.position);

        if (!boardPlane.Raycast(ray, out float distance))
            return;

        Vector3 worldPoint = ray.GetPoint(distance);
        Vector2Int gridPos = board.WorldToGrid(worldPoint);

        if (!board.IsInsideBoard(gridPos))
            return;

        Vector3 center = board.GridToWorld(gridPos);

        Handles.color = _toolMode == EditToolMode.PaintBlocked
            ? new Color(1f, 0f, 0f, 0.45f)
            : new Color(0f, 1f, 1f, 0.45f);

        Handles.DrawWireCube(center, new Vector3(board.CellSize, board.CellSize, 0f));
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

    private bool ContainsBlockedCell(Vector2Int cell)
    {
        for (int i = 0; i < _blockedCellsProperty.arraySize; i++)
        {
            SerializedProperty element = _blockedCellsProperty.GetArrayElementAtIndex(i);

            if (element.vector2IntValue == cell)
                return true;
        }

        return false;
    }

    private void AddBlockedCell(Vector2Int cell)
    {
        if (ContainsBlockedCell(cell))
            return;

        int index = _blockedCellsProperty.arraySize;
        _blockedCellsProperty.InsertArrayElementAtIndex(index);

        SerializedProperty element = _blockedCellsProperty.GetArrayElementAtIndex(index);
        element.vector2IntValue = cell;
    }

    private void RemoveBlockedCell(Vector2Int cell)
    {
        for (int i = 0; i < _blockedCellsProperty.arraySize; i++)
        {
            SerializedProperty element = _blockedCellsProperty.GetArrayElementAtIndex(i);

            if (element.vector2IntValue != cell)
                continue;

            _blockedCellsProperty.DeleteArrayElementAtIndex(i);
            return;
        }
    }
}

#endif