using UnityEngine;

public abstract class MiniGame3GridObject : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPosition;

    private SpriteRenderer _sr;

    public Vector2Int GridPosition => _gridPosition;

    protected virtual void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void SetGridPosition(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        UpdateSortingOrder(worldPosition.y);
    }

    private void UpdateSortingOrder(float worldY)
    {
        if (_sr == null) return;
        // Y가 낮을수록(앞) 높은 소팅 오더 → 앞에 그려짐
        _sr.sortingOrder = Mathf.RoundToInt(-worldY * 100f);
    }
}
