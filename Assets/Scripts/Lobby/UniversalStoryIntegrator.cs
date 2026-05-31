using System.Collections;
using UnityEngine;
using SilverCare.Common;

public class UniversalStoryIntegrator : MonoBehaviour
{
    [Header("추적 대상 UI 설정")]
    [Tooltip("감시할 결과창의 정확한 이름 (예: 다시 풀기, Clear)")]
    [SerializeField] private string targetUIName;
    [SerializeField] private StoryProgressManager.GameStage currentStage;
    
    [Header("클리어 연출 (Sound & TTS)")]
    [TextArea(2, 4)]
    [Tooltip("클리어 시 출력할 엔딩 대사")]
    [SerializeField] private string clearTTSMessage;
    
    [Tooltip("클리어 시 팡파르 효과음 재생 여부")]
    [SerializeField] private bool playClearSound = true;

    private IEnumerator Start()
    {
        // 결과창 UI가 생성/활성화될 때까지 백그라운드 추적
        while (true)
        {
            GameObject targetUI = GameObject.Find(targetUIName);
            
            if (targetUI != null && targetUI.activeInHierarchy)
            {
                OnStageCleared();
                break; 
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnStageCleared()
    {
        if (StoryProgressManager.Instance != null && 
            StoryProgressManager.Instance.CurrentStage == currentStage)
        {
            // 1. 스토리 다음 단계로 해금
            StoryProgressManager.Instance.AdvanceStage();
            
            // 2. 효과음 출력 (팀 공통 Audio 시스템 활용)
            if (playClearSound)
            {
                AudioManager.Instance?.PlayGameClear();
            }

            // 3. 스토리 대사 TTS 출력
            if (!string.IsNullOrEmpty(clearTTSMessage))
            {
                TTSManager.Instance?.Speak(clearTTSMessage);
            }
            
            Debug.Log($"[System] {currentStage} 클리어 완수. Sound 및 TTS 동기화 완료.");
        }
    }
}