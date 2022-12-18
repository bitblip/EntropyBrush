using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Collapse all wave tiles in the specified Tilemap.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class TilemapWaveCollapse : MonoBehaviour
{
    /// <summary>
    /// The tile map
    /// </summary>
    private Tilemap map;
    /// <summary>
    /// All tiles that need collapsed
    /// </summary>
    private List<WaveTile> waveTiles;
    /// <summary>
    /// Should wave wave tiles continue infinitely.
    /// </summary>
    public bool InfiniteCollapse;

    void Start()
    {
        // Get Tilemap component and setup collapsable tiles
        map = GetComponent<Tilemap>();
        var tiles = new TileBase[map.size.x * map.size.y];
        var totalTiles = map.GetTilesBlockNonAlloc(map.cellBounds, tiles);
        
        waveTiles = new List<WaveTile>();
        for (int i = 0; i < totalTiles; i++)
        {
            if(tiles[i] is WaveTile t)
            {
                waveTiles.Add(t);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Collapse one tile per frame
        CollapseOne();
    }

    /// <summary>
    /// Find and collapse the tile with lowest entropy
    /// </summary>
    private void CollapseOne()
    {
        if (waveTiles.Count > 0)
        {
            var waveTile = WaveBrushUtil.GetMinEntropyTile(waveTiles);

            var result = waveTile.Collapse(map);
            waveTiles.Remove(waveTile);

            // Collapse was not possible, move on
            if (result == null)
            {
                return;
            }

            // Infinite collapse
            foreach (var neighborVector in waveTile.Data.NeighborVectors)
            {
                var neighborTile = map.GetTile(waveTile.Position + neighborVector);
                if (neighborTile == null && InfiniteCollapse)
                {
                    var entpTile = ScriptableObject.CreateInstance<WaveTile>();
                    entpTile.Data = waveTile.Data;
                    map.SetTile(waveTile.Position + neighborVector, entpTile);
                    waveTiles.Add(entpTile);
                }
            }
        }
    }
}
