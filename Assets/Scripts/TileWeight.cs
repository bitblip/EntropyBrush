using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileWeight : IComparable
{
    public float Value { get; set; }
    public Tile Tile { get; set; }

    public int CompareTo(object obj)
    {
        if(obj is TileWeight otherWeight)
        {
            return Value.CompareTo(otherWeight.Value);
        }

        throw new ArgumentException();
    }

    public override bool Equals(object obj)
    {
        return (obj is TileWeight otherWeight) && otherWeight.Value == this.Value;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Tile.name}:{Value}";
    }
}
