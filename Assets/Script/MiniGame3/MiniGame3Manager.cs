using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class MiniGame3Manager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int _width = 8;
    [SerializeField, Min(1)] private int _height = 8;
    [SerializeField, Min(0.01f)] private float _cellSize = 1f;
    [SerializeField] private Transform _origin;

    [Header("Scene Objects")]
    [SerializeField] private bool _autoCollectSceneObjects = true;
    [SerializeField] private bool _snapObjectsToGridOnStart = true;

    [SerializeField] private MiniGame3Player _player;
    [SerializeField] private MiniGame3Goal _goal;
    [SerializeField] private List<MiniGame3Wall> _walls = new List<MiniGame3Wall>();
    [SerializeField] private List<MiniGame3PushableObject> _pushableObjects = new List<MiniGame3PushableObject>();

    [Header("Player Visual")]
    [SerializeField] private SpriteRenderer _playerSpriteRenderer;

    [Tooltip("원본 플레이어 스프라이트가 오른쪽을 보고 있으면 true, 왼쪽을 보고 있으면 false")]
    [SerializeField] private bool _playerSpriteFacesRightByDefault = true;

    [Tooltip("true면 실제 이동 성공 시에만 방향 전환, false면 벽에 막혀도 입력 방향으로 바라봄")]
    [SerializeField] private bool _flipOnlyWhenMoveSucceeds = false;

    [Header("Movement")]
    [SerializeField] private bool _useKeyboardInput = true;
    [SerializeField, Min(0f)] private float _moveDuration = 0.12f;

    [Header("Gizmos")]
    [SerializeField] private bool _drawGridGizmos = true;
    [SerializeField] private bool _drawObjectGizmos = true;
    [SerializeField] private Color _gridGizmoColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color _originGizmoColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color _playerGizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color _goalGizmoColor = new Color(0.2f, 1f, 0.2f, 0.8f);
    [SerializeField] private Color _wallGizmoColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _pushableGizmoColor = new Color(1f, 0.6f, 0.1f, 0.8f);

    [Header("Events")]
    [SerializeField] private UnityEvent _onClear;

    private readonly HashSet<Vector2Int> _wallPositions = new HashSet<Vector2Int>();

    private readonly Dictionary<Vector2Int, MiniGame3PushableObject> _pushableByPosition =
        new Dictionary<Vector2Int, MiniGame3PushableObject>();

    private readonly Stack<MiniGame3Snapshot> _history = new Stack<MiniGame3Snapshot>();

    private readonly Dictionary<MiniGame3PushableObject, Vector2Int> _initialPushablePositions =
        new Dictionary<MiniGame3PushableObject, Vector2Int>();

    private Vector2Int _initialPlayerPosition;

    private bool _isInitialized;
    private bool _isMoving;
    private bool _isCleared;

    private struct MiniGame3Snapshot
    {
        public Vector2Int PlayerPosition;
        public Dictionary<MiniGame3PushableObject, Vector2Int> PushablePositions;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!_useKeyboardInput)
            return;

        if (_isCleared || _isMoving)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            TryMove(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            TryMove(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            TryMove(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            TryMove(Vector2Int.right);
        }
    }

    public void Initialize()
    {
        _isInitialized = false;
        _isMoving = false;
        _isCleared = false;

        _history.Clear();
        _wallPositions.Clear();
        _pushableByPosition.Clear();
        _initialPushablePositions.Clear();

        if (_autoCollectSceneObjects)
        {
            CollectSceneObjects();
        }

        if (!ValidateRequiredReferences())
            return;

        InitializePlayerVisual();

        RegisterWalls();
        RegisterPushableObjects();

        if (!ValidateInitialPositions())
            return;

        _initialPlayerPosition = _player.GridPosition;

        foreach (KeyValuePair<Vector2Int, MiniGame3PushableObject> pair in _pushableByPosition)
        {
            _initialPushablePositions[pair.Value] = pair.Key;
        }

        if (_snapObjectsToGridOnStart)
        {
            SnapAllObjectsToGrid();
        }

        _isInitialized = true;

        CheckClear();
    }

    public bool TryMove(Vector2Int direction)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("MiniGame3Manager is not initialized.");
            return false;
        }

        if (_isCleared || _isMoving)
            return false;

        if (!IsCardinalDirection(direction))
        {
            Debug.LogWarning($"Invalid move direction: {direction}");
            return false;
        }

        Vector2Int playerFrom = _player.GridPosition;
        Vector2Int playerTo = playerFrom + direction;

        if (!_flipOnlyWhenMoveSucceeds)
        {
            UpdatePlayerFlipX(direction);
        }

        if (!CanPlayerMoveTo(
                playerTo,
                direction,
                out MiniGame3PushableObject pushedObject,
                out Vector2Int pushFrom,
                out Vector2Int pushTo))
        {
            return false;
        }

        if (_flipOnlyWhenMoveSucceeds)
        {
            UpdatePlayerFlipX(direction);
        }

        SaveSnapshot();

        if (pushedObject != null)
        {
            MovePushableLogical(pushedObject, pushFrom, pushTo);
        }

        _player.SetGridPosition(playerTo);

        if (_moveDuration <= 0f)
        {
            ApplyImmediateVisualPosition(_player, playerTo);

            if (pushedObject != null)
            {
                ApplyImmediateVisualPosition(pushedObject, pushTo);
            }

            CheckClear();
            return true;
        }

        StartCoroutine(MoveRoutine(playerFrom, playerTo, pushedObject, pushFrom, pushTo));
        return true;
    }

    public void ResetMiniGame()
    {
        if (!_isInitialized)
            return;

        if (_isMoving)
            return;

        _history.Clear();
        _isCleared = false;

        _pushableByPosition.Clear();

        _player.SetGridPosition(_initialPlayerPosition);
        _player.SetWorldPosition(GridToWorld(_initialPlayerPosition));

        foreach (KeyValuePair<MiniGame3PushableObject, Vector2Int> pair in _initialPushablePositions)
        {
            MiniGame3PushableObject pushable = pair.Key;

            if (pushable == null)
                continue;

            Vector2Int position = pair.Value;

            pushable.SetGridPosition(position);
            pushable.SetWorldPosition(GridToWorld(position));

            _pushableByPosition[position] = pushable;
        }

        CheckClear();
    }

    public void UndoLastMove()
    {
        if (!_isInitialized)
            return;

        if (_isMoving)
            return;

        if (_history.Count <= 0)
            return;

        MiniGame3Snapshot snapshot = _history.Pop();
        ApplySnapshot(snapshot);
    }

    public void OnClickUp()
    {
        TryMove(Vector2Int.up);
    }

    public void OnClickDown()
    {
        TryMove(Vector2Int.down);
    }

    public void OnClickLeft()
    {
        TryMove(Vector2Int.left);
    }

    public void OnClickRight()
    {
        TryMove(Vector2Int.right);
    }

    [ContextMenu("Collect Scene Objects")]
    private void CollectSceneObjects()
    {
        if (_player == null)
        {
            _player = FindFirstSceneObject<MiniGame3Player>();
        }

        if (_goal == null)
        {
            _goal = FindFirstSceneObject<MiniGame3Goal>();
        }

        _walls.Clear();
        _walls.AddRange(FindSceneObjects<MiniGame3Wall>());

        _pushableObjects.Clear();
        _pushableObjects.AddRange(FindSceneObjects<MiniGame3PushableObject>());
    }

    [ContextMenu("Snap Objects To Grid")]
    private void SnapAllObjectsToGridByContextMenu()
    {
        if (_autoCollectSceneObjects)
        {
            CollectSceneObjects();
        }

        if (_player != null)
        {
            _player.SetWorldPosition(GridToWorld(_player.GridPosition));
        }

        if (_goal != null)
        {
            _goal.SetWorldPosition(GridToWorld(_goal.GridPosition));
        }

        foreach (MiniGame3Wall wall in _walls)
        {
            if (wall == null)
                continue;

            wall.SetWorldPosition(GridToWorld(wall.GridPosition));
        }

        foreach (MiniGame3PushableObject pushable in _pushableObjects)
        {
            if (pushable == null)
                continue;

            pushable.SetWorldPosition(GridToWorld(pushable.GridPosition));
        }
    }

    private void InitializePlayerVisual()
    {
        if (_playerSpriteRenderer != null)
            return;

        if (_player == null)
            return;

        _playerSpriteRenderer = _player.GetComponentInChildren<SpriteRenderer>(true);

        if (_playerSpriteRenderer == null)
        {
            Debug.LogWarning("Player SpriteRenderer is missing. Player flipX will not work.");
        }
    }

    private void UpdatePlayerFlipX(Vector2Int direction)
    {
        if (_playerSpriteRenderer == null)
            return;

        if (direction.x == 0)
            return;

        if (_playerSpriteFacesRightByDefault)
        {
            _playerSpriteRenderer.flipX = direction.x > 0;
        }
        else
        {
            _playerSpriteRenderer.flipX = direction.x < 0;
        }
    }

    private bool ValidateRequiredReferences()
    {
        bool isValid = true;

        if (_player == null)
        {
            Debug.LogError("MiniGame3Player is missing.");
            isValid = false;
        }

        if (_goal == null)
        {
            Debug.LogError("MiniGame3Goal is missing.");
            isValid = false;
        }

        return isValid;
    }

    private void RegisterWalls()
    {
        foreach (MiniGame3Wall wall in _walls)
        {
            if (wall == null)
                continue;

            Vector2Int position = wall.GridPosition;

            if (!IsInside(position))
            {
                Debug.LogWarning($"Wall is outside grid. Position: {position}, Object: {wall.name}");
                continue;
            }

            if (!_wallPositions.Add(position))
            {
                Debug.LogWarning($"Duplicated wall position: {position}, Object: {wall.name}");
            }
        }
    }

    private void RegisterPushableObjects()
    {
        foreach (MiniGame3PushableObject pushable in _pushableObjects)
        {
            if (pushable == null)
                continue;

            Vector2Int position = pushable.GridPosition;

            if (!IsInside(position))
            {
                Debug.LogWarning($"Pushable object is outside grid. Position: {position}, Object: {pushable.name}");
                continue;
            }

            if (_wallPositions.Contains(position))
            {
                Debug.LogWarning($"Pushable object is placed on wall. Position: {position}, Object: {pushable.name}");
                continue;
            }

            if (_pushableByPosition.ContainsKey(position))
            {
                Debug.LogWarning($"Duplicated pushable object position: {position}, Object: {pushable.name}");
                continue;
            }

            _pushableByPosition[position] = pushable;
        }
    }

    private bool ValidateInitialPositions()
    {
        bool isValid = true;

        Vector2Int playerPosition = _player.GridPosition;
        Vector2Int goalPosition = _goal.GridPosition;

        if (!IsInside(playerPosition))
        {
            Debug.LogError($"Player is outside grid. Position: {playerPosition}");
            isValid = false;
        }

        if (!IsInside(goalPosition))
        {
            Debug.LogError($"Goal is outside grid. Position: {goalPosition}");
            isValid = false;
        }

        if (_wallPositions.Contains(playerPosition))
        {
            Debug.LogError($"Player is placed on wall. Position: {playerPosition}");
            isValid = false;
        }

        if (_wallPositions.Contains(goalPosition))
        {
            Debug.LogError($"Goal is placed on wall. Position: {goalPosition}");
            isValid = false;
        }

        if (_pushableByPosition.ContainsKey(playerPosition))
        {
            Debug.LogError($"Player is placed on pushable object. Position: {playerPosition}");
            isValid = false;
        }

        return isValid;
    }

    private bool CanPlayerMoveTo(
        Vector2Int playerTo,
        Vector2Int direction,
        out MiniGame3PushableObject pushedObject,
        out Vector2Int pushFrom,
        out Vector2Int pushTo)
    {
        pushedObject = null;
        pushFrom = default;
        pushTo = default;

        if (!IsInside(playerTo))
            return false;

        if (IsWall(playerTo))
            return false;

        if (!_pushableByPosition.TryGetValue(playerTo, out pushedObject))
            return true;

        pushFrom = playerTo;
        pushTo = playerTo + direction;

        if (!CanPushTo(pushTo))
        {
            pushedObject = null;
            return false;
        }

        return true;
    }

    private bool CanPushTo(Vector2Int position)
    {
        if (!IsInside(position))
            return false;

        if (IsWall(position))
            return false;

        if (_pushableByPosition.ContainsKey(position))
            return false;

        return true;
    }

    private void MovePushableLogical(MiniGame3PushableObject pushable, Vector2Int from, Vector2Int to)
    {
        _pushableByPosition.Remove(from);
        _pushableByPosition[to] = pushable;

        pushable.SetGridPosition(to);
    }

    private IEnumerator MoveRoutine(
        Vector2Int playerFrom,
        Vector2Int playerTo,
        MiniGame3PushableObject pushedObject,
        Vector2Int pushFrom,
        Vector2Int pushTo)
    {
        _isMoving = true;

        Vector3 playerStart = GridToWorld(playerFrom);
        Vector3 playerEnd = GridToWorld(playerTo);

        Vector3 pushStart = Vector3.zero;
        Vector3 pushEnd = Vector3.zero;

        bool hasPushedObject = pushedObject != null;

        if (hasPushedObject)
        {
            pushStart = GridToWorld(pushFrom);
            pushEnd = GridToWorld(pushTo);
        }

        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / _moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            _player.SetWorldPosition(Vector3.Lerp(playerStart, playerEnd, t));

            if (hasPushedObject)
            {
                pushedObject.SetWorldPosition(Vector3.Lerp(pushStart, pushEnd, t));
            }

            yield return null;
        }

        _player.SetWorldPosition(playerEnd);

        if (hasPushedObject)
        {
            pushedObject.SetWorldPosition(pushEnd);
        }

        _isMoving = false;

        CheckClear();
    }

    private void SaveSnapshot()
    {
        MiniGame3Snapshot snapshot = new MiniGame3Snapshot
        {
            PlayerPosition = _player.GridPosition,
            PushablePositions = new Dictionary<MiniGame3PushableObject, Vector2Int>()
        };

        foreach (KeyValuePair<Vector2Int, MiniGame3PushableObject> pair in _pushableByPosition)
        {
            snapshot.PushablePositions[pair.Value] = pair.Key;
        }

        _history.Push(snapshot);
    }

    private void ApplySnapshot(MiniGame3Snapshot snapshot)
    {
        _isCleared = false;

        _player.SetGridPosition(snapshot.PlayerPosition);
        _player.SetWorldPosition(GridToWorld(snapshot.PlayerPosition));

        _pushableByPosition.Clear();

        foreach (KeyValuePair<MiniGame3PushableObject, Vector2Int> pair in snapshot.PushablePositions)
        {
            MiniGame3PushableObject pushable = pair.Key;

            if (pushable == null)
                continue;

            Vector2Int position = pair.Value;

            pushable.SetGridPosition(position);
            pushable.SetWorldPosition(GridToWorld(position));

            _pushableByPosition[position] = pushable;
        }
    }

    private void SnapAllObjectsToGrid()
    {
        _player.SetWorldPosition(GridToWorld(_player.GridPosition));
        _goal.SetWorldPosition(GridToWorld(_goal.GridPosition));

        foreach (MiniGame3Wall wall in _walls)
        {
            if (wall == null)
                continue;

            wall.SetWorldPosition(GridToWorld(wall.GridPosition));
        }

        foreach (MiniGame3PushableObject pushable in _pushableObjects)
        {
            if (pushable == null)
                continue;

            pushable.SetWorldPosition(GridToWorld(pushable.GridPosition));
        }
    }

    private void ApplyImmediateVisualPosition(MiniGame3GridObject gridObject, Vector2Int position)
    {
        gridObject.SetWorldPosition(GridToWorld(position));
    }

    private void CheckClear()
    {
        if (_isCleared)
            return;

        if (_player.GridPosition != _goal.GridPosition)
            return;

        _isCleared = true;

        Debug.Log("MiniGame3 Clear");
        _onClear?.Invoke();
    }

    private bool IsInside(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < _width &&
               position.y >= 0 &&
               position.y < _height;
    }

    private bool IsWall(Vector2Int position)
    {
        return _wallPositions.Contains(position);
    }

    private bool IsCardinalDirection(Vector2Int direction)
    {
        return direction == Vector2Int.up ||
               direction == Vector2Int.down ||
               direction == Vector2Int.left ||
               direction == Vector2Int.right;
    }

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        Vector3 basePosition = GetOriginPosition();

        return basePosition + new Vector3(
            gridPosition.x * _cellSize,
            gridPosition.y * _cellSize,
            0f);
    }

    private Vector3 GridCornerToWorld(float x, float y)
    {
        Vector3 basePosition = GetOriginPosition();

        return basePosition + new Vector3(
            x * _cellSize,
            y * _cellSize,
            0f);
    }

    private Vector3 GetOriginPosition()
    {
        return _origin != null ? _origin.position : transform.position;
    }

    private void OnDrawGizmos()
    {
        if (_drawGridGizmos)
        {
            DrawGridGizmos();
        }

        if (_drawObjectGizmos)
        {
            DrawObjectGizmos();
        }
    }

    private void DrawGridGizmos()
    {
        float safeCellSize = Mathf.Max(0.01f, _cellSize);

        Gizmos.color = _gridGizmoColor;

        float left = -0.5f;
        float right = _width - 0.5f;
        float bottom = -0.5f;
        float top = _height - 0.5f;

        for (int x = 0; x <= _width; x++)
        {
            float lineX = x - 0.5f;

            Vector3 start = GridCornerToWorld(lineX, bottom);
            Vector3 end = GridCornerToWorld(lineX, top);

            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= _height; y++)
        {
            float lineY = y - 0.5f;

            Vector3 start = GridCornerToWorld(left, lineY);
            Vector3 end = GridCornerToWorld(right, lineY);

            Gizmos.DrawLine(start, end);
        }

        Gizmos.color = _originGizmoColor;
        Gizmos.DrawSphere(GetOriginPosition(), safeCellSize * 0.08f);
    }

    private void DrawObjectGizmos()
    {
        float size = Mathf.Max(0.01f, _cellSize) * 0.75f;

        if (_goal != null)
        {
            DrawCellGizmo(_goal.GridPosition, _goalGizmoColor, size);
        }

        if (_player != null)
        {
            DrawCellGizmo(_player.GridPosition, _playerGizmoColor, size);
        }

        foreach (MiniGame3Wall wall in _walls)
        {
            if (wall == null)
                continue;

            DrawCellGizmo(wall.GridPosition, _wallGizmoColor, size);
        }

        foreach (MiniGame3PushableObject pushable in _pushableObjects)
        {
            if (pushable == null)
                continue;

            DrawCellGizmo(pushable.GridPosition, _pushableGizmoColor, size);
        }
    }

    private void DrawCellGizmo(Vector2Int gridPosition, Color color, float size)
    {
        Vector3 center = GridToWorld(gridPosition);

        Gizmos.color = color;
        Gizmos.DrawWireCube(center, new Vector3(size, size, 0f));
    }

    private static T FindFirstSceneObject<T>() where T : MonoBehaviour
    {
        T[] objects = FindSceneObjects<T>();
        return objects.Length > 0 ? objects[0] : null;
    }

    private static T[] FindSceneObjects<T>() where T : MonoBehaviour
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<T>(true);
#endif
    }
}