// SilverCare → Patch XR Scenes
// 모든 씬의 XR Origin에 XRLineVisualFixer를 추가
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class XRScenePatcher
{
    [MenuItem("SilverCare/Patch XR Scenes (XR 라인비주얼 오류 수정)")]
    public static void PatchAll()
    {
        string[] scenePaths =
        {
            "Assets/Scenes/MainLobby.unity",
            "Assets/Scenes/CardMatch.unity",
            "Assets/Scenes/Golf.unity",
            "Assets/Scenes/Quiz.unity",
            "Assets/Scenes/SongGuess.unity",
            "Assets/Scenes/GoStop.unity",
            "Assets/Scenes/BadukVR.unity",
            "Assets/Scenes/BadukReplay.unity",
            "Assets/Scenes/BadukPrediction.unity",
        };

        int count = 0;
        foreach (var path in scenePaths)
        {
            if (!System.IO.File.Exists(System.IO.Path.GetFullPath(path))) continue;

            var scene = EditorSceneManager.OpenScene(path);

#pragma warning disable CS0618
            var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
#pragma warning restore CS0618
            if (xrOrigin == null) continue;

            if (xrOrigin.GetComponent<SilverCare.Common.XRLineVisualFixer>() != null) continue;

            xrOrigin.gameObject.AddComponent<SilverCare.Common.XRLineVisualFixer>();
            EditorSceneManager.SaveScene(scene);
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료",
            $"{count}개 씬에 XRLineVisualFixer 추가 완료.", "확인");
    }
}
