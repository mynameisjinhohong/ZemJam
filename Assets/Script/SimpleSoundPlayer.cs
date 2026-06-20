using UnityEngine;

public class SimpleSoundPlayer : MonoBehaviour
{
    [SerializeField] private string _bgmKey;
    [SerializeField] private string _soundKey;
    [SerializeField] private bool _playOnStart;

    private void Start()
    {
        PlayBGM();
    }


    public void PlayBGM()
    {
        SoundManager.Instance.PlayBGM(_bgmKey);
    }

    public void PlaySound()
    {
        SoundManager.Instance.PlaySFX(_soundKey);
    }
}
