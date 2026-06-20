using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MiniGame4DotNode : MonoBehaviour
{
    [SerializeField] private int _id;

    public int Id => _id;
    public Vector3 Position => transform.position;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
#endif
}