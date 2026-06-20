using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private string _openingKey = "Opening";
    [SerializeField] private string _gameSceneName = "GameScene";

    private bool _started;

    private void Update()
    {
        if (_started) return;

        if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _started = true;
            CutSceneManager.Instance.Play(_openingKey);
        }
    }
}
