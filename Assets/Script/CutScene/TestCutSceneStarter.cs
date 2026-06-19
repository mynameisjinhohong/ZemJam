using UnityEngine;
using UnityEngine.InputSystem;

public class TestCutSceneStarter : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            CutSceneManager.Instance.Play("CutScene1");

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            CutSceneManager.Instance.Play("CutScene2");
    }
}