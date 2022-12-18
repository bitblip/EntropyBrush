using System;

/// <summary>
/// Class used to overcome Unity yaml serilizaton. Represents one row of a matrix.
/// </summary>
[Serializable]
public class MatrixRow
{
    public float[] Column;
}
