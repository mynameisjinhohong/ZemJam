using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EndingCreditImageScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _creditImage;

    [Header("Scroll Settings")]
    [SerializeField] private float _scrollSpeed = 80f;
    [SerializeField] private float _startOffset = 100f;
    [SerializeField] private float _endOffset = 200f;
    [SerializeField] private float _startDelay = 0.5f;
    [SerializeField] private float _endDelay = 0.5f;

    [Header("Input")]
    [SerializeField] private KeyCode _fastForwardKey = KeyCode.Space;
    [SerializeField] private float _fastForwardMultiplier = 4f;
    [SerializeField] private KeyCode _skipKey = KeyCode.Escape;

    [Header("Options")]
    [SerializeField] private bool _autoPlayOnEnable = false;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent _onFinished;

    private Coroutine _scrollRoutine;
    private bool _skipRequested;

    private float DeltaTime => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        if (_viewport == null && _creditImage != null)
        {
            _viewport = _creditImage.parent as RectTransform;
        }
    }

    private void OnEnable()
    {
        if (_autoPlayOnEnable)
        {
            Play();
        }
        else
        {
            ResetPosition();
        }
    }

    public void Play()
    {
        if (_viewport == null)
        {
            Debug.LogError("[EndingCreditImageScroller] Viewport가 할당되지 않았습니다.");
            return;
        }

        if (_creditImage == null)
        {
            Debug.LogError("[EndingCreditImageScroller] Credit Image가 할당되지 않았습니다.");
            return;
        }

        if (_scrollRoutine != null)
        {
            StopCoroutine(_scrollRoutine);
        }

        _scrollRoutine = StartCoroutine(ScrollRoutine());
    }

    public void Skip()
    {
        _skipRequested = true;
    }

    public void Stop()
    {
        if (_scrollRoutine != null)
        {
            StopCoroutine(_scrollRoutine);
            _scrollRoutine = null;
        }

        _skipRequested = false;
        ResetPosition();
    }

    private IEnumerator ScrollRoutine()
    {
        _skipRequested = false;

        PrepareRectTransform();
        ResetPosition();

        yield return Delay(_startDelay);

        float endY = GetEndY();

        while (_creditImage.anchoredPosition.y < endY)
        {
            if (IsSkipPressed())
            {
                break;
            }

            float multiplier = IsFastForwardPressed() ? _fastForwardMultiplier : 1f;
            float moveAmount = _scrollSpeed * multiplier * DeltaTime;

            Vector2 position = _creditImage.anchoredPosition;
            position.y = Mathf.Min(position.y + moveAmount, endY);
            _creditImage.anchoredPosition = position;

            yield return null;
        }

        yield return Delay(_endDelay);

        _scrollRoutine = null;
        _onFinished?.Invoke();
    }

    private void PrepareRectTransform()
    {
        Vector2 anchorMin = _creditImage.anchorMin;
        Vector2 anchorMax = _creditImage.anchorMax;
        Vector2 pivot = _creditImage.pivot;

        anchorMin.x = 0.5f;
        anchorMax.x = 0.5f;
        anchorMin.y = 0f;
        anchorMax.y = 0f;
        pivot.x = 0.5f;
        pivot.y = 1f;

        _creditImage.anchorMin = anchorMin;
        _creditImage.anchorMax = anchorMax;
        _creditImage.pivot = pivot;

        Canvas.ForceUpdateCanvases();
    }

    private void ResetPosition()
    {
        if (_creditImage == null) return;

        Vector2 position = _creditImage.anchoredPosition;
        position.y = -_startOffset;
        _creditImage.anchoredPosition = position;
    }

    private float GetEndY()
    {
        float viewportHeight = _viewport.rect.height;
        float imageHeight = _creditImage.rect.height;

        return viewportHeight + imageHeight + _endOffset;
    }

    private IEnumerator Delay(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            if (IsSkipPressed())
            {
                yield break;
            }

            elapsed += DeltaTime;
            yield return null;
        }
    }

    private bool IsFastForwardPressed()
    {
        return _fastForwardKey != KeyCode.None && Input.GetKey(_fastForwardKey);
    }

    private bool IsSkipPressed()
    {
        if (_skipRequested)
        {
            return true;
        }

        if (_skipKey != KeyCode.None && Input.GetKeyDown(_skipKey))
        {
            _skipRequested = true;
            return true;
        }

        return false;
    }
}