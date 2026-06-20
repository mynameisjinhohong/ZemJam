using UnityEngine;

public abstract class MiniGame3GridObject : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPosition;

    public Vector2Int GridPosition => _gridPosition;

    public void SetGridPosition(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }
}