// Assets/Scripts/Lobby/VRLobby.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using SilverCare.Common;

public class VRLobby : MonoBehaviour
{
    const string EnvRootName = "SilverCareLobbyEnvironment";

    Canvas _canvas;
    Text _recText;
    Text _scoreText;

    static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
    static readonly Color PanelColor = new(0.96f, 0.94f, 0.88f, 0.98f);
    static readonly Color InkColor = new(0.10f, 0.13f, 0.16f);
    static readonly Color MutedColor = new(0.34f, 0.39f, 0.43f);
    static readonly Color AccentColor = new(0.08f, 0.38f, 0.42f);
    static readonly string[] StylizedTreeResourcePaths =
    {
        "StylizedNature/tree_a",
        "StylizedNature/tree_b",
        "StylizedNature/tree_c",
        "StylizedNature/tree_d",
        "StylizedNature/tree_e",
        "StylizedNature/tree_f",
        "StylizedNature/tree_g",
        "StylizedNature/tree_h",
        "StylizedNature/tree_i",
        "StylizedNature/tree_j",
        "StylizedNature/tree_k"
    };

    void Start()
    {
        ApplyVisualQuality();
        EnsureEventSystem();
        BuildUI();
        PlaceCanvas();
        BuildLobbyEnvironment();
        StartCoroutine(UpdateInfoTexts());
    }

