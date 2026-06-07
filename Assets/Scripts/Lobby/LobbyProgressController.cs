using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyProgressController : MonoBehaviour
{
    [SerializeField] private bool restrictByStoryProgress = false;

    private IEnumerator Start()
    {
        // Wait for VRLobby to finish creating runtime buttons.
        yield return new WaitForSeconds(0.5f);
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        Button btnBaduk = FindButtonByName("바둑 사활");
        Button btnBadukReplay = FindButtonByName("바둑 복기");
        Button btnBadukPrediction = FindButtonByName("수 예측하기");
        Button btnCardMatch = FindButtonByName("카드 맞추기");
        Button btnGoStop = FindButtonByName("고스톱");
        Button btnGolf = FindButtonByName("골프");

        if (!restrictByStoryProgress || StoryProgressManager.Instance == null)
        {
            SetAllButtons(btnBaduk, btnBadukReplay, btnBadukPrediction, btnCardMatch, btnGoStop, btnGolf);
            return;
        }

        var currentStage = StoryProgressManager.Instance.CurrentStage;
        if (currentStage == StoryProgressManager.GameStage.FreePlay)
        {
            SetAllButtons(btnBaduk, btnBadukReplay, btnBadukPrediction, btnCardMatch, btnGoStop, btnGolf);
            return;
        }

        bool badukStage = currentStage == StoryProgressManager.GameStage.Baduk;
        SetButtonState(btnBaduk, badukStage);
        SetButtonState(btnBadukReplay, badukStage);
        SetButtonState(btnBadukPrediction, badukStage);
        SetButtonState(btnCardMatch, currentStage == StoryProgressManager.GameStage.CardMatch);
        SetButtonState(btnGoStop, currentStage == StoryProgressManager.GameStage.GoStop);
        SetButtonState(btnGolf, currentStage == StoryProgressManager.GameStage.Golf);
    }

    private static void SetAllButtons(params Button[] buttons)
    {
        foreach (var button in buttons)
            SetButtonState(button, true);
    }

    private static Button FindButtonByName(string buttonName)
    {
        GameObject go = GameObject.Find(buttonName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static void SetButtonState(Button button, bool interactable)
    {
        if (button == null)
            return;

        button.interactable = interactable;

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        Color color = image.color;
        color.a = interactable ? 1f : 0.3f;
        image.color = color;
    }
}
