using System.IO;
using UnityEditor;
using UnityEngine;

public class ModifyReadWrite : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Assets/Modify Read/Write To False")]
    static void ModifySelectedAssetsToFalse()
    {
        // Get all currently selected objects in the Unity Editor
        Object[] selectedObjects = Selection.objects;

        foreach (Object selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                // Get the path of the selected asset
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);

                // Read the content of the asset as text
                string[] lines = File.ReadAllLines(assetPath);

                bool modified = false;

                // Modify the content as needed
                for (int i = 0; i < lines.Length; i++)
                {
                    Debug.Log(lines[i]);
                    if (lines[i].Contains("m_IsReadable: 1"))
                    {
                        // Modify the value here (change 1 to 0)
                        lines[i] = lines[i].Replace("m_IsReadable: 1", "m_IsReadable: 0");
                        modified = true;
                    }
                    else if (lines[i].Contains("m_IsReadable: 0"))
                    {
                        // If already "m_IsReadable: 0", skip this line
                        continue;
                    }
                }

                if (modified)
                {
                    // Write the modified content back to the asset
                    File.WriteAllLines(assetPath, lines);

                    // Refresh the asset database to reflect the changes in the Unity Editor
                    AssetDatabase.Refresh();
                    
                    Debug.Log("Modified false");
                }
            }
        }
    }
    
    [MenuItem("Assets/Modify Read/Write To False The Other Way")]
    static void ModifySelectedAssetsToFalseTheOtherWay()
    {
        // Get all currently selected objects in the Unity Editor
        Object[] selectedObjects = Selection.objects;

        foreach (Object selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                // Get the path of the selected asset
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                

                // Modify the content as needed
                var loadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                loadedMesh.UploadMeshData(false);
                EditorUtility.SetDirty(loadedMesh);
                AssetDatabase.SaveAssets();
                
                // Refresh the asset database to reflect the changes in the Unity Editor
                AssetDatabase.Refresh();
                Debug.Log("Modified true");
            }
        }
    }

    [MenuItem("Assets/Modify Read/Write To True")]
    static void ModifySelectedAssetsToTrue()
    {
        // Get all currently selected objects in the Unity Editor
        Object[] selectedObjects = Selection.objects;

        foreach (Object selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                // Get the path of the selected asset
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);

                // Read the content of the asset as text
                string[] lines = File.ReadAllLines(assetPath);

                bool modified = false;

                // Modify the content as needed
                for (int i = 0; i < lines.Length; i++)
                {
                    Debug.Log(lines[i]);
                    if (lines[i].Contains("m_IsReadable: 0"))
                    {
                        // Modify the value here (change 0 to 1)
                        lines[i] = lines[i].Replace("m_IsReadable: 0", "m_IsReadable: 1");
                        modified = true;
                    }
                    else if (lines[i].Contains("m_IsReadable: 1"))
                    {
                        // If already "m_IsReadable: 1", skip this line
                        continue;
                    }
                }

                if (modified)
                {
                    // Write the modified content back to the asset
                    File.WriteAllLines(assetPath, lines);

                    // Refresh the asset database to reflect the changes in the Unity Editor
                    AssetDatabase.Refresh();
                    Debug.Log("Modified true");
                }
            }
        }
    }
    
    [MenuItem("Assets/Modify Read/Write To True The Other Way")]
    static void ModifySelectedAssetsToTrueTheOtherWay()
    {
        // Get all currently selected objects in the Unity Editor
        Object[] selectedObjects = Selection.objects;

        foreach (Object selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                // Get the path of the selected asset
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                

                // Modify the content as needed
                var loadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                loadedMesh.UploadMeshData(true);
                EditorUtility.SetDirty(loadedMesh);
                AssetDatabase.SaveAssets();
                
                // Refresh the asset database to reflect the changes in the Unity Editor
                AssetDatabase.Refresh();
                Debug.Log("Modified true");
            }
        }
    }
#endif
}
