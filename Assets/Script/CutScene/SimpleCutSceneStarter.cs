using UnityEngine;
using UnityEngine.Events;

public class SimpleCutSceneStarter : MonoBehaviour
{
    [SerializeField] private string _cutSceneKey;

    public void StartCutScene(string cutSceneKey)
    {
        if(CutSceneManager.Instance != null)
        {
            CutSceneManager.Instance.Play(_cutSceneKey);
        }
    }
}
