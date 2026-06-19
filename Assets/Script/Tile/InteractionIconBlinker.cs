using System.Collections;
using UnityEngine;

public class InteractionIconBlinker : MonoBehaviour
{
    [Header("Scale Animation")]
    [SerializeField] private float _minScale = 0.9f;
    [SerializeField] private float _maxScale = 1.15f;
    [SerializeField] private float _scaleDuration = 0.35f;

    private Vector3 _initialScale;
    private Coroutine _scaleRoutine;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (_initialScale == Vector3.zero)
            _initialScale = transform.localScale;

        _scaleRoutine = StartCoroutine(ScaleRoutine());
    }

    private void OnDisable()
    {
        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
        }

        transform.localScale = _initialScale;
    }

    private IEnumerator ScaleRoutine()
    {
        while (true)
        {
            yield return ScaleTo(_minScale, _maxScale);
            yield return ScaleTo(_maxScale, _minScale);
        }
    }

    private IEnumerator ScaleTo(float fromMultiplier, float toMultiplier)
    {
        float elapsed = 0f;

        Vector3 fromScale = _initialScale * fromMultiplier;
        Vector3 toScale = _initialScale * toMultiplier;

        while (elapsed < _scaleDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / _scaleDuration;
            transform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        transform.localScale = toScale;
    }
}