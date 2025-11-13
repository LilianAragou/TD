using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string cardID; // "2" ou "3" (carte active)
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private GameObject tempCard; // copie temporaire

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsCardActive()) return;

        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.6f;

        // Crée une copie visuelle temporaire
        tempCard = Instantiate(gameObject, canvas.transform);
        Destroy(tempCard.GetComponent<DraggableCard>()); // la copie ne doit pas être draggable
        tempCard.transform.SetAsLastSibling(); // s'affiche au-dessus
        tempCard.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (tempCard == null) return;

        // Fait suivre la souris par la copie
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
        if (!IsCardActive()) return;

        canvasGroup.alpha = 1f;
        if (tempCard != null) Destroy(tempCard);

        // Raycast vers la scène pour détecter une tour
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider != null)
        {
            TowerController tower = hit.collider.GetComponent<TowerController>();
            if (tower != null)
            {
                ApplyCardEffectToTower(tower);
                ResetCard();
                return;
            }
        }

        // Sinon, retour à la position d’origine
        rectTransform.position = originalPosition;
    }

    private bool IsCardActive()
    {
        if (cardID == "2") return DrawingcardController.card2 && DrawingcardController.card2Timer <= 0f;
        if (cardID == "3") return DrawingcardController.card3 && !DrawingcardController.card3Used;
        return false;
    }

    private void ApplyCardEffectToTower(TowerController tower)
    {
        Debug.Log($"Card {cardID} applied to tower: {tower.name}");

        switch (cardID)
        {
            case "2":
                tower.Orage(
                    DrawingcardController.card2Damage,
                    DrawingcardController.card2Duration
                );
                DrawingcardController.card2Timer = DrawingcardController.card2Cooldown;
                break;

            case "3":
                tower.BuffFoudre();
                DrawingcardController.card3Used = true;
                break;
        }
    }

    private void ResetCard()
    {
        rectTransform.position = originalPosition;
    }
}
