using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WaveDistortionEffect : MonoBehaviour
{
    [Range(0f, 0.1f)]  [SerializeField] private float amplitude = 0.02f;
    [Range(0f, 50f)]   [SerializeField] private float frequency = 10f;
    [Range(0f, 100f)]  [SerializeField] private float speed = 10f;

    private Material mat;

    private void Awake()
    {
        Shader shader = Shader.Find("Custom/UI/WaveDistortion");
        if (shader == null) { Debug.LogError("WaveDistortion 쉐이더를 찾을 수 없습니다."); return; }

        mat = new Material(shader);
        GetComponent<Image>().material = mat;
    }

    public void Init(float amp, float freq, float spd)
    {
        amplitude = amp;
        frequency = freq;
        speed = spd;
    }

    private void Update()
    {
        if (mat == null) return;
        mat.SetFloat("_WaveAmplitude", amplitude);
        mat.SetFloat("_WaveFrequency", frequency);
        mat.SetFloat("_WaveSpeed", speed);
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
