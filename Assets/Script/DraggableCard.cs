using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string cardID; // "2" ou "3"
    
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

    // --- AJOUT : Gestion visuelle (Griser) ---
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
    // -----------------------------------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsCardActive()) return;

        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.6f; // Légère transparence pendant le drag

        // Copie visuelle
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
        canvasGroup.alpha = 1f; // Reset opacité si on lâche (sera écrasé par Update de toute façon)
        if (tempCard != null) Destroy(tempCard);

        if (!IsCardActive()) 
        {
            ResetCard();
            return;
        }

        // Raycast 3D
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            TowerController tower = hit.collider.GetComponent<TowerController>();
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
            // Dispo si on possède la carte ET que le timer est fini (<= 0)
            return DrawingcardController.card2 && DrawingcardController.card2Timer <= 0f;
        }
        if (cardID == "3") 
        {
            // Dispo si on possède la carte ET qu'elle n'est pas marquée comme utilisée
            return DrawingcardController.card3 && !DrawingcardController.card3Used;
        }
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
        }
    }

    private void ResetCard()
    {
        rectTransform.position = originalPosition;
    }
}