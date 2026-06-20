using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public const string GameSceneName = "GameScene";
    public const string EndSceneName = "EndScene";


    public static GameManager Instance { get; private set; }

    [Header("Game Scene State")]
    [SerializeField] private bool _isFirstGameSceneEnter = true;

    private Vector2Int _lastPlayerGridPos;
    private bool _hasLastPlayerGridPos;

    private readonly HashSet<string> _clearedInteractableIds = new();

    public bool IsFirstGameSceneEnter => _isFirstGameSceneEnter;
    public bool HasLastPlayerGridPos => _hasLastPlayerGridPos;
    public Vector2Int LastPlayerGridPos => _lastPlayerGridPos;

    private bool _canPlayerMove = true;
    public bool CanPlayerMove => _canPlayerMove;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제 (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때마다 실행되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 로드된 씬이 GameScene인지 확인
        if (scene.name == GameSceneName)
        {
            // 처음 입장한 상태라면
            if (_isFirstGameSceneEnter)
            {
                MarkGameSceneEntered(); // false로 변경

                // CutSceneManager를 통해 가이드 UI 출력
                if (CutSceneManager.Instance != null)
                {
                    CutSceneManager.Instance.ShowUIOnly("Guide0");
                }
                else
                {
                    Debug.LogWarning("CutSceneManager 인스턴스를 찾을 수 없습니다.");
                }
            }
        }
    }

    public void SavePlayerReturnState(Vector2Int playerGridPos)
    {
        _lastPlayerGridPos = playerGridPos;
        _hasLastPlayerGridPos = true;
    }

    public void MarkGameSceneEntered()
    {
        _isFirstGameSceneEnter = false;
    }

    public void MarkInteractableCleared(string interactableId)
    {
        if (string.IsNullOrWhiteSpace(interactableId))
        {
            Debug.LogWarning("Interactable ID is null or empty.");
            return;
        }

        _clearedInteractableIds.Add(interactableId);
    }

    public bool IsInteractableCleared(string interactableId)
    {
        if (string.IsNullOrWhiteSpace(interactableId))
            return false;

        return _clearedInteractableIds.Contains(interactableId);
    }

    public void LoadMiniGameScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MiniGame scene name is null or empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToGameScene()
    {
        SceneManager.LoadScene(GameSceneName);
    }
    public void GoToEndScene()
    {
        SceneManager.LoadScene(EndSceneName);
    }

    public bool IsCurrentSceneGameScene()
    {
        return SceneManager.GetActiveScene().name == GameSceneName;
    }

    public void ClearSavedPlayerPosition()
    {
        _hasLastPlayerGridPos = false;
        _lastPlayerGridPos = default;
    }

    public void DisablePlayerMovement()
    {
        _canPlayerMove = false;
    }
    public void EnablePlayerMovement()
    {
        _canPlayerMove = true;
    }
}