using UnityEngine;
using UnityEngine.Events;

public class GridInteractable : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPos;

    [Header("Identity")]
    [SerializeField] private string _interactableId;

    [Header("Interaction")]
    [SerializeField] private bool _triggerOnEnter = false;
    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private UnityEvent _onInteracted;

    [Header("Scene Transition")]
    [SerializeField] private bool _loadSceneOnInteract;
    [SerializeField] private string _targetSceneName;
    [SerializeField] private bool _disappearAfterInteract = true;

    private GridBoard _board;

    public Vector2Int GridPos => _gridPos;
    public bool TriggerOnEnter => _triggerOnEnter;
    public bool BlocksMovement => _blocksMovement;
    public string InteractableId => _interactableId;

    private void Awake()
    {
        if (GameManager.Instance != null &&
            _disappearAfterInteract &&
            GameManager.Instance.IsInteractableCleared(_interactableId))
        {
            gameObject.SetActive(false);
        }
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

        _onInteracted?.Invoke();

        if (_disappearAfterInteract && GameManager.Instance != null)
        {
            GameManager.Instance.MarkInteractableCleared(_interactableId);
        }

        if (_loadSceneOnInteract)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance is null.");
                return;
            }

            GameManager.Instance.SavePlayerReturnState(player.GridPos);
            GameManager.Instance.LoadMiniGameScene(_targetSceneName);
        }
        else if (_disappearAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }
}