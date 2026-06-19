using System.Collections.Generic;
using UnityEngine;

public class GridBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(10, 10);
    [SerializeField] private float _cellSize = 1f;

    [Header("Blocked Cells")]
    [SerializeField] private List<Vector2Int> _blockedCells = new();

    [Header("Interactables")]
    [SerializeField] private List<GridInteractable> _interactables = new();

    private readonly HashSet<Vector2Int> _blockedCellSet = new();
    private readonly Dictionary<Vector2Int, GridInteractable> _interactableMap = new();

    public Vector2Int BoardSize => _boardSize;
    public float CellSize => _cellSize;

    private void Awake()
    {
        BuildBoardData();
    }

    private void BuildBoardData()
    {
        _blockedCellSet.Clear();
        _interactableMap.Clear();

        foreach (Vector2Int blockedCell in _blockedCells)
        {
            if (!IsInsideBoard(blockedCell))
            {
                Debug.LogWarning($"Blocked cell is outside board: {blockedCell}");
                continue;
            }

            _blockedCellSet.Add(blockedCell);
        }

        foreach (GridInteractable interactable in _interactables)
        {
            if (interactable == null)
                continue;

            Vector2Int pos = interactable.GridPos;

            if (!IsInsideBoard(pos))
            {
                Debug.LogWarning($"Interactable is outside board: {interactable.name}, Pos: {pos}");
                continue;
            }

            if (_interactableMap.ContainsKey(pos))
            {
                Debug.LogWarning($"Multiple interactables exist at same grid position: {pos}");
                continue;
            }

            interactable.Init(this);
            _interactableMap.Add(pos, interactable);
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return transform.position + new Vector3(
            gridPos.x * _cellSize,
            gridPos.y * _cellSize,
            0f
        );
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        return new Vector2Int(
            Mathf.RoundToInt(localPos.x / _cellSize),
            Mathf.RoundToInt(localPos.y / _cellSize)
        );
    }

    public bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 &&
               pos.y >= 0 &&
               pos.x < _boardSize.x &&
               pos.y < _boardSize.y;
    }

    public bool IsBlocked(Vector2Int pos)
    {
        return _blockedCellSet.Contains(pos);
    }

    public bool CanMoveTo(Vector2Int pos)
    {
        if (!IsInsideBoard(pos))
            return false;

        if (IsBlocked(pos))
            return false;

        return true;
    }

    public bool TryGetInteractableAt(Vector2Int pos, out GridInteractable interactable)
    {
        return _interactableMap.TryGetValue(pos, out interactable);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;

        for (int x = 0; x < _boardSize.x; x++)
        {
            for (int y = 0; y < _boardSize.y; y++)
            {
                Vector3 worldPos = transform.position + new Vector3(
                    x * _cellSize,
                    y * _cellSize,
                    0f
                );

                Gizmos.DrawWireCube(worldPos, Vector3.one * _cellSize);
            }
        }

        Gizmos.color = Color.red;

        foreach (Vector2Int blockedCell in _blockedCells)
        {
            Vector3 worldPos = transform.position + new Vector3(
                blockedCell.x * _cellSize,
                blockedCell.y * _cellSize,
                0f
            );

            Gizmos.DrawCube(worldPos, Vector3.one * _cellSize * 0.8f);
        }
    }
#endif
}