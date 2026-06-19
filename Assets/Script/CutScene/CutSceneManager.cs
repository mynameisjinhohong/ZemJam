using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance { get; private set; }

    public enum FrameType { Normal, Choice }

    [Serializable]
    public class CharacterChoice
    {
        public Sprite sprite;
        public Vector2 anchoredPosition;
        public Vector2 size;
        public bool isCorrect;
    }

    [Serializable]
    public class SwayImageData
    {
        public Sprite sprite;
        public Vector2 anchoredPosition;
        public Vector2 size;
        [Header("흔들림 설정")]
        public float swayAngle = 10f;
        public float swaySpeed = 1f;
        [Tooltip("기울기 방향 오프셋 (양수=오른쪽, 음수=왼쪽)")]
        public float swayOffset = 0f;
        [Tooltip("회전 기준점 Y (0=하단, 0.5=중앙, 1=상단)")]
        [Range(0f, 1f)]
        public float pivotY = 0f;
        [Header("일렁임 설정")]
        [Tooltip("위치 일렁임 강도 (픽셀)")]
        public float wobbleAmount = 5f;
        [Tooltip("일렁임 속도")]
        public float wobbleSpeed = 2.3f;
        [Header("찌그러짐 설정")]
        public bool useDistortion = false;
        [Range(0f, 0.1f)]  public float distortionAmplitude = 0.02f;
        [Range(0f, 50f)]   public float distortionFrequency = 10f;
        [Range(0f, 100f)]  public float distortionSpeed = 10f;
    }

    [Serializable]
    public class CutSceneFrame
    {
        public Sprite sprite;
        public FrameType frameType;
        [TextArea] public string[] dialogues;
        public CharacterChoice[] choices;
        public SwayImageData[] swayImages;
    }

    [Serializable]
    public class CutSceneEntry
    {
        public string key;
        public CutSceneFrame[] frames;
    }

    [Header("UI")]
    [SerializeField] private Image cutSceneImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject characterButtonGroup;
    [SerializeField] private Button characterButtonPrefab;
    [SerializeField] private Transform swayImageParent;
    [SerializeField] private GameObject swayImagePrefab;

    [Header("컷신 목록 (Key + 프레임 배열)")]
    [SerializeField] private CutSceneEntry[] cutSceneEntries;

    [Header("설정")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Dictionary<string, CutSceneFrame[]> cutSceneMap;
    private CutSceneFrame[] currentFrames;
    private int frameIndex = 0;
    private int dialogueIndex = 0;
    private bool isPlaying = false;
    private bool waitingForInput = false;
    private bool waitingForChoice = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cutSceneMap = new Dictionary<string, CutSceneFrame[]>();
        foreach (var entry in cutSceneEntries)
        {
            if (!string.IsNullOrEmpty(entry.key))
                cutSceneMap[entry.key] = entry.frames;
        }

        cutSceneImage.gameObject.SetActive(false);
    }

    public void Play(string key)
    {
        if (!cutSceneMap.TryGetValue(key, out CutSceneFrame[] frames))
        {
            Debug.LogWarning($"CutScene 키를 찾을 수 없음: {key}");
            return;
        }

        if (isPlaying) StopAllCoroutines();

        currentFrames = frames;
        frameIndex = 0;
        isPlaying = true;
        StartCoroutine(PlayCutScene());
    }

    private IEnumerator PlayCutScene()
    {
        cutSceneImage.gameObject.SetActive(true);

        while (frameIndex < currentFrames.Length)
        {
            CutSceneFrame frame = currentFrames[frameIndex];
            cutSceneImage.sprite = frame.sprite;
            dialogueIndex = 0;

            yield return StartCoroutine(Fade(0f, 1f));

            List<GameObject> swayObjects = SpawnSwayImages(frame.swayImages);

            if (frame.frameType == FrameType.Normal)
            {
                if (frame.dialogues.Length > 0)
                {
                    dialogueText.gameObject.SetActive(true);
                    while (dialogueIndex < frame.dialogues.Length)
                    {
                        dialogueText.text = frame.dialogues[dialogueIndex];
                        waitingForInput = true;
                        yield return new WaitUntil(() => !waitingForInput);
                        dialogueIndex++;
                    }
                    dialogueText.gameObject.SetActive(false);
                }
                else
                {
                    waitingForInput = true;
                    yield return new WaitUntil(() => !waitingForInput);
                }
            }
            else if (frame.frameType == FrameType.Choice)
            {
                yield return StartCoroutine(PlayChoiceFrame(frame));
            }

            DestroySwayImages(swayObjects);

            yield return StartCoroutine(Fade(1f, 0f));
            frameIndex++;
        }

        isPlaying = false;
        cutSceneImage.gameObject.SetActive(false);
        OnCutSceneEnd();
    }

    private List<GameObject> SpawnSwayImages(SwayImageData[] swayImages)
    {
        var list = new List<GameObject>();
        if (swayImages == null || swayImages.Length == 0) return list;
        if (swayImagePrefab == null || swayImageParent == null) return list;

        foreach (SwayImageData data in swayImages)
        {
            GameObject obj = Instantiate(swayImagePrefab, swayImageParent);

            Image img = obj.GetComponent<Image>();
            img.sprite = data.sprite;

            SwayEffect sway = obj.GetComponent<SwayEffect>();
            sway.Init(data.swayAngle, data.swaySpeed, data.swayOffset, data.pivotY, data.wobbleAmount, data.wobbleSpeed);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = data.size;
            rt.anchoredPosition = data.anchoredPosition;

            if (data.useDistortion)
            {
                WaveDistortionEffect distortion = obj.AddComponent<WaveDistortionEffect>();
                distortion.Init(data.distortionAmplitude, data.distortionFrequency, data.distortionSpeed);
            }

            list.Add(obj);
        }

        return list;
    }

    private void DestroySwayImages(List<GameObject> list)
    {
        foreach (GameObject obj in list)
            Destroy(obj);
    }

    private IEnumerator PlayChoiceFrame(CutSceneFrame frame)
    {
        if (characterButtonPrefab == null) { Debug.LogError("characterButtonPrefab 이 없습니다"); yield break; }
        if (characterButtonGroup == null) { Debug.LogError("characterButtonGroup 이 없습니다"); yield break; }
        if (frame.choices == null || frame.choices.Length == 0) { Debug.LogError("Choices 배열이 비어있습니다"); yield break; }

        characterButtonGroup.SetActive(true);

        foreach (CharacterChoice choice in frame.choices)
        {
            Button btn = Instantiate(characterButtonPrefab, characterButtonGroup.transform);

            Image btnImage = btn.GetComponent<Image>();
            btnImage.sprite = choice.sprite;
            try { btnImage.alphaHitTestMinimumThreshold = 0.1f; }
            catch { Debug.LogWarning($"{(choice.sprite != null ? choice.sprite.name : "null")} 텍스처에 Read/Write Enabled 를 켜주세요."); }

            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = choice.size;
            rt.anchoredPosition = choice.anchoredPosition;

            bool isCorrect = choice.isCorrect;
            btn.onClick.AddListener(() => OnCharacterClicked(isCorrect));
        }

        waitingForChoice = true;
        yield return new WaitUntil(() => !waitingForChoice);

        foreach (Transform child in characterButtonGroup.transform)
            Destroy(child.gameObject);

        characterButtonGroup.SetActive(false);
    }

    private void OnCharacterClicked(bool isCorrect)
    {
        if (isCorrect)
        {
            Debug.Log("이미지 버튼 클릭 성공");
            waitingForChoice = false;
        }
        else
        {
            Debug.Log("실패");
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        Color color = cutSceneImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            cutSceneImage.color = color;
            yield return null;
        }

        color.a = to;
        cutSceneImage.color = color;
    }

    private void Update()
    {
        if (isPlaying && waitingForInput && Mouse.current.leftButton.wasPressedThisFrame)
            waitingForInput = false;
    }

    private void OnCutSceneEnd() => Debug.Log("CutScene 종료");
}
