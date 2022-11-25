using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Tilemaps;
using static UnityEditor.ObjectChangeEventStream;

[Serializable]
public class TileAdjacencyMatrix
{
    public Tile Tile;
    public List<MatrixRow> Rows;

    public int Sum;

    public TileAdjacencyMatrix()
    {

    }

    public TileAdjacencyMatrix(int rows, int columns, Tile t)
    {
        Rows = new List<MatrixRow>(rows);
        for (int i = 0; i < rows; i++)
        {
            Rows.Add(new MatrixRow() { Column = new float[columns] });
        }

        Tile = t;
    }

    public float this[int i, int j]
    {
        get => Rows[i].Column[j];
        set => Rows[i].Column[j] = value;
    }

    public override string ToString()
    {
        var columns = Rows[0].Column.Length;
        var builder = new StringBuilder(Rows.Count * (columns + 1));
        foreach(var row in Rows)
        {
            builder.Append(string.Join(',', row.Column));
            builder.Append(Environment.NewLine);
        }

        return builder.ToString();
    }
}
