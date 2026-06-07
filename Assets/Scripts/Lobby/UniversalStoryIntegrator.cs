using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SilverCare.Common;

public class UniversalStoryIntegrator : MonoBehaviour
{
    [Header("추적 대상 UI 설정")]
    [SerializeField] private string targetUIName;
    [SerializeField] private StoryProgressManager.GameStage currentStage;

    [Header("클리어 연출")]
    [SerializeField] private bool playClearSound = true;

    private IEnumerator Start()
    {
        while (true)
        {
            GameObject targetUI = GameObject.Find(targetUIName);
            if (targetUI != null && targetUI.activeInHierarchy)
            {
                HandleStageCleared();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void HandleStageCleared()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool firstClear = StoryProgressManager.Instance != null &&
                          StoryProgressManager.Instance.TryMarkSceneCleared(sceneName);

        if (playClearSound && firstClear)
            AudioManager.Instance?.PlayGameClear();

        if (firstClear)
            StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.GetActivityForScene(sceneName).Value);

        Debug.Log($"[Story] Integrated clear detected in {sceneName}. Legacy stage hint: {currentStage}");
    }
}
