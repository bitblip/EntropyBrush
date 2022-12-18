using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.GridPalette;

/// <summary>
/// Custom tile palette brush that can create wave tiles
/// </summary>
[CustomGridBrush(true, false, false, "Wave Brush")]
public class WaveBrush : GridBrush
{
    /// <summary>
    /// Adjadency data
    /// </summary>
    public TileMapAdjacencyData TileData;

    /// <summary>
    /// Radius in which wave tiles will be placed
    /// </summary>
    [Range(1, 4)]
    public int Size = 1;

    /// <summary>
    /// Resolve wave tile state immediatly
    /// </summary>
    public bool AutoCollapse;

    /// <summary>
    /// Shape of the brush
    /// </summary>
    public bool Square;

    public WaveBrush() : base()
    {
        Debug.Log("Init?");
    }
    private void OnEnable()
    {
        Debug.Log("On enable wave brush");
    }

    private void OnDisable()
    {
        Debug.Log("on disable wave brush");
    }

    public override void Pick(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, Vector3Int pickStart)
    {
        base.Pick(gridLayout, brushTarget, position, pickStart);

        var tt = gridLayout.GetPrefabDefinition();

        //internal static GridPalette GetGridPaletteFromPaletteAsset(Object palette)
        //{
        //    string assetPath = AssetDatabase.GetAssetPath(palette);
        //    GridPalette paletteAsset = AssetDatabase.LoadAssetAtPath<GridPalette>(assetPath);
        //    return paletteAsset;        //}

        var assetPath = AssetDatabase.GetAssetPath(GridPaintingState.palette);
        var paletteAsset = AssetDatabase.LoadAssetAtPath<GridPalette>(assetPath);

        if (paletteAsset != null)
        {
            GameObject palette = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;

            var existingData = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TileMapAdjacencyData)) as TileMapAdjacencyData;

            var subassets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subasset in subassets)
            {
                if(subasset is TileMapAdjacencyData d)
                {
                    AssetDatabase.RemoveObjectFromAsset(subasset);
                }
            }

            var soi = Instantiate(TileData);
            soi.name = "Save palette adj data here";
            AssetDatabase.AddObjectToAsset(soi, paletteAsset);
            AssetDatabase.SaveAssets();


            //var inst = ScriptableObject.Instantiate(TileData);
            //inst.name = "Test add tile data";

            //AssetDatabase.AddObjectToAsset(inst, paletteAsset);
            //PrefabUtility.ApplyPrefabInstance(palette, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();
        }
    }

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        // Lets put down the real tile and the quantum tiles
        base.Paint(grid, brushTarget, position);


        var tileMap = brushTarget.GetComponent<Tilemap>();
        // Only draw if a tile is selected
        if (cells.Length != 1)
        {
            Debug.LogWarning("Wave brush only supports selecting one tile at a time.");
            return;
        }

        // A tile must be selected
        if (!(cells[0].tile is Tile))
        {
            Debug.LogWarning("Select a tile to paint.");
            return;
        }

        // Clear preview
        tileMap.ClearAllEditorPreviewTiles();

        var addTiles = new List<WaveTile>();
        // Place Wave Tiles in all brush tiles.
        foreach(var neighbor in GetRadiusBrushCells())
        {
            var pos = position + neighbor;
            var neighborTile = tileMap.GetTile(position + neighbor);
            if(neighborTile == null || AutoCollapse)
            {
                // we will not overrite existing tiles of the same sprite
                if(neighborTile is Tile existingTile && cells[0].tile is Tile bTile)
                {
                    if(existingTile.sprite == bTile.sprite)
                    {
                        continue;
                    }
                }

                // Replace with an entropy tile
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

    /// <summary>
    /// The set of all tiles that exist inside the brush given Size n
    /// </summary>
    /// <returns>cell positions</returns>
    public List<Vector3Int> GetRadiusBrushCells()
    {
        // TODO: Something different depending on the grid cell type
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
}
