using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor to show button that will generate adjencies
/// </summary>
[CustomEditor(typeof(TilemapAdjacenciesGenerator), true)]
public class TilemapAdjacencyGeneratorEditor : Editor
{
    SerializedProperty genProp;

    void OnEnable()
    {
        genProp = serializedObject.FindProperty(nameof(TilemapAdjacenciesGenerator.BrushData));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(genProp);

        if (genProp?.objectReferenceValue is Object p)
        {
            var mEditor = Editor.CreateEditor(p);
            mEditor.OnInspectorGUI();
        }

        if (GUILayout.Button("Generate"))
        {
            if (serializedObject.targetObject is TilemapAdjacenciesGenerator generator)
            {
                if(generator.Generate())
                {
                    EditorUtility.SetDirty(genProp.objectReferenceValue);
                    AssetDatabase.SaveAssetIfDirty(genProp.objectReferenceValue);
                    AssetDatabase.Refresh();
                }
            }
        }
        serializedObject.ApplyModifiedProperties();

    }
}
