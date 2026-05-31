using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyProgressController : MonoBehaviour
{
    private IEnumerator Start()
    {
        // VRLobby.cs가 UI 캔버스와 버튼을 동적 생성할 시간을 확보 (0.5초 비동기 대기)
        yield return new WaitForSeconds(0.5f);
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (StoryProgressManager.Instance == null) return;

        var currentStage = StoryProgressManager.Instance.CurrentStage;

        // VRLobby가 생성한 버튼 객체들을 이름(Name)으로 런타임 탐색하여 가로채기
        Button btnBaduk = FindButtonByName("바둑 사활");
        Button btnBadukReplay = FindButtonByName("바둑 복기");     // 스토리 비활성 콘텐츠
        Button btnBadukPred = FindButtonByName("수 예측하기");   // 스토리 비활성 콘텐츠
        Button btnCard = FindButtonByName("카드 맞추기");
        Button btnGoStop = FindButtonByName("고스톱");
        Button btnGolf = FindButtonByName("골프");

        // 자유 모드 개방 시: 모든 버튼 활성화
        if (currentStage == StoryProgressManager.GameStage.FreePlay)
        {
            SetButtonState(btnBaduk, true);
            SetButtonState(btnBadukReplay, true);
            SetButtonState(btnBadukPred, true);
            SetButtonState(btnCard, true);
            SetButtonState(btnGoStop, true);
            SetButtonState(btnGolf, true);
            return;
        }

        // 스토리 모드: 현재 진행 중인 스테이지 1개만 활성화하고 나머지는 전부 잠금
        SetButtonState(btnBaduk, currentStage == StoryProgressManager.GameStage.Baduk);
        SetButtonState(btnBadukReplay, false); 
        SetButtonState(btnBadukPred, false);
        SetButtonState(btnCard, currentStage == StoryProgressManager.GameStage.CardMatch);
        SetButtonState(btnGoStop, currentStage == StoryProgressManager.GameStage.GoStop);
        SetButtonState(btnGolf, currentStage == StoryProgressManager.GameStage.Golf);
    }

    private Button FindButtonByName(string btnName)
    {
        GameObject go = GameObject.Find(btnName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private void SetButtonState(Button btn, bool interactable)
    {
        if (btn == null) return;
        
        btn.interactable = interactable;

        // 시각적 비활성화 (딤 처리: 투명도를 30%로 낮춤)
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = interactable ? 1f : 0.3f; 
            img.color = c;
        }
    }
}