using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance { get; private set; }

    public enum FrameType { Normal, Choice }
    public enum FadeStyle { Transparent, BlackScreen }

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
        public FadeStyle fadeStyle;
        
        [Header("프레임 개별 페이드 시간 설정")]
        [Tooltip("0이면 하단의 글로벌 기본 페이드 인 시간을 따릅니다.")]
        public float fadeInDuration = 0f;
        [Tooltip("0이면 하단의 글로벌 기본 페이드 아웃 시간을 따릅니다.")]
        public float fadeOutDuration = 0f;

        [Header("특정 UI 띄우기 설정 (선택 사항)")]
        [Tooltip("등록한 Custom UI의 Key 이름을 적으세요. 비어있으면 띄우지 않습니다.")]
        public string customUIKey;
        [Tooltip("체크하면 프리팹 자체 설정을 무시하고 아래의 위치/크기를 강제로 적용합니다.")]
        public bool overrideUIPosSize = false;
        public Vector2 customUIPosition;
        public Vector2 customUISize;

        [TextArea] public string[] dialogues;
        public CharacterChoice[] choices;
        public SwayImageData[] swayImages;
    }

    [Serializable]
    public class CutSceneEntry
    {
        public string key;
        public CutSceneFrame[] frames;
        public UnityEvent onEndEvent;
    }

    // [새로 추가] 딕셔너리 매핑을 위한 직렬화 클래스
    [Serializable]
    public class CustomUIEntry
    {
        public string key;
        public GameObject uiPrefab;
    }

    [Header("UI")]
    [SerializeField] private Image cutSceneImage;
    [SerializeField] private Image blackScreenImage; 
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject characterButtonGroup;
    [SerializeField] private Button characterButtonPrefab;
    [SerializeField] private Transform swayImageParent;
    [SerializeField] private GameObject swayImagePrefab;
    
    [Header("컷신 목록 (Key + 프레임 배열)")]
    [SerializeField] private CutSceneEntry[] cutSceneEntries;

    [Header("커스텀 UI 설정")]
    [SerializeField] private Transform customUIParent; // 커스텀 UI가 생성될 부모 Canvas/오브젝트
    [SerializeField] private CustomUIEntry[] customUIEntries; // 인스펙터 등록용 배열

    [Header("이동 제어 이벤트")]
    public UnityEvent onBlockMovement;   // 커스텀 UI가 떴을 때 캐릭터 이동을 막는 이벤트
    public UnityEvent onUnblockMovement; // 커스텀 UI가 꺼졌을 때 캐릭터 이동을 푸는 이벤트

    [Header("글로벌 기본 설정")]
    [Tooltip("기본 페이드 인(나타나기) 속도")]
    [SerializeField] private float defaultFadeInDuration = 0.5f;
    [Tooltip("기본 페이드 아웃(사라지기) 속도")]
    [SerializeField] private float defaultFadeOutDuration = 0.5f;
    [Tooltip("초당 출력할 글자 수")]
    [SerializeField] private float _typingSpeed = 30f;

    private Dictionary<string, CutSceneFrame[]> cutSceneMap;
    private Dictionary<string, UnityEvent> cutSceneEvents;
    private Dictionary<string, GameObject> customUIMap; // [새로 추가] 런타임 관리용 딕셔너리
    
    private CutSceneFrame[] currentFrames;
    private int frameIndex = 0;
    private int dialogueIndex = 0;
    private bool isPlaying = false;
    private bool waitingForInput = false;
    private bool waitingForChoice = false;
    private bool waitingForCustomInput = false; // [새로 추가] 커스텀 UI 입력 대기 플래그
    private bool _isTyping = false;
    private bool _skipTyping = false;
    private string _currentKey;


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cutSceneMap = new Dictionary<string, CutSceneFrame[]>();
        cutSceneEvents= new Dictionary<string, UnityEvent>();
        foreach (var entry in cutSceneEntries)
        {
            if (!string.IsNullOrEmpty(entry.key))
            {
                cutSceneMap[entry.key] = entry.frames;
                cutSceneEvents[entry.key] = entry.onEndEvent;
            }
        }

        // [새로 추가] 커스텀 UI 딕셔너리 초기화
        customUIMap = new Dictionary<string, GameObject>();
        foreach (var entry in customUIEntries)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.uiPrefab != null)
            {
                customUIMap[entry.key] = entry.uiPrefab;
            }
        }

        cutSceneImage.gameObject.SetActive(false);
        if (blackScreenImage != null) blackScreenImage.gameObject.SetActive(false);
    }

    public void Play(string key)
    {
        if (!cutSceneMap.TryGetValue(key, out CutSceneFrame[] frames))
        {
            Debug.LogWarning($"CutScene 키를 찾을 수 없음: {key}");
            return;
        }

        if (isPlaying) StopAllCoroutines();

        _currentKey= key;
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

            float currentInDuration = frame.fadeInDuration > 0f ? frame.fadeInDuration : defaultFadeInDuration;
            yield return StartCoroutine(FadeInOut(frame, true, currentInDuration));

            List<GameObject> swayObjects = SpawnSwayImages(frame.swayImages);

            // [새로 추가] 커스텀 UI 생성 및 입력 대기 로직
            if (!string.IsNullOrEmpty(frame.customUIKey))
            {
                GameObject spawnedUI = SpawnCustomUI(frame);
                if (spawnedUI != null)
                {
                    onBlockMovement.Invoke(); // 캐릭터 이동 중지 이벤트 호출
                    waitingForCustomInput = true;
                    
                    // 유저가 클릭/스페이스바를 누를 때까지 코루틴 대기
                    yield return new WaitUntil(() => !waitingForCustomInput);
                    
                    onUnblockMovement.Invoke(); // 캐릭터 이동 해제 이벤트 호출
                    Destroy(spawnedUI); // UI 제거
                }
            }

            if (frame.frameType == FrameType.Normal)
            {
                if (frame.dialogues.Length > 0)
                {
                    dialogueText.transform.parent.gameObject.SetActive(true);
                    while (dialogueIndex < frame.dialogues.Length)
                    {
                        yield return StartCoroutine(TypeText(frame.dialogues[dialogueIndex]));
                        waitingForInput = true;
                        yield return new WaitUntil(() => !waitingForInput);
                        dialogueIndex++;
                    }
                    dialogueText.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    // 커스텀 UI 대기를 처리한 뒤, 대사가 없다면 일반 입력 대기 수행
                    if (string.IsNullOrEmpty(frame.customUIKey))
                    {
                        waitingForInput = true;
                        yield return new WaitUntil(() => !waitingForInput);
                    }
                }
            }
            else if (frame.frameType == FrameType.Choice)
            {
                yield return StartCoroutine(PlayChoiceFrame(frame));
            }

            DestroySwayImages(swayObjects);

            float currentOutDuration = frame.fadeOutDuration > 0f ? frame.fadeOutDuration : defaultFadeOutDuration;
            yield return StartCoroutine(FadeInOut(frame, false, currentOutDuration));
            
            frameIndex++;
        }

        isPlaying = false;
        cutSceneImage.gameObject.SetActive(false);
        OnCutSceneEnd();
    }

    // [새로 추가] 커스텀 UI 동적 생성 함수
    private GameObject SpawnCustomUI(CutSceneFrame frame)
    {
        if (!customUIMap.TryGetValue(frame.customUIKey, out GameObject prefab))
        {
            Debug.LogWarning($"등록되지 않은 Custom UI Key입니다: {frame.customUIKey}");
            return null;
        }

        Transform parent = customUIParent != null ? customUIParent : this.transform;
        GameObject uiObj = Instantiate(prefab, parent);

        if (frame.overrideUIPosSize)
        {
            RectTransform rt = uiObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = frame.customUIPosition;
                rt.sizeDelta = frame.customUISize;
            }
        }

        return uiObj;
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

    private IEnumerator FadeInOut(CutSceneFrame frame, bool isFadeIn, float duration)
    {
        float elapsed = 0f;
        float from = isFadeIn ? 0f : 1f;
        float to = isFadeIn ? 1f : 0f;

        Color imgColor = cutSceneImage.color;
        
        if (frame.fadeStyle == FadeStyle.BlackScreen && blackScreenImage != null)
        {
            Color blackColor = blackScreenImage.color;
            blackScreenImage.gameObject.SetActive(true);
            
            imgColor.a = 1f;
            cutSceneImage.color = imgColor;

            from = isFadeIn ? 1f : 0f;
            to = isFadeIn ? 0f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blackColor.a = Mathf.Lerp(from, to, elapsed / duration);
                blackScreenImage.color = blackColor;
                yield return null;
            }
            blackColor.a = to;
            blackScreenImage.color = blackColor;

            if (isFadeIn) blackScreenImage.gameObject.SetActive(false);
        }
        else
        {
            if (blackScreenImage != null) blackScreenImage.gameObject.SetActive(false);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                imgColor.a = Mathf.Lerp(from, to, elapsed / duration);
                cutSceneImage.color = imgColor;
                yield return null;
            }
            imgColor.a = to;
            cutSceneImage.color = imgColor;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        bool inputPressed = Mouse.current.leftButton.wasPressedThisFrame ||
                            Keyboard.current.spaceKey.wasPressedThisFrame;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            var box = dialogueText.transform.parent.gameObject;
            box.SetActive(!box.activeSelf);
        }

        if (!inputPressed) return;

        // [수정] 커스텀 UI가 떠 있다면 최우선적으로 입력을 감지해 꺼트림
        if (waitingForCustomInput)
        {
            waitingForCustomInput = false;
        }
        else if (_isTyping)
        {
            _skipTyping = true;
        }
        else if (waitingForInput)
        {
            waitingForInput = false;
        }
    }

    private IEnumerator TypeText(string text)
    {
        _isTyping = true;
        _skipTyping = false;
        dialogueText.text = "";

        float delay = _typingSpeed > 0f ? 1f / _typingSpeed : 0f;

        foreach (char c in text)
        {
            if (_skipTyping)
            {
                dialogueText.text = text;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
    }

    private void OnCutSceneEnd()
    {
        cutSceneEvents[_currentKey].Invoke();
    }
}