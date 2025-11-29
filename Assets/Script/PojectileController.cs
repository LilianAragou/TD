using UnityEngine;
using System.Collections.Generic;

public class PojectileController : MonoBehaviour
{
    [Header("Configuration")]
    public Transform target;
    public float damage;
    public float speed;
    public string towerID;
    public TowerController mummyTower; // Référence à la tour mère
    public GameObject aoePrefab;

    [Header("Patrouille (TwinWind)")]
    public Transform centerPointForPatrol;
    public float radiusForPatrol = 2f;
    public bool isPatrol = false;
    public float angleForPatrol = 0f;

    [Header("États")]
    public bool isPanickShot = false;
    private float lifeTime = 0f;
    private float maxLifeTime = 5f;

    // Optimisation : pour ne pas chercher les ennemis sur toute la map
    private int enemyLayerMask;

    void Start()
    {
        // On récupère le mask pour Physics.OverlapSphere
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Default");
    }

    void Update()
    {
        lifeTime += Time.deltaTime;

        // --- MOUVEMENT STANDARD VERS CIBLE ---
        if (target != null && !isPatrol)
        {
            // Mouvement sur le plan X/Z uniquement (on ignore la hauteur Y pour éviter de tirer dans le sol/ciel)
            Vector3 direction = target.position - transform.position;
            direction.y = 0; 

            if (direction != Vector3.zero)
            {
                // Rotation visuelle vers la cible
                transform.rotation = Quaternion.LookRotation(direction);
                transform.position += direction.normalized * speed * Time.deltaTime;
            }

            // Détection manuelle de proximité (backup si la collision physique rate)
            if (Vector3.Distance(transform.position, target.position) < 0.2f)
            {
                HitTarget(target.gameObject);
            }
        }
        // --- MOUVEMENT ORBITAL (TWINWIND) ---
        else if (towerID == "TwinWind")
        {
            if (centerPointForPatrol != null && isPatrol)
            {
                angleForPatrol += speed * Time.deltaTime;
                
                // Calcul trigonométrique sur X et Z (Plan horizontal 3D)
                float x = centerPointForPatrol.position.x + radiusForPatrol * Mathf.Cos(angleForPatrol);
                float z = centerPointForPatrol.position.z + radiusForPatrol * Mathf.Sin(angleForPatrol);
                
                transform.position = new Vector3(x, 0, z); // Y=1f pour flotter au-dessus du sol
                
                // Rotation tangentielle (pour faire joli)
                Vector3 nextPos = new Vector3(
                    centerPointForPatrol.position.x + radiusForPatrol * Mathf.Cos(angleForPatrol + 0.1f),
                    0,
                    centerPointForPatrol.position.z + radiusForPatrol * Mathf.Sin(angleForPatrol + 0.1f)
                );
                transform.LookAt(nextPos);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        // --- NETTOYAGE ---
        else if (target == null && !isPatrol && !isPanickShot)
        {
            // Si la cible est morte avant l'impact, on détruit le projectile
            Destroy(gameObject);
        }

        // Sécurité temps de vie max
        if (lifeTime > maxLifeTime && towerID != "TwinWind")
        {
            Destroy(gameObject);
        }
    }

    // --- GESTION DES COLLISIONS 3D ---
    
    void OnTriggerEnter(Collider other)
    {
        // Si on touche un ennemi
        if (other.CompareTag("Enemy"))
        {
            // Pour TwinWind, on touche sans détruire
            if (towerID == "TwinWind")
            {
                HitTarget(other.gameObject); // Applique dégâts + recul
                // On ne détruit PAS le projectile ici car c'est une aura
            }
            else
            {
                // Pour les autres, on touche et on détruit (géré dans HitTarget)
                HitTarget(other.gameObject);
            }
        }
    }

    // --- LOGIQUE D'IMPACT PAR TYPE DE TOUR ---

    void HitTarget(GameObject hitObject)
    {
        EnemyController enemyHealth = hitObject.GetComponent<EnemyController>();
        if (enemyHealth == null) return;

        // Application des dégâts de base
        // Note: La variable 'damage' a déjà été boostée par la Carte 5 dans TowerController.FireProjectile
        
        switch (towerID)
        {
            case "ThorTotem":
                ApplyDamage(enemyHealth, damage);
                Destroy(gameObject);
                break;

            case "ThorHammer":
                // Logique : Si on tue la cible, on ricoche
                bool isKill = (enemyHealth.health <= damage);
                ApplyDamage(enemyHealth, damage);

                if (isKill)
                {
                    // --- CARTE 1 : Double Ricochet ---
                    // "Toutes les tours du marteau ricochent 2 éclairs au lieu d'un seul sur un kill"
                    int bounceCount = DrawingcardController.card1 ? 2 : 1;
                    
                    Ricochet(bounceCount, hitObject.transform);
                }
                
                Destroy(gameObject);
                break;

            case "ThorPillar":
                // Dégâts de zone
                Collider[] areaHits = Physics.OverlapSphere(transform.position, 1.5f, enemyLayerMask);
                foreach (var hit in areaHits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        EnemyController areaEnemy = hit.GetComponent<EnemyController>();
                        if (areaEnemy != null)
                        {
                            ApplyDamage(areaEnemy, damage);
                            
                            // --- CARTE 4 : Stun Augmenté ---
                            // "Augmente la durée de stun de pilier orageux de 0.75s"
                            float stunTime = 0.75f;
                            if (DrawingcardController.card4) stunTime += 0.75f; // Total 1.5s

                            areaEnemy.GetStunned(stunTime);
                        }
                    }
                }
                Destroy(gameObject);
                break;

            case "SkadiTower":
                enemyHealth.getSlowed(10f, 3f, towerID);
                ApplyDamage(enemyHealth, damage);
                Destroy(gameObject);
                break;

            case "SkadiCryo":
                ApplyDamage(enemyHealth, damage);
                enemyHealth.SkadiShotCalculator += 1f;
                Destroy(gameObject);
                break;

            case "NecroTower":
                ApplyDamage(enemyHealth, damage);
                enemyHealth.DebuffDamage();
                Destroy(gameObject);
                break;

            case "HelheimSanctuary":
                if (enemyHealth.health <= damage && mummyTower != null)
                {
                    mummyTower.ReduceCooldown(0.1f);
                }
                ApplyDamage(enemyHealth, damage);
                enemyHealth.DebuffDamage();
                Destroy(gameObject);
                break;

            case "VoidEye":
                ApplyDamage(enemyHealth, damage);
                if (enemyHealth.health <= enemyHealth.maxHealth * 0.2f)
                {
                    // Exécution des faibles PV
                    float execDmg = enemyHealth.health + 1f;
                    enemyHealth.TakeDamage(execDmg, mummyTower != null ? mummyTower.dmgType : DamageType.Base);
                    if (mummyTower) mummyTower.stockedDamage += enemyHealth.health * 0.2f;
                }
                Destroy(gameObject);
                break;

            case "VolcanoTower":
                // Spawn de la zone de lave
                GameObject aoe = Instantiate(aoePrefab, transform.position, Quaternion.identity);
                AOEManager aoeManager = aoe.GetComponent<AOEManager>();
                if (aoeManager != null)
                {
                    aoeManager.mummyTower = mummyTower;
                    aoeManager.damage = damage;
                    aoeManager.radius = 0.8f;
                    aoeManager.duration = 10f;
                    aoeManager.tickInterval = 0.1f;
                }
                Destroy(gameObject);
                break;

            case "TwinWind":
                // Recul + Dégâts (sans détruire l'aura)
                Vector3 pushDir = (hitObject.transform.position - transform.position).normalized;
                pushDir.y = 0; // Pas de vol plané
                enemyHealth.getKnockBacked(1.5f, pushDir);
                ApplyDamage(enemyHealth, damage);
                break;
                
            // ... Ajoutez les autres cas (InfernalForge, Baldr, etc.) ici si besoin ...
            
            default:
                // Tir standard
                ApplyDamage(enemyHealth, damage);
                Destroy(gameObject);
                break;
        }
    }

    // Helper pour appliquer les dégâts
    void ApplyDamage(EnemyController enemy, float amount)
    {
        if (enemy == null) return;
        
        // On utilise le type de la tour, sinon Physique par défaut
        DamageType type = (mummyTower != null) ? mummyTower.dmgType : DamageType.Base;
        enemy.TakeDamage(amount, type);

        // Stockage des stats
        if (mummyTower != null)
        {
            mummyTower.stockedDamage += amount * (1 + enemy.debuffDamage / 100f);
        }
    }

    // Logique de Ricochet (ThorHammer / Carte 1)
    void Ricochet(int targetsToFind, Transform deadTargetTransform)
    {
        if (mummyTower == null) return;

        // Cherche les ennemis proches
        Collider[] nearby = Physics.OverlapSphere(transform.position, mummyTower.attackRange, enemyLayerMask);
        
        int count = 0;
        foreach (Collider col in nearby)
        {
            if (count >= targetsToFind) break; // On s'arrête si on a trouvé le nombre max de cibles (1 ou 2)
            
            // On ne re-touche pas le mort et on vérifie le tag
            if (col.transform == deadTargetTransform || !col.CompareTag("Enemy")) continue;

            EnemyController chainEnemy = col.GetComponent<EnemyController>();
            if (chainEnemy != null && chainEnemy.health > 0)
            {
                // On applique les dégâts directement (effet éclair instantané)
                ApplyDamage(chainEnemy, damage);
                
                // ICI : Ajouter un effet visuel (LineRenderer) entre deadTargetTransform et chainEnemy.transform serait top
                
                count++;
            }
        }
    }
}