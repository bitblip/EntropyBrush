using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine.Tilemaps;
using static UnityEditor.ObjectChangeEventStream;

[Serializable]
public class TileAdjacencyMatrix
{
    public Tile Tile;
    public List<MatrixRow> Rows;

    public float[] TileWeights;
    public float Entropy { get; set; }

    public int Sum;

    public TileAdjacencyMatrix()
    {

    }

    public List<List<float>> Columns {
        get
        {
            var columns = Rows[0].Column.Length;
            var rows = Rows.Count;
            var data = new List<List<float>>();

            for(int j = 0; j < columns; j++)
            {
                data.Add(new List<float>());
                for(int i = 0; i < rows; i++)
                {
                    var c = data[j];
                    c.Add(Rows[i].Column[j]);
                }
            }

            return data;
        }
    }

    public TileAdjacencyMatrix(int rows, int columns, Tile t)
    {
        Rows = new List<MatrixRow>(rows);
        for (int i = 0; i < rows; i++)
        {
            Rows.Add(new MatrixRow() { Column = new float[columns] });
        }

        TileWeights = new float[columns];
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

    public static bool operator !=(TileAdjacencyMatrix left, TileAdjacencyMatrix right)
    {
        return !(left == right);
    }

    public static bool operator ==(TileAdjacencyMatrix left, TileAdjacencyMatrix right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if(obj is TileAdjacencyMatrix other)
        {
            for (int i = 0; i < other.Rows.Count; i++)
            {
                MatrixRow row = other.Rows[i];
                for (int j = 0; j < row.Column.Length; j++)
                {
                    if (other[i,j ] != this[i,j])
                    {
                        return false;
                    }
                }
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}
