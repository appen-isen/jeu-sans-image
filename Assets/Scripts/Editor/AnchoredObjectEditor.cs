using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AnchoredObject))]
public class AnchoredObjectEditor : Editor
{
    [Header("Connect via editor")]
    Anchor myAnchor;
    Anchor partnerAnchor;
    bool matchScales = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Connect the anchors directly via the editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        myAnchor = (Anchor) EditorGUILayout.ObjectField("My Anchor", myAnchor, typeof(Anchor), true);
        partnerAnchor = (Anchor) EditorGUILayout.ObjectField("Partner Anchor", partnerAnchor, typeof(Anchor), true);
        matchScales = EditorGUILayout.Toggle("Match Scales?", matchScales);

        AnchoredObject obj = (AnchoredObject)target;

        if (GUILayout.Button("Connect anchors"))
        {
            if (!myAnchor || !partnerAnchor)
            {
                Debug.LogWarning("Anchors not assigned", this);
                return;
            }
            obj.AnchorTo(myAnchor, partnerAnchor, matchScales);
        }
    }
}


#endif