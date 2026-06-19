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
    [SerializeField] private string _jumpStateName = "Jump";

    [Header("Animation Settings")]
    [SerializeField] private bool _waitJumpAnimationEnd = true;
    [SerializeField] private float _jumpAnimationFallbackDuration = 0.35f;

    [Header("Player Settings")]
    [SerializeField] private Vector2Int _startGridPos;
    [SerializeField] private float _moveDuration = 0.1f;

    [Header("Jump Settings")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Z;

    [Header("Interaction")]
    [SerializeField] private bool _interactOnEnter = true;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    [Header("Action Icons")]
    [SerializeField] private GameObject _interactionIconObject;
    [SerializeField] private GameObject _jumpIconObject;

    private Vector2Int _gridPos;
    private Vector2Int _facingDir = Vector2Int.down;

    private bool _isMoving;
    private Coroutine _moveRoutine;

    private int _idleStateHash;
    private int _walkStateHash;
    private int _jumpStateHash;

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
        _jumpStateHash = Animator.StringToHash(_jumpStateName);
    }

    private void Start()
    {
        _gridPos = _startGridPos;
        transform.position = _board.GridToWorld(_gridPos);

        SetInteractionIconVisible(false);
        SetJumpIconVisible(false);

        UpdateActionIconsVisibility();

        PlayIdleAnimation();
    }

    private void Update()
    {
        if (_isMoving)
            return;

        UpdateActionIconsVisibility();

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

        UpdateActionIconsVisibility();
    }

    private bool TryMove(Vector2Int dir, bool allowJump)
    {
        if (dir == Vector2Int.zero)
            return false;

        _facingDir = dir;

        UpdateSpriteFlip(dir);

        Vector2Int targetPos = _gridPos + dir;

        bool requiresJump = _board.RequiresJump(_gridPos, targetPos);

        if (!_board.CanMoveTo(_gridPos, targetPos, allowJump))
        {
            UpdateActionIconsVisibility();
            return false;
        }

        bool isJumpMove = requiresJump && allowJump;

        _board.TryGetInteractableAt(targetPos, out GridInteractable targetInteractable);

        MoveTo(targetPos, targetInteractable, isJumpMove);

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

    private void MoveTo(
        Vector2Int targetGridPos,
        GridInteractable targetInteractable,
        bool isJumpMove
    )
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(
            MoveRoutine(targetGridPos, targetInteractable, isJumpMove)
        );
    }

    private IEnumerator MoveRoutine(
        Vector2Int targetGridPos,
        GridInteractable targetInteractable,
        bool isJumpMove
    )
    {
        _isMoving = true;

        SetInteractionIconVisible(false);
        SetJumpIconVisible(false);

        if (isJumpMove)
            PlayJumpAnimation();
        else
            PlayWalkAnimation();

        Vector2Int previousGridPos = _gridPos;

        // 현재 위치를 먼저 갱신한다.
        // 이후 높이 판정은 _gridPos 기준으로 처리된다.
        _gridPos = targetGridPos;

        Vector3 startWorldPos = _board.GridToWorld(previousGridPos);
        Vector3 targetWorldPos = _board.GridToWorld(targetGridPos);

        float moveDuration = isJumpMove ? _jumpAnimationFallbackDuration : _moveDuration;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);

            yield return null;
        }

        transform.position = targetWorldPos;

        // 핵심 변경:
        // 점프 이동이면 위치 이동이 끝난 뒤에도 Jump 애니메이션이 끝날 때까지 기다린다.
        if (isJumpMove && _waitJumpAnimationEnd)
        {
            yield return WaitForCurrentAnimationEnd(
                _jumpStateHash,
                _jumpAnimationFallbackDuration
            );
        }

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
            UpdateActionIconsVisibility();
        }
    }

    private IEnumerator WaitForCurrentAnimationEnd(int stateHash, float fallbackDuration)
    {
        if (_animator == null)
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        // Animator.Play 직후 같은 프레임에는 상태 정보가 아직 갱신되지 않을 수 있으므로 1프레임 대기
        yield return null;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // 상태 이름이 잘못되었거나 Transition 때문에 현재 상태가 Jump가 아니면 fallback 시간만큼 대기
        if (stateInfo.shortNameHash != stateHash)
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        // Jump 애니메이션이 loop면 normalizedTime이 계속 증가하므로,
        // 점프 애니메이션은 Loop Time을 꺼두는 것을 권장한다.
        while (true)
        {
            stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.shortNameHash != stateHash)
                yield break;

            if (stateInfo.normalizedTime >= 1f)
                yield break;

            yield return null;
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
        if (_board.TryGetInteractableAt(_gridPos, out GridInteractable currentInteractable) &&
            IsSameHeightAsPlayer(currentInteractable))
        {
            return currentInteractable;
        }

        Vector2Int frontPos = _gridPos + _facingDir;

        if (_board.TryGetInteractableAt(frontPos, out GridInteractable frontInteractable) &&
            IsSameHeightAsPlayer(frontInteractable))
        {
            return frontInteractable;
        }

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

    private bool HasNearbyJumpableCell()
    {
        foreach (Vector2Int dir in AdjacentDirs)
        {
            Vector2Int targetPos = _gridPos + dir;

            if (IsJumpableCell(targetPos))
                return true;
        }

        return false;
    }

    private bool IsJumpableCell(Vector2Int targetPos)
    {
        if (!_board.IsInsideBoard(targetPos))
            return false;

        if (!_board.RequiresJump(_gridPos, targetPos))
            return false;

        return _board.CanMoveTo(_gridPos, targetPos, true);
    }

    private bool IsSameHeightAsPlayer(GridInteractable interactable)
    {
        if (interactable == null)
            return false;

        int playerHeight = _board.GetHeight(_gridPos);
        int interactableHeight = _board.GetHeight(interactable.GridPos);

        return playerHeight == interactableHeight;
    }

    private void UpdateActionIconsVisibility()
    {
        UpdateInteractionIconVisibility();
        UpdateJumpIconVisibility();
    }

    private void UpdateInteractionIconVisibility()
    {
        GridInteractable nearbyInteractable = FindNearbyInteractable();

        SetInteractionIconVisible(nearbyInteractable != null);
    }

    private void UpdateJumpIconVisibility()
    {
        SetJumpIconVisible(HasNearbyJumpableCell());
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (_interactionIconObject == null)
            return;

        if (_interactionIconObject.activeSelf == visible)
            return;

        _interactionIconObject.SetActive(visible);
    }

    private void SetJumpIconVisible(bool visible)
    {
        if (_jumpIconObject == null)
            return;

        if (_jumpIconObject.activeSelf == visible)
            return;

        _jumpIconObject.SetActive(visible);
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
    }

    private void PlayIdleAnimation()
    {
        if (_animator == null)
            return;

        _animator.Play(_idleStateHash, 0, 0f);
    }

    private void PlayWalkAnimation()
    {
        if (_animator == null)
            return;

        _animator.Play(_walkStateHash, 0, 0f);
    }

    private void PlayJumpAnimation()
    {
        if (_animator == null)
            return;

        _animator.Play(_jumpStateHash, 0, 0f);
    }
}