using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryProgressManager : MonoBehaviour
{
    public static StoryProgressManager Instance { get; private set; }

    // 게임 진행 상태 정의
    public enum GameStage { Baduk, CardMatch, GoStop, Golf, FreePlay }
    public GameStage CurrentStage { get; private set; } = GameStage.Baduk;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 방지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 다음 게임 스테이지로 이동
    public void AdvanceStage()
    {
        if (CurrentStage < GameStage.FreePlay)
        {
            CurrentStage++;
            Debug.Log($"[System] 스토리 단계 업데이트: {CurrentStage}");
        }
    }
}
