using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Grille")]
    public int gridSize = 5;            // 5x5
    public float tileSpacing = 1.1f;
    public GameObject tilePrefab;

    [Header("Prefabs")]
    public GameObject nexusPrefab;      // à poser au centre (tag "Nexus")

    [Header("Layers & Tags")]
    public string tileLayerName = "Tile"; // créer le Layer "Tile" dans Project Settings

    public GameObject[,] tiles { get; private set; }

    void Start()
    {
        CreateGrid();
        PlaceCenterNexus();
    }

    void CreateGrid()
    {
        if (!tilePrefab)
        {
            Debug.LogError("TileManager: Aucun tilePrefab assigné.");
            return;
        }

        tiles = new GameObject[gridSize, gridSize];
        float offset = (gridSize - 1) / 2f;

        int tileLayer = LayerMask.NameToLayer(tileLayerName);
        if (tileLayer < 0) Debug.LogWarning($"Layer \"{tileLayerName}\" introuvable. Pense à le créer.");

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 pos = new Vector3((x - offset) * tileSpacing,  0f, (y - offset) * tileSpacing);
                // Respecte la rotation du prefab
                GameObject tile = Instantiate(tilePrefab, pos, tilePrefab.transform.rotation, transform);
                tile.name = $"Tile_{x}_{y}";
                if (tileLayer >= 0) tile.layer = tileLayer;

                // Assure Collider 3D non-trigger pour le raycast
                var col = tile.GetComponent<Collider>();
                if (!col) col = tile.AddComponent<BoxCollider>();
                col.isTrigger = false;

                // Assure Tile.cs
                if (!tile.GetComponent<Tile>()) tile.AddComponent<Tile>();

                tiles[x, y] = tile;
            }
        }
    }

    void PlaceCenterNexus()
    {
        if (!nexusPrefab)
        {
            Debug.LogWarning("TileManager: Aucun nexusPrefab assigné, le centre restera vide.");
            return;
        }

        int c = gridSize / 2; // pour 5 -> 2
        var centerTile = tiles[c, c];
        var tileComp = centerTile.GetComponent<Tile>();

        if (tileComp && tileComp.CanAccept(nexusPrefab))
        {
            var ok = tileComp.TryPlace(nexusPrefab);
            if (!ok)
                Debug.LogWarning("TileManager: Impossible de poser le Nexus (tuile occupée ?).");
            else
            {
                var placed = centerTile.transform.GetChild(centerTile.transform.childCount - 1).gameObject;
                if (placed && placed.tag != "Nexus") placed.tag = "Nexus";
            }
        }
        else
        {
            Debug.LogWarning("TileManager: La tuile centrale ne peut pas accepter le Nexus.");
        }
    }
}
