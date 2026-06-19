using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MiniGame4DotConnectManager : MonoBehaviour
{
    [Serializable]
    public class ConnectionRule
    {
        public MiniGame4DotNode from;
        public MiniGame4DotNode to;
    }

    private struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly int A;
        public readonly int B;

        public EdgeKey(int a, int b)
        {
            if (a < b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public bool Equals(EdgeKey other)
        {
            return A == other.A && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (A * 397) ^ B;
            }
        }

        public override string ToString()
        {
            return $"{A}-{B}";
        }
    }

    [Header("Node")]
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _nodeLayer;
    [SerializeField] private MiniGame4DotNode _startNode;
    [SerializeField] private List<MiniGame4DotNode> _nodes = new();

    [Header("Answer")]
    [SerializeField] private List<ConnectionRule> _clearRules = new();

    [Header("Line")]
    [SerializeField] private LineRenderer _linePrefab;
    [SerializeField] private Transform _lineRoot;
    [SerializeField] private float _lineZ = 0f;

    [Header("Drag")]
    [SerializeField] private float _nodeDetectRadius = 0.25f;

    [Tooltip("true면 정답에 포함된 연결만 허용합니다. false면 오답 연결도 그려지고, 최종 비교에서 실패합니다.")]
    [SerializeField] private bool _connectOnlyAnswerEdges = true;

    [Tooltip("true면 이미 지나간 점을 다시 방문할 수 있습니다.")]
    [SerializeField] private bool _allowRevisitNode = true;

    [Tooltip("true면 클리어 순간 선을 유지합니다. false면 클리어해도 선을 지웁니다.")]
    [SerializeField] private bool _keepLinesOnClear = true;

    [Header("Gizmo")]
    [SerializeField] private bool _drawAnswerGizmos = true;

    [Tooltip("true면 Play 중에도 정답 기즈모를 그립니다. Game View에서 선이 2개로 보이면 false로 두십시오.")]
    [SerializeField] private bool _drawAnswerGizmosInPlayMode = false;

    [SerializeField] private float _startNodeGizmoRadius = 0.35f;

    [Header("Event")]
    [SerializeField] private UnityEvent _onClear;

    private readonly HashSet<EdgeKey> _answerEdges = new();
    private readonly HashSet<EdgeKey> _currentEdges = new();
    private readonly HashSet<int> _visitedNodeIds = new();

    private readonly Dictionary<EdgeKey, LineRenderer> _createdLineByEdge = new();

    private MiniGame4DotNode _lastNode;
    private LineRenderer _previewLine;

    private bool _isDragging;
    private bool _isCleared;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        BuildAnswerEdges();
        ValidateNodeIds();
    }

    private void Update()
    {
        if (_isCleared)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag();
        }

        if (_isDragging)
        {
            UpdatePreviewLine();
            TryConnectNodeUnderPointer();

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
    }

    private void TryBeginDrag()
    {
        if (_startNode == null)
        {
            Debug.LogError("[MiniGame4] StartNode가 설정되지 않았습니다.");
            return;
        }

        if (_linePrefab == null)
        {
            Debug.LogError("[MiniGame4] LinePrefab이 설정되지 않았습니다.");
            return;
        }

        if (!TryGetNodeUnderPointer(out MiniGame4DotNode node))
            return;

        if (node != _startNode)
            return;

        ResetCurrentState();

        _isDragging = true;
        _lastNode = _startNode;
        _visitedNodeIds.Add(_startNode.Id);

        _previewLine = CreateLine("MiniGame4 Preview Line");
        SetLinePosition(_previewLine, _lastNode.Position, GetMouseWorldPosition());
    }

    private void TryConnectNodeUnderPointer()
    {
        if (!TryGetNodeUnderPointer(out MiniGame4DotNode node))
            return;

        if (node == _lastNode)
            return;

        if (!_allowRevisitNode && _visitedNodeIds.Contains(node.Id))
            return;

        EdgeKey edge = new EdgeKey(_lastNode.Id, node.Id);

        if (_currentEdges.Contains(edge))
            return;

        if (_createdLineByEdge.ContainsKey(edge))
            return;

        if (_connectOnlyAnswerEdges && !_answerEdges.Contains(edge))
            return;

        MiniGame4DotNode previousNode = _lastNode;

        _currentEdges.Add(edge);
        _visitedNodeIds.Add(node.Id);

        LineRenderer fixedLine = CreateLine($"MiniGame4 Line {edge}");
        SetLinePosition(fixedLine, previousNode.Position, node.Position);

        _createdLineByEdge.Add(edge, fixedLine);

        _lastNode = node;

        // 중요:
        // 점에 닿은 바로 그 프레임에 Preview Line이 방금 생성된 Fixed Line과 겹쳐 보일 수 있으므로,
        // Preview Line을 현재 점 위치로 즉시 접어둔다.
        if (_previewLine != null)
        {
            SetLinePosition(_previewLine, node.Position, node.Position);
        }

        if (IsClearConditionSatisfied())
        {
            CompleteClear();
        }
    }

    private void EndDrag()
    {
        if (!_isDragging)
            return;

        if (IsClearConditionSatisfied())
        {
            CompleteClear();
            return;
        }

        ResetCurrentState();
    }

    private bool IsClearConditionSatisfied()
    {
        if (_currentEdges.Count != _answerEdges.Count)
            return false;

        foreach (EdgeKey answerEdge in _answerEdges)
        {
            if (!_currentEdges.Contains(answerEdge))
                return false;
        }

        return true;
    }

    private void CompleteClear()
    {
        _isDragging = false;
        _isCleared = true;

        DestroyPreviewLine();

        if (!_keepLinesOnClear)
        {
            ClearCreatedLines();
        }

        _onClear?.Invoke();
    }

    private void ResetCurrentState()
    {
        _isDragging = false;
        _lastNode = null;

        _currentEdges.Clear();
        _visitedNodeIds.Clear();

        DestroyPreviewLine();
        ClearCreatedLines();
    }

    private void BuildAnswerEdges()
    {
        _answerEdges.Clear();

        foreach (ConnectionRule rule in _clearRules)
        {
            if (rule == null || rule.from == null || rule.to == null)
                continue;

            if (rule.from == rule.to)
            {
                Debug.LogWarning("[MiniGame4] 자기 자신과 연결된 정답 규칙은 무시됩니다.");
                continue;
            }

            EdgeKey edge = new EdgeKey(rule.from.Id, rule.to.Id);

            if (!_answerEdges.Add(edge))
            {
                Debug.LogWarning($"[MiniGame4] 중복된 정답 연결이 있습니다: {edge}");
            }
        }
    }

    private void ValidateNodeIds()
    {
        HashSet<int> ids = new();

        foreach (MiniGame4DotNode node in _nodes)
        {
            if (node == null)
                continue;

            if (!ids.Add(node.Id))
            {
                Debug.LogError($"[MiniGame4] 중복된 DotNode ID가 있습니다: {node.Id}", node);
            }
        }
    }

    private bool TryGetNodeUnderPointer(out MiniGame4DotNode node)
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        Collider2D col = Physics2D.OverlapCircle(
            mouseWorld,
            _nodeDetectRadius,
            _nodeLayer
        );

        if (col != null)
        {
            node = col.GetComponentInParent<MiniGame4DotNode>();
            return node != null;
        }

        node = null;
        return false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (_camera == null)
            return Vector3.zero;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(_camera.transform.position.z - _lineZ);

        Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
        worldPos.z = _lineZ;

        return worldPos;
    }

    private LineRenderer CreateLine(string lineName)
    {
        Transform parent = _lineRoot != null ? _lineRoot : transform;

        LineRenderer line = Instantiate(_linePrefab, parent);
        line.name = lineName;
        line.useWorldSpace = true;
        line.positionCount = 2;

        return line;
    }

    private void SetLinePosition(LineRenderer line, Vector3 from, Vector3 to)
    {
        if (line == null)
            return;

        from.z = _lineZ;
        to.z = _lineZ;

        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private void UpdatePreviewLine()
    {
        if (_previewLine == null || _lastNode == null)
            return;

        SetLinePosition(_previewLine, _lastNode.Position, GetMouseWorldPosition());
    }

    private void DestroyPreviewLine()
    {
        if (_previewLine != null)
        {
            Destroy(_previewLine.gameObject);
            _previewLine = null;
        }
    }

    private void ClearCreatedLines()
    {
        foreach (LineRenderer line in _createdLineByEdge.Values)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        _createdLineByEdge.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_drawAnswerGizmos)
            return;

        if (Application.isPlaying && !_drawAnswerGizmosInPlayMode)
            return;

        Gizmos.color = Color.cyan;

        foreach (ConnectionRule rule in _clearRules)
        {
            if (rule == null || rule.from == null || rule.to == null)
                continue;

            Gizmos.DrawLine(rule.from.Position, rule.to.Position);
        }

        if (_startNode != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_startNode.Position, _startNodeGizmoRadius);
        }
    }
#endif
}