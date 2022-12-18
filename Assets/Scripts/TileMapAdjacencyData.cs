using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Data storage asset for a tile palette.
/// </summary>
[CreateAssetMenu]
public class TileMapAdjacencyData : ScriptableObject
{
    /// <summary>
    /// Lookup adjacency by tile
    /// </summary>
    public Dictionary<Tile, TileAdjacencyMatrix> Adjacencies;

    /// <summary>
    /// Ordered list of tiles, corresponding to column.
    /// </summary>
    public List<TileAdjacencyMatrix> AdjacenciesList;

    /// <summary>
    /// Begining with (0,1,0) (North/Up), proceeding clockwise, (1,0,0) (East/Right), (0,-1,0) (South/Down), (-1, 0, 0) (West/Left)
    /// </summary>
    public Vector3Int[] NeighborVectors = new Vector3Int[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    /// <summary>
    /// Load the de-seraized data into the lookup.
    /// </summary>
    /// <param name="reset">Overwrite existing state</param>
    public void PrimeDictionary(bool reset = false)
    {
        if(Adjacencies != null && !reset)
        {
            return;
        }

        Adjacencies = new Dictionary<Tile, TileAdjacencyMatrix>();
        foreach(var m in AdjacenciesList)
        {
            Adjacencies.Add(m.Tile, m);
        }
    }

    /// <summary>
    /// Get the column number of the specified tile
    /// </summary>
    /// <param name="t">The tile</param>
    /// <returns>column number of -1 if not found</returns>
    public int ColumnOf(Tile t)
    {
        for (int i = 0; i < AdjacenciesList.Count; i++)
        {
            if (AdjacenciesList[i].Tile == t)
            {
                return i;

            }
        }

        return -1;
    }

    /// <summary>
    /// Get the row number of the specified vector
    /// </summary>
    /// <param name="direction">The vector</param>
    /// <returns>row number or -1 if not found</returns>
    public int RowOf(Vector3Int direction)
    {
        // TODO: Figure out how to make order part of the data
        for (int i = 0; i < NeighborVectors.Length; i++)
        {
            if(direction == NeighborVectors[i])
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Normalize all matrix rows so they sum to one.
    /// </summary>
    internal void NormalizeRowSpace()
    {
        foreach(var data in AdjacenciesList)
        {
            foreach(var row in data.Rows)
            {
                var total = row.Column.Sum();
                if(total > 0)
                {
                    for (int i = 0; i < row.Column.Length; i++)
                    {
                        row.Column[i] = row.Column[i] / total;
                    }
                }
            }
        }

        PrimeDictionary(true);
    }

    /// <summary>
    /// Get the adjacency matrix of the specified tile
    /// </summary>
    /// <param name="t">The tile</param>
    /// <returns>adjacency matrix</returns>
    public TileAdjacencyMatrix this[Tile t]
    {
        get 
        {
            // Not sure when the right time is to load the data into the lookup
            if(Adjacencies == null && AdjacenciesList != null && AdjacenciesList.Count > 0)
            {
                PrimeDictionary();
            }

            if(t != null&& Adjacencies != null && Adjacencies.ContainsKey(t))
            {
                return Adjacencies[t];
            }
            return null;
        }
    }

    /// <summary>
    /// Setup the adjacency matrix for all tiles.
    /// </summary>
    /// <param name="tiles">Set of all tiles in a palette.</param>
    private void InitAdjacencyMatrix(TileBase[] tiles)
    {
        AdjacenciesList = new List<TileAdjacencyMatrix>(tiles.Length);
        foreach (var t in tiles)
        {
            var adjM = new TileAdjacencyMatrix(4, tiles.Length, (Tile)t);
            AdjacenciesList.Add(adjM);
        }

        PrimeDictionary(true);
    }

    /// <summary>
    /// Enumber all grid cells in the map, counting adjacency observations.
    /// </summary>
    /// <param name="map">The palette map</param>
    /// <returns>success</returns>
    public void Generate(Tilemap map)
    {
        // Collect all tiles from the map
        var distinctSprites = new Sprite[100];
        map.GetUsedSpritesNonAlloc(distinctSprites);

        var distinctTiles = new TileBase[map.GetUsedTilesCount()];
        map.GetUsedTilesNonAlloc(distinctTiles);

        // Setup for possible tiles
        InitAdjacencyMatrix(distinctTiles);

        var min = map.cellBounds.min;
        var max = map.cellBounds.max;
        var neighborSet = NeighborVectors;
        // Loop over the bounds of the map
        for (int x = min.x; x < max.x; x++)
        {
            for (int y = min.y; y < max.y; y++)
            {
                var cell = new Vector3Int(x, y);
                var tile = map.GetTile(cell);
                if (tile != null)
                {
                    var m = this[(Tile)tile];

                    // add observations from adjacencies
                    for (int row = 0; row < neighborSet.Length; row++)
                    {
                        var neighborVector = neighborSet[row];
                        var tileObs = map.GetTile(cell + neighborVector);

                        if (tileObs != null)
                        {
                            var column = this.ColumnOf((Tile)tileObs);
                            m[row, column] += 1;

                            m.ObservationCount += 1;
                        }
                    }
                }
            }
        }

        // Normalize
        NormalizeRowSpace();
    }
}
