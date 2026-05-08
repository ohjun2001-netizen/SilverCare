// Assets/Editor/SilverCareTestSetup.cs
// Unity 메뉴: SilverCare → Full Test Setup
// 테스트 카드 텍스처 4장 + 카드 프리팹 + CardMatch 씬 참조 연결을 한 번에 처리
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SilverCareTestSetup
{
    const string TEX_FOLDER  = "Assets/Textures/TestCards";
    const string PREFAB_PATH = "Assets/Prefabs/Card.prefab";
    const string SCENE_PATH  = "Assets/Scenes/CardMatch.unity";

    [MenuItem("SilverCare/Full Test Setup (테스트 세팅 전체)")]
    public static void FullSetup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("플레이 중", "Stop 후 실행하세요.", "확인");
            return;
        }

        EnsureFolders();

        var textures = CreateTestTextures();
        CreateCardPrefab(textures[0]);
        SetupCardMatchScene(textures);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SCENE_PATH);

        EditorUtility.DisplayDialog("완료!",
            "카드 텍스처 4장  →  Assets/Textures/TestCards/\n" +
            "카드 프리팹      →  Assets/Prefabs/Card.prefab\n" +
            "CardMatch 씬 참조 연결 완료\n\n" +
            "Play 버튼을 눌러 마우스로 카드를 클릭해 보세요!", "확인");
    }

    // ── 폴더 ─────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(TEX_FOLDER))
            AssetDatabase.CreateFolder("Assets/Textures", "TestCards");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    // ── 테스트 텍스처 생성 ────────────────────────────────────────
    static Texture2D[] CreateTestTextures()
    {
        // 카드 4쌍 = 4종류 텍스처 (배경색, 숫자)
        (Color bg, int num, string name)[] configs =
        {
            (new Color(0.82f, 0.15f, 0.15f), 1, "TestCard_1"),
            (new Color(0.15f, 0.33f, 0.82f), 2, "TestCard_2"),
            (new Color(0.15f, 0.62f, 0.22f), 3, "TestCard_3"),
            (new Color(0.78f, 0.58f, 0.08f), 4, "TestCard_4"),
        };

        var result = new Texture2D[configs.Length];

        for (int i = 0; i < configs.Length; i++)
        {
            string path    = $"{TEX_FOLDER}/{configs[i].name}.png";
            string absPath = Path.GetFullPath(path);

            var tex  = MakeCardTexture(256, 256, configs[i].bg, configs[i].num);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(absPath, png);
        }

        AssetDatabase.Refresh();

        for (int i = 0; i < configs.Length; i++)
        {
            string path = $"{TEX_FOLDER}/{configs[i].name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.textureType = TextureImporterType.Default; imp.SaveAndReimport(); }
            result[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        return result;
    }

    static Texture2D MakeCardTexture(int w, int h, Color bg, int num)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // 배경
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            tex.SetPixel(x, y, bg);

        // 흰 테두리 (두께 8px)
        int b = 8;
        for (int x = b; x < w - b; x++)
        {
            tex.SetPixel(x, b,         Color.white);
            tex.SetPixel(x, h - b - 1, Color.white);
        }
        for (int y = b; y < h - b; y++)
        {
            tex.SetPixel(b,         y, Color.white);
            tex.SetPixel(w - b - 1, y, Color.white);
        }

        // 중앙에 큰 숫자
        DrawNumber(tex, num, w / 2 - 24, h / 2 - 32, 10, Color.white);

        tex.Apply();
        return tex;
    }

    // 3x5 픽셀 폰트 (scale 배 확대)
    static readonly bool[][] NumberPixels =
    {
        // 1
        new bool[]
        {
            false, true, false,
            true,  true, false,
            false, true, false,
            false, true, false,
            true,  true, true,
        },
        // 2
        new bool[]
        {
            true,  true,  true,
            false, false, true,
            false, true,  false,
            true,  false, false,
            true,  true,  true,
        },
        // 3
        new bool[]
        {
            true,  true,  true,
            false, false, true,
            false, true,  true,
            false, false, true,
            true,  true,  true,
        },
        // 4
        new bool[]
        {
            true,  false, true,
            true,  false, true,
            true,  true,  true,
            false, false, true,
            false, false, true,
        },
    };

    static void DrawNumber(Texture2D tex, int num, int startX, int startY, int scale, Color fg)
    {
        if (num < 1 || num > 4) return;
        var pat = NumberPixels[num - 1]; // 3cols x 5rows

        for (int row = 0; row < 5; row++)
        for (int col = 0; col < 3; col++)
        {
            if (!pat[row * 3 + col]) continue;
            int px = startX + col * scale;
            int py = startY + (4 - row) * scale; // 아래에서 위로
            for (int dy = 0; dy < scale; dy++)
            for (int dx = 0; dx < scale; dx++)
            {
                int fx = px + dx, fy = py + dy;
                if (fx >= 0 && fx < tex.width && fy >= 0 && fy < tex.height)
                    tex.SetPixel(fx, fy, fg);
            }
        }
    }

    // ── 카드 프리팹 생성 ──────────────────────────────────────────
    static void CreateCardPrefab(Texture2D previewTex)
    {
        var card = new GameObject("Card");

        // 앞면 (Euler 0,180,0 → 카메라 쪽(-Z)을 향함)
        var frontGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frontGO.name = "FrontFace";
        frontGO.transform.SetParent(card.transform, false);
        frontGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        frontGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Object.DestroyImmediate(frontGO.GetComponent<MeshCollider>());

        if (previewTex != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = previewTex;
            mat.SetFloat("_Glossiness", 0f);
            frontGO.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // 뒷면 (파란 카드백)
        var backGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backGO.name = "BackFace";
        backGO.transform.SetParent(card.transform, false);
        backGO.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        backGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Object.DestroyImmediate(backGO.GetComponent<MeshCollider>());

        var backMat = new Material(Shader.Find("Standard"));
        backMat.color = new Color(0.12f, 0.22f, 0.50f);
        backMat.SetFloat("_Glossiness", 0f);
        backGO.GetComponent<Renderer>().sharedMaterial = backMat;

        // 클릭 감지용 BoxCollider
        var col = card.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 1f, 0.05f);

        // CardController 부착 + 참조 연결
        var cc = card.AddComponent<SilverCare.CardMatch.CardController>();
        var so = new SerializedObject(cc);
        so.FindProperty("frontFaceObject").objectReferenceValue = frontGO;
        so.FindProperty("backFaceObject").objectReferenceValue  = backGO;
        so.FindProperty("frontRenderer").objectReferenceValue   = frontGO.GetComponent<MeshRenderer>();
        so.ApplyModifiedProperties();

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(card, PREFAB_PATH, out ok);
        Object.DestroyImmediate(card);

        if (!ok) Debug.LogError("[TestSetup] 카드 프리팹 저장 실패");
        else     Debug.Log($"[TestSetup] 카드 프리팹 저장: {PREFAB_PATH}");
    }

    // ── CardMatch 씬 설정 ─────────────────────────────────────────
    static void SetupCardMatchScene(Texture2D[] textures)
    {
        // 씬 열기 (없으면 새로 생성)
        bool exists = File.Exists(Path.GetFullPath(SCENE_PATH));
        var scene   = exists
            ? EditorSceneManager.OpenScene(SCENE_PATH)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // CardMatchManager 오브젝트
        var managerGO = GameObject.Find("CardMatchManager") ?? new GameObject("CardMatchManager");
        if (managerGO.GetComponent<SilverCare.CardMatch.CardMatchGameManager>() == null)
            managerGO.AddComponent<SilverCare.CardMatch.CardMatchGameManager>();

        // Managers 싱글턴 (GameSceneManager 등)
        if (GameObject.Find("Managers") == null)
        {
            var mgrs = new GameObject("Managers");
            mgrs.AddComponent<SilverCare.Common.GameSceneManager>();
            mgrs.AddComponent<SilverCare.Common.PlayerDataManager>();
            mgrs.AddComponent<SilverCare.Common.AudioManager>();
            mgrs.AddComponent<SilverCare.Common.TTSManager>();
        }

        // 카드 배치용 부모 오브젝트
        var cardParentGO = GameObject.Find("CardParent") ?? new GameObject("CardParent");
        cardParentGO.transform.position = Vector3.zero;

        // CardMatchGameManager 직렬화 필드 연결
        var mgr = managerGO.GetComponent<SilverCare.CardMatch.CardMatchGameManager>();
        var so  = new SerializedObject(mgr);

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        so.FindProperty("cardPrefab").objectReferenceValue  =
            prefabAsset != null ? prefabAsset.GetComponent<SilverCare.CardMatch.CardController>() : null;
        so.FindProperty("cardParent").objectReferenceValue  = cardParentGO.transform;
        so.FindProperty("pairCount").intValue               = 4;
        so.FindProperty("columnCount").intValue             = 4;
        so.FindProperty("xSpacing").floatValue              = 1.2f;
        so.FindProperty("zSpacing").floatValue              = 1.5f;
        so.FindProperty("startOffset").vector3Value         = new Vector3(-1.8f, 1.0f, 1.5f);

        var texProp = so.FindProperty("cardFrontTextures");
        texProp.arraySize = textures.Length;
        for (int i = 0; i < textures.Length; i++)
            texProp.GetArrayElementAtIndex(i).objectReferenceValue = textures[i];

        so.ApplyModifiedProperties();

        // 카메라 — 카드들을 바라보도록 배치
        var cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        // 카드 중심: x=0, y=1.0, z=2.25 (1.5 + 0.75 = 반쯤)
        Vector3 cardCenter = new Vector3(0f, 1.0f, 2.25f);
        cam.transform.position = new Vector3(0f, 3.0f, -3.5f);
        cam.transform.rotation = Quaternion.LookRotation(
            cardCenter - cam.transform.position, Vector3.up);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.clearFlags      = CameraClearFlags.SolidColor;

        // 간단한 방향광
        var existing = Object.FindObjectOfType<Light>();
        if (existing == null)
        {
            var lightGO = new GameObject("Directional Light");
            var light   = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        }

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log("[TestSetup] CardMatch 씬 설정 완료");
    }
}
