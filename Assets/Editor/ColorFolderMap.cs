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

    public List<Entry> entries = new List<Entry>();

    /// Returns folder path for color, or null if not found (Editor + Runtime safe)
    public string GetFolder(Color color) {
        string hexColor = color.ToHexString();
        foreach (var entry in entries) {
            if (entry.color.ToHexString() == hexColor) {
                return SoundTexturesFolder + entry.folderName;
            }
        }

        return null;
    }
}
