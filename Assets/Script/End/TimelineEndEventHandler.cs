using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class TimelineEndEventHandler : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private UnityEvent _onTimelineEnd;

    private void OnEnable()
    {
        if (_director != null)
            _director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (_director != null)
            _director.stopped -= OnDirectorStopped;
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director != _director)
            return;

        _onTimelineEnd?.Invoke();
    }
}