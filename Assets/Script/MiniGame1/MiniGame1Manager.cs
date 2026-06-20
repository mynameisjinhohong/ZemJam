using System.Collections;
using UnityEngine;

public class MiniGame1Manager : MonoBehaviour
{
    [Header("Pieces")]
    [Tooltip("에디터에서 완성 형태로 배치하세요.")]
    [SerializeField] private PuzzlePiece[] _pieces;
    [Tooltip("거리 측정 기준이 되는 조각 인덱스 (0부터 시작)")]
    [SerializeField] private int _referencePieceIndex = 0;

    [Header("Start Positions (Pieces 순서와 동일)")]
    [Tooltip("각 피스의 게임 시작 위치. 비워두면 에디터 위치 그대로 시작.")]
    [SerializeField] private Vector2[] _startPositions;

    [Header("Completion Settings")]
    [Tooltip("모든 피스의 위치 오차 합이 이 값 이하면 성공 (월드 단위)")]
    [SerializeField] private float _maxTotalError = 0.5f;

    [Header("Snap to Correct Position")]
    [SerializeField] private float _snapDuration = 0.4f;

    [Header("Completion Sequence")]
    [Tooltip("비활성화 상태로 씬에 배치")]
    [SerializeField] private GameObject _resultImage1Obj;
    [Tooltip("비활성화 상태로 씬에 배치. 위치는 ResultImage1 기준 오프셋으로 자동 설정됨")]
    [SerializeField] private GameObject _resultImage2Obj;
    [SerializeField] private Vector3 _resultImage2Offset = new(0.17f, -0.82f, 0f);
    [Tooltip("결과 이미지 위치 기준으로 쓸 퍼즐 조각 인덱스 (Pieces 배열 기준, 0부터 시작)")]
    [SerializeField] private int _resultAnchorPieceIndex;
    [Tooltip("앵커 위치에서 모든 완성 이미지에 공통 적용되는 오프셋")]
    [SerializeField] private Vector3 _anchorOffset = new(-0.16f, 0.22f, 0f);
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _afterFadeDelay = 0.5f;
    [SerializeField] private float _beforeGroupMoveDelay = 0.5f;

    [Header("Group Move (이미지 1+2 함께 이동)")]
    [SerializeField] private Vector3 _groupMoveTargetPosition;
    [SerializeField] private float _groupMoveDuration = 0.5f;

    [Header("ResultImage2 Fall")]
    [SerializeField] private Vector3 _resultImage2FallTargetPosition;
    [SerializeField] private Vector3 _resultImage2FallTargetRotation;
    [SerializeField] private float _resultImage2FallDelay = 0.5f;
    [SerializeField] private float _resultImage2FallDuration = 1f;

    private Vector2[] _correctOffsets;
    private bool _success;
    private Vector3 _anchorPositionAtCompletion;

    private void Start()
    {
        if (_pieces == null || _pieces.Length < 2)
        {
            Debug.LogError("PuzzleManager: Pieces 배열에 2개 이상의 피스를 등록해주세요.");
            return;
        }

        RecordCorrectOffsets();
        ApplyStartPositions();
    }

    private void RecordCorrectOffsets()
    {
        _correctOffsets = new Vector2[_pieces.Length];
        Vector2 refPos = _pieces[_referencePieceIndex].transform.position;

        for (int i = 0; i < _pieces.Length; i++)
            _correctOffsets[i] = (Vector2)_pieces[i].transform.position - refPos;
    }

    private void ApplyStartPositions()
    {
        if (_startPositions == null || _startPositions.Length == 0) return;

        for (int i = 0; i < _pieces.Length && i < _startPositions.Length; i++)
            _pieces[i].transform.position = new Vector3(_startPositions[i].x, _startPositions[i].y, 0f);
    }

    private void Update()
    {
        if (_success || _correctOffsets == null) return;

        float totalError = CalculateTotalError();

        if (totalError <= _maxTotalError)
        {
            _success = true;
            _anchorPositionAtCompletion = _resultAnchorPieceIndex < _pieces.Length
                ? _pieces[_resultAnchorPieceIndex].transform.position
                : Vector3.zero;
            StartCoroutine(PlayCompletionSequence());
        }
    }

    private float CalculateTotalError()
    {
        Vector2 refPos = _pieces[_referencePieceIndex].transform.position;
        float totalError = 0f;

        for (int i = 0; i < _pieces.Length; i++)
        {
            if (i == _referencePieceIndex) continue;
            Vector2 idealPos = refPos + _correctOffsets[i];
            totalError += Vector2.Distance(_pieces[i].transform.position, idealPos);
        }

        return totalError;
    }

