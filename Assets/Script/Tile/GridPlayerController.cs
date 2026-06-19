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

    [Header("Interaction Icon")]
    [SerializeField] private GameObject _interactionIconObject;

    private Vector2Int _gridPos;
    private Vector2Int _facingDir = Vector2Int.down;

    private bool _isMoving;
    private Coroutine _moveRoutine;

    public Vector2Int GridPos => _gridPos;
    public Vector2Int FacingDir => _facingDir;
    public bool IsMoving => _isMoving;

    private static readonly Vector2Int[] AdjacentDirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        _gridPos = _startGridPos;
        transform.position = _board.GridToWorld(_gridPos);

        SetInteractionIconVisible(false);
        UpdateInteractionIconVisibility();
    }

    private void Update()
    {
        if (_isMoving)
            return;

        // 핵심 변경:
        // 이동 중이 아닐 때는 매 프레임 현재 위치 기준으로 아이콘 상태를 갱신한다.
        // 이제 바라보는 방향과 무관하게 주변 1칸에 Interactable이 있으면 켜진다.
        UpdateInteractionIconVisibility();

        HandleMoveInput();
        HandleInteractInput();
    }

    private void HandleMoveInput()
    {
        Vector2Int dir = GetPressedDirection();

        if (dir == Vector2Int.zero)
            return;

        TryMove(dir);
    }

    private void HandleInteractInput()
    {
        if (!Input.GetKeyDown(_interactKey))
            return;

        GridInteractable interactable = GetAvailableInteractable();

        if (interactable == null)
            return;

        interactable.Interact(this);

        UpdateInteractionIconVisibility();
    }

    private bool TryMove(Vector2Int dir)
    {
        if (dir == Vector2Int.zero)
            return false;

        _facingDir = dir;

        Vector2Int targetPos = _gridPos + dir;

        if (!_board.CanMoveTo(targetPos))
        {
            // 이동은 못 해도 방향은 바뀌었으므로,
            // 상호작용 대상 선택 우선순위에는 영향을 줄 수 있다.
            UpdateInteractionIconVisibility();
            return false;
        }

        _board.TryGetInteractableAt(targetPos, out GridInteractable targetInteractable);

        MoveTo(targetPos, targetInteractable);
        return true;
    }

    private Vector2Int GetPressedDirection()
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

    private Vector2Int GetHeldDirection()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            return Vector2Int.up;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            return Vector2Int.down;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            return Vector2Int.left;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
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

        SetInteractionIconVisible(false);

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

        bool continuedMove = TryContinueHeldMove();

        if (!continuedMove)
            UpdateInteractionIconVisibility();
    }

    private bool TryContinueHeldMove()
    {
        Vector2Int heldDir = GetHeldDirection();

        if (heldDir == Vector2Int.zero)
            return false;

        // 핵심 변경:
        // 키가 눌려 있어도 실제 이동에 실패하면 false를 반환한다.
        return TryMove(heldDir);
    }

    private GridInteractable GetAvailableInteractable()
    {
        // E를 눌렀을 때 상호작용할 대상.
        // 여러 개가 주변에 있을 수 있으므로 우선순위는 유지한다.
        //
        // 1. 현재 밟고 있는 칸
        // 2. 바라보고 있는 앞 칸
        // 3. 나머지 상하좌우 한 칸

        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable))
            return currentInteractable;

        Vector2Int frontPos = _gridPos + _facingDir;

        if (_board.TryGetInteractableAt(frontPos, out GridInteractable frontInteractable))
            return frontInteractable;

        foreach (Vector2Int dir in AdjacentDirs)
        {
            if (dir == _facingDir)
                continue;

            Vector2Int checkPos = _gridPos + dir;

            if (_board.TryGetInteractableAt(checkPos, out GridInteractable adjacentInteractable))
                return adjacentInteractable;
        }

        return null;
    }

    private GridInteractable FindNearbyInteractable()
    {
        // 아이콘 표시 기준.
        // 이 함수는 바라보는 방향을 전혀 보지 않는다.
        // 현재 칸 또는 상하좌우 한 칸에 Interactable이 있으면 반환한다.

        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable))
            return currentInteractable;

        foreach (Vector2Int dir in AdjacentDirs)
        {
            Vector2Int checkPos = _gridPos + dir;

            if (_board.TryGetInteractableAt(checkPos, out GridInteractable adjacentInteractable))
                return adjacentInteractable;
        }

        return null;
    }

    private void UpdateInteractionIconVisibility()
    {
        GridInteractable nearbyInteractable = FindNearbyInteractable();

        SetInteractionIconVisible(nearbyInteractable != null);
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (_interactionIconObject == null)
            return;

        if (_interactionIconObject.activeSelf == visible)
            return;

        _interactionIconObject.SetActive(visible);
    }
}