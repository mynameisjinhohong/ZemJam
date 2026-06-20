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

    [Header("Background Groups")]
    [SerializeField] private CanvasGroup _dayBGGroup;
    [SerializeField] private CanvasGroup _nightBGGroup;

    [Header("Puzzle")]
    [SerializeField] private MiniGame2ClockPuzzleController _clockPuzzle;
    [SerializeField, Range(1, 12)] private int _answerHour = 7;

    [Header("Transition Settings")]
    [SerializeField] private float _transitionDuration = 2f;

    [Header("Sun Arc Movement")]
    [SerializeField] private Vector2 _sunStartPosition = new Vector2(500f, 250f);
    [SerializeField] private Vector2 _sunEndPosition = new Vector2(-500f, -300f);
    [SerializeField] private float _sunArcHeight = 180f;

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

    private void Start()
    {
        if(CutSceneManager.Instance != null) 
            CutSceneManager.Instance.ShowUIOnly("Guide4");
        CutSceneManager.Instance.FadeInFromBlackScreen(2f);
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

        SetCanvasGroupAlpha(_dayBGGroup, 1f);
        SetCanvasGroupAlpha(_nightBGGroup, 0f);
    }

    /// <summary>
    /// �¾� �̹��� Button�� OnClick�� �����Ͻʽÿ�.
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

        if (_clockPuzzle != null)
        {
            _clockPuzzle.SetInteractable(false);
        }

        float time = 0f;

        while (time < _transitionDuration)
        {
            time += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(time / _transitionDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, normalizedTime);

            UpdateSunMovement(easedT);
            UpdateBackgroundFade(easedT);

            yield return null;
        }

        UpdateSunMovement(1f);
        UpdateBackgroundFade(1f);

        if (_sun != null)
        {
            _sun.gameObject.SetActive(false);
        }

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

    private void UpdateBackgroundFade(float t)
    {
        SetCanvasGroupAlpha(_dayBGGroup, 1f - t);
        SetCanvasGroupAlpha(_nightBGGroup, t);
    }

    private Vector2 EvaluateArcPosition(Vector2 start, Vector2 end, float arcHeight, float t)
    {
        Vector2 linearPosition = Vector2.Lerp(start, end, t);

        // t = 0, 1������ 0
        // t = 0.5���� �ִ� ����
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

        _onClear?.Invoke();

        Debug.Log("MiniGame2 Clear");

        yield break;
    }

    private void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
            return;

        group.alpha = alpha;
    }

    public void OnClear()
    {
        GameManager.Instance.GoToEndScene();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _answerHour = Mathf.Clamp(_answerHour, 1, 12);
        _transitionDuration = Mathf.Max(0f, _transitionDuration);
    }
#endif
}