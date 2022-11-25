using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.Tilemaps;
using static UnityEditor.FilePathAttribute;
using Codice.Client.BaseCommands;
using Unity.VisualScripting;

[CreateAssetMenu]
[CustomGridBrush(true, false, false, "Wave Brush")]
public class WaveBrush : GridBrush
{
    public TileMapAdjacencyData TileData;
    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        Debug.Log("CUSTOM PAINT");

        // Lets put down the real tile and the quantum tiles
        base.Paint(grid, brushTarget, position);
        var tileMap = brushTarget.GetComponent<Tilemap>();

        var addTiles = new List<TileChangeData>();
        foreach(var neighbor in TileData.NeighborVectors)
        {
            var pos = position + neighbor;
            var nTile = tileMap.GetTile(position + neighbor);
            if(nTile == null)
            {
                var entpTile = CreateInstance<WaveTile>();
                entpTile.Data = TileData;
                tileMap.SetTile(pos, entpTile);
            }
        }
    }
}
