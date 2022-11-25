using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Tilemaps.Tile;

[CreateAssetMenu]
public class WaveTile : Tile
{
    public TileMapAdjacencyData Data;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        Debug.Log("Refresh wave tile");
        base.RefreshTile(position, tilemap);
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        Debug.Log("Animate wave?");
        var stateMatrix = ComputeStateMatrix(position, tilemap);
        var possible = GetPossibleTiles(stateMatrix);
        var sprites = new List<Sprite>();
        foreach(var t in possible)
        {
            sprites.Add(t.sprite);
        }

        tileAnimationData.animatedSprites = sprites.ToArray();


        return true;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // I mean, it depends on your neighbors

        // I should look at my neighbors, to see who's real.
        // compute my state based on the knowns from my neighbors
        // if my neighbor is a Tile, it's a collapsed state and has forces my state
        // not sure how to compute that yet
        // if my neighbor is a WaveTile, which I'm skeptical is available on a preview step
        // I don't want to multiply by 0 on a possiility
        // two entropy tiles are allowed to disagree about the tile between them.
        // how do I eliminate possibilities if 4 tiles only agree on one 1 tile possiility

        // foreach neighor
        // multiply probabilities into my own state matrix
        // cells with non zero values are possibilities
        // Add down the columns since the matrix is THIS cell, the direction row keeps
        // tiles from contribuiting weight on an axis for which is has none

        // First, determine is any of this is available in preview

        // TODO: Figure out why N/S are coming out 0 for grass
        var stateMatrix = ComputeStateMatrix(position, tilemap);
        // Conceputally, at this point, statematrix has been zeroed.
        // any survivors are allowed
        // I wish I could visualize this in the inspector
        // Possible if I set a real tile, I think
        HashSet<Tile> possible = GetPossibleTiles(stateMatrix);


        // Randomly pick from what is possible
        if(possible.Count > 0)
        {
            var pick = Random.Range(0, possible.Count);
            var tile = possible.ElementAt(pick);

            tileData.sprite = tile.sprite;
            tileData.color = tile.color;
            tileData.transform = tile.transform;
            tileData.gameObject = tile.gameObject;
            tileData.flags = tile.flags;
            tileData.colliderType = tile.colliderType;
        }
    }

    private TileAdjacencyMatrix ComputeStateMatrix(Vector3Int position, ITilemap tilemap)
    {
        var stateMatrix = new TileAdjacencyMatrix(Data.NeighborVectors.Length, Data.AdjacenciesList.Count, null);
        // initialize the state to 1?
        foreach (var row in stateMatrix.Rows)
        {
            for (int i = 0; i < row.Column.Length; i++)
            {
                row.Column[i] = 1;
            }
        }

        foreach (var neighor in Data.NeighborVectors)
        {
            var nTile = tilemap.GetTile(position + neighor);
            if (nTile != null)
            {
                // are we neighboring a quantum state or a collapsed state?
                if (nTile is WaveTile q)
                {
                    // what does this neighbor tell us about what should go here?
                    // A neighbor wave data is contextually internal, it only has data about what is might be.
                    // What if I transpose and multiply?
                    // Better write that out
                    var data = q.Data;

                }
                else if (nTile is Tile collapsed)
                {
                    // what does this neighbor tell us about what should go here?
                    var data = Data[collapsed];
                    // I need the information from this neighbor that points at me
                    var rowNum = Data.RowOf(-neighor);
                    var information = data.Rows[rowNum];
                    // Format information in from the incoming perspective
                    // this is opposite the training data.

                    // Because this is an exact tile, it is necessary to multiply out other incoming
                    // vectors such that the collapsed tile can deny impossible probailities.

                    // SO... go through every row and apply the rule from impinges from the collapsed direction
                    var rowTotal = information.Total();
                    for (int i = 0; i < stateMatrix.Rows.Count; i++)
                    {
                        for (int j = 0; j < information.Column.Length; j++)
                        {
                            var count = information.Column[j];
                            stateMatrix[i, j] *= (count / rowTotal);
                        }
                    }
                }
            }
        }

        return stateMatrix;
    }

    private HashSet<Tile> GetPossibleTiles(TileAdjacencyMatrix stateMatrix)
    {
        // I really need to know probabilities to do this right.

        var possible = new HashSet<Tile>();
        foreach (var row in stateMatrix.Rows)
        {
            for (int j = 0; j < row.Column.Length; j++)
            {
                var v = row.Column[j];
                if (v > 0)
                {
                    possible.Add(Data.AdjacenciesList[j].Tile);
                }
            }
        }

        return possible;
    }
}
