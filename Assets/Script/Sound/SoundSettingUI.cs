using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 💡 EventTrigger 시스템을 쓰기 위해 필요합니다.

public class SoundSettingUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Test SFX Setting")]
    [Tooltip("슬라이더를 놓을 때 재생할 효과음 파일명을 적으세요.")]
    [SerializeField] private string _testSFXName = "ClickSound";

    private void Start()
    {
        // 1. 슬라이더의 값 범위(Min/Max) 초기화
        // 오디오 믹서 공식(Log10)을 쓰기 때문에 최소값은 0이 아닌 0.0001f로 잡아야 에러가 나지 않습니다.
        InitSlider(_masterSlider);
        InitSlider(_bgmSlider);
        InitSlider(_sfxSlider);

        // 2. 실시간 볼륨 조절 이벤트 바인딩 (드래그하는 동안 실시간 반영)
        _masterSlider.onValueChanged.AddListener(val => SoundManager.Instance.SetMasterVolume(val));
        _bgmSlider.onValueChanged.AddListener(val => SoundManager.Instance.SetBGMVolume(val));
        _sfxSlider.onValueChanged.AddListener(val => SoundManager.Instance.SetSFXVolume(val));

        // 3. SFX 슬라이더에서 마우스를 뗄 때만 테스트 사운드가 나도록 EventTrigger 바인딩
        AddPointerUpEvent(_sfxSlider.gameObject);
    }

    private void InitSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = 1f; // 기본값은 최대 볼륨
    }

    // 오브젝트에 EventTrigger를 동적으로 추가하고 PointerUp 이벤트를 연결하는 함수
    private void AddPointerUpEvent(GameObject targetObject)
    {
        if (targetObject == null) return;

        // 컴포넌트가 없으면 추가, 있으면 가져옴
        EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetObject.AddComponent<EventTrigger>();
        }

        // PointerUp (마우스 클릭 해제 / 터치 떼기) 엔트리 생성
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        
        // 마우스를 뗄 때 실행할 메서드 연결
        entry.callback.AddListener((data) => { OnSFXSliderPointerUp(); });
        
        // 트리거 목록에 추가
        trigger.triggers.Add(entry);
    }

    // SFX 슬라이더에서 손을 뗄 때 호출될 함수
    private void OnSFXSliderPointerUp()
    {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(_testSFXName))
        {
            // 현재 설정된 SFX 볼륨 크기대로 테스트 사운드 재생
            SoundManager.Instance.PlaySFX(_testSFXName);
            Debug.Log($"SFX 테스트 사운드 재생: {_testSFXName}");
        }
    }
}