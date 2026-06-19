using UnityEngine;

public class SwayEffect : MonoBehaviour
{
    [Header("흔들림 설정")]
    [SerializeField] private float swayAngle = 10f;
    [SerializeField] private float swaySpeed = 1f;
    [Tooltip("기울기 방향 오프셋 (양수=오른쪽, 음수=왼쪽)")]
    [SerializeField] private float swayOffset = 0f;
    [Tooltip("회전 기준점 Y (0=하단, 0.5=중앙, 1=상단)")]
    [Range(0f, 1f)]
    [SerializeField] private float pivotY = 0f;

    [Header("일렁임 설정")]
    [Tooltip("위치 일렁임 강도 (픽셀)")]
    [SerializeField] private float wobbleAmount = 5f;
    [Tooltip("일렁임 속도")]
    [SerializeField] private float wobbleSpeed = 2.3f;

    private RectTransform rt;
    private float timeOffset;
    private Vector2 basePosition;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        rt.pivot = new Vector2(0.5f, pivotY);
    }

    private void Start()
    {
        // anchoredPosition이 외부에서 세팅된 이후 기준점으로 저장
        basePosition = rt.anchoredPosition;
    }

    public void Init(float angle, float speed, float offset, float pY, float wobble, float wobbleSpd)
    {
        swayAngle = angle;
        swaySpeed = speed;
        swayOffset = offset;
        pivotY = pY;
        wobbleAmount = wobble;
        wobbleSpeed = wobbleSpd;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        rt.pivot = new Vector2(0.5f, pivotY);
    }

    private void Update()
    {
        // 주파수 두 개를 합쳐 불규칙한 일렁임
        float rotation = Mathf.Sin(Time.time * swaySpeed + timeOffset) * swayAngle
                       + Mathf.Sin(Time.time * swaySpeed * 1.7f + timeOffset) * swayAngle * 0.15f
                       + swayOffset;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotation);

        // X/Y 위치 미세 진동 (서로 다른 주파수로 자연스럽게)
        float wx = Mathf.Sin(Time.time * wobbleSpeed + timeOffset + 1f) * wobbleAmount;
        float wy = Mathf.Sin(Time.time * wobbleSpeed * 0.7f + timeOffset + 2.5f) * wobbleAmount * 0.5f;
        rt.anchoredPosition = basePosition + new Vector2(wx, wy);
    }
}
