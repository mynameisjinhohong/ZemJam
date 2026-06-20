using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGame2ClockPuzzleController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Clock References")]
    [SerializeField] private RectTransform _clockArea;
    [SerializeField] private RectTransform _hourHand;

    [Header("Hand Rotation Settings")]
    [Tooltip("시침 이미지가 12시 방향을 가리키기 위해 필요한 Z 회전값입니다. 현재 시침 UI가 가로 이미지라면 90으로 둡니다.")]
    [SerializeField] private float _handBaseZRotation = 90f;

    [Header("Settings")]
    [SerializeField] private bool _snapOnRelease = true;

    private bool _interactable;
    private int _answerHour = 12;
    private int _currentHour = 12;

    public event Action OnCorrect;

    public int CurrentHour => _currentHour;
    public bool IsInteractable => _interactable;

    private void Awake()
    {
        ApplyHourRotation(12);
    }

    public void SetInteractable(bool value)
    {
        _interactable = value;
    }

    public void SetAnswer(int hour)
    {
        _answerHour = Mathf.Clamp(hour, 1, 12);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable)
            return;

        UpdateHandRotation(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_interactable)
            return;

        UpdateHandRotation(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_interactable)
            return;

        UpdateHandRotation(eventData);

        if (_snapOnRelease)
        {
            SnapHandToCurrentHour();
        }

        CheckAnswer();
    }

    private void UpdateHandRotation(PointerEventData eventData)
    {
        if (_hourHand == null)
            return;

        RectTransform targetArea = _clockArea != null
            ? _clockArea
            : transform as RectTransform;

        if (targetArea == null)
            return;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        if (!success)
            return;

        if (localPoint.sqrMagnitude <= 0.0001f)
            return;

        float rawAngle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;

        // 12시 방향을 0도로 보기 위한 보정값
        float clockAngle = NormalizeAngle(90f - rawAngle);

        ApplyClockAngle(clockAngle);

        _currentHour = AngleToHour(clockAngle);
    }

    private void ApplyClockAngle(float clockAngle)
    {
        if (_hourHand == null)
            return;

        // clockAngle 기준:
        // 0도   = 12시
        // 30도  = 1시
        // 60도  = 2시
        // 90도  = 3시
        //
        // 시침 이미지가 가로 방향이고, 12시를 가리키기 위해 Z 90이 필요하므로
        // 최종 회전값 = 기준 회전값 - 시계 각도
        float finalZRotation = _handBaseZRotation - clockAngle;

        _hourHand.localEulerAngles = new Vector3(0f, 0f, finalZRotation);
    }

    private void ApplyHourRotation(int hour)
    {
        hour = Mathf.Clamp(hour, 1, 12);

        float clockAngle = HourToClockAngle(hour);

        ApplyClockAngle(clockAngle);

        _currentHour = hour;
    }

    private int AngleToHour(float clockAngle)
    {
        clockAngle = NormalizeAngle(clockAngle);

        int hour = Mathf.RoundToInt(clockAngle / 30f);

        if (hour == 0)
            hour = 12;

        return Mathf.Clamp(hour, 1, 12);
    }

    private float HourToClockAngle(int hour)
    {
        hour = Mathf.Clamp(hour, 1, 12);

        if (hour == 12)
            return 0f;

        return hour * 30f;
    }

    private void SnapHandToCurrentHour()
    {
        ApplyHourRotation(_currentHour);
    }

    private void CheckAnswer()
    {
        if (_currentHour == _answerHour)
        {
            OnCorrect?.Invoke();
        }
        else
        {
            Debug.Log($"MiniGame2 Wrong Answer. Current: {_currentHour}, Answer: {_answerHour}");
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _answerHour = Mathf.Clamp(_answerHour, 1, 12);
    }
#endif
}