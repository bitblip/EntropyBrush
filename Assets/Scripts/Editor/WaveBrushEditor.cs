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

    private GameObject brushTarget => GridPaintingState.scenePaintTarget;

    private GridBrushEditorBase brushEditor => GridPaintingState.activeBrushEditor;

    public Tilemap tilemap
    {
        get
        {
            if (brushTarget != null)
            {
                return brushTarget.GetComponent<Tilemap>();
            }
            return null;
        }
    }

    public override void OnSelectionInspectorGUI()
    {

        var select = GridSelection.position;

        // Inspecting multiple isn't really helpful
        if (tilemap != null && select != null)
        {
            var t = tilemap.GetTile(select.min);
            if (t is WaveTile waveTile)
            {
                // Update state from neighbors real quick

                GUILayout.Label(waveTile.Position.ToString());

                WaveBrushUtil.DrawAdjInspector(waveTile.State, waveBrush.TileData);

                if(GUILayout.Button("Recompute"))
                {
                    waveTile.State = waveTile.ComputeStateMatrix(select.min, null, tilemap);
                }
            }
            else if(t is Tile tile && waveBrush.TileData[tile] is TileAdjacencyMatrix def)
            {
                WaveBrushUtil.DrawAdjInspector(def, waveBrush.TileData);
            }
        }

        base.OnSelectionInspectorGUI();
    }

    public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        base.OnPaintSceneGUI(grid, brushTarget, position, tool, executing);

        // Only support selecting one tile.
        if(brush.cellCount != 1)
        {
            Debug.LogWarning("Wave brush only supports selecting one tile at a time.");
            return;
        }

        // A tile must be selected
        if (!(brush.cells[0].tile is Tile))
        {
            return;
        }


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

        var cellVectors = waveBrush.GetRadiusBrushCells();

        foreach (var n in cellVectors)
        {
            var p = pos + n;

            // Don't preview ontop of existing tiles. thanks
            var existingTile = tileMap.GetTile(p);
            if (existingTile != null && !waveBrush.AutoCollapse)
            {
                continue;
            }

            // Don't overrwrite the sprite we're paiting with
            if(existingTile is Tile t && cell.tile is Tile bTile)
            {
                if(t.sprite == bTile.sprite)
                {
                    continue;
                }
            }

            // Should I seed an entropy tile here? There might already be one there?

            // Can I draw multiple tiles with low opacity?
            var entpTile = CreateInstance<WaveTile>();
            entpTile.Data = waveBrush.TileData;
            entpTile.name = $"Entropy tile {p}";

            tileMap.SetEditorPreviewTile(p, entpTile);
            tileMap.SetEditorPreviewTransformMatrix(p, tileMap.orientationMatrix);
            tileMap.SetEditorPreviewColor(p, entpTile.color);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Generate"))
        {
            var tileMap = GridPaintingState.palette.GetComponentInChildren<Tilemap>();
            if (waveBrush.TileData.Generate(tileMap))
            {
                EditorUtility.SetDirty(waveBrush.TileData);
                AssetDatabase.SaveAssetIfDirty(waveBrush.TileData);
                AssetDatabase.Refresh();
            }
        }
    }
}
