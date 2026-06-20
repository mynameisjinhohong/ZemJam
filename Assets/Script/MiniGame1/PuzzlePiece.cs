using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public bool Interactable = true;

    private Camera _cam;
    private SpriteRenderer _sr;
    private int _defaultSortOrder;
    private Vector3 _dragOffset;

    private void Awake()
    {
        _cam = Camera.main;
        _sr = GetComponent<SpriteRenderer>();
        _defaultSortOrder = _sr != null ? _sr.sortingOrder : 0;
    }

    private void OnMouseDown()
    {
        if (!Interactable) return;
        _dragOffset = transform.position - GetMouseWorldPos();
        if (_sr != null) _sr.sortingOrder = 100;
    }

    private void OnMouseDrag()
    {
        if (!Interactable) return;
        transform.position = GetMouseWorldPos() + _dragOffset;
    }

    private void OnMouseUp()
    {
        if (_sr != null) _sr.sortingOrder = _defaultSortOrder;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(_cam.transform.position.z);
        return _cam.ScreenToWorldPoint(mouse);
    }
}
