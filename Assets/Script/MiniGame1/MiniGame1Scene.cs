using System.Collections;
using UnityEngine;

public class MiniGame1Scene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame1PlayerController _player;

    [Header("Spawn")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Intro Settings")]
    [SerializeField] private float _introStartOffsetX = -3f;
    [SerializeField] private float _introDuration = 1f;

    [Header("Death Settings")]
    [SerializeField] private float _deathY = -5f;
    [SerializeField] private float _deathRespawnDelay = 0.5f;

    private bool _isDead;

    private void Start()
    {
        StartCoroutine(PlayIntro());
        if(CutSceneManager.Instance != null) 
            CutSceneManager.Instance.ShowUIOnly("Guide1");
    }

    private void Update()
    {
        if (!_isDead && _player.transform.position.y < _deathY)
            StartCoroutine(OnPlayerDead());
    }

    private IEnumerator OnPlayerDead()
    {
        _isDead = true;
        _player.SetInputEnabled(false);
        _player.SetAutoMove(0f);

        yield return new WaitForSeconds(_deathRespawnDelay);

        yield return PlayIntro();

        _isDead = false;
    }

    private IEnumerator PlayIntro()
    {
        Vector3 spawnPos = _spawnPoint.position;
        Vector3 startPos = spawnPos + Vector3.right * _introStartOffsetX;

        _player.Teleport(startPos);
        _player.SetInputEnabled(false);
        _player.SetAutoMove(1f);

        yield return new WaitForSeconds(_introDuration);

        _player.SetAutoMove(0f);
        _player.SetInputEnabled(true);
    }
}
