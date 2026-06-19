using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGame2ClockPuzzleController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Clock References")]
    [SerializeField] private RectTransform _clockArea;
    [SerializeField] private RectTransform _hourHand;

    [Header("Settings")]
    [SerializeField] private bool _snapOnRelease = true;

    private bool _interactable;
    private int _answerHour = 12;
    private int _currentHour = 12;

    public event Action OnCorrect;

    public int CurrentHour => _currentHour;
    public bool IsInteractable => _interactable;

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

        // 12시 방향을 0도로 보기 위한 보정
        float clockAngle = NormalizeAngle(90f - rawAngle);

        // UI에서 시계방향 회전은 보통 z축 음수 방향
        _hourHand.localEulerAngles = new Vector3(0f, 0f, -clockAngle);

        _currentHour = AngleToHour(clockAngle);
    }

    private int AngleToHour(float clockAngle)
    {
        clockAngle = NormalizeAngle(clockAngle);

        int hour = Mathf.RoundToInt(clockAngle / 30f);

        if (hour == 0)
            hour = 12;

        return Mathf.Clamp(hour, 1, 12);
    }

    private void SnapHandToCurrentHour()
    {
        if (_hourHand == null)
            return;

        float snappedAngle = _currentHour == 12
            ? 0f
            : _currentHour * 30f;

        _hourHand.localEulerAngles = new Vector3(0f, 0f, -snappedAngle);
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