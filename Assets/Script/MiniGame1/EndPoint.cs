using UnityEngine;

public class EndPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        CutSceneManager.Instance.Play("MiniGame1_End");
    }
}
