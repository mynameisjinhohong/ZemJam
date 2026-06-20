using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICharacterHoverEffect : MonoBehaviour
{
    [Header("Hover Effect")]
    [SerializeField] private float _hoverScale = 1.08f;
    [SerializeField] private float _hoverYOffset = 25f;
    [SerializeField] private float _duration = 0.12f;

    [Header("Optional")]
    [SerializeField] private bool _bringToFrontOnHover = true;
    [SerializeField] private bool _useUnscaledTime = true;

    private RectTransform _rectTransform;

    private Vector3 _originScale;
    private Vector2 _originAnchoredPosition;
    private int _originSiblingIndex;

    private Coroutine _currentCoroutine;
    private bool _isHovering;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        _originScale = _rectTransform.localScale;
        _originAnchoredPosition = _rectTransform.anchoredPosition;
        _originSiblingIndex = transform.GetSiblingIndex();
    }

    private void OnEnable()
    {
        ResetInstant();
    }

    /// <summary>
    /// EventTrigger - Pointer Enter에 연결
    /// </summary>
    public void HoverEnter(BaseEventData eventData)
    {
        HoverEnter();
    }

    /// <summary>
    /// 파라미터 없는 호출용
    /// </summary>
    public void HoverEnter()
    {
        if (_isHovering) return;

        _isHovering = true;

        if (_bringToFrontOnHover)
        {
            transform.SetAsLastSibling();
        }

        Vector3 targetScale = _originScale * _hoverScale;
        Vector2 targetPosition = _originAnchoredPosition + new Vector2(0f, _hoverYOffset);

        PlayTween(targetScale, targetPosition);
    }

    /// <summary>
    /// EventTrigger - Pointer Exit에 연결
    /// </summary>
    public void HoverExit(BaseEventData eventData)
    {
        HoverExit();
    }

    /// <summary>
    /// 파라미터 없는 호출용
    /// </summary>
    public void HoverExit()
    {
        if (!_isHovering) return;

        _isHovering = false;

        PlayTween(_originScale, _originAnchoredPosition, () =>
        {
            if (_bringToFrontOnHover)
            {
                transform.SetSiblingIndex(_originSiblingIndex);
            }
        });
    }

    private void PlayTween(Vector3 targetScale, Vector2 targetPosition, System.Action onComplete = null)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(TweenRoutine(targetScale, targetPosition, onComplete));
    }

    private IEnumerator TweenRoutine(Vector3 targetScale, Vector2 targetPosition, System.Action onComplete)
    {
        Vector3 startScale = _rectTransform.localScale;
        Vector2 startPosition = _rectTransform.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / _duration);
            t = EaseOutCubic(t);

            _rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            _rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);

            yield return null;
        }

        _rectTransform.localScale = targetScale;
        _rectTransform.anchoredPosition = targetPosition;

        _currentCoroutine = null;
        onComplete?.Invoke();
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void ResetInstant()
    {
        if (_rectTransform == null) return;

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        _isHovering = false;
        _rectTransform.localScale = _originScale;
        _rectTransform.anchoredPosition = _originAnchoredPosition;
    }
}