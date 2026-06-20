using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio; // 💡 오디오 믹서를 쓰기 위해 꼭 필요합니다!

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("아까 만든 MainMixer를 여기에 끌어다 넣으세요.")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Audio Clips")]
    public AudioClip[] bgmClips;
    public AudioClip[] sfxClips;

    private Dictionary<string, AudioClip> _bgmDic = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> _sfxDic = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Init()
    {
        foreach (var clip in bgmClips)
            if (clip != null && !_bgmDic.ContainsKey(clip.name)) _bgmDic.Add(clip.name, clip);

        foreach (var clip in sfxClips)
            if (clip != null && !_sfxDic.ContainsKey(clip.name)) _sfxDic.Add(clip.name, clip);
    }

    public void PlayBGM(string clipName, float volume = 1f)
    {
        if (_bgmDic.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.Log("Play" + clipName);
            _bgmSource.clip = clip;
            _bgmSource.volume = volume; // 이건 AudioSource 자체의 기본 볼륨입니다.
            _bgmSource.loop = true;
            _bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlaySFX(string clipName, float volume = 1f)
    {
        if (_sfxDic.TryGetValue(clipName, out AudioClip clip))
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }

    // ==========================================
    // [오디오 믹서 마스터/BGM/SFX 볼륨 조절 기능]
    // UI Slider(0.0001 ~ 1 사이의 값)의 OnValueChanged 이벤트에 연결해서 쓰세요.
    // ==========================================

    public void SetMasterVolume(float volume)
    {
        // 볼륨이 0이 되면 로그 계산 시 에러가 나므로 최소값을 0.0001f로 보정해줍니다.
        volume = Mathf.Clamp(volume, 0.0001f, 1f); 
        _audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
    }

    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        _audioMixer.SetFloat("BGM", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        _audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
    }
}