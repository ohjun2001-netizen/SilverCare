// Assets/Scripts/Golf/Editor/GolfParkResourceSetup.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SilverCare.Golf.EditorTools
{
    /// <summary>
    /// 한국식 파크 골프 코스에 쓰이는 임포트 프리팹을 Resources/GolfPark/ 로 복사한다.
    /// Resources 밖 프리팹은 빌드에서 Resources.Load 로 불러올 수 없으므로,
    /// AssetDatabase.CopyAsset(새 GUID 생성, 의존 메시/머티리얼은 GUID 참조로 유지)으로 사본을 만든다.
    /// 에디터 로드 시 누락분만 자동 복사하고, 메뉴로 수동 재실행도 가능하다.
    /// </summary>
    [InitializeOnLoad]
    public static class GolfParkResourceSetup
    {
        const string TargetFolder = "Assets/Resources/GolfPark";

        // 원본 경로 → Resources/GolfPark/ 사본 이름(확장자 제외)
        static readonly (string src, string name)[] Prefabs =
        {
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Bridge_15_Left.prefab",   "PP_Bridge_15_Left"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Bridge_15_Middle.prefab", "PP_Bridge_15_Middle"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Bridge_15_Right.prefab",  "PP_Bridge_15_Right"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Lake_Ground_04.prefab",   "PP_Lake_Ground_04"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Meadow_Path_05.prefab",   "PP_Meadow_Path_05"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Floor_Tile_05.prefab",    "PP_Floor_Tile_05"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Floor_Tile_15.prefab",    "PP_Floor_Tile_15"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Daffodil_03.prefab",      "PP_Daffodil_03"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Sunflower_04.prefab",     "PP_Sunflower_04"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Hyacinth_04.prefab",      "PP_Hyacinth_04"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Small_Fence_01.prefab",   "PP_Small_Fence_01"),
            ("Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Rock_Pile_Forest_Moss_05.prefab", "PP_Rock_Pile_05"),
            ("Assets/Parks And Nature Pack/Prefab/ChairA.prefab",                             "ChairA"),
            ("Assets/Darth_Artisan/Free_Trees/Prefabs/Fir_Tree.prefab",                       "Fir_Tree"),
        };

        static GolfParkResourceSetup()
        {
            // 도메인 리로드 시 누락분만 조용히 복사. 모두 있으면 아무 일도 하지 않는다.
            EditorApplication.delayCall += () => CopyMissing(false);
        }

        [MenuItem("Tools/Golf/Copy Park Prefabs to Resources")]
        public static void CopyAllMenu()
        {
            CopyMissing(true);
        }

        static void CopyMissing(bool verbose)
        {
            EnsureFolder();

            int copied = 0;
            foreach (var entry in Prefabs)
            {
                string dst = $"{TargetFolder}/{entry.name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null)
                    continue; // 이미 존재

                var src = AssetDatabase.LoadAssetAtPath<GameObject>(entry.src);
                if (src == null)
                {
                    if (verbose)
                        Debug.LogWarning($"[GolfParkResourceSetup] 원본 프리팹 없음: {entry.src}");
                    continue;
                }

                if (AssetDatabase.CopyAsset(entry.src, dst))
                    copied++;
                else if (verbose)
                    Debug.LogWarning($"[GolfParkResourceSetup] 복사 실패: {entry.src} → {dst}");
            }

            if (copied > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[GolfParkResourceSetup] {copied}개 프리팹을 {TargetFolder} 로 복사했습니다.");
            }
            else if (verbose)
            {
                Debug.Log("[GolfParkResourceSetup] 복사할 프리팹이 없습니다(모두 최신).");
            }
        }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(TargetFolder))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "GolfPark");
        }
    }
}
#endif
