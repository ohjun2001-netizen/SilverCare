// Assets/Editor/CardPrefabBuilder.cs
// Unity 메뉴: SilverCare → Create Card Prefab
using UnityEngine;
using UnityEditor;

public static class CardPrefabBuilder
{
    [MenuItem("SilverCare/Create Card Prefab")]
    public static void CreateCardPrefab()
    {
        // 저장 폴더 확인
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 루트 오브젝트
        var card = new GameObject("Card");

        // 앞면 Quad
        var frontGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frontGO.name = "FrontFace";
        frontGO.transform.SetParent(card.transform, false);
        frontGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        frontGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Object.DestroyImmediate(frontGO.GetComponent<MeshCollider>());

        // 뒷면 Quad (180도 뒤집어서 반대면)
        var backGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backGO.name = "BackFace";
        backGO.transform.SetParent(card.transform, false);
        backGO.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        backGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Object.DestroyImmediate(backGO.GetComponent<MeshCollider>());

        // 뒷면 색 (진한 파랑)
        var backMat = new Material(Shader.Find("Standard"));
        backMat.color = new Color(0.15f, 0.25f, 0.55f);
        backGO.GetComponent<Renderer>().sharedMaterial = backMat;

        // 클릭 감지용 BoxCollider (카드 전체 크기)
        var col = card.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 1f, 0.05f);

        // CardController 부착 및 참조 연결
        var cc = card.AddComponent<SilverCare.CardMatch.CardController>();

        var so = new SerializedObject(cc);
        so.FindProperty("frontFaceObject").objectReferenceValue = frontGO;
        so.FindProperty("backFaceObject").objectReferenceValue  = backGO;
        so.FindProperty("frontRenderer").objectReferenceValue   = frontGO.GetComponent<MeshRenderer>();
        so.ApplyModifiedProperties();

        // 프리팹 저장
        string path = "Assets/Prefabs/Card.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(card, path, out success);
        Object.DestroyImmediate(card);

        if (success)
        {
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", "Assets/Prefabs/Card.prefab 생성 완료!\n\nCardMatchManager의 Card Prefab 칸에 드래그해서 연결하세요.", "확인");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        else
        {
            EditorUtility.DisplayDialog("실패", "프리팹 저장에 실패했습니다.", "확인");
        }
    }
}
