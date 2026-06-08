using System.Collections;
using System.Collections.Generic;
using SilverCare.Common;
using UnityEngine;
using UnityEngine.UI;

public class StoryProgressManager : MonoBehaviour
{
    public enum GameStage
    {
        Baduk,
        CardMatch,
        GoStop,
        Golf,
        FreePlay
    }

    public enum StoryActivity
    {
        BadukProblem,
        BadukReplay,
        BadukPrediction,
        CardMatch,
        GoStop,
        Golf
    }

    const string PlayerPrefsPrefix = "StoryActivityCleared_";
    const string OverlayRootName = "StoryNoticeOverlay";

    static readonly StoryActivity[] ActivityOrder =
    {
        StoryActivity.BadukProblem,
        StoryActivity.BadukReplay,
        StoryActivity.BadukPrediction,
        StoryActivity.CardMatch,
        StoryActivity.GoStop,
        StoryActivity.Golf
    };

    public static StoryProgressManager Instance { get; private set; }

    readonly HashSet<StoryActivity> _clearedActivities = new HashSet<StoryActivity>();

    Canvas _overlayCanvas;
    CanvasGroup _overlayCanvasGroup;
    Text _overlayTitle;
    Text _overlayBody;
    Coroutine _overlayRoutine;

    public GameStage CurrentStage { get; private set; } = GameStage.Baduk;
    public int ClearedActivityCount => _clearedActivities.Count;
    public int TotalActivityCount => ActivityOrder.Length;
    public bool HasCompletedAllActivities => ClearedActivityCount >= TotalActivityCount;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
            RefreshStage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AdvanceStage()
    {
        RefreshStage();
        Debug.Log($"[Story] Current stage refreshed: {CurrentStage}");
    }

    public bool HasClearedActivity(StoryActivity activity)
    {
        return _clearedActivities.Contains(activity);
    }

    public bool TryMarkActivityCleared(StoryActivity activity)
    {
        if (_clearedActivities.Contains(activity))
            return false;

        _clearedActivities.Add(activity);
        SaveProgress(activity);
        RefreshStage();
        Debug.Log($"[Story] First clear recorded: {activity} ({ClearedActivityCount}/{TotalActivityCount})");
        return true;
    }

    public bool TryMarkSceneCleared(string sceneName)
    {
        StoryActivity? activity = GetActivityForScene(sceneName);
        return activity.HasValue && TryMarkActivityCleared(activity.Value);
    }

    public bool TryHandleBaseGameClear(string gameTitle, int score)
    {
        StoryActivity? activity = GetActivityForGameTitle(gameTitle);
        if (!activity.HasValue)
            return false;

        bool firstClear = TryMarkActivityCleared(activity.Value);
        if (firstClear)
            SpeakClearNarration(activity.Value);

        return firstClear;
    }

    public void ShowIntroNarration(string message, string clipKey = null, float duration = 6f)
    {
        ShowStoryPanel("오늘의 활동 안내", message, duration);
        if (!string.IsNullOrWhiteSpace(clipKey))
            TTSManager.Instance?.SpeakClip(clipKey, interruptCurrent: true);
    }

    public void SpeakClearNarration(StoryActivity activity)
    {
        string message = GetClearNarration(activity);
        ShowStoryPanel($"{GetActivityDisplayName(activity)} 완료", message, 6.5f);
        string key = HasCompletedAllActivities ? "clear_all" : $"clear_{activity.ToString().ToLower()}";
        TTSManager.Instance?.SpeakClip(key, interruptCurrent: true);
    }

    public void ShowStoryPanel(string title, string message, float duration = 6f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_overlayRoutine != null)
            StopCoroutine(_overlayRoutine);

