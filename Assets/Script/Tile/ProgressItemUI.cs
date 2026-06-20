using UnityEngine;
using UnityEngine.UI;

public class ProgressItemUI : MonoBehaviour
{
    [SerializeField] private Image[] _images;

    private void Start()
    {
        if(GameManager.Instance == null) { return; }

        for(int i = 0; i < _images.Length; i++)
        {
            _images[i].enabled = i < GameManager.Instance.CurrentInteractableIndex;
            Debug.Log($"images{i} enabled {i < GameManager.Instance.CurrentInteractableIndex}");
        }
    }
}
