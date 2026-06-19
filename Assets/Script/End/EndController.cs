using UnityEngine;
using UnityEngine.Events;

public class EndController : MonoBehaviour
{
    [SerializeField] private UnityEvent _onSuccess;
    [SerializeField] private UnityEvent _onFailure;

    public void OnSuccess()
    {
        _onSuccess.Invoke();
    }

    public void OnFail()
    {
        _onFailure.Invoke();
    }
}
