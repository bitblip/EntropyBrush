using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WaveBrushUtil
{
    public static void DrawAdjInspector(TileAdjacencyMatrix t, TileMapAdjacencyData data)
    {
        var width = Screen.width / 1.5f;
        var columnSize = width / (data.AdjacenciesList.Count + 3);


        GUILayout.BeginHorizontal();

        // Tile summary header
        GUILayout.Label("", GUILayout.Height(columnSize), GUILayout.Width(columnSize));
        Texture2D texture = null;
        if(t.Tile != null)
        {
            texture = AssetPreview.GetAssetPreview(t.Tile);
            if (texture != null)
            {
                //Draws the texture where we have defined our Label (empty space)
                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
            }
            GUILayout.Label(t.Tile.name);
        }

        GUILayout.Label(t.Sum.ToString());
        GUILayout.EndHorizontal();

        // Header row
        GUILayout.BeginHorizontal();
        // First column is the direction
        GUILayout.Label("", GUILayout.Height(columnSize), GUILayout.Width(columnSize));
        foreach (var c in data.AdjacenciesList)
        {
            texture = AssetPreview.GetAssetPreview(c.Tile);
            GUILayout.Label("", GUILayout.Height(columnSize), GUILayout.Width(columnSize));
            if (texture != null)
            {
                var lastRec = GUILayoutUtility.GetLastRect();
                lastRec.width = columnSize;
                //Draws the texture where we have defined our Label (empty space)
                GUI.DrawTexture(lastRec, texture);
            }
            GUILayout.Space(5);
        }
        // Last column is the total
        GUILayout.Label("Total", GUILayout.Height(30));
        GUILayout.EndHorizontal();

        foreach (var r in t.Rows)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetRowName(t.Rows.IndexOf(r)), GUILayout.Width(columnSize));
            {
                foreach (var c in r.Column)
                {
                    GUILayout.Label(c.ToString("0.###"), GUILayout.Width(columnSize));
                    GUILayout.Space(5);
                }
            }
            // row total
            GUILayout.Label(r.Total().ToString(), GUILayout.Width(columnSize));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(15);

        // tile weights
        GUILayout.BeginHorizontal();
        // First column is the direction
        GUILayout.Label("", GUILayout.Width(columnSize));
        {
            foreach(var weight in t.TileWeights)
            {                
                GUILayout.Label(weight.ToString("0.######"), GUILayout.Width(columnSize));
                GUILayout.Space(5);
            }
        }
        

        // Entropy
        GUILayout.Label(t.Entropy.ToString("0.###########"), GUILayout.Width(columnSize));
        GUILayout.EndHorizontal();
    }

    public static WaveTile GetMinEntropyTile(List<WaveTile> tiles)
    {
        if(tiles == null || tiles.Count == 0)
        {
            return null;
        }

        // This hurts, find the min entropy
        var minCanidates = new List<WaveTile>();
        float minEntropy = float.MaxValue;
        foreach (var t in tiles)
        {
            float bias = GetBias(t);
            if (t.State.Entropy + bias < minEntropy)
            {
                minEntropy = t.State.Entropy + bias + .01f;
                minCanidates.Add(t);
            }
        }

        // Since tiles could have the same entropy, pick randomly between them
        // first, exclude false positive min tiles
        var minTiles = new List<WaveTile>();
        foreach(var t in minCanidates)
        {
            var bias = GetBias(t);
            if (t.State.Entropy + bias < minEntropy)
            {
                minTiles.Add(t);
            }
        }

        // Whatever is left is tied for min
        var randIndex = Random.Range(0, minTiles.Count);

        return minTiles[randIndex];
    }

    private static float GetBias(WaveTile t)
    {
        // hack to focus around the origin
        return Mathf.Log(Mathf.Max(1, t.Position.magnitude));
    }

    private static string GetRowName(int r)
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
