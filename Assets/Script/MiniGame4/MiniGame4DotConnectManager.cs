using System;
using System.Collections;
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
    [SerializeField] private List<MiniGame4DotNode> _nodes = new();

    [Header("Answer")]
    [SerializeField] private List<ConnectionRule> _clearRules = new();

    [Header("Line")]
    [SerializeField] private MiniGame4Line _linePrefab;
    [SerializeField] private Transform _lineRoot;
    [SerializeField] private float _lineZ = 0f;

    [Header("Drag")]
    [SerializeField] private float _nodeDetectRadius = 0.25f;

    [Tooltip("true면 이미 지나간 점을 다시 방문할 수 있습니다.")]
    [SerializeField] private bool _allowRevisitNode = true;

    [Header("Clear Sequence")]
    [SerializeField] private SpriteRenderer _ribbonRenderer;

    [Tooltip("성공 후 라인이 사라지는 시간입니다.")]
    [SerializeField] private float _lineFadeOutDuration = 0.35f;

    [Tooltip("라인이 사라진 뒤 리본이 나타나는 시간입니다.")]
    [SerializeField] private float _ribbonFadeInDuration = 0.35f;

    [Tooltip("리본 페이드인이 끝난 뒤 GameEndEvent 실행 전 대기 시간입니다.")]
    [SerializeField] private float _delayBeforeGameEndEvent = 0.2f;

    [Tooltip("성공 연출 후 라인 오브젝트를 Destroy할지 여부입니다.")]
    [SerializeField] private bool _destroyLinesAfterFadeOut = true;

    [Header("Gizmo")]
    [SerializeField] private bool _drawAnswerGizmos = true;

    [Tooltip("true면 Play 중에도 정답 기즈모를 그립니다.")]
    [SerializeField] private bool _drawAnswerGizmosInPlayMode = false;

    [Header("Event")]
    [SerializeField] private UnityEvent _gameEndEvent;

    private readonly HashSet<EdgeKey> _answerEdges = new();
    private readonly HashSet<EdgeKey> _currentEdges = new();
    private readonly HashSet<int> _visitedNodeIds = new();

    private readonly Dictionary<EdgeKey, MiniGame4Line> _createdLineByEdge = new();

    private MiniGame4DotNode _lastNode;
    private MiniGame4Line _previewLine;

    private bool _isDragging;
    private bool _isCleared;
    private bool _isPlayingClearSequence;

    private Coroutine _clearSequenceCoroutine;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        BuildAnswerEdges();
        ValidateNodeIds();
        InitializeRibbon();
    }

    private void Update()
    {
        if (_isCleared || _isPlayingClearSequence)
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
        if (_linePrefab == null)
        {
            Debug.LogError("[MiniGame4] LinePrefab이 설정되지 않았습니다.");
            return;
        }

        if (!TryGetNodeUnderPointer(out MiniGame4DotNode node))
            return;

        ResetCurrentState();

        _isDragging = true;
        _lastNode = node;
        _visitedNodeIds.Add(node.Id);

        _previewLine = CreateLine("MiniGame4 Preview Line");
        SetLinePosition(_previewLine, _lastNode.Position, GetMouseWorldPosition());
        _previewLine.SetAlpha(1f);
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

        MiniGame4DotNode previousNode = _lastNode;

        _currentEdges.Add(edge);
        _visitedNodeIds.Add(node.Id);

        MiniGame4Line fixedLine = CreateLine($"MiniGame4 Line {edge}");
        SetLinePosition(fixedLine, previousNode.Position, node.Position);
        fixedLine.SetAlpha(1f);

        _createdLineByEdge.Add(edge, fixedLine);

        _lastNode = node;

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
        if (_isCleared || _isPlayingClearSequence)
            return;

        _isDragging = false;
        _isCleared = true;
        _isPlayingClearSequence = true;

        DestroyPreviewLine();

        if (_clearSequenceCoroutine != null)
        {
            StopCoroutine(_clearSequenceCoroutine);
        }

        _clearSequenceCoroutine = StartCoroutine(CoPlayClearSequence());
    }

    private IEnumerator CoPlayClearSequence()
    {
        yield return CoFadeOutLinesAndFadeInRibbon();

        if (_destroyLinesAfterFadeOut)
        {
            ClearCreatedLines();
        }

        if (_delayBeforeGameEndEvent > 0f)
        {
            yield return new WaitForSeconds(_delayBeforeGameEndEvent);
        }

        _gameEndEvent?.Invoke();

        _isPlayingClearSequence = false;
    }

    private IEnumerator CoFadeOutLinesAndFadeInRibbon()
    {
        if (_ribbonRenderer != null)
        {
            _ribbonRenderer.gameObject.SetActive(true);
            SetRibbonAlpha(0f);
        }

        float lineDuration = Mathf.Max(0f, _lineFadeOutDuration);
        float ribbonDuration = Mathf.Max(0f, _ribbonFadeInDuration);
        float totalDuration = Mathf.Max(lineDuration, ribbonDuration);

        if (totalDuration <= 0f)
        {
            SetAllLineAlpha(0f);
            SetRibbonAlpha(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            if (lineDuration > 0f)
            {
                float lineT = Mathf.Clamp01(elapsed / lineDuration);
                float lineAlpha = Mathf.Lerp(1f, 0f, lineT);
                SetAllLineAlpha(lineAlpha);
            }
            else
            {
                SetAllLineAlpha(0f);
            }

            if (ribbonDuration > 0f)
            {
                float ribbonT = Mathf.Clamp01(elapsed / ribbonDuration);
                float ribbonAlpha = Mathf.Lerp(0f, 1f, ribbonT);
                SetRibbonAlpha(ribbonAlpha);
            }
            else
            {
                SetRibbonAlpha(1f);
            }

            yield return null;
        }

        SetAllLineAlpha(0f);
        SetRibbonAlpha(1f);
    }

    private void SetAllLineAlpha(float alpha)
    {
        foreach (MiniGame4Line line in _createdLineByEdge.Values)
        {
            if (line != null)
            {
                line.SetAlpha(alpha);
            }
        }
    }

    private void InitializeRibbon()
    {
        if (_ribbonRenderer == null)
            return;

        _ribbonRenderer.gameObject.SetActive(true);
        SetRibbonAlpha(0f);
    }

    private void SetRibbonAlpha(float alpha)
    {
        if (_ribbonRenderer == null)
            return;

        Color color = _ribbonRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        _ribbonRenderer.color = color;
    }

    private void ResetCurrentState()
    {
        _isDragging = false;
        _lastNode = null;

        _currentEdges.Clear();
        _visitedNodeIds.Clear();

        DestroyPreviewLine();
        ClearCreatedLines();

        if (!_isCleared)
        {
            InitializeRibbon();
        }
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

    private MiniGame4Line CreateLine(string lineName)
    {
        Transform parent = _lineRoot != null ? _lineRoot : transform;

        MiniGame4Line line = Instantiate(_linePrefab, parent);
        line.name = lineName;
        line.SetCamera(_camera);

        return line;
    }

    private void SetLinePosition(MiniGame4Line line, Vector3 from, Vector3 to)
    {
        if (line == null)
            return;

        from.z = _lineZ;
        to.z = _lineZ;

        line.SetPosition(from, to);
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
        foreach (MiniGame4Line line in _createdLineByEdge.Values)
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
    }
#endif
}