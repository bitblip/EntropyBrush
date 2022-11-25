using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileMapAdjacencyData : ScriptableObject
{
    public Dictionary<Tile, TileAdjacencyMatrix> Adjacencies;

    public List<TileAdjacencyMatrix> AdjacenciesList;

    public Vector3Int[] NeighborVectors = new Vector3Int[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

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
        foreach (var m in AdjacenciesList)
        {
            if (m.Tile == t)
            {
                return AdjacenciesList.IndexOf(m);

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
                if(1 > 0)
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
        get {

            if(t != null&& Adjacencies != null && Adjacencies.ContainsKey(t))
            {
                return Adjacencies[t];
            }
            return null;
        }
    }
}
