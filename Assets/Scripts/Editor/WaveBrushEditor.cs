using Codice.Client.BaseCommands;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.FilePathAttribute;
using System.Linq;

[CustomEditor(typeof(WaveBrush))]
public class WaveBrushEditor : GridBrushEditor
{
    private WaveBrush waveBrush { get { return target as WaveBrush; } }

    public override void OnSelectionInspectorGUI()
    {
        GUILayout.Label("Wave tile inspect?");
        base.OnSelectionInspectorGUI();
    }

    public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        base.OnPaintSceneGUI(grid, brushTarget, position, tool, executing);


        var tileMap = brushTarget.GetComponent<Tilemap>();
        tileMap.ClearAllEditorPreviewTiles();
        // I just wana draw a little square
        if (tool != GridBrushBase.Tool.Paint)
            return;
        var pos = position.min;

        // Preview what you intend to paint
        // I'm not sure why calling base doesn't do this
        var tile = (Tile)brush.cells[0].tile;
        tileMap.SetEditorPreviewTile(pos, tile);
        tileMap.SetEditorPreviewTransformMatrix(pos, tileMap.orientationMatrix);
        tileMap.SetEditorPreviewColor(pos, tile.color);

        // TODO: Not sure what it means to use the selection size... probably the size of the NxN collapse grid
        GridBrush.BrushCell cell = brush.cells[0];

        // TODO: When best to load data into lookup?
        waveBrush.TileData.PrimeDictionary();
        var tileWeights = waveBrush.TileData[(Tile)cell.tile];

        if (tileWeights == null)
            return;

        // Use weights to guess neighbors
        foreach (var n in waveBrush.TileData.NeighborVectors)
        {
            var p = pos + n;

            // Should I seed an entropy tile here? There might already be one there?

            // Can I draw multiple tiles with low opacity?
            var entpTile = CreateInstance<WaveTile>();
            entpTile.Data = waveBrush.TileData;
            entpTile.name = $"Entropy tile {p}";

            tileMap.SetEditorPreviewTile(p, entpTile);
            tileMap.SetEditorPreviewTransformMatrix(p, tileMap.orientationMatrix);
            tileMap.SetEditorPreviewColor(p, entpTile.color);

            // Mark these for delete?

            // The question is, what is the highest weighted tile in this direction
            var minDelta = float.MaxValue;
            Tile maxTile = null;
            var rowIndex = waveBrush.TileData.RowOf(n);
            var rand = Random.Range(0, 1f);
            for (int i = 0; i < tileWeights.Rows[rowIndex].Column.Length; i++)
            {
                var weightvalue = tileWeights.Rows[rowIndex].Column[i];

                if(weightvalue == 0)
                {
                    // So... 0 means there's no chance of this, it's really forbidden.
                    continue;
                }

                var weightChance = weightvalue / (float)tileWeights.Sum;
                var chanceDelta = Mathf.Abs(rand - weightChance);
                if (chanceDelta < minDelta)
                {
                    minDelta = chanceDelta;
                    maxTile = waveBrush.TileData.AdjacenciesList[i].Tile;
                }
            }


            if (maxTile != null)
            {
                // alpha?
                //var color = maxTile.color;
                //maxTile.color = maxTile.color.WithAlpha(.5f);
                //tileMap.SetEditorPreviewTile(p, maxTile);
                //maxTile.color = color;
                //tileMap.SetEditorPreviewTransformMatrix(p, tileMap.orientationMatrix);
                //tileMap.SetEditorPreviewColor(p, new Color(1,1,1,.5f));
            }

            //PaintPreview(grid, brushTarget, p + n);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Generate"))
        {
            // TODO: Relocate generate to the brush
            //if (generator.Generate())
            //{
            //    EditorUtility.SetDirty(genProp.objectReferenceValue);
            //    AssetDatabase.SaveAssetIfDirty(genProp.objectReferenceValue);
            //    AssetDatabase.Refresh();
            //}
        }
    }
}
