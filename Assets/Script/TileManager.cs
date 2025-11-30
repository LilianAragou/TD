using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Grille")]
    public int gridSize = 5;            // 5x5
    public float tileSpacing = 1.1f;    // Espace entre les centres des tuiles
    public GameObject tilePrefab;

    [Header("Prefabs")]
    public GameObject nexusPrefab;      // à poser au centre

    [Header("Layers & Tags")]
    public string tileLayerName = "Tile"; // IMPORTANT : Créer ce Layer dans Unity

    // Accesseur public pour d'autres scripts (TowerController s'en servira)
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
            Debug.LogError("TileManager: Aucun tilePrefab assigné !");
            return;
        }

        tiles = new GameObject[gridSize, gridSize];
        
        // Calcul de l'offset pour que la grille soit centrée sur (0,0,0)
        float offset = (gridSize - 1) / 2f;

        // Récupération sécurisée du Layer
        int tileLayer = LayerMask.NameToLayer(tileLayerName);
        if (tileLayer < 0) 
        {
            Debug.LogWarning($"Attention: Le Layer '{tileLayerName}' n'existe pas. Les tuiles seront sur 'Default'.");
            tileLayer = LayerMask.NameToLayer("Default");
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Positionnement sur le plan X/Z (Y=0)
                Vector3 pos = new Vector3((x - offset) * tileSpacing, 0f, (y - offset) * tileSpacing);
                
                // Instantiation
                GameObject tileObj = Instantiate(tilePrefab, pos, tilePrefab.transform.rotation, transform);
                tileObj.name = $"Tile_{x}_{y}";
                tileObj.layer = tileLayer;

                // --- SÉCURITÉ COLLIDER ---
                // On ne rajoute un BoxCollider que si le prefab n'en a pas déjà un (MeshCollider, etc.)
                Collider col = tileObj.GetComponent<Collider>();
                if (col == null) 
                {
                    col = tileObj.AddComponent<BoxCollider>();
                }
                // Important pour le Raycast de placement de tour : ce n'est PAS un trigger
                col.isTrigger = false; 

                // --- MODIFICATION : Assignation des coordonnées ---
                Tile tileScript = tileObj.GetComponent<Tile>();
                if (tileScript == null) 
                {
                    tileScript = tileObj.AddComponent<Tile>();
                }

                // On injecte les coordonnées de la grille dans la tuile
                tileScript.x = x;
                tileScript.y = y;
                // --------------------------------------------------

                tiles[x, y] = tileObj;
            }
        }
    }

    void PlaceCenterNexus()
    {
        if (!nexusPrefab)
        {
            Debug.LogWarning("TileManager: Aucun nexusPrefab assigné.");
            return;
        }

        // Trouver le centre de la grille
        int c = gridSize / 2; 
        
        // Sécurité si la grille n'a pas été créée
        if (tiles == null || tiles[c,c] == null) return;

        var centerTile = tiles[c, c];
        var tileComp = centerTile.GetComponent<Tile>();

        if (tileComp != null && tileComp.CanAccept(nexusPrefab))
        {
            bool ok = tileComp.TryPlace(nexusPrefab);
            if (ok)
            {
                // Récupère l'objet Nexus qu'on vient de placer (c'est le dernier enfant)
                Transform nexusTransform = centerTile.transform.GetChild(centerTile.transform.childCount - 1);
                
                // Force le Tag "Nexus" pour la logique de jeu
                if (!nexusTransform.CompareTag("Nexus")) 
                {
                    nexusTransform.tag = "Nexus";
                }
                
                // Ajoute un Collider au Nexus si nécessaire (pour être ciblé)
                if (nexusTransform.GetComponent<Collider>() == null)
                {
                    var box = nexusTransform.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = false; 
                    // Ajuste la taille selon le modèle (approximatif)
                    box.size = new Vector3(1, 2, 1); 
                    box.center = new Vector3(0, 1, 0);
                }
            }
        }
        else
        {
            Debug.LogWarning("TileManager: Impossible de placer le Nexus au centre (Tuile occupée ou invalide).");
        }
    }
}