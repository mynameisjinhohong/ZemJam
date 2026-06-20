using UnityEngine;

public class MiniGame3Goal : MiniGame3GridObject
{
    public GameObject _button;

    public void OnClear()
    {
        _button.SetActive(false);
        CutSceneManager.Instance.Play("MiniGame2_End");
    }
}