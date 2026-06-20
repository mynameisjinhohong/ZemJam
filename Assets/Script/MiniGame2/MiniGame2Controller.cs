using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum MiniGame2State
{
    Day,
    TransitionToNight,
    Night,
    Cleared
}

public class MiniGame2Controller : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _sun;
    [SerializeField] private RectTransform _moon;
    [SerializeField] private CanvasGroup _moonGroup;
    [SerializeField] private CanvasGroup _nightOverlayGroup;
    [SerializeField] private CanvasGroup _catEyesGroup;
    [SerializeField] private MiniGame2ClockPuzzleController _clockPuzzle;

    [Header("Puzzle Settings")]
    [SerializeField, Range(1, 12)] private int _answerHour = 7;

    [Header("Transition Settings")]
    [SerializeField] private float _transitionDuration = 1.5f;
    [SerializeField] private float _catEyesFadeDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _nightOverlayAlpha = 0.65f;

    [Header("Sun Arc Movement")]
    [SerializeField] private Vector2 _sunStartPosition = new Vector2(500f, 300f);
    [SerializeField] private Vector2 _sunEndPosition = new Vector2(-500f, -250f);
    [SerializeField] private float _sunArcHeight = 250f;

    [Header("Moon Arc Movement")]
    [SerializeField] private Vector2 _moonStartPosition = new Vector2(500f, -250f);
    [SerializeField] private Vector2 _moonEndPosition = new Vector2(350f, 250f);
    [SerializeField] private float _moonArcHeight = 120f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onClear;

    private MiniGame2State _state = MiniGame2State.Day;
    private Coroutine _transitionCoroutine;

    public MiniGame2State State => _state;

    private void Awake()
    {
        InitializeView();

        if (_clockPuzzle != null)
        {
            _clockPuzzle.SetAnswer(_answerHour);
            _clockPuzzle.SetInteractable(false);
        }
    }

    private void OnEnable()
    {
        if (_clockPuzzle != null)
        {
            _clockPuzzle.OnCorrect += HandleClockCorrect;
        }
    }

    private void OnDisable()
    {
        if (_clockPuzzle != null)
        {
            _clockPuzzle.OnCorrect -= HandleClockCorrect;
        }
    }

    private void InitializeView()
    {
        if (_sun != null)
        {
            _sun.anchoredPosition = _sunStartPosition;
            _sun.gameObject.SetActive(true);
        }

        if (_moon != null)
        {
            _moon.anchoredPosition = _moonStartPosition;
            _moon.gameObject.SetActive(true);
        }

        SetCanvasGroupAlpha(_moonGroup, 0f);
        SetCanvasGroupAlpha(_nightOverlayGroup, 0f);
        SetCanvasGroupAlpha(_catEyesGroup, 0f);
    }

    /// <summary>
    /// 태양 이미지 Button의 OnClick에 연결하십시오.
    /// </summary>
    public void OnClickSun()
    {
        if (_state != MiniGame2State.Day)
            return;

        if (_transitionCoroutine != null)
            return;

        _transitionCoroutine = StartCoroutine(TransitionToNight());
    }

    private IEnumerator TransitionToNight()
    {
        _state = MiniGame2State.TransitionToNight;

        float time = 0f;

        while (time < _transitionDuration)
        {
            time += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(time / _transitionDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, normalizedTime);

            UpdateSunMovement(easedT);
            UpdateMoonMovement(easedT);
            UpdateNightFade(easedT);

            yield return null;
        }

        UpdateSunMovement(1f);
        UpdateMoonMovement(1f);
        UpdateNightFade(1f);

        if (_sun != null)
        {
            _sun.gameObject.SetActive(false);
        }

        yield return FadeCanvasGroup(_catEyesGroup, 0f, 1f, _catEyesFadeDuration);

        _state = MiniGame2State.Night;
        _transitionCoroutine = null;

        if (_clockPuzzle != null)
        {
            _clockPuzzle.SetInteractable(true);
        }
    }

    private void UpdateSunMovement(float t)
    {
        if (_sun == null)
            return;

        _sun.anchoredPosition = EvaluateArcPosition(
            _sunStartPosition,
            _sunEndPosition,
            _sunArcHeight,
            t
        );
    }

    private void UpdateMoonMovement(float t)
    {
        if (_moon == null)
            return;

        _moon.anchoredPosition = EvaluateArcPosition(
            _moonStartPosition,
            _moonEndPosition,
            _moonArcHeight,
            t
        );

        SetCanvasGroupAlpha(_moonGroup, t);
    }

    private void UpdateNightFade(float t)
    {
        SetCanvasGroupAlpha(_nightOverlayGroup, Mathf.Lerp(0f, _nightOverlayAlpha, t));
    }

    private Vector2 EvaluateArcPosition(Vector2 start, Vector2 end, float arcHeight, float t)
    {
        Vector2 linearPosition = Vector2.Lerp(start, end, t);

        // t = 0 또는 1에서는 0, t = 0.5에서 최대값이 되는 포물선
        float arc = 4f * arcHeight * t * (1f - t);

        linearPosition.y += arc;

        return linearPosition;
    }

    private void HandleClockCorrect()
    {
        if (_state != MiniGame2State.Night)
            return;

        StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        _state = MiniGame2State.Cleared;

        if (_clockPuzzle != null)
        {
            _clockPuzzle.SetInteractable(false);
        }

        yield return FadeCanvasGroup(_catEyesGroup, 1f, 0.35f, 0.15f);
        yield return FadeCanvasGroup(_catEyesGroup, 0.35f, 1f, 0.15f);

        _onClear?.Invoke();

        Debug.Log("MiniGame2 Clear");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            SetCanvasGroupAlpha(group, to);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        group.alpha = to;
    }

    private void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
            return;

        group.alpha = alpha;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _answerHour = Mathf.Clamp(_answerHour, 1, 12);
        _transitionDuration = Mathf.Max(0f, _transitionDuration);
        _catEyesFadeDuration = Mathf.Max(0f, _catEyesFadeDuration);
    }
#endif
}