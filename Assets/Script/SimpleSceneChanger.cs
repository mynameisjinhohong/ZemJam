using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneChanger : MonoBehaviour
{
    public void ReturnToGameScene()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null.");
            SceneManager.LoadScene("GameScene");
            return;
        }

        GameManager.Instance.ReturnToGameScene();
    }

    public void GoToStartScene()
    {
        if (GameManager.Instance == null)
        {
            return;
        }
        GameManager.Instance.GoToStartScene();
    }
}
