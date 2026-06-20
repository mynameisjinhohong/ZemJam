using UnityEngine;
using UnityEngine.Events;

public class SimpleCutSceneStarter : MonoBehaviour
{
    [SerializeField] private string _cutSceneKey;

    private void Start()
    {
        CutSceneManager.Instance.ShowUISequence("GuideE1", "GuideE2", "GuideE3");
    }

    public void StartCutScene()
    {
        if(GameManager.Instance.CanPlayerMove == false) return;

        if(CutSceneManager.Instance != null)
        {
            CutSceneManager.Instance.Play(_cutSceneKey);
        }
    }
}
