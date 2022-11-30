using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Tilemaps.Tile;

[CreateAssetMenu]
public class WaveTile : Tile
{
    public TileMapAdjacencyData Data;

    public TileAdjacencyMatrix State;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // I need to know if my neighbor state has changed so I can propogate
        // Or, I need to tell my neighbors that i have changed.
        base.RefreshTile(position, tilemap);

        var newState = ComputeStateMatrix(position, tilemap);

        if(newState != State)
        {
            State = newState;

            // Convert to a real tile if we can

            // Let hope the framework will trigger this from any neighbor
            foreach(var neighborVector in Data.NeighborVectors)
            {
                if(tilemap.GetTile(position + neighborVector) is WaveTile waveTile)
                {
                    Debug.Log("Cascading collapse");
                    // How do I stop from over cascading?
                    //waveTile.RefreshTile(position + neighborVector, tilemap);
                }
            }
        }

    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        var possible = GetPossibleTiles(State);
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

        State = ComputeStateMatrix(position, tilemap);
        // Conceputally, at this point, statematrix has been zeroed.
        // any survivors are allowed
        // I wish I could visualize this in the inspector
        // Possible if I set a real tile, I think
        HashSet<Tile> possible = GetPossibleTiles(State);


        // Randomly pick from what is possible
        if(possible.Count > 0)
        {
            var pick = Random.Range(0, possible.Count);
            var tile = possible.ElementAt(pick);

            var confidence = 1 / possible.Count;

            var confidenceColor = Color.Lerp(Color.red, Color.white, confidence);

            tileData.sprite = tile.sprite;
            tileData.color = tile.color * confidenceColor;
            tileData.transform = tile.transform;
            tileData.gameObject = tile.gameObject;
            tileData.flags = tile.flags;
            tileData.colliderType = tile.colliderType;
        }

    }

    public TileAdjacencyMatrix ComputeStateMatrix(Vector3Int position, ITilemap tilemap)
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

        foreach (var neighborVector in Data.NeighborVectors)
        {
            var neighborTile = tilemap.GetTile(position + neighborVector);
            if (neighborTile != null)
            {
                // are we neighboring a quantum state or a collapsed state?
                if (neighborTile is WaveTile q)
                {
                    // what does this neighbor tell us about what should go here?
                    // A neighbor wave data is contextually internal, it only has data about what is might be.

                    // what does this neighbor tell us about what should go here?
                    var data = q;
                    // I need the information from this neighbor that points at me
                    var neighborState = q.State;
                    if (neighborState != null)
                    {
                        var inboundRow = Data.RowOf(-neighborVector);
                        var inboundInfo = neighborState.Rows[inboundRow];
                        // Since this neighbor is a wave tile, we only multiply probability agasint the inbound row
                        for (int j = 0; j < stateMatrix.Rows[inboundRow].Column.Length; j++)
                        {
                            stateMatrix.Rows[inboundRow].Column[j] *= inboundInfo.Column[j];
                        }
                    }

                }
                else if (neighborTile is Tile collapsed)
                {
                    // what does this neighbor tell us about what should go here?
                    var data = Data[collapsed];
                    // I need the information from this neighbor that points at me
                    var rowNum = Data.RowOf(-neighborVector);
                    var information = data.Rows[rowNum];
                    // Format information in from the incoming perspective
                    // this is opposite the training data.

                    // Because this is an exact tile, it is necessary to multiply out other incoming
                    // vectors such that the collapsed tile can deny impossible probailities.

                    // SO... go through every row and apply the rule from impinges from the collapsed direction
                    for (int i = 0; i < stateMatrix.Rows.Count; i++)
                    {
                        for (int j = 0; j < information.Column.Length; j++)
                        {
                            var p = information.Column[j];
                            stateMatrix[i, j] *= p;
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
