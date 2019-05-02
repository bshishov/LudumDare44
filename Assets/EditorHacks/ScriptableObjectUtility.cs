using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScriptableObjectUtility
{
    public static void CreateAsset<T>() where T : MonoBehaviour
    {
        var assetName = "/New " + typeof(T).ToString();

        var asset = GameObject.Instantiate(new GameObject());
        asset.AddComponent<T>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}{assetName}.prefab");

        PrefabUtility.SaveAsPrefabAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
