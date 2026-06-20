using UnityEngine;

public class MiniGame1PlayerController : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.05f; // 반지름을 조금 더 정밀하게 줄였습니다.
    [SerializeField] private LayerMask _groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Animator State Names")]
    [SerializeField] private string _idleStateName = "CatIdle";
    [SerializeField] private string _walkStateName = "CatWalk";
    [SerializeField] private string _jumpStateName = "CatJump";

    [SerializeField] private float _walkAnimThreshold = 0.05f;

    private Rigidbody2D _rb;
    private Collider2D _col;

    private bool _isGrounded;
    private bool _wasGrounded;

    private float _jumpBufferTimer;

    private bool _inputEnabled = true;
    private float _autoMoveX = 0f;
    private float _horizontalInput;

    private int _idleStateHash;
    private int _walkStateHash;
    private int _jumpStateHash;

    // 주변 충돌체를 감지해 담아둘 배열 미리 선언 (가비지 컬렉터 방지)
    private Collider2D[] _groundCheckResults = new Collider2D[5];

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (!enabled)
        {
            _jumpBufferTimer = 0f;
        }
    }

    public void SetAutoMove(float speedX)
    {
        _autoMoveX = speedX;
    }

    public void Teleport(Vector3 worldPos)
    {
        _rb.linearVelocity = Vector2.zero;
        transform.position = worldPos;

        _isGrounded = true;
        _wasGrounded = true;

        PlayState(_idleStateHash, _idleStateName, true);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _idleStateHash = Animator.StringToHash(_idleStateName);
        _walkStateHash = Animator.StringToHash(_walkStateName);
        _jumpStateHash = Animator.StringToHash(_jumpStateName);

        if (_col != null)
        {
            var zeroFriction = new PhysicsMaterial2D("ZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };

            _col.sharedMaterial = zeroFriction;
        }

        if (_groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _groundCheck = go.transform;
        }
    }

    private void Start()
    {
        if (_col != null)
        {
            _groundCheck.localPosition = new Vector3(
                0f,
                _col.bounds.extents.y * -1f / transform.lossyScale.y,
                0f
            );
        }
    }

    private void Update()
    {
        _wasGrounded = _isGrounded;

        CheckGround();
        UpdateTimers();

        if (_inputEnabled)
            HandleJumpInput();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        HandleMove();
    }

    private void CheckGround()
    {
        LayerMask mask = _groundLayer.value == 0 ? ~0 : _groundLayer;

        // 💡 [버그 수정 핵심] 단순 중첩 검사가 아닌 영역 안의 모든 충돌체를 검사합니다.
        int count = Physics2D.OverlapCircleNonAlloc(
            _groundCheck.position,
            _groundCheckRadius,
            _groundCheckResults,
            mask
        );

        _isGrounded = false;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _groundCheckResults[i];

            // 1. 자기 자신의 충돌체(몸통)는 무시합니다.
            if (hit == _col) continue;

            // 2. 이펙트나 트리거 전용으로 만든 충돌체는 무시합니다.
            if (hit.isTrigger) continue;

            // 위 조건들을 피해 갔다면 진짜 밟을 수 있는 '땅'입니다.
            _isGrounded = true;
            break;
        }
    }

    private void UpdateTimers()
    {
        _jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleMove()
    {
        bool uiBlocked = GameManager.Instance != null && !GameManager.Instance.CanPlayerMove;

        // UI가 이동을 막더라도 강제 자동이동(인트로 등)은 허용
        if (uiBlocked && _autoMoveX == 0f)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _horizontalInput = 0f;
            return;
        }

        if (!uiBlocked && _inputEnabled)
        {
            _horizontalInput = 0f;

            if (Input.GetKey(KeyCode.A))
                _horizontalInput = -1f;
            else if (Input.GetKey(KeyCode.D))
                _horizontalInput = 1f;
        }
        else
        {
            _horizontalInput = _autoMoveX;
        }

        _rb.linearVelocity = new Vector2(
            _horizontalInput * _moveSpeed,
            _rb.linearVelocity.y
        );

        UpdateFacingDirection(_horizontalInput);
    }

    private void UpdateFacingDirection(float horizontal)
    {
        if (_spriteRenderer == null)
            return;

        if (horizontal < 0f)
            _spriteRenderer.flipX = false;
        else if (horizontal > 0f)
            _spriteRenderer.flipX = true;
    }

    private void HandleJumpInput()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerMove)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
            _jumpBufferTimer = _jumpBufferTime;

        // 확실하게 바닥에 딛고 서 있는 순간에만 점프 가능하게 수정
        if (_jumpBufferTimer > 0f && _isGrounded && Mathf.Abs(_rb.linearVelocity.y) < 0.01f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _jumpBufferTimer = 0f;
            _isGrounded = false;

            PlayState(_jumpStateHash, _jumpStateName, true);
        }
    }

    private void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (!_isGrounded)
        {
            PlayState(_jumpStateHash, _jumpStateName, false);
            return;
        }

        bool isWalk = Mathf.Abs(_rb.linearVelocity.x) > _walkAnimThreshold;

        if (isWalk)
            PlayState(_walkStateHash, _walkStateName, false);
        else
            PlayState(_idleStateHash, _idleStateName, false);
    }

    private void PlayState(int stateHash, string stateName, bool forceRestart)
    {
        if (_animator == null)
            return;

        AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);

        bool isCurrentState = currentState.shortNameHash == stateHash;

        if (isCurrentState && !forceRestart)
            return;

        if (forceRestart)
            _animator.Play(stateName, 0, 0f);
        else
            _animator.Play(stateName, 0);
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }
}