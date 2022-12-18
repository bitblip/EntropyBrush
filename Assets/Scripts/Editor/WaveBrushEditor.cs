using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Custom inspector for the Wave Brush
/// </summary>
[CustomEditor(typeof(WaveBrush))]
public class WaveBrushEditor : GridBrushEditor
{
    /// <summary>
    /// Wave brush instance
    /// </summary>
    private WaveBrush waveBrush { get { return target as WaveBrush; } }

    /// <summary>
    /// Target game object of the brush
    /// </summary>
    private GameObject brushTarget => GridPaintingState.scenePaintTarget;

    private GameObject paletteObject => GridPaintingState.palette;

    /// <summary>
    /// Tilemap being painted on
    /// </summary>
    public Tilemap tileMap
    {
        get
        {
            if (brushTarget != null)
            {
                return brushTarget.GetComponent<Tilemap>();
            }
            return null;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        GridPaintingState.paletteChanged += GridPaintingState_paletteChanged;

        EnsureBrushData(paletteObject);
    }

    private void GridPaintingState_paletteChanged(GameObject paletteObj)
    {
        Debug.Log("Pallet change!");

        EnsureBrushData(paletteObj);
    }

    private void EnsureBrushData(GameObject paletteObj)
    {
        var brushData = GetAdjacencyData(paletteObj);
        if (brushData == null)
        {
            brushData = InitPaletteAdjacencyData(paletteObj);
        }
        waveBrush.TileData = brushData;
    }

    private TileMapAdjacencyData InitPaletteAdjacencyData(GameObject paletteObj)
    {
        var assetPath = AssetDatabase.GetAssetPath(paletteObj);
        var paletteAsset = AssetDatabase.LoadAssetAtPath<GridPalette>(assetPath);

        // We have to have the palette asset
        if (paletteAsset != null)
        {
            // Create the brush data and attach it to the palette asset
            var waveData = CreateInstance<TileMapAdjacencyData>();
            waveData.name = $"Palette Adjacency Data";
            var tileMap = paletteObj.GetComponentInChildren<Tilemap>();
            waveData.Generate(tileMap);

            AssetDatabase.AddObjectToAsset(waveData, paletteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // if waveData goes null, save failed
            if(waveData == null)
            {
                Debug.LogWarning("Could not save wave brush data asset");
            }

            return waveData;
        }

        return null;
    }


    protected override void OnDisable()
    {
        base.OnDisable();


    }

    /// <summary>
    /// Draw inspector information about the selected tile
    /// </summary>
    public override void OnSelectionInspectorGUI()
    {

        var select = GridSelection.position;

        // Inspecting multiple isn't really helpful
        if (tileMap != null && select != null)
        {
            var t = tileMap.GetTile(select.min);
            if (t is WaveTile waveTile)
            {
                // Update state from neighbors real quick

                GUILayout.Label(waveTile.Position.ToString());

                WaveBrushUtil.DrawAdjInspector(waveTile.State, waveBrush.TileData);

                if(GUILayout.Button("Recompute"))
                {
                    waveTile.State = waveTile.ComputeStateMatrix(select.min, null, tileMap);
                }
            }
            else if(t is Tile tile && waveBrush.TileData[tile] is TileAdjacencyMatrix def)
            {
                WaveBrushUtil.DrawAdjInspector(def, waveBrush.TileData);
            }
        }

        base.OnSelectionInspectorGUI();
    }

    /// <summary>
    /// Add wave tiles to the map preview
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="brushTarget"></param>
    /// <param name="position"></param>
    /// <param name="tool"></param>
    /// <param name="executing"></param>
    public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        base.OnPaintSceneGUI(grid, brushTarget, position, tool, executing);

        // Only support selecting one tile.
        if(brush.cellCount != 1)
        {
            Debug.LogWarning("Wave brush only supports selecting one tile at a time.");
            return;
        }

        // A tile must be selected
        if (!(brush.cells[0].tile is Tile))
        {
            return;
        }

        // Setup the brush target
        tileMap.ClearAllEditorPreviewTiles();
        if (tool != GridBrushBase.Tool.Paint)
            return;
        var pos = position.min;

        // Preview the intented tile
        var selectedTile = (Tile)brush.cells[0].tile;
        tileMap.SetEditorPreviewTile(pos, selectedTile);
        tileMap.SetEditorPreviewTransformMatrix(pos, tileMap.orientationMatrix);
        tileMap.SetEditorPreviewColor(pos, selectedTile.color);

        // Ensure the data asset has been loaded 
        waveBrush.TileData.PrimeDictionary();

        // Get information about the selected tile
        var tileWeights = waveBrush.TileData[selectedTile];

        if (tileWeights == null)
            return;

        var cellVectors = waveBrush.GetRadiusBrushCells();
        // Preview wave tiles where appropiate
        foreach (var n in cellVectors)
        {
            var p = pos + n;

            // Don't preview ontop of existing tiles. thanks
            var existingTile = tileMap.GetTile(p);
            if (existingTile != null && !waveBrush.AutoCollapse)
            {
                continue;
            }

            // Don't overrwrite the sprite we're paiting with
            if(existingTile is Tile t && selectedTile is Tile bTile)
            {
                if(t.sprite == bTile.sprite)
                {
                    continue;
                }
            }

            // Place the entropy tile into the preview
            var entpTile = CreateInstance<WaveTile>();
            entpTile.Data = waveBrush.TileData;
            entpTile.name = $"Entropy tile {p}";

            tileMap.SetEditorPreviewTile(p, entpTile);
            tileMap.SetEditorPreviewTransformMatrix(p, tileMap.orientationMatrix);
            tileMap.SetEditorPreviewColor(p, entpTile.color);
        }
    }

    /// <summary>
    /// Inspector button the generate adjacencie data
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Generate"))
        {
            var assetPath = AssetDatabase.GetAssetPath(paletteObject);
            var paletteAsset = AssetDatabase.LoadAssetAtPath<GridPalette>(assetPath);

            // We have to have the palette asset
            if (paletteAsset != null)
            {
                // Load the brush data in the palette asset if it exists
                var dataAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TileMapAdjacencyData)) as TileMapAdjacencyData;
                if (dataAsset != null)
                {
                    var tileMap = paletteObject.GetComponentInChildren<Tilemap>();
                    waveBrush.TileData.Generate(tileMap);

                    SaveAdjacencyData(waveBrush.TileData);
                }
            }
        }
    }

    private TileMapAdjacencyData GetAdjacencyData(GameObject paletteObj)
    {
        var assetPath = AssetDatabase.GetAssetPath(paletteObj);
        var paletteAsset = AssetDatabase.LoadAssetAtPath<GridPalette>(assetPath);

        // We have to have the palette asset
        if (paletteAsset != null)
        {
            // Load the brush data in the palette asset if it exists
            var dataAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TileMapAdjacencyData)) as TileMapAdjacencyData;
            if (dataAsset != null)
            {
                return dataAsset;
            }
            else
            {
                Debug.Log("Could not find wave brush data asset");
            }
        }
        return null;
    }

    private void SaveAdjacencyData(TileMapAdjacencyData data)
    {
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssetIfDirty(data);
        AssetDatabase.Refresh();
    }
}