    private IEnumerator PlayCompletionSequence()
    {
        yield return SnapPiecesToCorrectPositions();

        ShowResultImagesBehindPieces();

        yield return new WaitForSeconds(_afterFadeDelay);

        yield return FadeOutPieces();

        yield return new WaitForSeconds(_beforeGroupMoveDelay);

        yield return MoveGroupToTarget();

        yield return new WaitForSeconds(_resultImage2FallDelay);

        yield return MoveResultImage2ToTarget();
    }

    private IEnumerator SnapPiecesToCorrectPositions()
    {
        Vector2 refPos = _pieces[_referencePieceIndex].transform.position;
        var startPositions = new Vector3[_pieces.Length];

        for (int i = 0; i < _pieces.Length; i++)
            startPositions[i] = _pieces[i].transform.position;

        float elapsed = 0f;

        while (elapsed < _snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _snapDuration);

            for (int i = 0; i < _pieces.Length; i++)
            {
                if (i == _referencePieceIndex) continue;
                Vector3 target = (Vector3)(refPos + _correctOffsets[i]);
                _pieces[i].transform.position = Vector3.Lerp(startPositions[i], target, t);
            }

            yield return null;
        }

        for (int i = 0; i < _pieces.Length; i++)
        {
            if (i == _referencePieceIndex) continue;
            _pieces[i].transform.position = (Vector3)(refPos + _correctOffsets[i]);
        }
    }

    private IEnumerator FadeOutPieces()
    {
        var pieceSrs = new SpriteRenderer[_pieces.Length];
        for (int i = 0; i < _pieces.Length; i++)
            pieceSrs[i] = _pieces[i].GetComponent<SpriteRenderer>();

        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;

            foreach (var sr in pieceSrs)
                SetAlpha(sr, 1f - t);

            yield return null;
        }

        foreach (var sr in pieceSrs)
            SetAlpha(sr, 0f);
    }

    private void ShowResultImagesBehindPieces()
    {
        Vector3 anchorPos = _anchorPositionAtCompletion + _anchorOffset;

        var sr1 = Activate(_resultImage1Obj);
        if (_resultImage1Obj != null)
        {
            _resultImage1Obj.transform.position = anchorPos;
            if (sr1 != null) sr1.sortingOrder = -2;
        }
        SetAlpha(sr1, 1f);

        var sr2 = Activate(_resultImage2Obj);
        if (_resultImage2Obj != null)
        {
            _resultImage2Obj.transform.position = anchorPos + _resultImage2Offset;
            if (sr2 != null) sr2.sortingOrder = -1;
        }
        SetAlpha(sr2, 1f);
    }

    private IEnumerator MoveGroupToTarget()
    {
        if (_resultImage1Obj == null && _resultImage2Obj == null) yield break;

        Vector3 start1 = _resultImage1Obj != null ? _resultImage1Obj.transform.position : Vector3.zero;
        Vector3 start2 = _resultImage2Obj != null ? _resultImage2Obj.transform.position : Vector3.zero;
        Vector3 offset = start2 - start1;

        float elapsed = 0f;

        while (elapsed < _groupMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _groupMoveDuration);

            if (_resultImage1Obj != null)
                _resultImage1Obj.transform.position = Vector3.Lerp(start1, _groupMoveTargetPosition, t);
            if (_resultImage2Obj != null)
                _resultImage2Obj.transform.position = Vector3.Lerp(start2, _groupMoveTargetPosition + offset, t);

            yield return null;
        }

        if (_resultImage1Obj != null)
            _resultImage1Obj.transform.position = _groupMoveTargetPosition;
        if (_resultImage2Obj != null)
            _resultImage2Obj.transform.position = _groupMoveTargetPosition + offset;
    }

    private IEnumerator MoveResultImage2ToTarget()
    {
        if (_resultImage2Obj == null) yield break;

        var tf = _resultImage2Obj.transform;
        tf.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);
        Quaternion targetRot = Quaternion.Euler(_resultImage2FallTargetRotation);

        float elapsed = 0f;

        while (elapsed < _resultImage2FallDuration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.Pow(elapsed / _resultImage2FallDuration, 3f);

            tf.SetPositionAndRotation(
                Vector3.Lerp(startPos, _resultImage2FallTargetPosition, eased),
                Quaternion.Lerp(startRot, targetRot, eased)
            );

            yield return null;
        }

        tf.SetPositionAndRotation(_resultImage2FallTargetPosition, targetRot);
    }

    private SpriteRenderer Activate(GameObject obj)
    {
        if (obj == null) return null;
        obj.SetActive(true);
        return obj.GetComponent<SpriteRenderer>();
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        if (_pieces == null || _pieces.Length == 0) return;
        if (_referencePieceIndex >= _pieces.Length) return;

        Vector2 refPos = _pieces[_referencePieceIndex].transform.position;

        for (int i = 0; i < _pieces.Length; i++)
        {
            if (i == _referencePieceIndex) continue;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(refPos, _pieces[i].transform.position);
        }
    }
}
