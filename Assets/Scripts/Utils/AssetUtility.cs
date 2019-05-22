using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utils
{
    public static class AssetUtility
    {
#if UNITY_EDITOR
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

        public static List<T> FindAssetsOfType<T>() where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }
#endif
    }
}
