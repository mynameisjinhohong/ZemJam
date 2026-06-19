using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneChanger : MonoBehaviour
{
    public void MoveToScene(string _sceneName)
    {
        SceneManager.LoadScene( _sceneName);
    }
}
