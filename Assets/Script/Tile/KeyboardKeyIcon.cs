using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class KeyboardKeyIcon : MonoBehaviour
{
    [Header("Key")]
    [SerializeField] private KeyCode _keyCode = KeyCode.Space;

    [Header("Sprites")]
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _pressedSprite;

    [Header("Pressed Feel")]
    [SerializeField] private float _minPressedVisibleTime = 0.12f;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _hideRoutine;
    private bool _isHiding;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        SetNormalSprite();
    }

    private void OnEnable()
    {
        _isHiding = false;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        SetNormalSprite();
    }

    private void Update()
    {
        if (_isHiding)
            return;

        if (Input.GetKey(_keyCode))
        {
            SetPressedSprite();
        }
        else
        {
            SetNormalSprite();
        }
    }

    public void Show()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        _isHiding = false;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SetNormalSprite();
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        _isHiding = true;

        if (Input.GetKey(_keyCode))
        {
            SetPressedSprite();
            yield return new WaitForSeconds(_minPressedVisibleTime);
        }

        _isHiding = false;
        _hideRoutine = null;

        gameObject.SetActive(false);
    }

    private void SetNormalSprite()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            return;

        if (_normalSprite != null)
            _spriteRenderer.sprite = _normalSprite;
    }

    private void SetPressedSprite()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            return;

        if (_pressedSprite != null)
            _spriteRenderer.sprite = _pressedSprite;
    }
}