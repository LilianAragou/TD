using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string cardID; // "2", "3", "6", "9", "10", "11", "13", "15", "17"
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private GameObject tempCard;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    // --- Gestion visuelle (Griser si inutilisable) ---
    void Update()
    {
        if (canvasGroup == null) return;

        bool active = IsCardActive();

        if (active)
        {
            canvasGroup.alpha = 1f; // Visible
            canvasGroup.blocksRaycasts = true; // On peut cliquer/drag
        }
        else
        {
            canvasGroup.alpha = 0.3f; // Grisé / Transparent
            canvasGroup.blocksRaycasts = false; // On ne peut pas drag
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsCardActive()) return;

        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.6f; 

        // Copie visuelle pour le drag
        tempCard = Instantiate(gameObject, canvas.transform);
        Destroy(tempCard.GetComponent<DraggableCard>()); 
        
        tempCard.transform.SetAsLastSibling();
        
        CanvasGroup tempCG = tempCard.GetComponent<CanvasGroup>();
        if (tempCG) tempCG.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (tempCard == null) return;

        RectTransform tempRect = tempCard.GetComponent<RectTransform>();
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out pos
        );
        tempRect.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f; // Reset opacité
        if (tempCard != null) Destroy(tempCard);

        if (!IsCardActive()) 
        {
            ResetCard();
            return;
        }

        // --- CAS SPÉCIAL CARTE 10 (Sort Global) ---
        if (cardID == "10")
        {
            bool success = DrawingcardController.TryActivateCard10();
            
            if (success)
            {
                DrawingcardController.card10Used = true; 
            }
            ResetCard();
            return;
        }
        // ------------------------------------------

        // Raycast 3D pour trouver une tour sous la souris
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            TowerController tower = hit.collider.GetComponent<TowerController>();
            // Si le collider est sur un enfant, on cherche le parent
            if (tower == null) tower = hit.collider.GetComponentInParent<TowerController>();

            if (tower != null)
            {
                ApplyCardEffectToTower(tower);
                ResetCard();
                return;
            }
        }

        ResetCard();
    }

    // Vérifie l'état statique dans DrawingcardController
    private bool IsCardActive()
    {
        if (cardID == "2") return DrawingcardController.card2 && DrawingcardController.card2Timer <= 0f;
        if (cardID == "3") return DrawingcardController.card3 && !DrawingcardController.card3Used;
        if (cardID == "6") return DrawingcardController.card6 && !DrawingcardController.card6Used;
        if (cardID == "9") return DrawingcardController.card9 && !DrawingcardController.card9Used;
        if (cardID == "10") return DrawingcardController.card10 && !DrawingcardController.card10Used;
        if (cardID == "11") return DrawingcardController.card11 && DrawingcardController.card11Timer <= 0f;
        if (cardID == "13") return DrawingcardController.card13 && !DrawingcardController.card13Used;
        if (cardID == "15") return DrawingcardController.card15 && !DrawingcardController.card15Used;
        
        // --- CARTE 17 ---
        if (cardID == "17") return DrawingcardController.card17 && !DrawingcardController.card17Used;
        // ----------------
        
        return false;
    }

    private void ApplyCardEffectToTower(TowerController tower)
    {
        switch (cardID)
        {
            case "2": // Orage
                tower.Orage(DrawingcardController.card2Damage, DrawingcardController.card2Duration);
                DrawingcardController.card2Timer = DrawingcardController.card2Cooldown; 
                break;

            case "3": // Buff
                tower.BuffFoudre();
                DrawingcardController.card3Used = true; 
                break;

            case "6": // Scaling Speed
                tower.ActivateKillScaling();
                DrawingcardController.card6Used = true; 
                break;
            
            case "9": 
                DrawingcardController.card9Used = true; 
                break;

            case "11": // Surcharge
                tower.ActivateSurcharge(DrawingcardController.card11Duration);
                DrawingcardController.card11Timer = DrawingcardController.card11Cooldown;
                break;

            case "13": // Chaude Précision
                if (tower.towerID == "ForgeInfernale")
                {
                    tower.ActivateTwinForge();
                    DrawingcardController.card13Used = true; 
                }
                else
                {
                    Debug.Log("Cette carte ne fonctionne que sur une Forge Infernale !");
                }
                break;

            // --- CARTE 15 : FLAMME SOLITAIRE ---
            case "15":
                TileManager tm = FindObjectOfType<TileManager>();
                Tile myTile = tower.GetComponentInParent<Tile>();

                if (tm != null && myTile != null)
                {
                    bool isIsolated = true;
                    float minDistance = 3.0f;

                    for (int x = 0; x < tm.gridSize; x++)
                    {
                        for (int y = 0; y < tm.gridSize; y++)
                        {
                            GameObject tileObj = tm.tiles[x, y];
                            if (tileObj == null) continue;

                            Tile otherTile = tileObj.GetComponent<Tile>();
                            if (otherTile == myTile) continue;

                            if (otherTile.HasOccupant)
                            {
                                Transform occupant = otherTile.transform.GetChild(otherTile.transform.childCount - 1);
                                if (occupant.CompareTag("Tour"))
                                {
                                    float dist = Vector2.Distance(new Vector2(myTile.x, myTile.y), new Vector2(x, y));
                                    if (dist < minDistance)
                                    {
                                        isIsolated = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!isIsolated) break;
                    }

                    if (isIsolated)
                    {
                        tower.ActivateSolitaryFlame();
                        DrawingcardController.card15Used = true;
                        Debug.Log("Flamme Solitaire activée !");
                    }
                    else
                    {
                        Debug.Log("Condition échouée : La tour n'est pas isolée (Min 3 tuiles).");
                    }
                }
                else Debug.LogWarning("Erreur : Impossible de trouver le TileManager ou la Tuile.");
                break;

            // --- AJOUT CARTE 17 : INCANDESCENCE ---
            case "17":
                tower.ActivateIncandescence();
                DrawingcardController.card17Used = true;
                Debug.Log($"Incandescence activée sur {tower.towerID} !");
                break;
            // -------------------------------------
        }
    }

    private void ResetCard()
    {
        rectTransform.position = originalPosition;
    }
}