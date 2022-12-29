using Elsheimy.Components.Linears;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Tilemaps;

/// <summary>
/// The 1x1 adjacency definition for the Tile.
/// </summary>
[Serializable]
public class TileAdjacencyMatrix
{
    public Tile Tile;

    /// <summary>
    /// The serilizable format of the matrix
    /// </summary>
    public List<MatrixRow> Rows;

    public Matrix Matrix;

    /// <summary>
    /// Product of the weights in each column.
    /// </summary>
    public float[] TileWeights;

    /// <summary>
    /// Total entroy of the tile
    /// </summary>
    public float Entropy { get; set; }

    public int ObservationCount;

    public TileAdjacencyMatrix()
    {

    }

    /// <summary>
    /// Initialize a new matrix for the specified tile with with the specified size.
    /// </summary>
    /// <param name="rows">Number of rows</param>
    /// <param name="columns">Number of columns</param>
    /// <param name="t">The tile</param>
    public TileAdjacencyMatrix(int rows, int columns, Tile t)
    {
        Rows = new List<MatrixRow>(rows);
        for (int i = 0; i < rows; i++)
        {
            Rows.Add(new MatrixRow() { Column = new float[columns] });
        }

        TileWeights = new float[columns];
        Tile = t;

        Matrix = new Matrix(rows, columns);
    }

    /// <summary>
    /// Return the matrix in column major format.
    /// </summary>
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

    /// <summary>
    /// Value of the ith row and jth column.
    /// </summary>
    /// <param name="i">Row</param>
    /// <param name="j">Column</param>
    /// <returns>Aij</returns>
    public float this[int i, int j]
    {
        get => Rows[i].Column[j];
        set => Rows[i].Column[j] = value;
    }

    /// <summary>
    /// Matrix in a comma delimited format, rows seperated by new lines.
    /// </summary>
    /// <returns>string</returns>
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

    /// <summary>
    /// Two matrix A and B are equal if Aij = Bij for all i and j.
    /// </summary>
    /// <param name="left">Matrix A</param>
    /// <param name="right">Matrix B</param>
    /// <returns>not equal</returns>
    public static bool operator !=(TileAdjacencyMatrix left, TileAdjacencyMatrix right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Two matrix A and B are equal if Aij = Bij for all i and j.
    /// </summary>
    /// <param name="left">Matrix A</param>
    /// <param name="right">Matrix B</param>
    /// <returns>equal</returns>
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

    /// <summary>
    /// Two matrix A and B are equal if Aij = Bij for all i and j.
    /// </summary>
    /// <param name="obj">Other objectd</param>
    /// <returns>equality</returns>
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
