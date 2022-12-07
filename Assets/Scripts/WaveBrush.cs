using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.Tilemaps;
using static UnityEditor.FilePathAttribute;
using Unity.VisualScripting;
using System.Drawing;

[CustomGridBrush(true, false, false, "Wave Brush")]
public class WaveBrush : GridBrush
{
    public TileMapAdjacencyData TileData;

    [Range(1, 4)]
    public int Size;

    public bool AutoCollapse;

    public bool Square;

    // The brush needs to keep track of the wave tiles we have created
    // so we can collapse in order of lest entropy
    public HashSet<WaveTile> PaintedTiles;

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        // Lets put down the real tile and the quantum tiles
        base.Paint(grid, brushTarget, position);
        var tileMap = brushTarget.GetComponent<Tilemap>();

        tileMap.ClearAllEditorPreviewTiles();

        var addTiles = new List<WaveTile>();
        foreach(var neighbor in GetRadiusBrushCells())
        {
            var pos = position + neighbor;
            var nTile = tileMap.GetTile(position + neighbor);
            if(nTile == null || AutoCollapse)
            {
                // we will not overrite existing tiles of the same sprite
                if(nTile is Tile existingTile && cells[0].tile is Tile bTile)
                {
                    if(existingTile.sprite == bTile.sprite)
                    {
                        continue;
                    }
                }

                var entpTile = CreateInstance<WaveTile>();
                entpTile.Position = pos;
                entpTile.Data = TileData;
                tileMap.SetTile(pos, entpTile);

                // We don't have time to wait for TileMap to call compute on the tile
                entpTile.State = entpTile.ComputeStateMatrix(pos, null, tileMap);

                if(AutoCollapse)
                {
                    addTiles.Add(entpTile);
                }
            }
        }

        // Are there tiles to collapse?
        if(addTiles.Count > 0)
        {
            // Find the min tile.
            // TODO: Min heap data structure. Easy to to get when open source code is allowed. Would be academic misconduct.
            var totalTiles = addTiles.Count;
            for(int i = 0; i < totalTiles; i++)
            {
                var waveTile = addTiles[0];
                var minTile = WaveBrushUtil.GetMinEntropyTile(addTiles);

                var result = minTile.Collapse(tileMap);
                addTiles.Remove(minTile);
            }
        }
    }

    public List<Vector3Int> GetRadiusBrushCells()
    {
        // TODO: Something different depending on the grid cell type
        // TODO: Radius should probably be about the brush selection
        // create a set of vectors relative to the brush origin to fill the desired size.
        var cellVectors = new List<Vector3Int>();

        for (var x = -Size; x <= Size; x++)
        {
            var height = Size - Mathf.Abs(x);
            if(Square)
            {
                height = Size;
            }
            for (var y = -height; y <= height; y++)
            {
                // Exclude tiles being paited by the brush selection
                if(x != 0 || y != 0)
                {
                    cellVectors.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        return cellVectors;
    }

    public void CollapseWorld()
    {
        // Find the wave tile with the lowest entropy
    }
}
