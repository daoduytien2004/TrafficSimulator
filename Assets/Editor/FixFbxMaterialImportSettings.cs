using UnityEngine;
using UnityEditor;
using System.IO;

public class FixFbxMaterialImportSettings
{
    [MenuItem("Tools/Fix FBX Material Import Settings")]
    public static void FixSettings()
    {
        string path = "Assets/Fantastic City Generator";
        if (!Directory.Exists(path))
        {
            Debug.LogError($"Directory not found: {path}");
            return;
        }

        string[] fbxFiles = Directory.GetFiles(path, "*.fbx", SearchOption.AllDirectories);
        int total = fbxFiles.Length;
        int fixedCount = 0;

        Debug.Log($"Found {total} FBX files to process in {path}. Starting fix...");

        try
        {
            for (int i = 0; i < total; i++)
            {
                string file = fbxFiles[i];
                
                // Show a progress bar so the editor doesn't feel frozen
                EditorUtility.DisplayProgressBar(
                    "Fixing FBX Material Import Settings", 
                    $"Processing {Path.GetFileName(file)} ({i + 1}/{total})", 
                    (float)i / total
                );

                ModelImporter importer = AssetImporter.GetAtPath(file) as ModelImporter;
                if (importer != null)
                {
                    // Check if the material location is the obsolete legacy 'InPrefab' (0)
                    if (importer.materialLocation == ModelImporterMaterialLocation.InPrefab)
                    {
                        importer.materialLocation = ModelImporterMaterialLocation.External;
                        importer.SaveAndReimport();
                        fixedCount++;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Successfully fixed material import settings for {fixedCount} FBX models out of {total} total models!");
        
        EditorUtility.DisplayDialog(
            "Fix Completed", 
            $"Successfully fixed material import settings for {fixedCount} FBX models!\n\nYour city meshes should now successfully bind to their URP materials and display correctly.", 
            "Great!"
        );
    }
}
