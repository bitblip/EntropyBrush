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

                GUILayout.BeginHorizontal();

                Texture2D texture = AssetPreview.GetAssetPreview(t.Tile);
                GUILayout.Label("", GUILayout.Height(30), GUILayout.Width(30));
                if(texture != null)
                {
                    //Draws the texture where we have defined our Label (empty space)
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
                }

                GUILayout.Label(t.Tile.name);
                GUILayout.Label(t.Sum.ToString());
                GUILayout.EndHorizontal();

                // Draw header
                GUILayout.BeginHorizontal();
                // First column is the direction
                GUILayout.Label(" ", GUILayout.Height(30));
                foreach (var c in m.AdjacenciesList)
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(c.Tile);
                    GUILayout.Label(" ", GUILayout.Height(30));
                    if (tex != null)
                    {
                        var lastRec = GUILayoutUtility.GetLastRect();
                        lastRec.width = 30;
                        //Draws the texture where we have defined our Label (empty space)
                        GUI.DrawTexture(lastRec, tex);
                    }
                }
                // Last column is the total
                GUILayout.Label("Total", GUILayout.Height(30));
                GUILayout.EndHorizontal();

                foreach (var r in t.Rows)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(GetRowName(t.Rows.IndexOf(r)));
                    {
                        foreach(var c in r.Column)
                        {
                            GUILayout.Label(c.ToString("0.###"));
                        }
                    }
                    // row total
                    GUILayout.Label(r.Total().ToString());
                    GUILayout.EndHorizontal();
                }

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
