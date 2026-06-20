using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MiniGame4StarTwinkleAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _animator;
    [SerializeField] private int _layerIndex = 0;

    [Header("State Names")]
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _twinkleStateName = "Twinkle";

    [Header("Trigger")]
    [SerializeField] private string _twinkleTriggerName = "Twinkle";

    [Header("Random Interval")]
    [SerializeField] private Vector2 _initialDelayRange = new Vector2(0f, 2f);
    [SerializeField] private Vector2 _twinkleIntervalRange = new Vector2(2f, 5f);

    [Header("Random Twinkle Speed")]
    [SerializeField] private bool _randomizeTwinkleSpeed = true;

    [Tooltip("Twinkle 애니메이션 재생 속도 범위입니다. 1이 기본 속도입니다.")]
    [SerializeField] private Vector2 _twinkleSpeedRange = new Vector2(0.8f, 1.3f);

    [Tooltip("Twinkle이 끝난 뒤 복구할 Animator 기본 속도입니다.")]
    [SerializeField] private float _defaultAnimatorSpeed = 1f;

    [Header("Safety")]
    [Tooltip("Twinkle 상태 진입 감지 실패 시 사용할 최대 대기 시간입니다.")]
    [SerializeField] private float _waitForEnterStateTimeout = 0.5f;

    [Tooltip("Twinkle 상태 종료 감지 실패 시 사용할 기본 최대 대기 시간입니다. Twinkle 클립 길이보다 약간 길게 잡으십시오.")]
    [SerializeField] private float _twinkleFallbackDuration = 1.0f;

    [Tooltip("속도가 느린 Twinkle의 종료 감지를 위해 추가로 보정할 시간입니다.")]
    [SerializeField] private float _twinkleExitExtraWait = 0.2f;

    private int _idleStateHash;
    private int _twinkleStateHash;
    private int _twinkleTriggerHash;

    private Coroutine _twinkleCoroutine;
    private bool _isRunning;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _idleStateHash = Animator.StringToHash(_idleStateName);
        _twinkleStateHash = Animator.StringToHash(_twinkleStateName);
        _twinkleTriggerHash = Animator.StringToHash(_twinkleTriggerName);
    }

    private void OnEnable()
    {
        StartTwinkleLoop();
    }

    private void OnDisable()
    {
        StopTwinkleLoop();
    }

    public void StartTwinkleLoop()
    {
        if (_animator == null)
            return;

        if (_twinkleCoroutine != null)
        {
            StopCoroutine(_twinkleCoroutine);
        }

        _animator.speed = _defaultAnimatorSpeed;

        _isRunning = true;
        _twinkleCoroutine = StartCoroutine(CoTwinkleLoop());
    }

    public void StopTwinkleLoop()
    {
        _isRunning = false;

        if (_twinkleCoroutine != null)
        {
            StopCoroutine(_twinkleCoroutine);
            _twinkleCoroutine = null;
        }

        if (_animator != null)
        {
            _animator.ResetTrigger(_twinkleTriggerHash);
            _animator.speed = _defaultAnimatorSpeed;
        }
    }

    private IEnumerator CoTwinkleLoop()
    {
        float initialDelay = GetRandomDelay(_initialDelayRange);

        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        while (_isRunning)
        {
            float delay = GetRandomDelay(_twinkleIntervalRange);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return PlayTwinkleOnce();
        }
    }

    private IEnumerator PlayTwinkleOnce()
    {
        if (_animator == null)
            yield break;

        // 이미 Twinkle 중이면 중복 실행하지 않는다.
        if (IsCurrentState(_twinkleStateHash) || IsNextState(_twinkleStateHash))
            yield break;

        float twinkleSpeed = GetTwinkleSpeed();

        _animator.speed = twinkleSpeed;
        _animator.ResetTrigger(_twinkleTriggerHash);
        _animator.SetTrigger(_twinkleTriggerHash);

        // Animator가 Transition을 처리할 수 있도록 최소 1프레임 대기
        yield return null;

        yield return WaitUntilEnterTwinkle();
        yield return WaitUntilExitTwinkle(twinkleSpeed);

        // Twinkle이 끝난 뒤에는 반드시 기본 속도로 복구한다.
        if (_animator != null)
        {
            _animator.speed = _defaultAnimatorSpeed;
        }
    }

    private IEnumerator WaitUntilEnterTwinkle()
    {
        float elapsed = 0f;

        while (_isRunning && elapsed < _waitForEnterStateTimeout)
        {
            if (IsCurrentState(_twinkleStateHash) || IsNextState(_twinkleStateHash))
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitUntilExitTwinkle(float twinkleSpeed)
    {
        float elapsed = 0f;

        // Twinkle 속도가 느리면 실제 재생 시간이 길어진다.
        // 그래서 fallback 시간을 속도에 맞춰 보정한다.
        float safeSpeed = Mathf.Max(0.01f, twinkleSpeed);
        float adjustedFallbackDuration = (_twinkleFallbackDuration / safeSpeed) + _twinkleExitExtraWait;

        while (_isRunning && elapsed < adjustedFallbackDuration)
        {
            bool isTwinkle =
                IsCurrentState(_twinkleStateHash) ||
                IsNextState(_twinkleStateHash);

            if (!isTwinkle)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private float GetTwinkleSpeed()
    {
        if (!_randomizeTwinkleSpeed)
            return _defaultAnimatorSpeed;

        float min = Mathf.Min(_twinkleSpeedRange.x, _twinkleSpeedRange.y);
        float max = Mathf.Max(_twinkleSpeedRange.x, _twinkleSpeedRange.y);

        return Random.Range(min, max);
    }

    private bool IsCurrentState(int stateHash)
    {
        if (_animator == null)
            return false;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_layerIndex);
        return stateInfo.shortNameHash == stateHash;
    }

    private bool IsNextState(int stateHash)
    {
        if (_animator == null)
            return false;

        if (!_animator.IsInTransition(_layerIndex))
            return false;

        AnimatorStateInfo nextStateInfo = _animator.GetNextAnimatorStateInfo(_layerIndex);
        return nextStateInfo.shortNameHash == stateHash;
    }

    private float GetRandomDelay(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);

        return Random.Range(min, max);
    }
}