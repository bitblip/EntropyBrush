using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

/// <summary>
/// Custom editor to illustrate the generated adjencies
/// </summary>
[CustomEditor(typeof(TileMapAdjacencyData), true)]
public class TilemapAdjacencyMatrixEditor : Editor
{
    SerializedProperty adjM;

    void OnEnable()
    {
        adjM = serializedObject.FindProperty("Adjacencies");
    }

    public override void OnInspectorGUI()
    {
        //serializedObject.Update();
        //EditorGUILayout.PropertyField(adjM);
        //serializedObject.ApplyModifiedProperties();

        if(serializedObject.targetObject is TileMapAdjacencyData m && m.AdjacenciesList != null)
        {
            foreach(var t in m.AdjacenciesList)
            {
                if(t.Tile == null)
                {
                    continue;
                }

                WaveBrushUtil.DrawAdjInspector(t, m);

                GUILayout.Space(15);
            }
        }

        //base.OnInspectorGUI();
    }

    private string GetRowName(int r)
    {
        // Hack
        // TODO: Accomidate other dimentions
        switch (r)
        {
            case 0:
                return "N";
            case 1:
                return "E";
            case 2:
                return "S";
            case 3:
                return "W";
            default:
                return "";
        }
    }
}
