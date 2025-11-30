using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string cardID; // "2", "3", "6", "9", "10", "11"
    
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
        canvasGroup.alpha = 0.6f; // Légère transparence pendant le drag

        // Copie visuelle pour le drag
        tempCard = Instantiate(gameObject, canvas.transform);
        Destroy(tempCard.GetComponent<DraggableCard>()); // La copie n'a pas de logique
        
        tempCard.transform.SetAsLastSibling();
        
        // Important : la copie ne doit pas bloquer les rayons pour que le Raycast 3D marche dessous
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
        // Cette carte ne vise pas une tour, elle se déclenche n'importe où
        if (cardID == "10")
        {
            bool success = DrawingcardController.TryActivateCard10();
            
            if (success)
            {
                DrawingcardController.card10Used = true; // Consomme la carte
            }
            // Si pas d'ennemis (success = false), la carte n'est pas consommée
            
            ResetCard();
            return;
        }
        // ------------------------------------------

        // Raycast 3D pour trouver une tour sous la souris (Pour cartes 2, 3, 6, 9, 11)
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
        if (cardID == "2") 
        {
            return DrawingcardController.card2 && DrawingcardController.card2Timer <= 0f;
        }
        if (cardID == "3") 
        {
            return DrawingcardController.card3 && !DrawingcardController.card3Used;
        }
        if (cardID == "6")
        {
            return DrawingcardController.card6 && !DrawingcardController.card6Used;
        }
        if (cardID == "9")
        {
            return DrawingcardController.card9 && !DrawingcardController.card9Used;
        }
        if (cardID == "10")
        {
            // Active si on l'a piochée et qu'elle n'est pas encore utilisée
            return DrawingcardController.card10 && !DrawingcardController.card10Used;
        }
        // --- AJOUT CARTE 11 ---
        if (cardID == "11")
        {
            // Active si on l'a piochée (card11=true) et que le Cooldown est terminé
            return DrawingcardController.card11 && DrawingcardController.card11Timer <= 0f;
        }
        // ---------------------
        return false;
    }

    private void ApplyCardEffectToTower(TowerController tower)
    {
        switch (cardID)
        {
            case "2": // Orage
                tower.Orage(DrawingcardController.card2Damage, DrawingcardController.card2Duration);
                DrawingcardController.card2Timer = DrawingcardController.card2Cooldown; // Lance le cooldown
                break;

            case "3": // Buff
                tower.BuffFoudre();
                DrawingcardController.card3Used = true; // Marque comme utilisée
                break;

            case "6": // Scaling Speed
                tower.ActivateKillScaling();
                DrawingcardController.card6Used = true; // Marque comme utilisée
                break;
            
            case "9": 
                DrawingcardController.card9Used = true; 
                break;

            
            case "11":
                // On active la surcharge sur la tour ciblée
                tower.ActivateSurcharge(DrawingcardController.card11Duration);
                // On lance le cooldown global de 1min30
                DrawingcardController.card11Timer = DrawingcardController.card11Cooldown;
                break;
            // ----------------------------------
        }
    }

    private void ResetCard()
    {
        rectTransform.position = originalPosition;
    }
}