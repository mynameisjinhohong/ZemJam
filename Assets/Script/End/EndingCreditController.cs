using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EndingCreditController : MonoBehaviour
{
    [Serializable]
    public class CreditSection
    {
        public string title;
        public CreditMember[] members;
    }

    [Serializable]
    public class CreditMember
    {
        public string name;
        public Sprite image;
    }

    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;

    [Header("Prefabs")]
    [SerializeField] private EndingCreditSectionItem _sectionPrefab;
    [SerializeField] private EndingCreditMemberRowItem _memberRowPrefab;

    [Header("Credit Data")]
    [SerializeField] private bool _buildOnPlay = true;
    [SerializeField] private CreditSection[] _sections;

    [Header("Scroll Settings")]
    [SerializeField] private float _scrollSpeed = 80f;
    [SerializeField] private float _startPadding = 100f;
    [SerializeField] private float _endPadding = 200f;
    [SerializeField] private float _startDelay = 1f;
    [SerializeField] private float _endDelay = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Input")]
    [SerializeField] private KeyCode _fastForwardKey = KeyCode.Space;
    [SerializeField] private float _fastForwardMultiplier = 4f;
    [SerializeField] private KeyCode _skipKey = KeyCode.Escape;

    [Header("Options")]
    [SerializeField] private bool _autoPlayOnEnable = false;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private bool _clearPreviousItemsOnBuild = true;

    [Header("Events")]
    [SerializeField] private UnityEvent _onCreditFinished;

    private Coroutine _playRoutine;
    private bool _skipRequested;

    private float DeltaTime => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        if (_viewport == null && _content != null)
        {
            _viewport = _content.parent as RectTransform;
        }
    }

    private void OnEnable()
    {
        if (_autoPlayOnEnable)
        {
            PlayCredit();
        }
        else
        {
            ResetToStartPosition();
        }
    }

    public void PlayCredit()
    {
        if (_viewport == null)
        {
            Debug.LogError("[EndingCreditController] Viewport가 할당되지 않았습니다.");
            return;
        }

        if (_content == null)
        {
            Debug.LogError("[EndingCreditController] Content가 할당되지 않았습니다.");
            return;
        }

        if (_sectionPrefab == null)
        {
            Debug.LogError("[EndingCreditController] Section Prefab이 할당되지 않았습니다.");
            return;
        }

        if (_memberRowPrefab == null)
        {
            Debug.LogError("[EndingCreditController] Member Row Prefab이 할당되지 않았습니다.");
            return;
        }

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
        }

        _playRoutine = StartCoroutine(PlayCreditRoutine());
    }

    public void SkipCredit()
    {
        _skipRequested = true;
    }

    public void StopCredit()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _skipRequested = false;
        ResetToStartPosition();
    }

    private IEnumerator PlayCreditRoutine()
    {
        _skipRequested = false;

        if (_buildOnPlay)
        {
            BuildCredits();
        }

        PrepareLayout();
        ResetToStartPosition();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            yield return FadeCanvas(1f);
        }

        yield return Delay(_startDelay);

        float endY = GetEndY();

        while (_content.anchoredPosition.y < endY)
        {
            if (IsSkipPressed())
            {
                break;
            }

            float multiplier = IsFastForwardPressed() ? _fastForwardMultiplier : 1f;
            float moveAmount = _scrollSpeed * multiplier * DeltaTime;

            Vector2 position = _content.anchoredPosition;
            position.y = Mathf.Min(position.y + moveAmount, endY);
            _content.anchoredPosition = position;

            yield return null;
        }

        yield return Delay(_endDelay);

        if (_canvasGroup != null)
        {
            yield return FadeCanvas(0f);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        _playRoutine = null;
        _onCreditFinished?.Invoke();
    }

    private void BuildCredits()
    {
        if (_clearPreviousItemsOnBuild)
        {
            ClearContent();
        }

        if (_sections == null) return;

        for (int i = 0; i < _sections.Length; i++)
        {
            CreditSection sectionData = _sections[i];
            if (sectionData == null) continue;

            EndingCreditSectionItem sectionItem = Instantiate(_sectionPrefab, _content);
            sectionItem.SetTitle(sectionData.title);

            if (sectionData.members == null) continue;

            for (int j = 0; j < sectionData.members.Length; j++)
            {
                CreditMember memberData = sectionData.members[j];
                if (memberData == null) continue;

                EndingCreditMemberRowItem rowItem =
                    Instantiate(_memberRowPrefab, sectionItem.MemberRoot);

                rowItem.SetData(memberData.name, memberData.image);
            }
        }
    }

    private void ClearContent()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Destroy(_content.GetChild(i).gameObject);
        }
    }

    private void PrepareLayout()
    {
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        Canvas.ForceUpdateCanvases();

        float preferredHeight = LayoutUtility.GetPreferredHeight(_content);
        if (preferredHeight > 0f)
        {
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        Canvas.ForceUpdateCanvases();
    }

    private void ResetToStartPosition()
    {
        if (_content == null || _viewport == null) return;

        Vector2 anchorMin = _content.anchorMin;
        Vector2 anchorMax = _content.anchorMax;
        Vector2 pivot = _content.pivot;

        anchorMin.y = 0f;
        anchorMax.y = 0f;
        pivot.y = 1f;

        _content.anchorMin = anchorMin;
        _content.anchorMax = anchorMax;
        _content.pivot = pivot;

        Vector2 position = _content.anchoredPosition;
        position.y = -_startPadding;
        _content.anchoredPosition = position;
    }

    private float GetEndY()
    {
        float viewportHeight = _viewport.rect.height;
        float contentHeight = _content.rect.height;

        return viewportHeight + contentHeight + _endPadding;
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            if (IsSkipPressed())
            {
                break;
            }

            elapsed += DeltaTime;

            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
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