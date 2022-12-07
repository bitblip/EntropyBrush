using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileMapAdjacencyData : ScriptableObject
{
    public Dictionary<Tile, TileAdjacencyMatrix> Adjacencies;

    public List<TileAdjacencyMatrix> AdjacenciesList;

    public Vector3Int[] NeighborVectors = new Vector3Int[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
    public static Vector3Int[] CornersAndNeighbors = new Vector3Int[] { 
        Vector3Int.up, 
        Vector3Int.up + Vector3Int.right, 
        Vector3Int.right,
        Vector3Int.right + Vector3Int.down,
        Vector3Int.down, 
        Vector3Int.down + Vector3Int.left,
        Vector3Int.left,
        Vector3Int.left + Vector3Int.up
    };

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

    internal void Normalize()
    {
        foreach(var data in AdjacenciesList)
        {
            foreach(var row in data.Rows)
            {
                var total = row.Total();
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

    private void InitializeAdjMatrix(TileBase[] tiles)
    {
        AdjacenciesList = new List<TileAdjacencyMatrix>(tiles.Length);
        foreach (var t in tiles)
        {
            var adjM = new TileAdjacencyMatrix(4, tiles.Length, (Tile)t);
            AdjacenciesList.Add(adjM);
        }

        PrimeDictionary(true);
    }

    public bool Generate(Tilemap map)
    {
        // Editor logic
        var distinctSprites = new Sprite[100];
        map.GetUsedSpritesNonAlloc(distinctSprites);

        var distinctTiles = new TileBase[map.GetUsedTilesCount()];
        map.GetUsedTilesNonAlloc(distinctTiles);

        InitializeAdjMatrix(distinctTiles);

        var min = map.cellBounds.min;
        var max = map.cellBounds.max;
        var neighborSet = NeighborVectors;
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

                            m.Sum += 1;
                        }
                    }
                }
            }
        }

        // Normalize
        Normalize();

        return true;
    }
}
