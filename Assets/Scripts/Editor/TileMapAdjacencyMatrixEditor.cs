using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor to illustrate the generated adjacencies
/// </summary>
[CustomEditor(typeof(TileMapAdjacencyData), true)]
public class TilemapAdjacencyMatrixEditor : Editor
{
    SerializedProperty adjM;

    void OnEnable()
    {
        adjM = serializedObject.FindProperty("Adjacencies");
    }

    /// <summary>
    /// Draw information about the selected adjacency asset
    /// </summary>
    public override void OnInspectorGUI()
    {

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

    }
}