    static void ApplyVisualQuality()
    {
        QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 18f);
    }

    static void EnsureEventSystem()
    {
        XRUIUtility.EnsureEventSystem();
    }

    void PlaceCanvas()
    {
        var cam = Camera.main;
        if (cam == null || _canvas == null) return;

        _canvas.worldCamera = cam;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        var rt = _canvas.GetComponent<RectTransform>();
        Vector3 pos = cam.transform.position + forward * 2.15f;
        pos.y = 1.48f;
        rt.position = pos;
        rt.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void BuildLobbyEnvironment()
    {
        var old = GameObject.Find(EnvRootName);
        if (old != null) Destroy(old);

        var cam = Camera.main;
        if (cam == null || _canvas == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.67f, 0.84f, 0.94f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.74f, 0.84f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.56f, 0.64f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.34f, 0.31f, 0.24f);
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.54f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.72f, 0.86f, 0.90f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 9.5f;
        RenderSettings.fogEndDistance = 26.0f;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        var root = new GameObject(EnvRootName);
        Vector3 center = _canvas.transform.position;
        Vector3 ground = center - Vector3.up * 0.9875f;
        Quaternion wallRot = Quaternion.LookRotation(forward, Vector3.up);

        var floor = Prim(root, "ParkGrass", PrimitiveType.Cube,
            ground - Vector3.up * 0.0125f + forward * 0.45f,
            new Vector3(18.5f, 0.025f, 14.0f),
            new Color(0.42f, 0.62f, 0.34f));
        floor.transform.rotation = wallRot;

        SpawnSkyBackdrop(root, ground + forward * 7.1f, right, wallRot);
        SpawnPavilion(root, ground + forward * 0.44f, wallRot);
        SpawnStonePath(root, ground + Vector3.up * 0.014f - forward * 0.28f, wallRot);
        SpawnGardenBed(root, ground + Vector3.up * 0.025f - right * 3.15f + forward * 0.12f, wallRot);
        SpawnGardenBed(root, ground + Vector3.up * 0.025f + right * 3.15f + forward * 0.12f, wallRot);
        SpawnTree(root, ground - right * 4.25f + forward * 1.65f);
        SpawnTree(root, ground + right * 4.25f + forward * 1.50f);
        SpawnTree(root, ground - right * 5.10f - forward * 2.10f);
        SpawnTree(root, ground + right * 5.10f - forward * 2.15f);
        SpawnTree(root, ground - right * 5.65f - forward * 0.25f);
        SpawnTree(root, ground + right * 5.65f - forward * 0.20f);
        SpawnShrubRow(root, ground - right * 3.45f - forward * 0.95f, right, forward);
        SpawnShrubRow(root, ground + right * 3.45f - forward * 0.95f, -right, forward);
        SpawnFlowerCluster(root, ground - right * 2.2f - forward * 0.40f, new Color(0.90f, 0.46f, 0.42f));
        SpawnFlowerCluster(root, ground + right * 2.2f - forward * 0.40f, new Color(0.92f, 0.72f, 0.32f));
        SpawnFlowerRibbon(root, ground - right * 1.35f - forward * 1.15f, right, new Color(0.86f, 0.54f, 0.72f));
        SpawnFlowerRibbon(root, ground + right * 1.35f - forward * 1.15f, -right, new Color(0.98f, 0.78f, 0.40f));
        SpawnPond(root, ground + Vector3.up * 0.018f + right * 2.85f + forward * 1.65f, wallRot);
        SpawnFence(root, ground - forward * 2.25f, right, wallRot);
        SpawnExtendedPark(root, ground, right, forward, wallRot);
        SpawnDirtPatches(root, ground, right, forward, wallRot);
        SpawnGrassTufts(root, ground, right, forward);
        SpawnNaturalGroundDetails(root, ground, right, forward);
        SpawnCloud(root, center - right * 2.25f + forward * 2.15f + Vector3.up * 1.55f);
        SpawnCloud(root, center + right * 2.15f + forward * 2.05f + Vector3.up * 1.72f);
        SpawnCloud(root, center - right * 5.0f + forward * 4.8f + Vector3.up * 2.05f);
        SpawnCloud(root, center + right * 5.2f + forward * 4.4f + Vector3.up * 2.18f);
        SpawnSunLight(root, center - right * 2.1f + forward * 0.75f + Vector3.up * 1.35f, forward, right);
    }

    IEnumerator UpdateInfoTexts()
    {
        yield return null;

        var profile = PlayerDataManager.Instance?.Profile;

        if (_recText != null)
        {
            _recText.text = profile != null && profile.age >= 70
                ? "오늘은 기억력 활동부터 천천히 시작해보세요."
                : "원하는 활동을 골라 편하게 체험해보세요.";
        }

        if (_scoreText != null)
        {
            int high = PlayerDataManager.Instance?.GetHighScore("Baduk") ?? 0;
            _scoreText.text = high > 0 ? $"바둑 최고 점수: {high}점" : "큰 버튼을 눌러 게임을 시작하세요.";
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("LobbyCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;
        scaler.referencePixelsPerUnit = 140f;
        XRUIUtility.ConfigureWorldCanvas(canvasGO, _canvas);

        var rt = _canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(980, 680);
        rt.localScale = Vector3.one * 0.0018f;

        var bg = CreatePanel(rt, "Background", PanelColor);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        CreatePanel(bgrt, "TopAccent", AccentColor, new Vector2(0, 298), new Vector2(920, 6));

        MakeText(bgrt, "Title", "SilverCare", 46, FontStyle.Bold,
            new Vector2(0, 252), new Vector2(900, 64), AccentColor);
        MakeText(bgrt, "Subtitle", "어르신을 위한 VR 인지 · 여가 활동", 26, FontStyle.Normal,
            new Vector2(0, 205), new Vector2(900, 44), MutedColor);

        (string scene, string title, string desc, Color color)[] games =
        {
            ("BadukVR",     "바둑 사활",   "판단력과 집중력 활동", new Color(0.10f, 0.40f, 0.42f)),
            ("BadukReplay", "바둑 복기",   "수순을 따라보는 해설 활동", new Color(0.21f, 0.36f, 0.57f)),
            ("BadukPrediction", "수 예측하기", "다음 수를 맞히는 사고 활동", new Color(0.30f, 0.33f, 0.58f)),
            ("CardMatch",   "카드 맞추기", "기억력과 관찰력 활동", new Color(0.48f, 0.35f, 0.16f)),
            ("GoStop",      "고스톱",      "익숙한 화투 여가 활동", new Color(0.55f, 0.22f, 0.18f)),
            ("Golf",        "골프",        "가볍게 즐기는 신체 활동", new Color(0.25f, 0.48f, 0.22f)),
        };

        float cardW = 420f;
        float cardH = 94f;
        float gapX = 28f;
        float gapY = 14f;
        float startY = 124f;

        for (int i = 0; i < games.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;
            float x = (col == 0 ? -1f : 1f) * (cardW * 0.5f + gapX * 0.5f);
            float y = startY - row * (cardH + gapY);
            string sceneName = games[i].scene;

            MakeGameButton(bgrt, games[i].title, games[i].desc, games[i].color,
                    new Vector2(x, y), new Vector2(cardW, cardH))
                .onClick.AddListener(() => GoTo(sceneName));
        }

        CreatePanel(bgrt, "Divider", new Color(0.78f, 0.76f, 0.68f, 1f),
            new Vector2(0, -222), new Vector2(880, 2));

        _recText = MakeText(bgrt, "RecommendationText", "",
            23, FontStyle.Bold, new Vector2(0, -258), new Vector2(880, 36), AccentColor);
        _scoreText = MakeText(bgrt, "HighScoreText", "",
            20, FontStyle.Normal, new Vector2(0, -294), new Vector2(880, 32), MutedColor);
    }

    static void GoTo(string sceneName)
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    static GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreatePanel(RectTransform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var go = CreatePanel(parent, name, color);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        int fontSize, FontStyle style, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var text = go.GetComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static Button MakeGameButton(RectTransform parent, string title, string desc, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = color;

        MakeText(rt, "Title", title, 31, FontStyle.Bold,
            new Vector2(0, 20), new Vector2(size.x - 36, 42), Color.white);
        MakeText(rt, "Desc", desc, 20, FontStyle.Normal,
            new Vector2(0, -24), new Vector2(size.x - 36, 34), new Color(0.94f, 0.98f, 0.96f));

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;
        return btn;
    }

    static void SpawnSkyBackdrop(GameObject root, Vector3 horizonBase, Vector3 right, Quaternion rotation)
    {
        if (TrySpawnStylizedSky(root, horizonBase, right, rotation))
        {
            SpawnStylizedSun(root, horizonBase, right, rotation);
            return;
        }

        Vector3 r = right.normalized;

        var horizon = Prim(root, "WarmHorizonBand", PrimitiveType.Cube, horizonBase + Vector3.up * 0.22f,
            new Vector3(22.0f, 0.42f, 0.035f), new Color(0.78f, 0.86f, 0.70f));
        horizon.transform.rotation = rotation;

        var lowerSky = Prim(root, "LowerSkyGradient", PrimitiveType.Cube, horizonBase + Vector3.up * 1.05f,
            new Vector3(22.0f, 1.25f, 0.03f), new Color(0.76f, 0.88f, 0.93f));
        lowerSky.transform.rotation = rotation;

        var upperSky = Prim(root, "UpperSkyGradient", PrimitiveType.Cube, horizonBase + Vector3.up * 2.35f,
            new Vector3(22.0f, 1.55f, 0.03f), new Color(0.58f, 0.78f, 0.92f));
        upperSky.transform.rotation = rotation;

        var sun = Prim(root, "SoftMorningSun", PrimitiveType.Sphere, horizonBase - r * 5.2f + Vector3.up * 2.35f,
            new Vector3(0.44f, 0.44f, 0.04f), new Color(1.0f, 0.84f, 0.42f));
        sun.transform.rotation = rotation;
    }

    static void SpawnPavilion(GameObject root, Vector3 basePos, Quaternion rotation)
    {
        const float pavilionScale = 1.25f;

        var deck = Prim(root, "PavilionDeck", PrimitiveType.Cube, basePos + Vector3.up * 0.04f,
            new Vector3(4.9f * pavilionScale, 0.08f * pavilionScale, 2.7f * pavilionScale), new Color(0.58f, 0.38f, 0.20f));
        deck.transform.rotation = rotation;

        var roof = Prim(root, "PavilionRoof", PrimitiveType.Cube, basePos + Vector3.up * (1.62f * pavilionScale),
            new Vector3(5.35f * pavilionScale, 0.18f * pavilionScale, 3.05f * pavilionScale), new Color(0.34f, 0.18f, 0.11f));
        roof.transform.rotation = rotation;

        var roofCap = Prim(root, "PavilionRoofCap", PrimitiveType.Cube, basePos + Vector3.up * (1.78f * pavilionScale),
            new Vector3(4.55f * pavilionScale, 0.14f * pavilionScale, 2.35f * pavilionScale), new Color(0.42f, 0.23f, 0.14f));
        roofCap.transform.rotation = rotation;

        Vector3 right = rotation * Vector3.right;
        Vector3 forward = rotation * Vector3.forward;

        foreach (var offset in new[]
        {
            right * (2.18f * pavilionScale) + forward * (1.15f * pavilionScale),
            -right * (2.18f * pavilionScale) + forward * (1.15f * pavilionScale),
            right * (2.18f * pavilionScale) - forward * (1.15f * pavilionScale),
            -right * (2.18f * pavilionScale) - forward * (1.15f * pavilionScale)
        })
        {
            var post = Prim(root, "PavilionPost", PrimitiveType.Cube, basePos + offset + Vector3.up * (0.82f * pavilionScale),
                new Vector3(0.13f * pavilionScale, 1.55f * pavilionScale, 0.13f * pavilionScale), new Color(0.43f, 0.25f, 0.12f));
            post.transform.rotation = rotation;
        }
    }

    static void SpawnStonePath(GameObject root, Vector3 center, Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        for (int i = 0; i < 6; i++)
        {
            var stone = Prim(root, "SteppingStone", PrimitiveType.Cylinder,
                center - forward * (i * 0.42f),
                new Vector3(0.38f, 0.014f, 0.28f), new Color(0.70f, 0.68f, 0.61f));
            stone.transform.rotation = rotation * Quaternion.Euler(0, i * 17f, 0);
        }
    }

    static void SpawnGardenBed(GameObject root, Vector3 center, Quaternion rotation)
    {
        var bed = Prim(root, "GardenBed", PrimitiveType.Cube, center,
            new Vector3(1.55f, 0.05f, 2.8f), new Color(0.32f, 0.42f, 0.25f));
        bed.transform.rotation = rotation;
    }

    static void SpawnTree(GameObject root, Vector3 basePos)
    {
        if (TrySpawnStylizedTree(root, basePos)) return;

        var trunk = Prim(root, "TreeTrunk", PrimitiveType.Cylinder, basePos + Vector3.up * 0.55f,
            new Vector3(0.15f, 0.55f, 0.15f), new Color(0.35f, 0.21f, 0.11f));
        trunk.transform.rotation = Quaternion.Euler(0f, Mathf.PerlinNoise(basePos.x, basePos.z) * 18f, 0f);

        SpawnBranch(root, basePos + Vector3.up * 0.88f, basePos + new Vector3(0.28f, 1.12f, 0.10f), 0.035f);
        SpawnBranch(root, basePos + Vector3.up * 0.98f, basePos + new Vector3(-0.24f, 1.25f, -0.08f), 0.030f);
        SpawnBranch(root, basePos + Vector3.up * 0.76f, basePos + new Vector3(0.08f, 1.05f, -0.30f), 0.028f);
        SpawnTreeRoots(root, basePos);
        SpawnTreeCanopy(root, basePos);
    }

    static void SpawnTreeCanopy(GameObject root, Vector3 basePos)
    {
        Color[] colors =
        {
            new Color(0.18f, 0.38f, 0.19f),
            new Color(0.24f, 0.48f, 0.25f),
            new Color(0.31f, 0.57f, 0.32f),
            new Color(0.20f, 0.43f, 0.22f)
        };

        for (int i = 0; i < 11; i++)
        {
            float angle = i * 2.39996f + Mathf.PerlinNoise(basePos.x + i, basePos.z) * 0.5f;
            float radius = 0.10f + (i % 5) * 0.08f;
            float height = 1.12f + (i % 4) * 0.08f + Mathf.PerlinNoise(i, basePos.x) * 0.10f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            Vector3 scale = new Vector3(
                0.36f + (i % 3) * 0.08f,
                0.25f + (i % 4) * 0.045f,
                0.34f + (i % 5) * 0.045f);

            var leafMass = Prim(root, "TreeLeafCluster", PrimitiveType.Sphere, basePos + offset, scale, colors[i % colors.Length]);
            leafMass.transform.rotation = Quaternion.Euler(i * 13f, i * 31f, i * 7f);
        }
    }

    static void SpawnTreeRoots(GameObject root, Vector3 basePos)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f + Mathf.PerlinNoise(basePos.x + i, basePos.z) * 0.4f;
            Vector3 end = basePos + new Vector3(Mathf.Cos(angle) * 0.34f, 0.035f, Mathf.Sin(angle) * 0.34f);
            SpawnBranch(root, basePos + Vector3.up * 0.05f, end, 0.026f);
        }
    }

    static void SpawnBranch(GameObject root, Vector3 start, Vector3 end, float radius)
    {
        Vector3 mid = (start + end) * 0.5f;
        Vector3 dir = end - start;
        var branch = Prim(root, "TreeBranch", PrimitiveType.Cylinder, mid,
            new Vector3(radius, dir.magnitude * 0.5f, radius), new Color(0.31f, 0.18f, 0.09f));
        branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
    }

    static void SpawnFlowerCluster(GameObject root, Vector3 center, Color flowerColor)
    {
        for (int i = 0; i < 9; i++)
        {
            float x = (i % 3 - 1) * 0.16f;
            float z = (i / 3 - 1) * 0.14f;
            Prim(root, "FlowerStem", PrimitiveType.Cylinder, center + new Vector3(x, 0.10f, z),
                new Vector3(0.015f, 0.11f, 0.015f), new Color(0.22f, 0.42f, 0.20f));
            Prim(root, "Flower", PrimitiveType.Sphere, center + new Vector3(x, 0.23f, z),
                new Vector3(0.055f, 0.035f, 0.055f), flowerColor);
        }
    }

    static void SpawnShrubRow(GameObject root, Vector3 start, Vector3 sideDir, Vector3 forward)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = start + sideDir.normalized * (i * 0.26f) + forward * (Mathf.Sin(i) * 0.05f);
            Prim(root, "Shrub", PrimitiveType.Sphere, pos + Vector3.up * 0.10f,
                new Vector3(0.28f, 0.20f, 0.28f), new Color(0.22f, 0.46f, 0.24f));
            if (i % 2 == 0)
            {
                Prim(root, "ShrubFlower", PrimitiveType.Sphere, pos + Vector3.up * 0.22f,
                    new Vector3(0.045f, 0.035f, 0.045f), new Color(0.94f, 0.68f, 0.46f));
            }
        }
    }

    static void SpawnFlowerRibbon(GameObject root, Vector3 start, Vector3 sideDir, Color color)
    {
        Vector3 dir = sideDir.normalized;
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = start + dir * (i * 0.18f) + new Vector3(0f, 0f, Mathf.Sin(i * 0.7f) * 0.05f);
            Prim(root, "FlowerRibbonStem", PrimitiveType.Cylinder, pos + Vector3.up * 0.07f,
                new Vector3(0.012f, 0.08f, 0.012f), new Color(0.20f, 0.38f, 0.18f));
            Prim(root, "FlowerRibbonBloom", PrimitiveType.Sphere, pos + Vector3.up * 0.17f,
                new Vector3(0.045f, 0.030f, 0.045f), color);
        }
    }

    static void SpawnExtendedPark(GameObject root, Vector3 center, Vector3 right, Vector3 forward, Quaternion rotation)
    {
        Vector3 r = right.normalized;
        Vector3 f = forward.normalized;

        SpawnWalkingPath(root, center + Vector3.up * 0.014f - f * 1.0f, r, f, rotation);

        for (int i = -4; i <= 4; i++)
        {
            float x = i * 1.15f;
            float z = 3.15f + Mathf.Abs(i) * 0.18f;
            SpawnTree(root, center + r * x + f * z);
        }

        for (int i = -5; i <= 5; i += 2)
        {
            SpawnTree(root, center + r * (i * 1.05f) - f * 4.4f);
        }

        SpawnCurvedFlowerBorder(root, center - r * 5.15f + f * 0.55f, f, r, new Color(0.95f, 0.62f, 0.45f));
        SpawnCurvedFlowerBorder(root, center + r * 5.15f + f * 0.55f, f, -r, new Color(0.72f, 0.58f, 0.90f));

        SpawnRestBench(root, center - r * 4.2f + f * 2.0f, rotation);
        SpawnRestBench(root, center + r * 4.2f + f * 2.0f, rotation);
        SpawnGardenLantern(root, center - r * 4.85f + f * 1.2f);
        SpawnGardenLantern(root, center + r * 4.85f + f * 1.2f);
        SpawnGardenLantern(root, center - r * 2.35f - f * 3.5f);
        SpawnGardenLantern(root, center + r * 2.35f - f * 3.5f);

        SpawnLargePond(root, center + Vector3.up * 0.018f - r * 5.4f - f * 2.6f, rotation);
        SpawnReeds(root, center - r * 5.4f - f * 2.6f, r, f);
        SpawnBirdBath(root, center + r * 5.3f - f * 2.6f);
        SpawnDistantHills(root, center + f * 6.4f + Vector3.up * 0.18f, r, rotation);
    }

    static void SpawnReeds(GameObject root, Vector3 pondCenter, Vector3 right, Vector3 forward)
    {
        Vector3 r = right.normalized;
        Vector3 f = forward.normalized;

        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            Vector3 pos = pondCenter + r * (Mathf.Cos(angle) * 0.92f) + f * (Mathf.Sin(angle) * 0.48f);
            var reed = Prim(root, "PondReed", PrimitiveType.Cylinder,
                pos + Vector3.up * 0.16f,
                new Vector3(0.007f, 0.16f + (i % 4) * 0.025f, 0.007f), new Color(0.38f, 0.43f, 0.18f));
            reed.transform.rotation = Quaternion.Euler(10f + i % 5, i * 23f, -12f + i % 6);

            if (i % 3 == 0)
            {
                var grass = Prim(root, "PondGrassBlade", PrimitiveType.Cube,
                    pos + r * 0.025f + Vector3.up * 0.18f,
                    new Vector3(0.012f, 0.30f, 0.006f), new Color(0.30f, 0.47f, 0.20f));
                grass.transform.rotation = Quaternion.Euler(18f, i * 41f, -18f);
            }
        }
    }

    static void SpawnGrassTufts(GameObject root, Vector3 ground, Vector3 right, Vector3 forward)
    {
        Vector3 r = right.normalized;
        Vector3 f = forward.normalized;

        for (int i = 0; i < 44; i++)
        {
            float side = Mathf.Lerp(-7.8f, 7.8f, Mathf.Repeat(i * 0.381f, 1f));
            float depth = Mathf.Lerp(-5.6f, 5.7f, Mathf.Repeat(i * 0.617f, 1f));
            if (Mathf.Abs(side) < 1.2f && depth < 0.6f) continue;

            Vector3 basePos = ground + r * side + f * depth;
            Color grass = Color.Lerp(new Color(0.25f, 0.45f, 0.20f), new Color(0.47f, 0.65f, 0.31f), Mathf.Repeat(i * 0.27f, 1f));
            for (int blade = 0; blade < 3; blade++)
            {
                var tuft = Prim(root, "GrassBlade", PrimitiveType.Cube,
                    basePos + new Vector3((blade - 1) * 0.035f, 0.06f, Mathf.Sin(i + blade) * 0.025f),
                    new Vector3(0.018f, 0.12f + blade * 0.025f, 0.018f), grass);
                tuft.transform.rotation = Quaternion.Euler(0f, i * 23f + blade * 47f, 10f - blade * 8f);
            }
        }
    }

    static void SpawnNaturalGroundDetails(GameObject root, Vector3 ground, Vector3 right, Vector3 forward)
    {
        Vector3 r = right.normalized;
        Vector3 f = forward.normalized;

        for (int i = 0; i < 36; i++)
        {
            float side = Mathf.Lerp(-7.2f, 7.2f, Mathf.Repeat(i * 0.433f, 1f));
            float depth = Mathf.Lerp(-5.2f, 5.4f, Mathf.Repeat(i * 0.719f, 1f));
            Vector3 pos = ground + r * side + f * depth;

            if (i % 3 == 0)
            {
                var pebble = Prim(root, "NaturalPebble", PrimitiveType.Sphere,
                    pos + Vector3.up * 0.025f,
                    new Vector3(0.09f + (i % 4) * 0.015f, 0.025f, 0.055f + (i % 5) * 0.008f),
                    new Color(0.48f, 0.45f, 0.38f));
                pebble.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
            }
            else
            {
                var leaf = Prim(root, "FallenLeaf", PrimitiveType.Cube,
                    pos + Vector3.up * 0.018f,
                    new Vector3(0.13f, 0.006f, 0.045f),
                    i % 2 == 0 ? new Color(0.62f, 0.38f, 0.13f) : new Color(0.74f, 0.55f, 0.20f));
                leaf.transform.rotation = Quaternion.Euler(0f, i * 51f, 0f);
            }
        }
    }

    static void SpawnDirtPatches(GameObject root, Vector3 ground, Vector3 right, Vector3 forward, Quaternion rotation)
    {
        Vector3 r = right.normalized;
        Vector3 f = forward.normalized;

        for (int i = 0; i < 14; i++)
        {
            float side = Mathf.Lerp(-6.4f, 6.4f, Mathf.Repeat(i * 0.529f, 1f));
            float depth = Mathf.Lerp(-4.9f, 5.0f, Mathf.Repeat(i * 0.347f, 1f));
            if (Mathf.Abs(side) < 1.0f && depth < 0.4f) continue;

            var patch = Prim(root, "SoilPatch", PrimitiveType.Cylinder,
                ground + r * side + f * depth + Vector3.up * 0.006f,
                new Vector3(0.44f + (i % 3) * 0.12f, 0.006f, 0.25f + (i % 4) * 0.07f),
                new Color(0.31f, 0.23f, 0.15f));
            patch.transform.rotation = rotation * Quaternion.Euler(0f, i * 29f, 0f);
        }
    }

    static void SpawnWalkingPath(GameObject root, Vector3 start, Vector3 right, Vector3 forward, Quaternion rotation)
    {
        for (int i = 0; i < 12; i++)
        {
            float width = Mathf.Lerp(0.9f, 1.55f, i / 11f);
            Vector3 pos = start - forward * (i * 0.42f);
            var path = Prim(root, "GardenPath", PrimitiveType.Cube, pos,
                new Vector3(width, 0.014f, 0.32f), new Color(0.68f, 0.62f, 0.50f));
            path.transform.rotation = rotation;
        }
    }

    static void SpawnCurvedFlowerBorder(GameObject root, Vector3 start, Vector3 forward, Vector3 inward, Color color)
    {
        for (int i = 0; i < 14; i++)
        {
            Vector3 pos = start + forward * (i * 0.32f) + inward * (Mathf.Sin(i * 0.45f) * 0.24f);
            Prim(root, "BorderLeaf", PrimitiveType.Sphere, pos + Vector3.up * 0.06f,
                new Vector3(0.16f, 0.11f, 0.16f), new Color(0.25f, 0.48f, 0.24f));
            Prim(root, "BorderFlower", PrimitiveType.Sphere, pos + Vector3.up * 0.16f,
                new Vector3(0.045f, 0.035f, 0.045f), color);
        }
    }

    static void SpawnRestBench(GameObject root, Vector3 center, Quaternion rotation)
    {
        var seat = Prim(root, "ParkBenchSeat", PrimitiveType.Cube, center + Vector3.up * 0.07f,
            new Vector3(1.35f, 0.14f, 0.38f), new Color(0.50f, 0.32f, 0.16f));
        seat.transform.rotation = rotation;

        var back = Prim(root, "ParkBenchBack", PrimitiveType.Cube, center + Vector3.up * 0.34f + seat.transform.forward * 0.16f,
            new Vector3(1.35f, 0.36f, 0.10f), new Color(0.58f, 0.38f, 0.20f));
        back.transform.rotation = rotation;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                var leg = Prim(root, "ParkBenchLeg", PrimitiveType.Cube,
                    center + seat.transform.right * (x * 0.52f) + seat.transform.forward * (z * 0.12f) + Vector3.up * 0.10f,
                    new Vector3(0.08f, 0.20f, 0.08f), new Color(0.32f, 0.20f, 0.10f));
                leg.transform.rotation = rotation;
            }
        }
    }

    static void SpawnGardenLantern(GameObject root, Vector3 basePos)
    {
        Prim(root, "GardenLanternPost", PrimitiveType.Cylinder, basePos + Vector3.up * 0.32f,
            new Vector3(0.045f, 0.32f, 0.045f), new Color(0.34f, 0.22f, 0.12f));
        Prim(root, "GardenLanternBody", PrimitiveType.Cube, basePos + Vector3.up * 0.72f,
            new Vector3(0.22f, 0.20f, 0.22f), new Color(0.96f, 0.86f, 0.58f));

        var lightObj = new GameObject("GardenLanternLight");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.position = basePos + Vector3.up * 0.72f;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.48f);
        light.intensity = 0.55f;
        light.range = 1.4f;
    }

    static void SpawnLargePond(GameObject root, Vector3 center, Quaternion rotation)
    {
        var pond = Prim(root, "LargePond", PrimitiveType.Cylinder, center,
            new Vector3(1.15f, 0.018f, 0.72f), new Color(0.28f, 0.56f, 0.68f));
        pond.transform.rotation = rotation * Quaternion.Euler(0, -16f, 0);
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            Prim(root, "LargePondStone", PrimitiveType.Sphere,
                center + new Vector3(Mathf.Cos(a) * 0.92f, 0.04f, Mathf.Sin(a) * 0.52f),
                new Vector3(0.11f, 0.05f, 0.09f), new Color(0.58f, 0.56f, 0.48f));
        }
    }

    static void SpawnBirdBath(GameObject root, Vector3 center)
    {
        Prim(root, "BirdBathPost", PrimitiveType.Cylinder, center + Vector3.up * 0.28f,
            new Vector3(0.06f, 0.28f, 0.06f), new Color(0.58f, 0.56f, 0.50f));
        Prim(root, "BirdBathBowl", PrimitiveType.Cylinder, center + Vector3.up * 0.58f,
            new Vector3(0.34f, 0.045f, 0.34f), new Color(0.70f, 0.68f, 0.60f));
        Prim(root, "BirdBathWater", PrimitiveType.Cylinder, center + Vector3.up * 0.625f,
            new Vector3(0.28f, 0.012f, 0.28f), new Color(0.36f, 0.62f, 0.72f));
    }

    static void SpawnDistantHills(GameObject root, Vector3 center, Vector3 right, Quaternion rotation)
    {
        if (TrySpawnStylizedMountains(root, center, right, rotation)) return;

        for (int i = -3; i <= 3; i++)
        {
            var hill = Prim(root, "DistantHill", PrimitiveType.Sphere,
                center + right.normalized * (i * 1.4f),
                new Vector3(1.15f, 0.38f + Mathf.Abs(i) * 0.04f, 0.42f), new Color(0.38f, 0.56f, 0.38f));
            hill.transform.rotation = rotation;
        }
    }

    static void SpawnPond(GameObject root, Vector3 center, Quaternion rotation)
    {
        var water = Prim(root, "SmallPond", PrimitiveType.Cylinder, center,
            new Vector3(0.72f, 0.018f, 0.46f), new Color(0.32f, 0.58f, 0.68f));
        water.transform.rotation = rotation * Quaternion.Euler(0, 22f, 0);

        var rim = Prim(root, "PondStoneRim", PrimitiveType.Cylinder, center - Vector3.up * 0.01f,
            new Vector3(0.80f, 0.014f, 0.54f), new Color(0.58f, 0.56f, 0.48f));
        rim.transform.rotation = water.transform.rotation;

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Prim(root, "PondStone", PrimitiveType.Sphere,
                center + new Vector3(Mathf.Cos(angle) * 0.55f, 0.04f, Mathf.Sin(angle) * 0.34f),
                new Vector3(0.12f, 0.05f, 0.09f), new Color(0.56f, 0.54f, 0.48f));
        }
    }

    static void SpawnFence(GameObject root, Vector3 center, Vector3 right, Quaternion rotation)
    {
        Vector3 r = right.normalized;
        for (int i = -4; i <= 4; i++)
        {
            var post = Prim(root, "LowFencePost", PrimitiveType.Cube,
                center + r * (i * 0.42f) + Vector3.up * 0.18f,
                new Vector3(0.055f, 0.36f, 0.055f), new Color(0.54f, 0.36f, 0.18f));
            post.transform.rotation = rotation;
        }

        for (int rail = 0; rail < 2; rail++)
        {
            var bar = Prim(root, "LowFenceRail", PrimitiveType.Cube,
                center + Vector3.up * (0.16f + rail * 0.16f),
                new Vector3(3.6f, 0.045f, 0.045f), new Color(0.54f, 0.36f, 0.18f));
            bar.transform.rotation = rotation;
        }
    }

    static void SpawnCloud(GameObject root, Vector3 center)
    {
        Color cloud = new Color(0.94f, 0.96f, 0.94f, 1f);
        Prim(root, "CloudA", PrimitiveType.Sphere, center, new Vector3(0.42f, 0.16f, 0.16f), cloud);
        Prim(root, "CloudB", PrimitiveType.Sphere, center + new Vector3(0.25f, 0.04f, 0.02f), new Vector3(0.32f, 0.13f, 0.13f), cloud);
        Prim(root, "CloudC", PrimitiveType.Sphere, center - new Vector3(0.25f, -0.02f, 0.02f), new Vector3(0.30f, 0.12f, 0.12f), cloud);
    }

    static void SpawnPlant(GameObject root, Vector3 basePos)
    {
        Prim(root, "PlantPot", PrimitiveType.Cylinder, basePos + Vector3.up * 0.10f,
            new Vector3(0.18f, 0.16f, 0.18f), new Color(0.54f, 0.33f, 0.18f));

        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.09f, 0.36f + i * 0.025f, Mathf.Sin(angle) * 0.04f);
            var leaf = Prim(root, "Leaf", PrimitiveType.Sphere, basePos + offset,
                new Vector3(0.12f, 0.24f, 0.05f), new Color(0.20f, 0.42f, 0.23f));
            leaf.transform.rotation = Quaternion.Euler(18f, i * 72f, 25f);
        }
    }

    static void SpawnWindow(GameObject root, Vector3 center, Quaternion rotation)
    {
        var frame = Prim(root, "WindowFrame", PrimitiveType.Cube, center,
            new Vector3(0.72f, 0.55f, 0.035f), new Color(0.45f, 0.30f, 0.16f));
        frame.transform.rotation = rotation;

        var glass = Prim(root, "SoftSunWindow", PrimitiveType.Cube, center - frame.transform.forward * 0.025f,
            new Vector3(0.62f, 0.45f, 0.02f), new Color(0.82f, 0.93f, 0.96f));
        glass.transform.rotation = rotation;

        var mullionV = Prim(root, "WindowMullionV", PrimitiveType.Cube, center - frame.transform.forward * 0.05f,
            new Vector3(0.035f, 0.48f, 0.03f), new Color(0.45f, 0.30f, 0.16f));
        mullionV.transform.rotation = rotation;

        var mullionH = Prim(root, "WindowMullionH", PrimitiveType.Cube, center - frame.transform.forward * 0.055f,
            new Vector3(0.62f, 0.032f, 0.03f), new Color(0.45f, 0.30f, 0.16f));
        mullionH.transform.rotation = rotation;
    }

    static void SpawnBench(GameObject root, Vector3 center, Quaternion rotation)
    {
        var seat = Prim(root, "LobbyBenchSeat", PrimitiveType.Cube, center + Vector3.up * 0.18f,
            new Vector3(2.3f, 0.18f, 0.42f), new Color(0.53f, 0.34f, 0.18f));
        seat.transform.rotation = rotation;

        var back = Prim(root, "LobbyBenchBack", PrimitiveType.Cube, center + Vector3.up * 0.48f + seat.transform.forward * 0.18f,
            new Vector3(2.3f, 0.48f, 0.12f), new Color(0.62f, 0.43f, 0.25f));
        back.transform.rotation = rotation;

        for (int i = -1; i <= 1; i += 2)
        {
            var leg = Prim(root, "LobbyBenchLeg", PrimitiveType.Cube,
                center + seat.transform.right * (i * 0.86f) + Vector3.up * 0.01f,
                new Vector3(0.12f, 0.32f, 0.12f), new Color(0.33f, 0.20f, 0.10f));
            leg.transform.rotation = rotation;
        }
    }

    static void SpawnRug(GameObject root, Vector3 center, Quaternion rotation)
    {
        var rug = Prim(root, "QuietRug", PrimitiveType.Cube, center,
            new Vector3(3.0f, 0.012f, 1.35f), new Color(0.46f, 0.62f, 0.58f));
        rug.transform.rotation = rotation;

        var stripe = Prim(root, "QuietRugStripe", PrimitiveType.Cube, center + Vector3.up * 0.01f,
            new Vector3(2.65f, 0.012f, 0.18f), new Color(0.85f, 0.80f, 0.62f));
        stripe.transform.rotation = rotation;
    }

    static void SpawnSoftLight(GameObject root, Vector3 pos)
    {
        var lightObj = new GameObject("WarmLobbyLight");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.position = pos;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.88f, 0.62f);
        light.intensity = 1.35f;
        light.range = 4.2f;
    }

    static void SpawnSunLight(GameObject root, Vector3 pos, Vector3 forward, Vector3 right)
    {
        var lightObj = new GameObject("SoftSunlight");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.position = pos;
        lightObj.transform.rotation = Quaternion.LookRotation((forward - right * 0.35f - Vector3.up * 0.18f).normalized, Vector3.up);

        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.92f, 0.72f);
        light.intensity = 0.55f;
    }

    static bool TrySpawnStylizedTree(GameObject root, Vector3 basePos)
    {
        if (StylizedTreeResourcePaths.Length == 0) return false;

        int index = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.PerlinNoise(basePos.x * 0.37f + 12.1f, basePos.z * 0.37f + 3.4f) * StylizedTreeResourcePaths.Length),
            0,
            StylizedTreeResourcePaths.Length - 1);

        var prefab = Resources.Load<GameObject>(StylizedTreeResourcePaths[index]);
        if (prefab == null) return false;

        var tree = Object.Instantiate(prefab, root.transform);
        tree.name = $"Stylized_{prefab.name}";
        tree.transform.position = basePos + Vector3.up * 0.01f;
        tree.transform.rotation = Quaternion.Euler(
            0f,
            Mathf.PerlinNoise(basePos.x * 0.81f + 7.2f, basePos.z * 0.81f + 1.9f) * 360f,
            0f);

        float scale = Mathf.Lerp(0.80f, 1.22f, Mathf.PerlinNoise(basePos.x * 0.55f + 4.8f, basePos.z * 0.55f + 9.7f));
        tree.transform.localScale *= scale;
        RaiseStylizedCanopy(tree, scale);
        ConfigureSpawnedRenderers(tree, true);
        ToneStylizedTree(tree);
        return true;
    }

    static bool TrySpawnStylizedMountains(GameObject root, Vector3 center, Vector3 right, Quaternion rotation)
    {
        var prefab = Resources.Load<GameObject>("StylizedNature/mountain");
        if (prefab == null) return false;

        Vector3 r = right.normalized;
        Vector3 f = rotation * Vector3.forward;
        for (int i = -2; i <= 2; i++)
        {
            var mountain = Object.Instantiate(prefab, root.transform);
            mountain.name = $"Stylized_Mountain_{i + 2}";
            mountain.transform.position = center + r * (i * 2.9f) + f * (1.8f + Mathf.Abs(i) * 0.18f) - Vector3.up * 0.04f;
            mountain.transform.rotation = rotation * Quaternion.Euler(0f, i * 11f, 0f);
            float scale = i == 0 ? 2.3f : 1.7f + Mathf.Abs(i) * 0.28f;
            mountain.transform.localScale *= scale;
            ConfigureSpawnedRenderers(mountain, false);
        }

        return true;
    }

    static bool TrySpawnStylizedSky(GameObject root, Vector3 horizonBase, Vector3 right, Quaternion rotation)
    {
        var prefab = Resources.Load<GameObject>("StylizedNature/sky");
        if (prefab == null) return false;

        Vector3 r = right.normalized;
        Vector3 f = rotation * Vector3.forward;

        var sky = Object.Instantiate(prefab, root.transform);
        sky.name = "Stylized_Sky";
        sky.transform.position = horizonBase + Vector3.up * 2.05f + f * 2.8f;
        sky.transform.rotation = rotation;
        sky.transform.localScale *= 8.5f;
        ConfigureSpawnedRenderers(sky, false, false);

        for (int i = -1; i <= 1; i++)
        {
            var clone = Object.Instantiate(prefab, root.transform);
            clone.name = $"Stylized_Sky_{i + 2}";
            clone.transform.position = horizonBase + Vector3.up * 2.15f + f * (2.9f + Mathf.Abs(i) * 0.15f) + r * (i * 8.0f);
            clone.transform.rotation = rotation * Quaternion.Euler(0f, i * 2.5f, 0f);
            clone.transform.localScale *= 8.2f;
            ConfigureSpawnedRenderers(clone, false, false);
        }

        return true;
    }

    static void SpawnStylizedSun(GameObject root, Vector3 horizonBase, Vector3 right, Quaternion rotation)
    {
        Vector3 r = right.normalized;
        var sun = Prim(root, "SoftMorningSun", PrimitiveType.Sphere, horizonBase - r * 5.0f + Vector3.up * 2.45f,
            new Vector3(0.38f, 0.38f, 0.04f), new Color(1.0f, 0.86f, 0.46f));
        sun.transform.rotation = rotation;
    }

    static void ConfigureSpawnedRenderers(GameObject rootObject, bool receiveShadows, bool castShadows = true)
    {
        foreach (var collider in rootObject.GetComponentsInChildren<Collider>(true))
        {
            Object.Destroy(collider);
        }

        foreach (var renderer in rootObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }
    }

    static void ToneStylizedTree(GameObject treeRoot)
    {
        if (treeRoot == null) return;

        Color leafTarget = new Color(0.38f, 0.62f, 0.24f, 1f);
        Color trunkTarget = new Color(0.48f, 0.40f, 0.27f, 1f);

        foreach (var renderer in treeRoot.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.materials)
            {
                if (material == null) continue;

                Color baseColor = material.HasProperty("_Color") ? material.color : Color.white;
                bool looksLikeLeaf = baseColor.g > baseColor.r * 1.12f && baseColor.g > baseColor.b * 1.12f;
                Color toned = looksLikeLeaf
                    ? Color.Lerp(baseColor, leafTarget, 0.42f)
                    : Color.Lerp(baseColor, trunkTarget, 0.18f);

                toned.a = baseColor.a;
                if (material.HasProperty("_Color")) material.color = toned;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", toned);
            }
        }
    }

    static void RaiseStylizedCanopy(GameObject treeRoot, float scale)
    {
        if (treeRoot == null) return;

        float lift = Mathf.Lerp(0.45f, 0.78f, Mathf.InverseLerp(0.8f, 1.22f, scale));
        foreach (var renderer in treeRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;

            bool looksLikeLeaf = false;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                var color = material.HasProperty("_Color") ? material.color :
                    material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;

                if (color.g > color.r * 1.12f && color.g > color.b * 1.12f)
                {
                    looksLikeLeaf = true;
                    break;
                }
            }

            if (!looksLikeLeaf) continue;
            renderer.transform.localPosition += Vector3.up * lift;
        }
    }

    static GameObject Prim(GameObject root, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;

        var collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        var mat = new Material(Shader.Find("Standard"));
        Color varied = NaturalizeColor(name, pos, color);
        mat.color = varied;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", varied);
        ConfigureMaterial(name, mat, varied);

        var renderer = go.GetComponent<Renderer>();
        renderer.material = mat;
        renderer.shadowCastingMode = name.Contains("Sky") || name.Contains("Cloud") || name.Contains("Sun")
            ? ShadowCastingMode.Off
            : ShadowCastingMode.On;
        renderer.receiveShadows = !name.Contains("Sky") && !name.Contains("Cloud") && !name.Contains("Sun");
        return go;
    }

    static Color NaturalizeColor(string name, Vector3 pos, Color color)
    {
        if (name.Contains("Sky") || name.Contains("Sun")) return color;

        float noise = Mathf.PerlinNoise(pos.x * 1.91f + 12.4f, pos.z * 1.73f + pos.y * 0.31f);
        float amount = Mathf.Lerp(-0.08f, 0.08f, noise);
        if (name.Contains("Flower")) amount *= 0.45f;
        if (name.Contains("Water") || name.Contains("Pond")) amount *= 0.35f;

        Color adjusted = color * (1f + amount);
        adjusted.a = color.a;
        return adjusted;
    }

    static void ConfigureMaterial(string name, Material mat, Color color)
    {
        SetFloatIfPresent(mat, "_Metallic", 0f);
        SetFloatIfPresent(mat, "_Glossiness", 0.18f);
        Texture2D texture = null;
        Vector2 textureScale = Vector2.one;

        if (name.Contains("Water") || name.Contains("Pond"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.72f);
            SetFloatIfPresent(mat, "_Metallic", 0.05f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_ON");
            texture = GetProceduralTexture("water", new Color(0.21f, 0.50f, 0.62f), new Color(0.55f, 0.78f, 0.86f));
            textureScale = new Vector2(2.5f, 2f);
        }
        else if (name.Contains("Stone") || name.Contains("Path") || name.Contains("Hill") || name.Contains("Pebble"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.08f);
            texture = GetProceduralTexture("stone", new Color(0.38f, 0.36f, 0.31f), new Color(0.72f, 0.69f, 0.60f));
            textureScale = new Vector2(2.2f, 2.2f);
        }
        else if (name.Contains("Wood") || name.Contains("Trunk") || name.Contains("Branch") ||
                 name.Contains("Post") || name.Contains("Fence") || name.Contains("Bench") ||
                 name.Contains("Pavilion") || name.Contains("Deck") || name.Contains("Roof"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.12f);
            string key = name.Contains("Trunk") || name.Contains("Branch") ? "bark" : "wood";
            texture = GetProceduralTexture(key, new Color(0.24f, 0.13f, 0.06f), new Color(0.62f, 0.40f, 0.21f));
            textureScale = key == "bark" ? new Vector2(1.2f, 3.8f) : new Vector2(2.8f, 1.4f);
        }
        else if (name.Contains("FallenLeaf"))
        {
            texture = GetProceduralTexture("fallen_leaf", new Color(0.42f, 0.22f, 0.08f), new Color(0.82f, 0.58f, 0.18f));
            textureScale = new Vector2(1f, 2.4f);
        }
        else if (name.Contains("Grass") || name.Contains("Leaf") || name.Contains("Shrub") || name.Contains("Crown"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.22f);
            texture = GetProceduralTexture("grass_leaf", new Color(0.18f, 0.34f, 0.15f), new Color(0.56f, 0.72f, 0.34f));
            textureScale = name.Contains("ParkGrass") ? new Vector2(10f, 8f) : new Vector2(2f, 2f);
        }
        else if (name.Contains("Cloud"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.03f);
            texture = GetProceduralTexture("cloud", new Color(0.78f, 0.84f, 0.84f), new Color(1.0f, 1.0f, 0.96f));
        }
        else if (name.Contains("LanternBody"))
        {
            SetFloatIfPresent(mat, "_Glossiness", 0.55f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 0.35f);
            texture = GetProceduralTexture("paper_lamp", new Color(0.82f, 0.66f, 0.34f), new Color(1f, 0.92f, 0.62f));
        }
        else if (name.Contains("Sky") || name.Contains("Horizon"))
        {
            texture = GetProceduralTexture("sky", new Color(0.58f, 0.78f, 0.92f), new Color(0.90f, 0.94f, 0.84f));
            textureScale = new Vector2(1f, 1f);
        }
        else if (name.Contains("Flower"))
        {
            texture = GetProceduralTexture("petal", color * 0.78f, Color.Lerp(color, Color.white, 0.28f));
            textureScale = new Vector2(1.4f, 1.4f);
        }

        if (texture != null)
        {
            mat.mainTexture = texture;
            mat.mainTextureScale = textureScale;
        }

        if (texture != null && !name.Contains("Sky") && !name.Contains("Cloud") && !name.Contains("Sun"))
        {
            var normal = GetProceduralNormal($"{texture.name}_normal", texture);
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                SetFloatIfPresent(mat, "_BumpScale", name.Contains("Water") || name.Contains("Pond") ? 0.08f : 0.18f);
            }
        }
    }

    static void SetFloatIfPresent(Material mat, string propertyName, float value)
    {
        if (mat.HasProperty(propertyName)) mat.SetFloat(propertyName, value);
    }

    static Texture2D GetProceduralTexture(string key, Color dark, Color light)
    {
        string cacheKey = $"{key}_{ColorUtility.ToHtmlStringRGB(dark)}_{ColorUtility.ToHtmlStringRGB(light)}";
        if (TextureCache.TryGetValue(cacheKey, out var cached)) return cached;

        int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = $"SilverCare_{key}_Texture",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float n = Mathf.PerlinNoise(u * 7.5f + key.Length * 0.13f, v * 7.5f + 3.7f);
                float fine = Mathf.PerlinNoise(u * 26f + 5.1f, v * 26f + 8.9f);
                float t = Mathf.Clamp01(n * 0.78f + fine * 0.22f);

                if (key == "bark")
                {
                    float stripe = Mathf.Abs(Mathf.Sin((u * 12f + n * 2.2f) * Mathf.PI));
                    t = Mathf.Clamp01(t * 0.45f + stripe * 0.55f);
                }
                else if (key == "wood")
                {
                    float grain = Mathf.Abs(Mathf.Sin((v * 18f + n * 1.4f) * Mathf.PI));
                    t = Mathf.Clamp01(t * 0.40f + grain * 0.60f);
                }
                else if (key == "water")
                {
                    float ripple = Mathf.Sin((u * 15f + n * 2f) * Mathf.PI) * 0.5f + 0.5f;
                    t = Mathf.Clamp01(t * 0.50f + ripple * 0.50f);
                }
                else if (key == "sky")
                {
                    t = Mathf.Clamp01(1f - v * 0.85f + fine * 0.05f);
                }
                else if (key == "grass_leaf")
                {
                    float blade = Mathf.Abs(Mathf.Sin((u * 32f + n) * Mathf.PI));
                    t = Mathf.Clamp01(t * 0.65f + blade * 0.18f);
                }
                else if (key == "stone")
                {
                    float speckle = fine > 0.62f ? 0.18f : 0f;
                    t = Mathf.Clamp01(t * 0.86f + speckle);
                }

                texture.SetPixel(x, y, Color.Lerp(dark, light, t));
            }
        }

        texture.Apply(true, false);
        TextureCache[cacheKey] = texture;
        return texture;
    }

    static Texture2D GetProceduralNormal(string key, Texture2D source)
    {
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int size = source.width;
        var normal = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = key,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float left = source.GetPixel((x - 1 + size) % size, y).grayscale;
                float right = source.GetPixel((x + 1) % size, y).grayscale;
                float down = source.GetPixel(x, (y - 1 + size) % size).grayscale;
                float up = source.GetPixel(x, (y + 1) % size).grayscale;
                Vector3 n = new Vector3((left - right) * 1.8f, (down - up) * 1.8f, 1f).normalized;
                normal.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z, 1f));
            }
        }

        normal.Apply(true, false);
        TextureCache[key] = normal;
        return normal;
    }
}
