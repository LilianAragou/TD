using UnityEngine;

public class AOEManager : MonoBehaviour
{
    [Header("Stats")]
    public float damage;
    public float radius;
    public float duration;
    public float tickInterval;

    [Header("References")]
    public GameObject aoeEffect;
    public TowerController mummyTower; // On assume qu'elle est toujours assignée

    private float tickTimer;
    private float timeLeft;
    private int enemyLayerMask;

    void Start()
    {
        timeLeft = duration;
        tickTimer = 0f; // Dégâts immédiats au spawn

        // Optimisation : On récupère l'ID du layer "Enemy"
        // Si le layer n'existe pas, on scanne tout par défaut
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Default");

        // CORRECTION 3D : Mise à l'échelle sur X et Z (le sol)
        // On le fait une seule fois au démarrage
        if (aoeEffect != null)
        {
            aoeEffect.transform.localScale = new Vector3(radius * 2, 1f, radius * 2);
        }
    }

    void Update()
    {
        // Gestion de la durée de vie
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Gestion du rythme des dégâts (Tick)
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            ApplyDamageTick();
            tickTimer = tickInterval;
        }
    }

    void ApplyDamageTick()
    {
        // PHYSIQUE 3D : On détecte les sphères/capsules dans la zone
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, enemyLayerMask);

        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemyHealth = hit.GetComponent<EnemyController>();
                if (enemyHealth != null)
                {
                    // Application directe des dégâts via la tour
                    // Puisque la tour ne peut pas disparaître, on accède direct à ses stats
                    enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                    
                    // Mise à jour des dégâts stockés par la tour (pour les ultis/stats)
                    mummyTower.stockedDamage += damage * (1 + enemyHealth.debuffDamage / 100f);
                }
            }
        }
    }

    // Gizmo pour visualiser la zone dans l'éditeur Unity
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange semi-transparent
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}