using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public const string GameSceneName = "GameScene";
    public const string EndSceneName = "EndScene";
    public const string StartSceneName = "LobbyScene";
    public const string CreditSceneName = "CreditScene";

    public static GameManager Instance { get; private set; }

    [SerializeField] private string _bgmKey;
    [Header("Game Scene State")]
    [SerializeField] private bool _isFirstGameSceneEnter = true;

    [Header("Interactable Progress")]
    [SerializeField] private int _currentInteractableIndex = 0;

    private Vector2Int _lastPlayerGridPos;
    private bool _hasLastPlayerGridPos;

    private bool _canPlayerMove = true;

    private readonly HashSet<string> _clearedInteractableIds = new();

    public bool IsFirstGameSceneEnter => _isFirstGameSceneEnter;
    public bool HasLastPlayerGridPos => _hasLastPlayerGridPos;
    public Vector2Int LastPlayerGridPos => _lastPlayerGridPos;
    public bool CanPlayerMove => _canPlayerMove;
    public int CurrentInteractableIndex => _currentInteractableIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _canPlayerMove = true;

        if (scene.name != GameSceneName)
            return;

        if (!_isFirstGameSceneEnter)
            return;

        MarkGameSceneEntered();

        if (CutSceneManager.Instance != null)
        {
            CutSceneManager.Instance.ShowUISequence("Guide0_1", "Guide0_2");
        }
        else
        {
            Debug.LogWarning("CutSceneManager 인스턴스를 찾을 수 없습니다.");
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

    public void AdvanceInteractableIndex()
    {
        _currentInteractableIndex++;
    }

    public void SetInteractableIndex(int index)
    {
        if (index < 0)
        {
            Debug.LogWarning($"Invalid interactable index: {index}");
            return;
        }

        _currentInteractableIndex = index;
    }

    public void LoadMiniGameScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MiniGame scene name is null or empty.");
            return;
        }
        SoundManager.Instance.PlayBGM(_bgmKey);
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToGameScene()
    {
        SoundManager.Instance.StopBGM();
        SceneManager.LoadScene(GameSceneName);
    }

    public void GoToEndScene()
    {
        SceneManager.LoadScene(EndSceneName);
    }

    public void GoToStartScene()
    {
        SceneManager.LoadScene(StartSceneName);
    }

    public void GoToCreditScene()
    {
        SceneManager.LoadScene(CreditSceneName);
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