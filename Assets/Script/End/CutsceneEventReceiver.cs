using UnityEngine;

public class CutsceneEventReceiver : MonoBehaviour
{
    public void ShowDialogue1()
    {
        Debug.Log("대사 1 출력");

        // 예시
        // DialogueManager.Instance.Show("Intro_01");
    }

    public void PlayDoorSound()
    {
        Debug.Log("문 소리 재생");

        // 예시
        // SoundManager.Instance.PlaySFX("DoorOpen");
    }

    public void FadeOut()
    {
        Debug.Log("페이드 아웃");

        // 예시
        // FadeManager.Instance.FadeOut();
    }

    public void SpawnEnemy()
    {
        Debug.Log("적 등장");

        // 예시
        // enemy.SetActive(true);
    }
}