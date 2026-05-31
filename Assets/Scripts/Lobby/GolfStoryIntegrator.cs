using System.Collections;
using UnityEngine;
using SilverCare.Common;

public class GolfStoryIntegrator : MonoBehaviour
{
    private GameObject _resultPanel;

    private IEnumerator Start()
    {
        // 1단계: GolfUIManager가 UI 캔버스("GolfUI_Canvas")를 생성할 때까지 백그라운드 대기
        while (_resultPanel == null)
        {
            GameObject canvas = GameObject.Find("GolfUI_Canvas");
            if (canvas != null)
            {
                Transform resultT = canvas.transform.Find("ResultPanel");
                if (resultT != null) _resultPanel = resultT.gameObject;
            }
            yield return new WaitForSeconds(0.5f);
        }

        // 2단계: 결과창(ResultPanel)이 화면에 활성화되는 순간(게임 클리어)을 모니터링
        while (true)
        {
            if (_resultPanel.activeInHierarchy)
            {
                // 클리어 감지 성공 시 스토리 상태 업데이트 및 엔딩 TTS 출력
                if (StoryProgressManager.Instance != null &&
                    StoryProgressManager.Instance.CurrentStage == StoryProgressManager.GameStage.Golf)
                {
                    StoryProgressManager.Instance.AdvanceStage(); // 골프 -> 자유 모드로 최종 진척
                    TTSManager.Instance?.Speak("홀인! 멋진 스윙이었습니다. 오늘의 모든 일정을 완벽히 마쳤습니다! 이제부터는 원하시는 게임을 언제든지 자유롭게 선택하여 즐기실 수 있습니다.");
                }
                break; // 목적을 달성했으므로 감시 루프 종료
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}