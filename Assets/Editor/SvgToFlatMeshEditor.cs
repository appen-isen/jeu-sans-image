// SvgToFlatMeshEditor.cs
// Save to Assets/Editor/
// Requires com.unity.vectorgraphics package.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.VectorGraphics; // From com.unity.vectorgraphics

public class SvgToFlatMeshEditor : EditorWindow
{
    TextAsset svgFile;
    float pixelsPerUnit = 100.0f;
    Transform parentTransform;
    float meshScale = 1f;
    VectorUtils.TessellationOptions tessOptions = new VectorUtils.TessellationOptions()
    {
        StepDistance = 1.0f,
        MaxCordDeviation = 0.5f,
        MaxTanAngleDeviation = 0.1f,
        SamplingStepSize = 0.01f
    };

    [MenuItem("Tools/SVG → Flat Mesh Regions")]
    static void OpenWindow()
    {
        var w = GetWindow<SvgToFlatMeshEditor>("SVG → Flat Mesh");
        w.minSize = new Vector2(460, 320);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("SVG → Flat Mesh (separate GameObjects per fill color)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        svgFile = (TextAsset)EditorGUILayout.ObjectField("SVG File (.svg)", svgFile, typeof(TextAsset), false);
        pixelsPerUnit = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit", "Rasterization scale used by VectorUtils. Higher = more detail"), pixelsPerUnit);
        meshScale = EditorGUILayout.FloatField(new GUIContent("Global Mesh Scale", "Scale applied to resulting mesh in world units"), meshScale);
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tessellation Options", EditorStyles.boldLabel);
        tessOptions.StepDistance = EditorGUILayout.FloatField("Step Distance", tessOptions.StepDistance);
        tessOptions.MaxCordDeviation = EditorGUILayout.FloatField("Max Cord Deviation", tessOptions.MaxCordDeviation);
        tessOptions.MaxTanAngleDeviation = EditorGUILayout.FloatField("Max Tan Angle Deviation", tessOptions.MaxTanAngleDeviation);
        tessOptions.SamplingStepSize = EditorGUILayout.FloatField("Sampling Step Size", tessOptions.SamplingStepSize);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Meshes from SVG"))
        {
            if (svgFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an SVG (.svg) TextAsset.", "OK");
            }
            else
            {
                try
                {
                    GenerateMeshesFromSVG();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog("Error", "Exception: " + e.Message, "OK");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool creates one GameObject per fill-color region in the SVG. Each GameObject receives a MeshRenderer + MeshFilter and, optionally, a MeshCollider. Output meshes lie flat on the XZ plane (Y = 0).", MessageType.Info);
    }

    void GenerateMeshesFromSVG()
    {
        // Parse SVG
        var svgText = svgFile.text;
        if (string.IsNullOrEmpty(svgText))
        {
            EditorUtility.DisplayDialog("Error", "SVG file is empty.", "OK");
            return;
        }

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svgText));
        if (sceneInfo.Equals(default(SVGParser.SceneInfo)) || sceneInfo.Scene == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to import SVG. Make sure the file is valid and Vector Graphics package is installed.", "OK");
            return;
        }

        // Gather shapes by fill color. We'll traverse the scene tree.
        var shapesByColor = new Dictionary<Color, List<SceneNodeShapeEntry>>(new ColorEqualityComparer());

        TraverseAndCollectShapes(sceneInfo.Scene.Root, Matrix2D.identity, shapesByColor);

        if (shapesByColor.Count == 0)
        {
            EditorUtility.DisplayDialog("Result", "No filled shapes found in the SVG.", "OK");
            return;
        }

        // Create parent container
        GameObject container = new GameObject(Path.GetFileNameWithoutExtension(svgFile.name) + "_SVG_Meshes");
        if (parentTransform != null) container.transform.SetParent(parentTransform, false);

        // For each color group, tessellate shapes into geometry and build a mesh
        foreach (var kv in shapesByColor)
        {
            Color color = kv.Key;
            List<SceneNodeShapeEntry> entries = kv.Value;

            // Build a temporary scene that contains all these shapes combined (preserving transforms)
            Scene tmpScene = new Scene();
            tmpScene.Root = new SceneNode();
            tmpScene.Root.Children = new List<SceneNode>();

            foreach (var entry in entries)
            {
                // create a shallow copy Node with transform and the original shapes (the shape objects can be reused)
                SceneNode copyNode = new SceneNode()
                {
                    Transform = entry.Node.Transform, // keep transform
                    Shapes = new List<Shape>() { entry.Shape }
                };
                tmpScene.Root.Children.Add(copyNode);
            }

            // Tessellate the tmpScene
            var geoms = VectorUtils.TessellateScene(tmpScene, tessOptions);

            if (geoms == null || geoms.Count == 0)
            {
                Debug.LogWarning($"No geometry generated for color {color} (skipping).");
                continue;
            }

            // Build Mesh from geoms
            Mesh mesh = BuildMeshFromGeometries(geoms, meshScale);

            // Create GameObject for this color region
            string colorName = ColorToName(color);
            GameObject go = new GameObject($"Region_{colorName}");
            go.transform.SetParent(container.transform, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // Generate collider
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false; // keep non-convex for flat terrain; set to true if needed for rigidbodies

            // TODO: automatically assign audio triggers based on color
        }

        // Focus selection on created container
        Selection.activeGameObject = container;
        EditorUtility.DisplayDialog("Done", $"Generated {shapesByColor.Count} region GameObjects under '{container.name}'.", "OK");
    }

    // Recursively traverse scene nodes and collect filled shapes
    void TraverseAndCollectShapes(SceneNode node, Matrix2D parentTransform, Dictionary<Color, List<SceneNodeShapeEntry>> shapesByColor)
    {
        if (node == null) return;

        // Combine transforms (VectorGraphics uses Matrix2D)
        Matrix2D currentTransform = parentTransform * node.Transform;

        if (node.Shapes != null && node.Shapes.Count > 0)
        {
            foreach (var shape in node.Shapes)
            {
                if (shape == null) continue;
                // Only treat fills (SolidFill)
                if (shape.Fill is SolidFill sf)
                {
                    Color col = sf.Color;
                    // Note: color comes as linear RGBA. Convert to Unity's Color (already same type)
                    if (!shapesByColor.TryGetValue(col, out List<SceneNodeShapeEntry> list))
                    {
                        list = new List<SceneNodeShapeEntry>();
                        shapesByColor[col] = list;
                    }

                    // Store the shape together with a node that carries the proper transform
                    SceneNode fakeNode = new SceneNode()
                    {
                        Transform = currentTransform,
                        Shapes = new List<Shape>() { shape }
                    };
                    list.Add(new SceneNodeShapeEntry() { Node = fakeNode, Shape = shape });
                }
            }
        }

        if (node.Children != null && node.Children.Count > 0)
        {
            foreach (var c in node.Children)
                TraverseAndCollectShapes(c, currentTransform, shapesByColor);
        }
    }

    // Build a Mesh from VectorUtils.Geometry list
    Mesh BuildMeshFromGeometries(List<VectorUtils.Geometry> geoms, float globalScale)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();

        int baseIndex = 0;
        foreach (var g in geoms)
        {
            if (g == null || g.Vertices == null || g.Indices == null) continue;

            // Add vertices (VectorUtils uses Vector2 for geometry XY)
            for (int i = 0; i < g.Vertices.Length; i++)
            {
                var v2 = g.Vertices[i];
                // Map XY -> XZ plane; Y = 0
                Vector3 v3 = new Vector3(v2.x, 0f, v2.y) * globalScale;
                verts.Add(v3);

                // UVs: If geometry provides UV, use it; otherwise use XY mapped to UV
                if (g.UVs != null && g.UVs.Length == g.Vertices.Length)
                    uvs.Add(g.UVs[i]);
                else
                    uvs.Add(new Vector2(v2.x, v2.y));
            }

            // Add indices (triangles)
            for (int i = 0; i < g.Indices.Length; i += 3)
            {
                // VectorUtils yields triangles in clockwise winding; Unity expects clockwise for front? We'll keep it.
                indices.Add(baseIndex + g.Indices[i]);
                indices.Add(baseIndex + g.Indices[i + 1]);
                indices.Add(baseIndex + g.Indices[i + 2]);
            }

            baseIndex += g.Vertices.Length;
        }

        Mesh mesh = new Mesh();
        mesh.name = "SVG_Mesh";
        mesh.indexFormat = (verts.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);
        if (uvs != null && uvs.Count == verts.Count)
            mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Helper to produce a safe string for color names
    string ColorToName(Color c)
    {
        // Try to present RGBA hex
        Color32 cc = c;
        return $"{cc.r:X2}{cc.g:X2}{cc.b:X2}{(cc.a < 255 ? cc.a.ToString("X2") : "")}";
    }

    // Small helper type to keep a shape and associated node (with transform)
    class SceneNodeShapeEntry
    {
        public SceneNode Node;
        public Shape Shape;
    }

    // Simple color comparer for use as Dictionary key
    class ColorEqualityComparer : IEqualityComparer<Color>
    {
        public bool Equals(Color x, Color y)
        {
            // Compare with exactness; you could add tolerance if you want near-colors grouped
            return Mathf.Approximately(x.r, y.r) && Mathf.Approximately(x.g, y.g) &&
                   Mathf.Approximately(x.b, y.b) && Mathf.Approximately(x.a, y.a);
        }

        public int GetHashCode(Color obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Mathf.RoundToInt(obj.r * 255f);
                hash = hash * 23 + Mathf.RoundToInt(obj.g * 255f);
                hash = hash * 23 + Mathf.RoundToInt(obj.b * 255f);
                hash = hash * 23 + Mathf.RoundToInt(obj.a * 255f);
                return hash;
            }
        }
    }
}
