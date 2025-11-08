using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragTowerFromUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Références")]
    public GameObject towerPrefab;        // prefab monde (tag "Tour")
    public Canvas dragRootCanvas;         // Canvas principal UI
    public Image iconForDrag;             // icône source (sinon prend l'image du composant)

    [Header("Réglages")]
    public string tileLayerName = "Tile";
    public bool requireEmptyTile = true;

    // internes
    private RectTransform dragIconRT;
    private Image dragIconImg;
    private CanvasGroup dragIconCG;
    private int tileLayerMask;
    private Tile hoveredTile;

    void Awake()
    {
        tileLayerMask = 1 << LayerMask.NameToLayer(tileLayerName);

        if (!dragRootCanvas)
            dragRootCanvas = GetComponentInParent<Canvas>();

        if (!iconForDrag)
            iconForDrag = GetComponent<Image>(); // fallback
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!towerPrefab || !dragRootCanvas) return;

        // Crée l'icône qui suit la souris
        GameObject go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(dragRootCanvas.transform, false);
        dragIconRT = go.GetComponent<RectTransform>();
        dragIconImg = go.GetComponent<Image>();
        dragIconCG = go.GetComponent<CanvasGroup>();

        dragIconImg.raycastTarget = false;
        dragIconCG.blocksRaycasts = false;
        dragIconCG.alpha = 0.85f;

        if (iconForDrag) dragIconImg.sprite = iconForDrag.sprite;
        dragIconRT.sizeDelta = iconForDrag ? iconForDrag.rectTransform.sizeDelta : new Vector2(100, 100);

        UpdateDragIconPosition(eventData);
        UpdateHoveredTileHighlight(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragIconRT) return;
        UpdateDragIconPosition(eventData);
        UpdateHoveredTileHighlight(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconRT)
        {
            Destroy(dragIconRT.gameObject);
            dragIconRT = null;
        }

        // Tentative de placement
        Tile targetTile = GetTileUnderPointer(eventData);
        if (targetTile && targetTile.CanAccept(towerPrefab) && (!requireEmptyTile || !targetTile.HasOccupant))
        {
            bool ok = targetTile.TryPlace(towerPrefab);
            if (ok)
            {
                var placed = targetTile.transform.GetChild(targetTile.transform.childCount - 1).gameObject;
                if (placed && placed.tag != "Tour") placed.tag = "Tour";
            }
        }

        // Nettoie le highlight
        ClearHovered();
    }

    private void UpdateDragIconPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint);
        dragIconRT.anchoredPosition = localPoint;
    }

    private void UpdateHoveredTileHighlight(PointerEventData eventData)
    {
        Tile t = GetTileUnderPointer(eventData);
        if (t != hoveredTile)
        {
            ClearHovered();
            hoveredTile = t;
        }

        if (!hoveredTile) return;

        bool can = hoveredTile.CanAccept(towerPrefab) && (!requireEmptyTile || !hoveredTile.HasOccupant);
        if (can) hoveredTile.SetHighlightOK();
        else hoveredTile.SetHighlightBad();
    }

    private Tile GetTileUnderPointer(PointerEventData eventData)
    {
        // Convertit écran → monde
        Vector3 world;
        if (eventData.pressEventCamera)
            world = eventData.pressEventCamera.ScreenToWorldPoint(eventData.position);
        else
            world = Camera.main.ScreenToWorldPoint(eventData.position);

        Vector2 world2D = new Vector2(world.x, world.y);

        var hit = Physics2D.OverlapPoint(world2D, tileLayerMask);
        if (!hit) return null;

        var t = hit.GetComponent<Tile>();
        if (!t) t = hit.GetComponentInParent<Tile>();
        return t;
    }

    private void ClearHovered()
    {
        if (hoveredTile)
        {
            hoveredTile.SetHighlightNone();
            hoveredTile = null;
        }
    }
}
