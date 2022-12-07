using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Tilemaps.Tile;

public class WaveTile : Tile
{
    public TileMapAdjacencyData Data;

    public TileAdjacencyMatrix State;

    public Vector3Int Position;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // Save this, it's hard to figure out later
        Position = position;

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
                //Debug.Log($"Cascade {position + neighborVector}");
                if(tilemap.GetTile(position + neighborVector) is WaveTile waveTile)
                {
                    // How do I stop from over cascading?
                    waveTile.ComputeStateMatrix(position + neighborVector, tilemap);
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
        Position = position;
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

    /// <summary>
    /// Compute possible state from neighors
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilemap"></param>
    /// <returns></returns>
    public TileAdjacencyMatrix ComputeStateMatrix(Vector3Int position, ITilemap tilemap, Tilemap goTileMap = null)
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
            TileBase neighborTile = null;
            if(tilemap != null)
            {
                neighborTile = tilemap.GetTile(position + neighborVector);
            }
            else if (goTileMap != null)
            {
                neighborTile = goTileMap.GetTile(position + neighborVector);
            }

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

                    var outRow = Data.RowOf(neighborVector);
                    for (int j = 0; j < stateMatrix.Rows[outRow].Column.Length; j++)
                    {
                        stateMatrix.Rows[outRow].Column[j] *= information.Column[j];
                    }
                }
            }
        }

        // Multiply all the columns
        // Some rows sum > 1 now, not sure if that's a problem
        // TODO: Is it a problem if the probabilities sum > 1
        for (int j = 0; j < stateMatrix.Columns.Count; j++)
        {
            var p = 1f;
            List<float> column = stateMatrix.Columns[j];
            for (int i = 0; i < column.Count; i++)
            {
                p *= column[i];
            }
            stateMatrix.TileWeights[j] = p;
        }

        // Compute the shannon entropy from possile tile probailties
        // row total
        var weightLogSum = 0f;
        var sum = stateMatrix.TileWeights.Sum();

        foreach (var v in stateMatrix.TileWeights)
        {
            if (v > 0)
            {
                weightLogSum += v * Mathf.Log(v, 2);
            }
        }

        // Quick hack, increash entropy by the log of the distance from the origin
        // to keep it from running away in one direction.
        var logDistance = Mathf.Log(position.magnitude);

        // it might be hepful to sum the entropy of my neighbors
        // to get a better idea of the local area entropy
        // I think we get this for free by training on > 1X1

        stateMatrix.Entropy = 0;
        if (sum > 0)
        {
            stateMatrix.Entropy = Mathf.Log(sum, 2) - (weightLogSum / sum);
        }

        return stateMatrix;
    }

    private HashSet<Tile> GetPossibleTiles(TileAdjacencyMatrix stateMatrix)
    {
        // I really need to know probabilities to do this right.
        var possible = new HashSet<Tile>();
        for (int i = 0; i < State.TileWeights.Length; i++)
        {
            var v = State.TileWeights[i];
            if(v > 0)
            {
                possible.Add(Data.AdjacenciesList[i].Tile);
            }

        }
        
        return possible;
    }

    /// <summary>
    /// "Collapse" this tile by picking from the possibilities and replacing the tile in the map.
    /// </summary>
    /// <param name="map">The TileMap here the tile exists.</param>
    /// <returns></returns>
    public Tile Collapse(Tilemap map)
    {
        // weightSum == 0 means no tile works here
        var totalWeights = State.TileWeights.Sum();
        if (totalWeights == 0)
        {
            return null;
        }

        // Collect tiles that are allowed and compute max probability
        var allowed = new List<int>();
        var maxNormalizedWiehgt = 0f;
        var mostLikelyIndex = 0;
        for (int i = 0; i < State.TileWeights.Length; i++)
        {
            if(State.TileWeights[i] > 0)
            { 
                allowed.Add(i);
                var normalizedWeight = State.TileWeights[i] / totalWeights;
                if(normalizedWeight > maxNormalizedWiehgt)
                {
                    maxNormalizedWiehgt = normalizedWeight;
                    mostLikelyIndex = i;
                }
            }
        }

        // We need the pick to be indipendent to honor the probability.
        // TODO: Find a deterministic method to randomly pick from weighted values. Probably something about P values.
        var picks = new List<Tile>();
        var randomValue = Random.Range(0f, 1f);
        for (int i = 0; i < allowed.Count; i++)
        {
            int allowedTile = allowed[i];
            // reject picks that do not meet the random threashold
            var weight = State.TileWeights[allowedTile];
            if (weight >= randomValue)
            {
                picks.Add(Data.AdjacenciesList[allowedTile].Tile);
            }
        }

        // Take the most likely as a default pick.
        var pick = Data.AdjacenciesList[mostLikelyIndex].Tile;
        if(picks.Count > 0)
        {
            // From the random roll, these closer are closer to equal likelhood
            // a better picking solution is needed
            pick = picks[Random.Range(0, picks.Count)];
        }

        // Replace this tile with a real tile
        map.SetTile(Position, pick);

        //Debug.Log($"Collapsing {waveTile.Position}");
        // It's now necessary to update the neighboring wave tiles
        //waveTile.ComputeStateMatrix(waveTile.Position, null, map);
        foreach (var neighborVector in Data.NeighborVectors)
        {
            var neighborTile = map.GetTile(Position + neighborVector);
            if (neighborTile is WaveTile neighborWaveTile)
            {
                // Update neighbor state
                neighborWaveTile.State = neighborWaveTile.ComputeStateMatrix(Position + neighborVector, null, map);
            }
        }

        return pick;
    }
}
