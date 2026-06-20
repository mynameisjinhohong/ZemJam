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

    private Rigidbody2D _rb;
    private Collider2D _col;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _inputEnabled = true;
    private float _autoMoveX = 0f;

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }
    }

    public void SetAutoMove(float speedX) => _autoMoveX = speedX;

    public void Teleport(Vector3 worldPos)
    {
        _rb.linearVelocity = Vector2.zero;
        transform.position = worldPos;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        if (_col != null)
        {
            var zeroFriction = new PhysicsMaterial2D("ZeroFriction") { friction = 0f, bounciness = 0f };
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
            _groundCheck.localPosition = new Vector3(0f, _col.bounds.extents.y * -1f / transform.lossyScale.y, 0f);
    }

    private void Update()
    {
        CheckGround();
        UpdateTimers();

        if (_inputEnabled)
            HandleJumpInput();
    }

    private void FixedUpdate()
    {
        HandleMove();
    }

    private void CheckGround()
    {
        // Ground Layer가 Nothing(0)이면 모든 레이어 감지
        LayerMask mask = _groundLayer.value == 0 ? ~0 : _groundLayer;
        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, mask);

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
        float horizontal;

        if (_inputEnabled)
        {
            horizontal = 0f;
            if (Input.GetKey(KeyCode.A))
                horizontal = -1f;
            else if (Input.GetKey(KeyCode.D))
                horizontal = 1f;
        }
        else
        {
            horizontal = _autoMoveX;
        }

        _rb.linearVelocity = new Vector2(horizontal * _moveSpeed, _rb.linearVelocity.y);

        if (horizontal != 0f)
            transform.localScale = new Vector3(Mathf.Sign(horizontal), 1f, 1f);
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
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }
}
