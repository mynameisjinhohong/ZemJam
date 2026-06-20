using UnityEngine;

public class MiniGame1PlayerController : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _coyoteTime = 0.1f;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.1f;
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

    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private bool _inputEnabled = true;
    private float _autoMoveX = 0f;
    private float _horizontalInput;

    private int _idleStateHash;
    private int _walkStateHash;
    private int _jumpStateHash;

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (!enabled)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
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

        _isGrounded = Physics2D.OverlapCircle(
            _groundCheck.position,
            _groundCheckRadius,
            mask
        );

        if (_isGrounded)
            _coyoteTimer = _coyoteTime;
    }

    private void UpdateTimers()
    {
        _coyoteTimer -= Time.deltaTime;
        _jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleMove()
    {
        if (_inputEnabled)
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
            _spriteRenderer.flipX = true;
        else if (horizontal > 0f)
            _spriteRenderer.flipX = false;
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _jumpBufferTimer = _jumpBufferTime;

        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;

            // 바로 공중 상태로 처리
            _isGrounded = false;

            // 점프 순간에는 무조건 점프 애니메이션을 처음부터 재생
            PlayState(_jumpStateHash, _jumpStateName, true);
        }
    }

    private void UpdateAnimation()
    {
        if (_animator == null)
            return;

        // 핵심:
        // 땅에 닿지 않은 상태에서는 Idle / Walk로 절대 넘어가지 않음
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