using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class TilemapWaveCollapse : MonoBehaviour
{
    // Start is called before the first frame pdate
    private Tilemap map;
    private TileBase[] tiles;
    private List<WaveTile> waveTiles;

    public bool AutoCollapse;

    void Start()
    {
        map = GetComponent<Tilemap>();
        var tiles = new TileBase[map.size.x * map.size.y];
        var totalTiles = map.GetTilesBlockNonAlloc(map.cellBounds, tiles);
        
        waveTiles = new List<WaveTile>();
        for (int i = 0; i < totalTiles; i++)
        {
            if(tiles[i] is WaveTile t)
            {
                waveTiles.Add(t);
            }
        }

        //StartCoroutine(DoCollapse());
    }

    // Update is called once per frame
    void Update()
    {
        CollapseOne(null);
    }

    public IEnumerator DoCollapse()
    {
        // Find the wave tile with the loest entropy
        WaveTile waveTile = waveTiles[0];

        while(waveTile != null)
        {
            CollapseOne(waveTile);

            yield return new WaitForFixedUpdate();
        }

    }

    private void CollapseOne(WaveTile waveTile)
    {
        if (waveTiles.Count > 0)
        {
            waveTile = WaveBrushUtil.GetMinEntropyTile(waveTiles);

            var result = waveTile.Collapse(map);
            waveTiles.Remove(waveTile);

            // Collapse was not possible, move on
            if (result == null)
            {
                return;
            }

            // Infinite collapse
            foreach (var neighborVector in waveTile.Data.NeighborVectors)
            {
                var neighborTile = map.GetTile(waveTile.Position + neighborVector);
                if (neighborTile == null && AutoCollapse)
                {
                    var entpTile = ScriptableObject.CreateInstance<WaveTile>();
                    entpTile.Data = waveTile.Data;
                    map.SetTile(waveTile.Position + neighborVector, entpTile);
                    waveTiles.Add(entpTile);
                }
            }
        }
    }
}
