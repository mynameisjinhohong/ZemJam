using System.Collections;
using UnityEngine;

public class GridPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridBoard _board;

    [Header("Visual References")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;

    [Header("Animation State Names")]
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _walkStateName = "Walk";

    [Header("Player Settings")]
    [SerializeField] private Vector2Int _startGridPos;
    [SerializeField] private float _moveDuration = 0.1f;

    [Header("Jump Settings")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;

    [Header("Interaction")]
    [SerializeField] private bool _interactOnEnter = true;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    [Header("Interaction Icon")]
    [SerializeField] private GameObject _interactionIconObject;

    private Vector2Int _gridPos;
    private Vector2Int _facingDir = Vector2Int.down;

    private bool _isMoving;
    private Coroutine _moveRoutine;

    private int _idleStateHash;
    private int _walkStateHash;

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

    private void Awake()
    {
        _idleStateHash = Animator.StringToHash(_idleStateName);
        _walkStateHash = Animator.StringToHash(_walkStateName);
    }

    private void Start()
    {
        _gridPos = _startGridPos;
        transform.position = _board.GridToWorld(_gridPos);

        SetInteractionIconVisible(false);
        UpdateInteractionIconVisibility();

        PlayIdleAnimation();
    }

    private void Update()
    {
        if (_isMoving)
            return;

        UpdateInteractionIconVisibility();

        HandleMoveInput();
        HandleInteractInput();
    }

    private void HandleMoveInput()
    {
        Vector2Int dir = GetPressedDirection();

        // 방향키를 누르고 있는 상태에서 점프키만 새로 눌러도 점프 시도
        if (dir == Vector2Int.zero && Input.GetKeyDown(_jumpKey))
        {
            dir = GetHeldDirection();
        }

        if (dir == Vector2Int.zero)
            return;

        bool allowJump = Input.GetKey(_jumpKey);

        TryMove(dir, allowJump);
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

    private bool TryMove(Vector2Int dir, bool allowJump)
    {
        if (dir == Vector2Int.zero)
            return false;

        _facingDir = dir;

        UpdateSpriteFlip(dir);

        Vector2Int targetPos = _gridPos + dir;

        if (!_board.CanMoveTo(_gridPos, targetPos, allowJump))
        {
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
        PlayWalkAnimation();

        Vector2Int previousGridPos = _gridPos;

        // 현재 위치를 먼저 갱신.
        // 높이 판정은 _gridPos 기준으로 처리된다.
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
            targetInteractable.TriggerOnEnter &&
            IsSameHeightAsPlayer(targetInteractable))
        {
            targetInteractable.Interact(this);
        }

        bool continuedMove = TryContinueHeldMove();

        if (!continuedMove)
        {
            PlayIdleAnimation();
            UpdateInteractionIconVisibility();
        }
    }

    private bool TryContinueHeldMove()
    {
        Vector2Int heldDir = GetHeldDirection();

        if (heldDir == Vector2Int.zero)
            return false;

        bool allowJump = Input.GetKey(_jumpKey);

        return TryMove(heldDir, allowJump);
    }

    private GridInteractable GetAvailableInteractable()
    {
        // 1. 현재 밟고 있는 칸
        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable) &&
            IsSameHeightAsPlayer(currentInteractable))
        {
            return currentInteractable;
        }

        // 2. 바라보고 있는 앞 칸 우선
        Vector2Int frontPos = _gridPos + _facingDir;

        if (_board.TryGetInteractableAt(frontPos, out GridInteractable frontInteractable) &&
            IsSameHeightAsPlayer(frontInteractable))
        {
            return frontInteractable;
        }

        // 3. 나머지 상하좌우 한 칸
        foreach (Vector2Int dir in AdjacentDirs)
        {
            if (dir == _facingDir)
                continue;

            Vector2Int checkPos = _gridPos + dir;

            if (_board.TryGetInteractableAt(checkPos, out GridInteractable adjacentInteractable) &&
                IsSameHeightAsPlayer(adjacentInteractable))
            {
                return adjacentInteractable;
            }
        }

        return null;
    }

    private GridInteractable FindNearbyInteractable()
    {
        // UI 표시 기준:
        // 현재 칸 또는 상하좌우 한 칸에 있으면서,
        // 플레이어와 같은 높이인 Interactable만 감지한다.

        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable) &&
            IsSameHeightAsPlayer(currentInteractable))
        {
            return currentInteractable;
        }

        foreach (Vector2Int dir in AdjacentDirs)
        {
            Vector2Int checkPos = _gridPos + dir;

            if (_board.TryGetInteractableAt(checkPos, out GridInteractable adjacentInteractable) &&
                IsSameHeightAsPlayer(adjacentInteractable))
            {
                return adjacentInteractable;
            }
        }

        return null;
    }

    private bool IsSameHeightAsPlayer(GridInteractable interactable)
    {
        if (interactable == null)
            return false;

        int playerHeight = _board.GetHeight(_gridPos);
        int interactableHeight = _board.GetHeight(interactable.GridPos);

        return playerHeight == interactableHeight;
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

    private void UpdateSpriteFlip(Vector2Int dir)
    {
        if (_spriteRenderer == null)
            return;

        if (dir == Vector2Int.left)
        {
            _spriteRenderer.flipX = false;
        }
        else if (dir == Vector2Int.right)
        {
            _spriteRenderer.flipX = true;
        }

        // 위/아래 이동 시에는 flip 상태를 유지한다.
        // 위/아래 전용 애니메이션이 없으므로 마지막 좌우 방향을 유지하는 편이 자연스럽다.
    }

    private void PlayIdleAnimation()
    {
        if (_animator == null)
            return;

        _animator.Play(_idleStateHash);
    }

    private void PlayWalkAnimation()
    {
        if (_animator == null)
            return;

        _animator.Play(_walkStateHash);
    }
}