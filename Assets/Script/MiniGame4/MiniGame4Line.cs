using UnityEngine;

public class MiniGame4Line : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private LineRenderer _backLine;
    [SerializeField] private LineRenderer _frontLine;

    [Header("Camera")]
    [SerializeField] private Camera _camera;

    [Header("Pixel Width")]
    [SerializeField] private float _backLinePixelWidth = 3f;
    [SerializeField] private float _frontLinePixelWidth = 1f;

    [Header("Color")]
    [SerializeField] private Color _backLineColor = new Color(0.1f, 0.5f, 1f, 1f);
    [SerializeField] private Color _frontLineColor = Color.white;

    [Header("Sorting")]
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _backSortingOrder = 0;
    [SerializeField] private int _frontSortingOrder = 1;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        InitializeLine(_backLine, _backLineColor, _backSortingOrder);
        InitializeLine(_frontLine, _frontLineColor, _frontSortingOrder);

        RefreshWidth();
    }

    public void SetCamera(Camera targetCamera)
    {
        _camera = targetCamera;
        RefreshWidth();
    }

    public void SetPosition(Vector3 from, Vector3 to)
    {
        SetLinePosition(_backLine, from, to);
        SetLinePosition(_frontLine, from, to);

        RefreshWidth();
    }

    private void InitializeLine(LineRenderer line, Color color, int sortingOrder)
    {
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.positionCount = 2;

        line.startColor = color;
        line.endColor = color;

        line.sortingLayerName = _sortingLayerName;
        line.sortingOrder = sortingOrder;

        // 라인 끝을 둥글게 하고 싶으면 4~8 정도.
        // 픽셀 라인처럼 각진 느낌이면 0.
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
    }

    private void SetLinePosition(LineRenderer line, Vector3 from, Vector3 to)
    {
        if (line == null)
            return;

        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private void RefreshWidth()
    {
        if (_camera == null)
            return;

        float backWorldWidth = PixelToWorldWidth(_backLinePixelWidth);
        float frontWorldWidth = PixelToWorldWidth(_frontLinePixelWidth);

        ApplyWidth(_backLine, backWorldWidth);
        ApplyWidth(_frontLine, frontWorldWidth);
    }

    private void ApplyWidth(LineRenderer line, float width)
    {
        if (line == null)
            return;

        line.startWidth = width;
        line.endWidth = width;
    }

    private float PixelToWorldWidth(float pixelWidth)
    {
        if (_camera == null)
            return pixelWidth;

        if (!_camera.orthographic)
        {
            Debug.LogWarning("[MiniGame4Line] Perspective Camera에서는 픽셀 기반 LineRenderer 두께가 정확하지 않을 수 있습니다.");
            return pixelWidth * 0.01f;
        }

        float worldHeight = _camera.orthographicSize * 2f;
        float worldPerPixel = worldHeight / Screen.height;

        return pixelWidth * worldPerPixel;
    }

    public void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        SetLineAlpha(_backLine, alpha);
        SetLineAlpha(_frontLine, alpha);
    }

    private void SetLineAlpha(LineRenderer line, float alpha)
    {
        if (line == null)
            return;

        Color startColor = line.startColor;
        Color endColor = line.endColor;

        startColor.a = alpha;
        endColor.a = alpha;

        line.startColor = startColor;
        line.endColor = endColor;
    }
}