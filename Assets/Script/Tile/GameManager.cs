using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public const string GameSceneName = "GameScene";

    public static GameManager Instance { get; private set; }

    [Header("Game Scene State")]
    [SerializeField] private bool _isFirstGameSceneEnter = true;

    private Vector2Int _lastPlayerGridPos;
    private bool _hasLastPlayerGridPos;

    private readonly HashSet<string> _clearedInteractableIds = new();

    public bool IsFirstGameSceneEnter => _isFirstGameSceneEnter;
    public bool HasLastPlayerGridPos => _hasLastPlayerGridPos;
    public Vector2Int LastPlayerGridPos => _lastPlayerGridPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    public bool IsCurrentSceneGameScene()
    {
        return SceneManager.GetActiveScene().name == GameSceneName;
    }

    public void ClearSavedPlayerPosition()
    {
        _hasLastPlayerGridPos = false;
        _lastPlayerGridPos = default;
    }
}