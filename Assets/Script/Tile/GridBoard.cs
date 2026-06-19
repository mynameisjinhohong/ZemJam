using System.Collections.Generic;
using UnityEngine;

public class GridBoard : MonoBehaviour
{
    public const int DefaultHeight = 0;
    public const int BlockedHeight = 100;
    public const int MaxClimbHeight = 1;

    [Header("Board Settings")]
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(10, 10);
    [SerializeField] private float _cellSize = 1f;

    [Header("Cell Heights")]
    [SerializeField] private List<GridCellHeightData> _cellHeights = new();

    [Header("Interactables")]
    [SerializeField] private List<GridInteractable> _interactables = new();

    private readonly Dictionary<Vector2Int, int> _heightMap = new();
    private readonly Dictionary<Vector2Int, GridInteractable> _interactableMap = new();

    public Vector2Int BoardSize => _boardSize;
    public float CellSize => _cellSize;

    private void Awake()
    {
        BuildBoardData();
    }

    private void BuildBoardData()
    {
        _heightMap.Clear();
        _interactableMap.Clear();

        foreach (GridCellHeightData heightData in _cellHeights)
        {
            if (!IsInsideBoard(heightData.GridPos))
            {
                Debug.LogWarning($"Height cell is outside board: {heightData.GridPos}");
                continue;
            }

            _heightMap[heightData.GridPos] = heightData.Height;
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

    public int GetHeight(Vector2Int pos)
    {
        if (_heightMap.TryGetValue(pos, out int height))
            return height;

        return DefaultHeight;
    }

    public bool IsBlockedByHeight(Vector2Int pos)
    {
        return GetHeight(pos) >= BlockedHeight;
    }

    public bool CanMoveTo(Vector2Int from, Vector2Int to, bool allowJump)
    {
        if (!IsInsideBoard(to))
            return false;

        int fromHeight = GetHeight(from);
        int toHeight = GetHeight(to);

        if (toHeight >= BlockedHeight)
            return false;

        if (fromHeight >= BlockedHeight)
            return false;

        if (_interactableMap.TryGetValue(to, out GridInteractable interactable) &&
            interactable.BlocksMovement)
        {
            return false;
        }

        int heightDiff = toHeight - fromHeight;

        if (heightDiff <= 0)
            return true;

        if (heightDiff <= MaxClimbHeight)
            return allowJump;

        return false;
    }

    public bool RequiresJump(Vector2Int from, Vector2Int to)
    {
        if (!IsInsideBoard(to))
            return false;

        int fromHeight = GetHeight(from);
        int toHeight = GetHeight(to);

        if (fromHeight >= BlockedHeight || toHeight >= BlockedHeight)
            return false;

        int heightDiff = toHeight - fromHeight;

        return heightDiff > 0 && heightDiff <= MaxClimbHeight;
    }

    public bool TryGetInteractableAt(Vector2Int pos, out GridInteractable interactable)
    {
        return _interactableMap.TryGetValue(pos, out interactable);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawGridGizmos();
        DrawHeightGizmos();
    }

    private void DrawGridGizmos()
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
    }

    private void DrawHeightGizmos()
    {
        foreach (GridCellHeightData heightData in _cellHeights)
        {
            if (!IsInsideBoard(heightData.GridPos))
                continue;

            Vector3 worldPos = transform.position + new Vector3(
                heightData.GridPos.x * _cellSize,
                heightData.GridPos.y * _cellSize,
                0f
            );

            if (heightData.Height >= BlockedHeight)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(worldPos, Vector3.one * _cellSize * 0.8f);
            }
            else if (heightData.Height > 0)
            {
                Gizmos.color = Color.yellow;
                float size = Mathf.Clamp(0.35f + heightData.Height * 0.15f, 0.35f, 0.9f);
                Gizmos.DrawWireCube(worldPos, Vector3.one * _cellSize * size);
            }
        }
    }
#endif
}

[System.Serializable]
public struct GridCellHeightData
{
    public Vector2Int GridPos;
    public int Height;
}