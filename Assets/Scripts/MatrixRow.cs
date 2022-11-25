using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MatrixRow
{
    public float[] Column;

    public float Total()
    {
        var sum = 0f;
        for (int i = 0; i < Column.Length; i++)
        {
            sum += Column[i];
        }

        return sum;
    }
}
