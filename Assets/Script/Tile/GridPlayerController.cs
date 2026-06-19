using System.Collections;
using UnityEngine;

public class GridPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridBoard _board;

    [Header("Player Settings")]
    [SerializeField] private Vector2Int _startGridPos;
    [SerializeField] private float _moveDuration = 0.1f;

    [Header("Interaction")]
    [SerializeField] private bool _interactOnEnter = true;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    private Vector2Int _gridPos;
    private Vector2Int _facingDir = Vector2Int.down;

    private bool _isMoving;
    private Coroutine _moveRoutine;

    public Vector2Int GridPos => _gridPos;
    public Vector2Int FacingDir => _facingDir;
    public bool IsMoving => _isMoving;

    private void Start()
    {
        _gridPos = _startGridPos;
        transform.position = _board.GridToWorld(_gridPos);
    }

    private void Update()
    {
        if (_isMoving)
            return;

        HandleMoveInput();
        HandleInteractInput();
    }

    private void HandleMoveInput()
    {
        Vector2Int dir = GetInputDirection();

        if (dir == Vector2Int.zero)
            return;

        _facingDir = dir;

        Vector2Int targetPos = _gridPos + dir;

        if (!_board.CanMoveTo(targetPos))
            return;

        GridInteractable interactable = null;
        _board.TryGetInteractableAt(targetPos, out interactable);

        MoveTo(targetPos, interactable);
    }

    private void HandleInteractInput()
    {
        if (!Input.GetKeyDown(_interactKey))
            return;

        // 1. 현재 밟고 있는 칸에 상호작용 오브젝트가 있는 경우
        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable))
        {
            currentInteractable.Interact(this);
            return;
        }

        // 2. 바라보고 있는 앞 칸에 상호작용 오브젝트가 있는 경우
        Vector2Int frontPos = _gridPos + _facingDir;

        if (_board.TryGetInteractableAt(frontPos, out GridInteractable frontInteractable))
        {
            frontInteractable.Interact(this);
        }
    }

    private Vector2Int GetInputDirection()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return Vector2Int.up;

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return Vector2Int.down;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            return Vector2Int.left;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            return Vector2Int.right;

        return Vector2Int.zero;
    }

    private void MoveTo(Vector2Int targetGridPos, GridInteractable targetInteractable)
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(MoveRoutine(targetGridPos, targetInteractable));
    }

    private IEnumerator MoveRoutine(Vector2Int targetGridPos, GridInteractable targetInteractable)
    {
        _isMoving = true;

        Vector2Int previousGridPos = _gridPos;
        _gridPos = targetGridPos;

        Vector3 startWorldPos = _board.GridToWorld(previousGridPos);
        Vector3 targetWorldPos = _board.GridToWorld(targetGridPos);

        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / _moveDuration;
            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);

            yield return null;
        }

        transform.position = targetWorldPos;

        _isMoving = false;
        _moveRoutine = null;

        if (_interactOnEnter &&
            targetInteractable != null &&
            targetInteractable.TriggerOnEnter)
        {
            targetInteractable.Interact(this);
        }
    }
}