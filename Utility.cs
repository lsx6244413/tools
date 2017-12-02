using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class Utility
{

    static private string GetRelativeAssetsPath(string path)
    {
        return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
    }

    static private string GetChildToRootPath(GameObject child)
    {
        StringBuilder path = new StringBuilder();
        path.Append(child.name);
        var parent = child.transform.parent;
        while(parent)
        {
            path.Append("=>" + parent.name);
            parent = parent.transform.parent;
        }
        return path.ToString();
    }

    static private void FindReferenceAsset(string path, List<string> containExtensions, Action<string> onFind)
    {
        string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(s => containExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();

        UInt32 count = 0;
        string guid = AssetDatabase.AssetPathToGUID(path);
        foreach (var file in files)
        {
            if (Regex.IsMatch(File.ReadAllText(file), guid))
            {
                count++;
                onFind(file);
                
            }
        }
        Debug.LogWarning(string.Format("There are {0} assets that contain the selected asset", count));
    }

    //引用资源映射表
    static readonly Dictionary<Type, List<string>> ASSET_REFERENCE_DIC = new Dictionary<Type, List<string>> 
    { 
        {typeof(Shader), new List<string>() {".mat"}},
        //{typeof(Material), new List<string>() {".prefab"}},
    };

    [MenuItem("LSX/Find Asset Reference In Certain Type Assets", false, 1)]
    static private void FindAssetInCertainAssets()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path))
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            List<string> containExtensions = new List<string>();
            if (!ASSET_REFERENCE_DIC.TryGetValue(obj.GetType(), out containExtensions))
            {
                Debug.LogWarning("The type of the selected asset has not reference Asset");
                return;
            }

            FindReferenceAsset(path, containExtensions, (file) =>
            {
                Debug.Log(GetRelativeAssetsPath(file));
            });

        }
    }

    [MenuItem("LSX/Find Asset Reference In Prefab", false, 1)]
    static private void FindAssetInPrefab()
    {
         EditorSettings.serializationMode = SerializationMode.ForceText;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path))
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            List<string> containExtensions = new List<string> {".prefab"};
            FindReferenceAsset(path, containExtensions, (file) =>
            {
                var relativePath = GetRelativeAssetsPath(file);
                var go = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath) as GameObject;
                if (obj as Material)
                {
                    var renders = go.GetComponentsInChildren<Renderer>();
                    foreach(var render in renders)
                    {
                        var materials = render.sharedMaterials.ToList();
                        if (materials.Contains(obj as Material))
                        {
                            var namePath = GetChildToRootPath(render.gameObject);
                            Debug.Log(string.Format("In Prefab {0}  <{1}> contain the select asset", relativePath, namePath));
                        }
                            
                    }
                }
                
            });
        }
    }

}
