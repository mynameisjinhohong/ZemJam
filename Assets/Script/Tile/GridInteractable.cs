using UnityEngine;
using UnityEngine.Events;

public class GridInteractable : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPos;

    [Header("Identity")]
    [SerializeField] private string _interactableId;
    [SerializeField] private int _interactableIndex;

    [Header("Interaction")]
    [SerializeField] private bool _triggerOnEnter = false;
    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private UnityEvent _onInteracted;

    [Header("Scene Transition")]
    [SerializeField] private bool _loadSceneOnInteract = true;
    [SerializeField] private string _targetSceneName;
    [SerializeField] private bool _disappearAfterInteract = true;

    [Header("Progress")]
    [SerializeField] private bool _useIndexActivation = true;
    [SerializeField] private bool _markClearedOnInteract = true;
    [SerializeField] private bool _advanceIndexOnInteract = true;

    [Header("Runtime State")]
    [SerializeField] private bool _hasInteracted = false;

    private GridBoard _board;

    public Vector2Int GridPos => _gridPos;
    public bool TriggerOnEnter => _triggerOnEnter;
    public bool BlocksMovement => _blocksMovement;
    public string InteractableId => _interactableId;
    public int InteractableIndex => _interactableIndex;
    public bool HasInteracted => _hasInteracted;

    public void RefreshActiveState()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{name}: GameManager.Instance is null. Interactable activation was not refreshed.");
            return;
        }

        if (_disappearAfterInteract &&
            GameManager.Instance.IsInteractableCleared(_interactableId))
        {
            _hasInteracted = true;
            gameObject.SetActive(false);
            return;
        }

        if (_useIndexActivation)
        {
            bool shouldBeActive =
                _interactableIndex == GameManager.Instance.CurrentInteractableIndex;

            gameObject.SetActive(shouldBeActive);
            return;
        }

        gameObject.SetActive(true);
    }

    public void Init(GridBoard board)
    {
        _board = board;
        transform.position = _board.GridToWorld(_gridPos);
    }

    public void Interact(GridPlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{name}: Player is null.");
            return;
        }

        if (!gameObject.activeInHierarchy)
            return;

        if (_hasInteracted)
            return;

        _hasInteracted = true;

        _onInteracted?.Invoke();

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null.");
            return;
        }

        if (_markClearedOnInteract && _disappearAfterInteract)
        {
            GameManager.Instance.MarkInteractableCleared(_interactableId);
        }

        if (_advanceIndexOnInteract)
        {
            GameManager.Instance.AdvanceInteractableIndex();
        }

        if (_loadSceneOnInteract)
        {
            GameManager.Instance.SavePlayerReturnState(player.GridPos);

            CutSceneManager.Instance.FadeOutToBlackScreen(2f, () =>
            {
                GameManager.Instance.LoadMiniGameScene(_targetSceneName);
            });

            return;
        }

        if (_disappearAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }

    public void ResetInteractedState()
    {
        _hasInteracted = false;
    }
}