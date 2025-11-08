using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    [Header("Règles")]
    public bool singleOccupant = true;

    [Header("Couleurs de feedback")]
    public Color normalColor = Color.white;
    public Color okColor = new Color(0.5f, 1f, 0.5f, 1f);    // vert doux
    public Color badColor = new Color(1f, 0.5f, 0.5f, 1f);   // rouge doux

    private SpriteRenderer sr;

    public bool HasOccupant => transform.childCount > 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = normalColor;
    }

    /// <summary>Indique si cette tuile peut accepter un prefab (ex: Tour, Nexus).</summary>
    public bool CanAccept(GameObject prefabToPlace)
    {
        if (singleOccupant && HasOccupant) return false;
        return prefabToPlace != null;
    }

    /// <summary>Instancie le prefab centré et le parent à la tile.</summary>
    public bool TryPlace(GameObject prefabToPlace)
    {
        if (!CanAccept(prefabToPlace)) return false;

        var go = Instantiate(prefabToPlace, transform.position, Quaternion.identity, transform);
        go.transform.localPosition = Vector3.zero; // centré
        return true;
    }

    public void SetHighlightNone()
    {
        if (sr) sr.color = normalColor;
    }

    public void SetHighlightOK()
    {
        if (sr) sr.color = okColor;
    }

    public void SetHighlightBad()
    {
        if (sr) sr.color = badColor;
    }
}
