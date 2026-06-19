using UnityEngine;
using UnityEngine.Events;

public class GridInteractable : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPos;

    [Header("Interaction")]
    [SerializeField] private bool _triggerOnEnter = false;
    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private UnityEvent _onInteracted;

    private GridBoard _board;

    public Vector2Int GridPos => _gridPos;
    public bool TriggerOnEnter => _triggerOnEnter;
    public bool BlocksMovement => _blocksMovement;

    public void Init(GridBoard board)
    {
        _board = board;
        transform.position = _board.GridToWorld(_gridPos);
    }

    public void Interact(GridPlayerController player)
    {
        _onInteracted?.Invoke();
    }
}