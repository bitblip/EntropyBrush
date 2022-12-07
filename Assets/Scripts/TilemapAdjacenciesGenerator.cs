using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class TilemapAdjacenciesGenerator : MonoBehaviour
{
    public Tilemap Map;

    public TileMapAdjacencyData BrushData;

    // Start is called before the first frame update
    void Start()
    {
        if (Application.IsPlaying(gameObject))
        {
            // Play logic
            Map = GetComponent<Tilemap>();
        }
        else
        {
            Map = GetComponent<Tilemap>();
        }
    }

    private void OnValidate()
    {
    }

    public void InitializeAdjMatrix(TileBase[] tiles)
    {
        BrushData.AdjacenciesList = new List<TileAdjacencyMatrix>(tiles.Length);
        foreach (var t in tiles)
        {
            var adjM = new TileAdjacencyMatrix(4, tiles.Length, (Tile)t);
            BrushData.AdjacenciesList.Add(adjM);
        }

        BrushData.PrimeDictionary(true);
    }

    public bool Generate()
    {
        if (BrushData == null)
        {
            Debug.LogWarning("Missing adjacency data object");
            return false;
        }

        // Editor logic
        var distinctSprites = new Sprite[100];
        Map.GetUsedSpritesNonAlloc(distinctSprites);

        var distinctTiles = new TileBase[Map.GetUsedTilesCount()];
        Map.GetUsedTilesNonAlloc(distinctTiles);

        InitializeAdjMatrix(distinctTiles);

        var min = Map.cellBounds.min;
        var max = Map.cellBounds.max;
        var neighborSet = BrushData.NeighborVectors;
        for (int x = min.x; x < max.x; x++)
        {
            for (int y = min.y; y < max.y; y++)
            {
                var cell = new Vector3Int(x, y);
                var tile = Map.GetTile(cell);
                if (tile != null)
                {
                    var m = BrushData[(Tile)tile];

                    // add observations from adjacencies
                    for (int row = 0; row < neighborSet.Length; row++)
                    {
                        var neighborVector = neighborSet[row];
                        var tileObs = Map.GetTile(cell + neighborVector);

                        if(tileObs != null)
                        {
                            var column = BrushData.ColumnOf((Tile)tileObs);
                            if(tile.name == "Isometric_MedievalFantasy_Grass" && column == 3)
                            {
                                Debug.Log("Grass saw crossroads how?");
                            }
                            m[row, column] += 1;

                            m.Sum += 1;
                        }
                    }
                }
            }
        }

        // Normalize
        BrushData.Normalize();

        return true;
    }

}
