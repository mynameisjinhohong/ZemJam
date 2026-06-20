using UnityEngine;
using UnityEngine.Events;

public class SimpleCutSceneStarter : MonoBehaviour
{
    [SerializeField] private string _cutSceneKey;

    [SerializeField] private GameObject _selectImage;

    private void Start()
    {
        CutSceneManager.Instance.ShowUISequence("GuideE1", "GuideE2", "GuideE3");
    }

    public void ShowE0Bubble()
    {
        CutSceneManager.Instance.ShowUIOnly("GuideE0", ShowSelectImage);
    }

    public void StartCutScene()
    {
        if(GameManager.Instance.CanPlayerMove == false) return;

        if(CutSceneManager.Instance != null)
        {
            CutSceneManager.Instance.Play(_cutSceneKey);
        }
    }

    private void ShowSelectImage()
    {
        _selectImage.SetActive(true);   
    }
}
