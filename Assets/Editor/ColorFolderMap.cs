using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorFolderMap", menuName = "Config/Color Folder Map")]
public class ColorFolderMap : ScriptableObject {
    [Serializable]
    public struct Entry {
        public Color color;
        public string folderName;  // Example: "Gravel"
    }

    private const string SoundTexturesFolder = "Sound/Textures/";
    private const string ResourcesFolderPath = "Assets/Resources/";

    public List<Entry> entries = new List<Entry>();

    /// Returns folder path for color, or null if config not set (Editor + Runtime safe)
    /// Creates the folder if it does not exist
    public string GetFolder(Color color) {
        string hexColor = color.ToHexString();
        foreach (var entry in entries) {
            if (entry.color.ToHexString() == hexColor) {
                CreateFolderIfNonExistent(entry.folderName);
                return SoundTexturesFolder + entry.folderName;
            }
        }

        return null;
    }

    private void CreateFolderIfNonExistent(string folderName) {
        #if UNITY_EDITOR    // AssetDatabase is Editor-only
        if (!UnityEditor.AssetDatabase.IsValidFolder(ResourcesFolderPath + SoundTexturesFolder + folderName)) {
            UnityEditor.AssetDatabase.CreateFolder(ResourcesFolderPath + SoundTexturesFolder, folderName);
            UnityEditor.AssetDatabase.Refresh();
        }
        #endif
    }
}
