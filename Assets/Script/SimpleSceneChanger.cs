using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneChanger : MonoBehaviour
{
    public void ReturnToGameScene()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null.");
            return;
        }

        GameManager.Instance.ReturnToGameScene();
    }
}
