using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A custom tile representing a tile of undetermined state
/// </summary>
public class WaveTile : Tile
{
    /// <summary>
    /// Adjacency data for the palette
    /// </summary>
    public TileMapAdjacencyData Data;

    /// <summary>
    /// The state of this tile
    /// </summary>
    public TileAdjacencyMatrix State;

    /// <summary>
    /// The position of this tile on the map
    /// </summary>
    public Vector3Int Position;

    /// <summary>
    /// Update tile state
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilemap"></param>
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // Save this, it's hard to figure out later
        Position = position;

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

    /// <summary>
    /// Set of possible tiles to cycle through in an animation.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilemap"></param>
    /// <param name="tileAnimationData"></param>
    /// <returns></returns>
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        var possible = GetPossibleTiles();
        var sprites = new List<Sprite>();
        foreach(var t in possible)
        {
            sprites.Add(t.Tile.sprite);
        }

        tileAnimationData.animatedSprites = sprites.ToArray();


        return false;
    }
        
    /// <summary>
    /// Return the state of this tile as a sprite
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilemap"></param>
    /// <param name="tileData"></param>
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // Save this, it's hard to figure out later
        Position = position;

        // Update state from neighbor states
        State = ComputeStateMatrix(position, tilemap);

        // Merge all possible sprites
        // TODO: Optimize? This seems expensive.

        var possibleTiles = GetPossibleTiles();
        RenderTexture previewRenderTexture = null;
        foreach(var tile in possibleTiles)
        {
            var texture = tile.Tile.sprite.texture;
            // TODO: What happens when the tile textures are different sizes?
            // TODO: Find a better way to define the render texture
            if(previewRenderTexture == null)
            {
                previewRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
            }
        }

        // Randomly pick from possible tiles. Not weighted
        var possible = GetPossibleTiles();
        if(possible.Count > 0)
        {
            // take the most likely
            var pick = possible.Last().Tile;

            var confidence = 1 / possible.Count;

            var confidenceColor = Color.Lerp(Color.red, Color.white, confidence);

            tileData.sprite = pick.sprite;
            tileData.color = pick.color * confidenceColor;
            tileData.transform = pick.transform;
            tileData.gameObject = pick.gameObject;
            tileData.flags = pick.flags;
            tileData.colliderType = pick.colliderType;
        }

    }

    /// <summary>
    /// Compute  state from neighor states
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
                    
                    // Because this is a specific tile, it is necessary to multiply out other incoming
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
        var weightLogSum = 0f;
        var sum = stateMatrix.TileWeights.Sum();

        foreach (var v in stateMatrix.TileWeights)
        {
            if (v > 0)
            {
                weightLogSum += v * Mathf.Log(v, 2);
            }
        }

        stateMatrix.Entropy = 0;
        if (sum > 0)
        {
            stateMatrix.Entropy = Mathf.Log(sum, 2) - (weightLogSum / sum);
        }

        return stateMatrix;
    }

    /// <summary>
    /// Return tiles have columns without 0 entires
    /// </summary>
    /// <returns>set of possible tiles</returns>
    private List<TileWeight> GetPossibleTiles()
    {
        var queue = new PriorityQueue<TileWeight>();
        for (int i = 0; i < State.TileWeights.Length; i++)
        {
            var v = State.TileWeights[i];
            if(v > 0)
            {
                queue.Enqueue(new TileWeight { Value = v, Tile = Data.AdjacenciesList[i].Tile });
            }
        }

        var results = new List<TileWeight>(queue.Count);
        while(queue.Count > 0)
        {
            results.Add(queue.Dequeue());
        }
        
        return results;
    }

    /// <summary>
    /// "Collapse" this tile by picking from the possibilities and replacing the tile in the map.
    /// </summary>
    /// <param name="map">The Tilemap where the tile exists.</param>
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
