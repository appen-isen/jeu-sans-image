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
    Material editorMaterial;
    float pixelsPerUnit = 100.0f;
    Transform parentTransform;
    float meshScale = 1f;
    VectorUtils.TessellationOptions tessOptions = new VectorUtils.TessellationOptions() {
        StepDistance = 1.0f,
        MaxCordDeviation = 0.5f,
        MaxTanAngleDeviation = 0.1f,
        SamplingStepSize = 0.01f
    };
    Quaternion meshRotation = Quaternion.identity;

    [SerializeField] private ColorFolderMap colorFolderMap;

    [MenuItem("Tools/SVG → Flat Mesh Regions")]
    static void OpenWindow() {
        var w = GetWindow<SvgToFlatMeshEditor>("SVG → Flat Mesh");
        w.minSize = new Vector2(460, 320);
    }

    void OnGUI() {
        EditorGUILayout.LabelField("SVG → Flat Mesh (separate GameObjects per fill color)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        svgFile = (TextAsset)EditorGUILayout.ObjectField("SVG File (.svg)", svgFile, typeof(TextAsset), false);
        editorMaterial = (Material)EditorGUILayout.ObjectField("Default Material", editorMaterial, typeof(Material), false);
        pixelsPerUnit = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit", "Rasterization scale used by VectorUtils. Higher = more detail"), pixelsPerUnit);
        meshScale = EditorGUILayout.FloatField(new GUIContent("Global Mesh Scale", "Scale applied to resulting mesh in world units"), meshScale);
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tessellation Options", EditorStyles.boldLabel);
        tessOptions.StepDistance = EditorGUILayout.FloatField(new GUIContent("Step Distance", "From manual: The uniform tessellation step distance."), tessOptions.StepDistance);
        tessOptions.MaxCordDeviation = EditorGUILayout.FloatField(new GUIContent("Max Cord Deviation", "From manual: The maximum distance on the cord to a straight line between to points after which more tessellation will be generated"), tessOptions.MaxCordDeviation);
        tessOptions.MaxTanAngleDeviation = EditorGUILayout.FloatField(new GUIContent("Max Tan Angle Deviation", "From manual: The maximum angle (in degrees) between the curve tangent and the next point after which more tessellation will be generated"), tessOptions.MaxTanAngleDeviation);
        tessOptions.SamplingStepSize = EditorGUILayout.FloatField(new GUIContent("Sampling Step Size", "From manual: The number of samples used internally to evaluate the curves. More samples = higher quality. Should be between 0 and 1 (inclusive)"), tessOptions.SamplingStepSize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map rotation options", EditorStyles.boldLabel);
        meshRotation = Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent("Mesh Rotation (degrees)", "Rotation to apply to the generated meshes"), meshRotation.eulerAngles));
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Meshes from SVG")) {
            if (svgFile == null) {
                EditorUtility.DisplayDialog("Error", "Please assign an SVG (.svg) TextAsset.", "OK");
            }
            else {
                try {
                    GenerateMeshesFromSVG();
                }
                catch (Exception e) {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog("Error", "Exception: " + e.Message, "OK");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool creates one GameObject per fill-color region in the SVG. Each GameObject receives a MeshRenderer + MeshFilter and a MeshCollider. Output meshes lie flat on the XZ plane (Y = 0).", MessageType.Info);
    }

    void GenerateMeshesFromSVG() {
        // Parse SVG
        var svgText = svgFile.text;
        if (string.IsNullOrEmpty(svgText)) {
            EditorUtility.DisplayDialog("Error", "SVG file is empty.", "OK");
            return;
        }

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svgText));
        if (sceneInfo.Equals(default(SVGParser.SceneInfo)) || sceneInfo.Scene == null) {
            EditorUtility.DisplayDialog("Error", "Failed to import SVG. Make sure the file is valid and Vector Graphics package is installed.", "OK");
            return;
        }

        Vector2 sceneCenter = VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root).center;

        // Gather shapes by fill color. We'll traverse the scene tree.
        var shapesByColor = new Dictionary<Color, List<SceneNodeShapeEntry>>(new ColorEqualityComparer());
        var wallsByColor = new Dictionary<Color, List<BezierPathSegment[]>>(new ColorEqualityComparer());

        TraverseAndCollectShapes(sceneInfo.Scene.Root, Matrix2D.identity, shapesByColor, wallsByColor);

        if (shapesByColor.Count == 0) {
            EditorUtility.DisplayDialog("Result", "No filled shapes found in the SVG.", "OK");
            return;
        }

        if (wallsByColor.Count == 0)
        {
            EditorUtility.DisplayDialog("Result", "No wall shapes found in the SVG.", "OK");
            return;
        }

        // Create parent container
        GameObject container = new GameObject(Path.GetFileNameWithoutExtension(svgFile.name) + "_SVG_Meshes");
        if (parentTransform != null) {
            container.transform.SetParent(parentTransform, false);
        }

        // For each color group, tessellate shapes into geometry and build a mesh
        foreach (var kv in shapesByColor) {
            Color color = kv.Key;
            List<SceneNodeShapeEntry> entries = kv.Value;
            List<VectorUtils.Geometry> geoms = TesselateIntoGeometries(entries);

            if (geoms == null || geoms.Count == 0) {
                Debug.LogWarning($"No geometry generated for color {color} (skipping).");
                continue;
            }

            // Build Mesh from geoms
            Mesh mesh = BuildMeshFromGeometries(geoms, sceneCenter, meshScale);

            // Create GameObject for this color region
            string objectName = BuildObjectName(color, "Floor");
            BuildGameObject(objectName, color, mesh, container);
        }

        foreach (var kv in wallsByColor)
        {
            Color color = kv.Key;
            List<BezierPathSegment[]> entries = kv.Value;

            // Build Mesh from 
            Mesh mesh = BuildExtrudedMeshFromBeziers(entries, sceneCenter, meshScale, 5.0f);

            // Create GameObject for this wall color region
            string objectName = BuildObjectName(color, "Wall");
            BuildGameObject(objectName, color, mesh, container);
        }

        // Focus selection on created container
        Selection.activeGameObject = container;
        EditorUtility.DisplayDialog("Done", $"Generated {shapesByColor.Count} region GameObjects under '{container.name}'.", "OK");
    }

    // Tesselate a list of SceneNodeShapeEntry into VectorUtils.Geometry list
    List<VectorUtils.Geometry> TesselateIntoGeometries(List<SceneNodeShapeEntry> entries) {
        // Build a temporary scene that contains all these shapes combined (preserving transforms)
        Scene tmpScene = new Scene();
        tmpScene.Root = new SceneNode();
        tmpScene.Root.Children = new List<SceneNode>();

        foreach (var entry in entries) {
            // create a shallow copy Node with transform and the original shapes (the shape objects can be reused)
            SceneNode copyNode = new SceneNode() {
                Transform = entry.Node.Transform, // keep transform
                Shapes = new List<Shape>() { entry.Shape }
            };
            tmpScene.Root.Children.Add(copyNode);
        }

        // Tessellate the tmpScene
        return VectorUtils.TessellateScene(tmpScene, tessOptions);
    }

    string BuildObjectName(Color color, string prefix)
    {
        string colorName = ColorToName(color);
        return $"{prefix}_{colorName}";
    }

    void BuildGameObject(string objectName, Color color, Mesh mesh, GameObject container)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(container.transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        if (editorMaterial != null) {
            // instantiate a material so each region can have its own color without overwriting the original asset
            Material matInstance = new Material(editorMaterial);
            matInstance.color = color;
            mr.sharedMaterial = matInstance;
        }
        else {
            // Create a quick default material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mr.sharedMaterial = mat;
        }

        // Generate collider
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false; // keep non-convex for flat terrain; set to true if needed for rigidbodies

        // Add tag to disable mesh renderer before build
        go.tag = "EditorOnlyMeshRenderer";

        // Automatically assign audio triggers based on color
        string folder = colorFolderMap.GetFolder(color);
        if (folder != null)
        {
            // TODO: automatically assign audio triggers
        }
    }

    // Recursively traverse scene nodes and collect filled shapes and walls by color
    void TraverseAndCollectShapes(SceneNode node, Matrix2D parentTransform, Dictionary<Color, List<SceneNodeShapeEntry>> shapesByColor, Dictionary<Color, List<BezierPathSegment[]>> wallsByColor) {
        if (node == null) {
            return;
        }

        // Combine transforms (VectorGraphics uses Matrix2D)
        Matrix2D currentTransform = parentTransform * node.Transform;

        if (node.Shapes != null && node.Shapes.Count > 0) {
            foreach (var shape in node.Shapes) {
                if (shape == null) {
                    continue;
                }
                // Only treat fills (SolidFill) for floors
                if (shape.Fill is SolidFill sf) {
                    Color col = sf.Color;
                    // Note: color comes as linear RGBA. Convert to Unity's Color (already same type)
                    if (!shapesByColor.TryGetValue(col, out List<SceneNodeShapeEntry> list)) {
                        list = new List<SceneNodeShapeEntry>();
                        shapesByColor[col] = list;
                    }

                    // Store the shape together with a node that carries the proper transform
                    SceneNode fakeNode = new SceneNode() {
                        Transform = currentTransform,
                        Shapes = new List<Shape>() { shape }
                    };
                    list.Add(new SceneNodeShapeEntry() { Node = fakeNode, Shape = shape });
                }
                
                // Treat contours as walls, and only those with stroke color defined
                if (shape.Contours != null && shape.Contours.Length > 0 && shape.PathProps.Stroke != null)
                {
                    Color wallColor = shape.PathProps.Stroke.Color;
                    if (!wallsByColor.TryGetValue(wallColor, out List<BezierPathSegment[]> wallList)) {
                        wallList = new List<BezierPathSegment[]>();
                        wallsByColor[wallColor] = wallList;
                    }

                    // Add all contours as wall segments
                    foreach (BezierContour contour in shape.Contours)
                    {
                        wallList.Add(contour.Segments);
                    }
                }
            }
        }

        if (node.Children != null && node.Children.Count > 0) {
            foreach (var c in node.Children) {
                TraverseAndCollectShapes(c, currentTransform, shapesByColor, wallsByColor);
            }
        }
    }

    // Build a Mesh from VectorUtils.Geometry list
    Mesh BuildMeshFromGeometries(List<VectorUtils.Geometry> geoms, Vector2 geomsCenter, float globalScale) {
        // Forget about UVs (unnecessary for our use case)
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        int baseIndex = 0;
        foreach (VectorUtils.Geometry g in geoms) {
            if (g == null || g.Vertices == null || g.Indices == null) {
                continue;
            }

            // Add vertices (VectorUtils uses Vector2 for geometry XY)
            for (int i = 0; i < g.Vertices.Length; i++) {
                Vector2 v2 = g.Vertices[i];
                
                // Map XY -> XZ plane; Y = 0
                Vector3 v3 = new Vector3(v2.x-geomsCenter.x, 0f, -v2.y+geomsCenter.y) * globalScale;
                v3 = meshRotation * v3; // Apply rotation
                verts.Add(v3);
            }

            // Add indices (triangles)
            for (int i = 0; i < g.Indices.Length; i += 3) {
                int i1 = baseIndex + g.Indices[i];
                int i2 = baseIndex + g.Indices[i + 1];
                int i3 = baseIndex + g.Indices[i + 2];
                if (!IsClockwise( g.Vertices[i1], g.Vertices[i2], g.Vertices[i3] ))
                {
                    // Add triangle with reversed winding
                    indices.Add(i1);
                    indices.Add(i3);
                    indices.Add(i2);
                }
                else
                {
                    // Add triangle with correct winding
                    indices.Add(i3);
                    indices.Add(i1);
                    indices.Add(i2);
                }
            }

            baseIndex += g.Vertices.Length;
        }

        Mesh mesh = new Mesh();
        mesh.name = "SVG_Mesh";
        mesh.indexFormat = (verts.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Helper to determine if 3 points are in clockwise order
    bool IsClockwise(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
    }

    // Build an extruded Mesh from geometries
    Mesh BuildExtrudedMeshFromBeziers(List<BezierPathSegment[]> beziers, Vector2 geomsCenter, float globalScale, float height){
        // Forget about UVs (unnecessary for our use case)
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        Vector3 geomsCenter3D = new Vector3(geomsCenter.x, 0f, -geomsCenter.y);

        // Treat each path separately as a closed shape to extrude
        foreach (BezierPathSegment[] bezier in beziers)
        {
            // Add vertices: low and high for each point
            for (int i=0; i<bezier.Length; i++)
            {
                Vector2 v2 = bezier[i].P0;
                Vector3 v3_low = meshRotation * (new Vector3(v2.x, 0f, -v2.y) - geomsCenter3D) * globalScale;
                Vector3 v3_high = meshRotation * (new Vector3(v2.x, height, -v2.y) - geomsCenter3D) * globalScale;
                verts.Add(v3_low);
                verts.Add(v3_low);  // Back face duplicate
                verts.Add(v3_high);
                verts.Add(v3_high); // Back face duplicate
            }

            // Add indices for triangles
            for (int i = 0; i < bezier.Length; i++)
            {
                int next_i = (i + 1) % bezier.Length;

                // Each quad between points i and nextI is made of two triangles, double sided
                int low0 = i*4;
                int high0 = i*4 +2;
                int low1 = next_i*4;
                int high1 = next_i*4 +2;

                // Triangle 1
                indices.Add(low0);
                indices.Add(high0);
                indices.Add(low1);

                // Triangle 2
                indices.Add(high0);
                indices.Add(high1);
                indices.Add(low1);

                // Triangle 1 (back face)
                indices.Add(low1 +1);
                indices.Add(high0 +1);
                indices.Add(low0 +1);

                // Triangle 2 (back face)
                indices.Add(low1 +1);
                indices.Add(high1 +1);
                indices.Add(high0 +1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SVG_ExtrudedMesh";
        mesh.indexFormat = (verts.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Helper to produce a safe string for color names
    string ColorToName(Color c) {
        // Try to present RGBA hex
        Color32 cc = c;
        return $"{cc.r:X2}{cc.g:X2}{cc.b:X2}{(cc.a < 255 ? cc.a.ToString("X2") : "")}";
    }

    // Small helper type to keep a shape and associated node (with transform)
    class SceneNodeShapeEntry {
        public SceneNode Node;
        public Shape Shape;
    }

    // Simple color comparer for use as Dictionary key
    class ColorEqualityComparer : IEqualityComparer<Color> {
        public bool Equals(Color x, Color y) {
            // Compare with exactness; you could add tolerance if you want near-colors grouped
            return Mathf.Approximately(x.r, y.r) && Mathf.Approximately(x.g, y.g) &&
                   Mathf.Approximately(x.b, y.b) && Mathf.Approximately(x.a, y.a);
        }

        public int GetHashCode(Color obj) {
            unchecked {
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
