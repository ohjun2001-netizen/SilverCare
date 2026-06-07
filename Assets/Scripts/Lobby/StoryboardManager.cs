using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class StoryboardManager : MonoBehaviour
{
    [Header("스토리 안내 (씬 진입 시 자동 출력)")]
    [TextArea(3, 5)]
    [SerializeField] private string introMessage;
    [SerializeField] private float displayDuration = 6.0f;

    private TextMeshProUGUI _storyText;
    private CanvasGroup _canvasGroup;

    private IEnumerator Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _storyText = GetComponentInChildren<TextMeshProUGUI>();

        if (_storyText == null)
        {
            Debug.LogError("[Story] StoryboardManager could not find TextMeshProUGUI.");
            yield break;
        }

        while (Camera.main == null)
            yield return new WaitForSeconds(0.1f);

        yield return null;
        yield return new WaitForEndOfFrame();

        string resolvedMessage = ResolveIntroMessage();
        _storyText.text = resolvedMessage;

        if (StoryProgressManager.Instance != null)
            StoryProgressManager.Instance.ShowIntroNarration(resolvedMessage, displayDuration);

        gameObject.SetActive(false);
    }

    private string ResolveIntroMessage()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (StoryProgressManager.Instance != null)
        {
            string generated = StoryProgressManager.Instance.GetIntroMessageForScene(sceneName);
            if (!string.IsNullOrWhiteSpace(generated))
                return generated;
        }

        if (!LooksLikePlaceholder(introMessage))
            return introMessage;

        return introMessage;
    }

    private static bool LooksLikePlaceholder(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return true;

        string trimmed = message.Trim().ToLowerInvariant();
        return trimmed.StartsWith("test");
    }
}