        _overlayRoutine = StartCoroutine(ShowOverlayRoutine(
            string.IsNullOrWhiteSpace(title) ? "안내" : title,
            message,
            duration));
    }

    public string GetClearNarration(StoryActivity activity)
    {
        string activityName = GetActivityDisplayName(activity);

        if (HasCompletedAllActivities)
        {
            return $"{activityName} 활동까지 잘 마치셨습니다. 오늘 준비한 여섯 가지 활동을 모두 둘러보시며, 바둑판과 놀이, 가벼운 운동까지 추억처럼 천천히 이어오셨어요. 이제부터는 마음에 드는 게임을 자유롭게 고르시고, 쉬는 시간마다 그때그때 끌리는 활동을 편하게 즐기시면 됩니다.";
        }

        StoryActivity? next = GetNextUnclearedActivity();
        string nextName = next.HasValue ? GetActivityDisplayName(next.Value) : "다음 활동";
        string progressLine = $"오늘 활동 {TotalActivityCount}개 중 {ClearedActivityCount}개를 완료하셨어요. 잠시 숨을 고르고, 다음에는 {nextName} 활동으로 천천히 이어가 보겠습니다.";

        switch (activity)
        {
            case StoryActivity.BadukProblem:
                return $"첫 사활 문제를 차분하게 해결하셨습니다. 마주 앉아 바둑판을 들여다보던 옛 감각처럼, 돌의 자리를 살피는 집중력이 아주 좋았습니다. {progressLine}";
            case StoryActivity.BadukReplay:
                return $"복기 활동을 끝까지 따라오셨습니다. 예전에 누군가와 한 수 한 수 되짚어 보던 대화처럼, 수순의 흐름을 읽는 감각이 자연스럽게 살아나고 있습니다. {progressLine}";
            case StoryActivity.BadukPrediction:
                return $"다음 수를 예측하는 활동을 잘 마치셨습니다. 잠시 멈춰 생각을 한 번 더 펼쳐보는 힘이 차분하게 이어지고 있습니다. {progressLine}";
            case StoryActivity.CardMatch:
                return $"카드 맞추기 활동을 즐겁게 마치셨습니다. 그림을 보고 기억을 맞춰 가는 과정이 오래된 물건이나 풍경을 떠올리는 시간처럼 편안하게 이어졌습니다. {progressLine}";
            case StoryActivity.GoStop:
                return $"고스톱 한 판을 편안하게 마치셨습니다. 익숙한 화투 그림을 마주하며 명절이나 모임의 분위기를 떠올리듯, 손끝 감각과 판단력이 자연스럽게 살아났습니다. {progressLine}";
            case StoryActivity.Golf:
                return $"골프 활동을 시원하게 마치셨습니다. 바람 좋은 날 가볍게 몸을 푸는 산책처럼, 리듬과 타이밍을 편안하게 잘 살려 주셨습니다. {progressLine}";
            default:
                return $"{activityName} 활동을 잘 마치셨습니다. {progressLine}";
        }
    }

    public string GetIntroMessageForScene(string sceneName)
    {
        StoryActivity? activity = GetActivityForScene(sceneName);
        if (!activity.HasValue)
        {
            return "오늘도 편안한 마음으로 하루 활동을 시작해 보겠습니다. 서두르지 말고, 눈앞에 보이는 놀이와 운동을 하나씩 천천히 즐기시며 익숙한 기억을 떠올려 보세요.";
        }

        switch (activity.Value)
        {
            case StoryActivity.BadukProblem:
                return "오늘의 첫 활동은 바둑 사활 문제입니다. 조용히 바둑판을 마주 보고 한 수를 고민하던 시간처럼, 돌이 놓일 자리를 천천히 살펴보세요. 한 문제만 맞혀도 오늘의 활동이 기분 좋게 시작됩니다.";
            case StoryActivity.BadukReplay:
                return "이번에는 바둑 복기 활동입니다. 이미 놓인 수를 따라가며 흐름을 읽어 보면, 누군가와 대국을 이야기하던 기억처럼 차분한 집중이 살아납니다.";
            case StoryActivity.BadukPrediction:
                return "이제 수 예측하기 활동입니다. 잠시 멈춘 장면을 보고 다음 수를 가볍게 상상해 보세요. 정답보다도, 한 번 더 생각을 펼쳐보는 과정 자체가 좋은 훈련이 됩니다.";
            case StoryActivity.CardMatch:
                return "이번 활동은 카드 맞추기입니다. 같은 그림을 찾아보며 익숙한 사물과 풍경을 떠올리고, 기억을 하나씩 연결하는 시간을 가져보세요.";
            case StoryActivity.GoStop:
                return "이제 고스톱 활동입니다. 익숙한 화투 그림을 보며 모임 자리의 정겨운 분위기를 떠올려 보세요. 승부보다 재미와 감각을 살리는 시간이 되면 충분합니다.";
            case StoryActivity.Golf:
                return "마지막은 골프 활동입니다. 어깨와 손목의 힘을 살짝 풀고, 바람을 가르듯 가볍게 스윙해 보세요. 너무 세지 않아도 괜찮고, 시원한 타격감을 느끼는 것만으로도 충분합니다.";
            default:
                return "오늘도 편안한 마음으로 하루 활동을 시작해 보겠습니다. 서두르지 말고, 눈앞에 보이는 놀이와 운동을 하나씩 천천히 즐기시며 익숙한 기억을 떠올려 보세요.";
        }
    }

    public static string GetActivityDisplayName(StoryActivity activity)
    {
        switch (activity)
        {
            case StoryActivity.BadukProblem:
                return "바둑 사활";
            case StoryActivity.BadukReplay:
                return "바둑 복기";
            case StoryActivity.BadukPrediction:
                return "수 예측하기";
            case StoryActivity.CardMatch:
                return "카드 맞추기";
            case StoryActivity.GoStop:
                return "고스톱";
            case StoryActivity.Golf:
                return "골프";
            default:
                return "활동";
        }
    }

    public static StoryActivity? GetActivityForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Baduk":
            case "BadukVR":
                return StoryActivity.BadukProblem;
            case "BadukReplay":
                return StoryActivity.BadukReplay;
            case "BadukPrediction":
                return StoryActivity.BadukPrediction;
            case "CardMatch":
                return StoryActivity.CardMatch;
            case "GoStop":
                return StoryActivity.GoStop;
            case "Golf":
                return StoryActivity.Golf;
            default:
                return null;
        }
    }

    static StoryActivity? GetActivityForGameTitle(string gameTitle)
    {
        switch (gameTitle)
        {
            case "카드 짝맞추기":
                return StoryActivity.CardMatch;
            case "고스톱":
                return StoryActivity.GoStop;
            case "Golf":
            case "골프":
                return StoryActivity.Golf;
            default:
                return null;
        }
    }

    void LoadProgress()
    {
        _clearedActivities.Clear();
        foreach (StoryActivity activity in ActivityOrder)
        {
            if (PlayerPrefs.GetInt(GetPrefsKey(activity), 0) == 1)
                _clearedActivities.Add(activity);
        }
    }

    void SaveProgress(StoryActivity activity)
    {
        PlayerPrefs.SetInt(GetPrefsKey(activity), 1);
        PlayerPrefs.Save();
    }

    void RefreshStage()
    {
        if (HasCompletedAllActivities)
        {
            CurrentStage = GameStage.FreePlay;
            return;
        }

        if (!HasClearedActivity(StoryActivity.BadukProblem) ||
            !HasClearedActivity(StoryActivity.BadukReplay) ||
            !HasClearedActivity(StoryActivity.BadukPrediction))
        {
            CurrentStage = GameStage.Baduk;
            return;
        }

        if (!HasClearedActivity(StoryActivity.CardMatch))
        {
            CurrentStage = GameStage.CardMatch;
            return;
        }

        if (!HasClearedActivity(StoryActivity.GoStop))
        {
            CurrentStage = GameStage.GoStop;
            return;
        }

        CurrentStage = HasClearedActivity(StoryActivity.Golf) ? GameStage.FreePlay : GameStage.Golf;
    }

    StoryActivity? GetNextUnclearedActivity()
    {
        foreach (StoryActivity activity in ActivityOrder)
        {
            if (!_clearedActivities.Contains(activity))
                return activity;
        }

        return null;
    }

    void EnsureOverlay()
    {
        if (_overlayCanvas != null)
            return;

        var root = new GameObject(OverlayRootName);
        DontDestroyOnLoad(root);

        _overlayCanvas = root.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.WorldSpace;
        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = 500;
        XRUIUtility.ConfigureWorldCanvas(root, _overlayCanvas);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(760f, 260f);
        rootRect.localScale = Vector3.one * 0.00155f;

        _overlayCanvasGroup = root.AddComponent<CanvasGroup>();

        var shadow = CreateUIObject("Shadow", root.transform);
        var shadowRect = shadow.GetComponent<RectTransform>();
        shadowRect.anchoredPosition = new Vector2(0f, -8f);
        shadowRect.sizeDelta = new Vector2(620f, 190f);
        var shadowImage = shadow.AddComponent<Image>();
        shadowImage.color = new Color(0.12f, 0.16f, 0.18f, 0.18f);

        var bg = CreateUIObject("Panel", root.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(620f, 190f);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.97f, 0.95f, 0.90f, 0.88f);

        var accent = CreateUIObject("Accent", bg.transform);
        var accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchoredPosition = new Vector2(0f, 66f);
        accentRect.sizeDelta = new Vector2(500f, 3f);
        var accentImage = accent.AddComponent<Image>();
        accentImage.color = new Color(0.18f, 0.47f, 0.42f, 0.95f);

        _overlayTitle = CreateText(
            bg.transform,
            "Title",
            24,
            FontStyle.Bold,
            new Vector2(0f, 42f),
            new Vector2(520f, 34f),
            new Color(0.12f, 0.22f, 0.24f));
        _overlayBody = CreateText(
            bg.transform,
            "Body",
            18,
            FontStyle.Normal,
            new Vector2(0f, -18f),
            new Vector2(520f, 92f),
            new Color(0.23f, 0.25f, 0.25f));

        _overlayCanvas.gameObject.SetActive(false);
    }

    void RefreshOverlayTransform(float extraYOffset = 0f)
    {
        if (_overlayCanvas == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform canvasTransform = _overlayCanvas.transform;
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = cam.transform.forward;
        forward.Normalize();

        Vector3 targetPosition = cam.transform.position
                                 + forward * 1.9f
                                 + cam.transform.up * (0.62f + extraYOffset);
        canvasTransform.position = targetPosition;
        canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - cam.transform.position, Vector3.up);
    }

    IEnumerator HideOverlayAfterDelay(float duration)
    {
        float visibleDuration = Mathf.Min(duration, 5f);
        float elapsed = 0f;
        while (elapsed < visibleDuration)
        {
            elapsed += Time.deltaTime;
            float settle = Mathf.Clamp01(elapsed / 0.25f);
            RefreshOverlayTransform(Mathf.Lerp(0.03f, 0f, settle));
            yield return null;
        }

        const float fadeDuration = 1f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            _overlayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            RefreshOverlayTransform(Mathf.Lerp(0f, 0.02f, fadeElapsed / fadeDuration));
            yield return null;
        }

        _overlayCanvasGroup.alpha = 1f;
        _overlayCanvas.gameObject.SetActive(false);
        _overlayRoutine = null;
    }

    IEnumerator ShowOverlayRoutine(string title, string message, float duration)
    {
        EnsureOverlay();

        yield return null;
        yield return new WaitForEndOfFrame();

        _overlayTitle.text = title;
        _overlayBody.text = message;
        _overlayCanvasGroup.alpha = 0f;
        _overlayCanvas.gameObject.SetActive(true);

        const float introDuration = 0.25f;
        float introElapsed = 0f;
        while (introElapsed < introDuration)
        {
            introElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(introElapsed / introDuration);
            _overlayCanvasGroup.alpha = t;
            RefreshOverlayTransform(Mathf.Lerp(0.05f, 0f, t));
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        _overlayCanvasGroup.alpha = 1f;
        RefreshOverlayTransform();

        yield return HideOverlayAfterDelay(duration);
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = CreateUIObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = go.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static string GetPrefsKey(StoryActivity activity)
    {
        return PlayerPrefsPrefix + activity;
    }
}
