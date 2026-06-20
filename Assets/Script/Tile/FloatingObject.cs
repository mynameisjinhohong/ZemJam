using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Floating Settings")]
    [SerializeField] private float _amplitude = 0.25f; // 위아래 이동 거리
    [SerializeField] private float _frequency = 1.5f;  // 움직이는 속도
    [SerializeField] private bool _randomStartOffset = true;

    private Vector3 _startLocalPosition;
    private float _timeOffset;

    private void Awake()
    {
        _startLocalPosition = transform.localPosition;

        if (_randomStartOffset)
        {
            _timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        float yOffset = Mathf.Sin((Time.time * _frequency) + _timeOffset) * _amplitude;

        transform.localPosition = _startLocalPosition + new Vector3(0f, yOffset, 0f);
    }
}