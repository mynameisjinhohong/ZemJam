using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;

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

        [Range(0f, 0.1f)] public float distortionAmplitude = 0.02f;
        [Range(0f, 50f)] public float distortionFrequency = 10f;
        [Range(0f, 100f)] public float distortionSpeed = 10f;
    }

    [Serializable]
    public class CutSceneFrame
    {
        public Sprite sprite;
        public FrameType frameType;
        public FadeStyle fadeStyle;

        [Header("프레임 개별 페이드 시간 설정")]
        [Tooltip("컷신 진입 전 일반 화면이 서서히 까매지는 속도 (첫 프레임에만 동작)")]
        public float preCutsceneFadeOutDuration = 0f;

        [Tooltip("0이면 상단의 글로벌 기본 페이드 인 시간을 따릅니다.")]
        public float fadeInDuration = 0f;

        [Tooltip("0이면 상단의 글로벌 기본 페이드 아웃 시간을 따릅니다.")]
        public float fadeOutDuration = 0f;

        [Header("페이드 아웃 SFX 설정")]
        [Tooltip("이 프레임이 페이드 아웃될 때 재생할 SoundManager SFX Key. 비워두면 재생하지 않습니다.")]
        public string fadeOutSfxKey;

        [Tooltip("페이드 아웃 SFX 볼륨")]
        [Range(0f, 1f)]
        public float fadeOutSfxVolume = 1f;

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

    [Serializable]
    public class CustomUIEntry
    {
        public string key;
        public GameObject uiPrefab;
    }

    // ==========================================
    // 💡 변경점: 인스펙터에서 찾기 쉽도록 [글로벌 기본 설정]을 최상단으로 올렸습니다!
    // ==========================================
    [Header("글로벌 기본 설정 (시간 조절)")]

    [Tooltip("기본 페이드 인(나타나기) 속도")]
    [SerializeField] private float defaultFadeInDuration = 0.5f;

    [Tooltip("기본 페이드 아웃(사라지기) 속도")]
    [SerializeField] private float defaultFadeOutDuration = 0.5f;

    [Tooltip("씬 이동 후 암전에서 밝아질 때의 페이드 인 시간")]
    [SerializeField] private float sceneLoadFadeInDuration = 1.0f;

    [Tooltip("커스텀 UI가 닫힐 때 투명해지며 사라지는 속도")]
    [SerializeField] private float customUIFadeOutDuration = 0.2f;

    [Tooltip("초당 출력할 글자 수")]
    [SerializeField] private float _typingSpeed = 30f;

    [Header("오디오 설정")]
    [SerializeField] private string _cutSceneBGMKey;

    [Header("UI 연결")]
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
    [SerializeField] private Transform customUIParent;
    [SerializeField] private CustomUIEntry[] customUIEntries;

    [Header("이동 제어 이벤트")]
    public UnityEvent onBlockMovement;
    public UnityEvent onUnblockMovement;

    private Dictionary<string, CutSceneFrame[]> cutSceneMap;
    private Dictionary<string, UnityEvent> cutSceneEvents;
    private Dictionary<string, GameObject> customUIMap;

    private CutSceneFrame[] currentFrames;
    private int frameIndex = 0;
    private int dialogueIndex = 0;

    private bool isPlaying = false;
    private bool waitingForInput = false;
    private bool waitingForChoice = false;
    private bool waitingForCustomInput = false;
    private bool _isTyping = false;
    private bool _skipTyping = false;

    private string _currentKey;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        cutSceneMap = new Dictionary<string, CutSceneFrame[]>();
        cutSceneEvents = new Dictionary<string, UnityEvent>();

        foreach (var entry in cutSceneEntries)
        {
            if (!string.IsNullOrEmpty(entry.key))
            {
                cutSceneMap[entry.key] = entry.frames;
                cutSceneEvents[entry.key] = entry.onEndEvent;
            }
        }

        customUIMap = new Dictionary<string, GameObject>();

        foreach (var entry in customUIEntries)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.uiPrefab != null)
            {
                customUIMap[entry.key] = entry.uiPrefab;
            }
        }

        cutSceneImage.gameObject.SetActive(false);

        if (blackScreenImage != null)
        {
            blackScreenImage.gameObject.SetActive(false);
        }
    }

    public void Play(string key)
    {
        if (!cutSceneMap.TryGetValue(key, out CutSceneFrame[] frames))
        {
            Debug.LogWarning($"CutScene 키를 찾을 수 없음: {key}");
            return;
        }

        if (isPlaying)
        {
            StopAllCoroutines();
        }

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(_cutSceneBGMKey))
        {
            SoundManager.Instance.PlayBGM(_cutSceneBGMKey);
        }

        _currentKey = key;
        currentFrames = frames;
        frameIndex = 0;
        dialogueIndex = 0;

        isPlaying = true;
        waitingForInput = false;
        waitingForChoice = false;
        waitingForCustomInput = false;
        _isTyping = false;
        _skipTyping = false;

        StartCoroutine(PlayCutScene());
    }

    private IEnumerator PlayCutScene()
    {
        if (onBlockMovement != null)
        {
            onBlockMovement.Invoke();
        }

        // 💡 1. 컷신 시작 전: 일반 게임 화면을 서서히 검은 화면으로 페이드 아웃 (첫 프레임의 설정값 사용)
        float preFadeDuration = currentFrames.Length > 0 ? currentFrames[0].preCutsceneFadeOutDuration : 0f;

        if (blackScreenImage != null && preFadeDuration > 0f)
        {
            blackScreenImage.gameObject.SetActive(true);
            Color blackColor = blackScreenImage.color;
            blackColor.a = 0f;
            blackScreenImage.color = blackColor;

            float elapsed = 0f;
            while (elapsed < preFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackColor.a = Mathf.Lerp(0f, 1f, elapsed / preFadeDuration);
                blackScreenImage.color = blackColor;
                yield return null;
            }
            blackColor.a = 1f;
            blackScreenImage.color = blackColor;
        }

        // 💡 2. 첫 프레임 상태 사전 세팅 (깜빡임 완벽 차단)
        if (currentFrames.Length > 0)
        {
            CutSceneFrame firstFrame = currentFrames[0];
            Color imgColor = cutSceneImage.color;

            if (firstFrame.fadeStyle == FadeStyle.BlackScreen && blackScreenImage != null)
            {
                blackScreenImage.gameObject.SetActive(true);
                Color blackColor = blackScreenImage.color;
                blackColor.a = 1f;
                blackScreenImage.color = blackColor;

                imgColor.a = 1f;
                cutSceneImage.color = imgColor;
            }
            else
            {
                imgColor.a = 0f;
                cutSceneImage.color = imgColor;
            }
        }

        cutSceneImage.gameObject.SetActive(true);

        while (frameIndex < currentFrames.Length)
        {
            CutSceneFrame frame = currentFrames[frameIndex];
            cutSceneImage.sprite = frame.sprite;
            dialogueIndex = 0;

            float currentInDuration = frame.fadeInDuration > 0f
                ? frame.fadeInDuration
                : defaultFadeInDuration;

            yield return StartCoroutine(FadeInOut(frame, true, currentInDuration));

            List<GameObject> swayObjects = SpawnSwayImages(frame.swayImages);

            if (!string.IsNullOrEmpty(frame.customUIKey))
            {
                GameObject spawnedUI = SpawnCustomUI(frame);

                if (spawnedUI != null)
                {
                    waitingForCustomInput = true;

                    yield return new WaitUntil(() => !waitingForCustomInput);

                    yield return StartCoroutine(FadeOutCanvasGroup(spawnedUI, customUIFadeOutDuration));

                    Destroy(spawnedUI);
                }
            }

            if (frame.frameType == FrameType.Normal)
            {
                if (frame.dialogues != null && frame.dialogues.Length > 0)
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

            float currentOutDuration = frame.fadeOutDuration > 0f
                ? frame.fadeOutDuration
                : defaultFadeOutDuration;

            PlayFadeOutSFX(frame);

            yield return StartCoroutine(FadeInOut(frame, false, currentOutDuration));

            frameIndex++;
        }

        isPlaying = false;
        cutSceneImage.gameObject.SetActive(false);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        OnCutSceneEnd();
    }

    private void PlayFadeOutSFX(CutSceneFrame frame)
    {
        if (frame == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(frame.fadeOutSfxKey))
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning($"[CutSceneManager] SoundManager.Instance가 없어 FadeOut SFX를 재생할 수 없습니다. SFX Key: {frame.fadeOutSfxKey}");
            return;
        }

        SoundManager.Instance.PlaySFX(frame.fadeOutSfxKey, frame.fadeOutSfxVolume);
    }

    private GameObject SpawnCustomUI(CutSceneFrame frame)
    {
        if (!customUIMap.TryGetValue(frame.customUIKey, out GameObject prefab))
        {
            Debug.LogWarning($"등록되지 않은 Custom UI Key입니다: {frame.customUIKey}");
            return null;
        }

        GameObject uiObj = customUIParent != null
            ? Instantiate(prefab, customUIParent, false)
            : Instantiate(prefab);

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

    private IEnumerator FadeOutCanvasGroup(GameObject uiObj, float duration)
    {
        if (uiObj == null)
        {
            yield break;
        }

        CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = uiObj.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private List<GameObject> SpawnSwayImages(SwayImageData[] swayImages)
    {
        var list = new List<GameObject>();

        if (swayImages == null || swayImages.Length == 0)
        {
            return list;
        }

        if (swayImagePrefab == null || swayImageParent == null)
        {
            return list;
        }

        foreach (SwayImageData data in swayImages)
        {
            GameObject obj = Instantiate(swayImagePrefab, swayImageParent);

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = data.sprite;
            }

            SwayEffect sway = obj.GetComponent<SwayEffect>();
            if (sway != null)
            {
                sway.Init(
                    data.swayAngle,
                    data.swaySpeed,
                    data.swayOffset,
                    data.pivotY,
                    data.wobbleAmount,
                    data.wobbleSpeed
                );
            }

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = data.size;
                rt.anchoredPosition = data.anchoredPosition;
            }

            if (data.useDistortion)
            {
                WaveDistortionEffect distortion = obj.AddComponent<WaveDistortionEffect>();
                distortion.Init(
                    data.distortionAmplitude,
                    data.distortionFrequency,
                    data.distortionSpeed
                );
            }

            list.Add(obj);
        }

        return list;
    }

    private void DestroySwayImages(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    private IEnumerator PlayChoiceFrame(CutSceneFrame frame)
    {
        if (characterButtonPrefab == null)
        {
            Debug.LogError("characterButtonPrefab 이 없습니다");
            yield break;
        }

        if (characterButtonGroup == null)
        {
            Debug.LogError("characterButtonGroup 이 없습니다");
            yield break;
        }

        if (frame.choices == null || frame.choices.Length == 0)
        {
            Debug.LogError("Choices 배열이 비어있습니다");
            yield break;
        }

        characterButtonGroup.SetActive(true);

        foreach (CharacterChoice choice in frame.choices)
        {
            Button btn = Instantiate(characterButtonPrefab, characterButtonGroup.transform);

            Image btnImage = btn.GetComponent<Image>();

            if (btnImage != null)
            {
                btnImage.sprite = choice.sprite;

                try
                {
                    btnImage.alphaHitTestMinimumThreshold = 0.1f;
                }
                catch
                {
                    Debug.LogWarning($"{(choice.sprite != null ? choice.sprite.name : "null")} 텍스처에 Read/Write Enabled 를 켜주세요.");
                }
            }

            RectTransform rt = btn.GetComponent<RectTransform>();

            if (rt != null)
            {
                rt.sizeDelta = choice.size;
                rt.anchoredPosition = choice.anchoredPosition;
            }

            bool isCorrect = choice.isCorrect;
            btn.onClick.AddListener(() => OnCharacterClicked(isCorrect));
        }

        waitingForChoice = true;
        yield return new WaitUntil(() => !waitingForChoice);

        foreach (Transform child in characterButtonGroup.transform)
        {
            Destroy(child.gameObject);
        }

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

            blackColor.a = from;
            blackScreenImage.color = blackColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blackColor.a = Mathf.Lerp(from, to, elapsed / duration);
                blackScreenImage.color = blackColor;
                yield return null;
            }

            blackColor.a = to;
            blackScreenImage.color = blackColor;

            if (isFadeIn)
            {
                blackScreenImage.gameObject.SetActive(false);
            }
        }
        else
        {
            if (blackScreenImage != null)
            {
                blackScreenImage.gameObject.SetActive(false);
            }

            imgColor.a = from;
            cutSceneImage.color = imgColor;

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
        if (!isPlaying)
        {
            return;
        }

        bool inputPressed =
            Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            var box = dialogueText.transform.parent.gameObject;
            box.SetActive(!box.activeSelf);
        }

        if (!inputPressed)
        {
            return;
        }

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

        // 1. 전체 문장을 미리 다 적어두어 중앙 정렬 위치(틀)를 확고하게 고정합니다.
        dialogueText.text = text;
        
        // 2. 처음에는 한 글자도 보이지 않게 투명 처리합니다.
        dialogueText.maxVisibleCharacters = 0;

        float delay = _typingSpeed > 0f ? 1f / _typingSpeed : 0f;

        // 3. 내부적으로 텍스트를 계산하여 실제 출력될 글자 수를 가져옵니다.
        dialogueText.ForceMeshUpdate();
        int totalCharacters = dialogueText.textInfo.characterCount;

        // 4. 글자를 0개부터 1개씩 추가로 '보여주기만' 합니다.
        for (int i = 0; i <= totalCharacters; i++)
        {
            if (_skipTyping)
            {
                // 클릭 시 전체 문장을 한 번에 띄웁니다.
                dialogueText.maxVisibleCharacters = totalCharacters;
                break;
            }

            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
    }
    private void OnCutSceneEnd()
    {
        if (onUnblockMovement != null)
        {
            onUnblockMovement.Invoke();
        }

        if (cutSceneEvents.TryGetValue(_currentKey, out UnityEvent endEvent))
        {
            endEvent?.Invoke();
        }
    }

    // ==========================================
    // [독립 UI 팝업 전용 함수]
    // ==========================================
    public void ShowUIOnly(string uiKey, Action onComplete = null)
    {
        StartCoroutine(ShowUISequenceRoutine(new[] { uiKey }, onComplete));
    }

    public void ShowUISequence(params string[] uiKeys)
    {
        StartCoroutine(ShowUISequenceRoutine(uiKeys, null));
    }

    // 💡 새로운 기능: UI 팝업이 끝나면 원하는 함수 실행
    public void ShowUISequenceWithCallback(Action onComplete, params string[] uiKeys)
    {
        StartCoroutine(ShowUISequenceRoutine(uiKeys, onComplete));
    }

    private IEnumerator ShowUISequenceRoutine(string[] uiKeys, Action onComplete)
    {
        if (onBlockMovement != null) onBlockMovement.Invoke();

    foreach (string uiKey in uiKeys)
    {
        if (!customUIMap.TryGetValue(uiKey, out GameObject prefab)) continue;

        GameObject spawnedUI = customUIParent != null
            ? Instantiate(prefab, customUIParent, false)
            : Instantiate(prefab);

        yield return null;

        yield return new WaitUntil(() =>
            Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame
        );

        // UI 사라지는 시간 (변수가 없다면 defaultFadeOutDuration으로 사용하세요)
        yield return StartCoroutine(FadeOutCanvasGroup(spawnedUI, 0.2f)); 
        Destroy(spawnedUI);
    }

    if (onUnblockMovement != null) onUnblockMovement.Invoke();

    // 💡 모든 UI가 닫힌 직후에 함수 실행
    onComplete?.Invoke();
}   

    // ==========================================
    // [씬 전환 시 암전 해제 로직]
    // ==========================================

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (blackScreenImage != null && blackScreenImage.gameObject.activeSelf)
        {
            FadeInFromBlackScreen(sceneLoadFadeInDuration);
        }
    }

    public void ClearBlackScreenInstantly()
    {
        if (blackScreenImage != null)
        {
            blackScreenImage.gameObject.SetActive(false);

            Color c = blackScreenImage.color;
            c.a = 0f;
            blackScreenImage.color = c;
        }
    }

    // ==========================================
    // 💡 글로벌 콜백 페이드 함수 (다른 스크립트 씬 전환용)
    // ==========================================

    public void FadeInFromBlackScreen(float duration = 1f, Action onComplete = null)
    {
        StartCoroutine(FadeInFromBlackScreenRoutine(duration, onComplete));
    }

    private IEnumerator FadeInFromBlackScreenRoutine(float duration, Action onComplete)
    {
        if (blackScreenImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Color c = blackScreenImage.color;
        float elapsed = 0f;
        Debug.Log("FadeInFromBlackStart");
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            blackScreenImage.color = c;
            yield return null;
        }

        c.a = 0f;
        blackScreenImage.color = c;
        blackScreenImage.gameObject.SetActive(false);
        Debug.Log("FadeInFromBlackEnd");
        onComplete?.Invoke();
    }

    public void FadeOutToBlackScreen(float duration = 1f, Action onComplete = null)
    {
        StartCoroutine(FadeOutToBlackScreenRoutine(duration, onComplete));
    }

    private IEnumerator FadeOutToBlackScreenRoutine(float duration, Action onComplete)
    {
        if (blackScreenImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        blackScreenImage.gameObject.SetActive(true);
        Color c = blackScreenImage.color;
        c.a = 0f;
        blackScreenImage.color = c;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            blackScreenImage.color = c;
            yield return null;
        }

        c.a = 1f;
        blackScreenImage.color = c;

        onComplete?.Invoke();
    }
}