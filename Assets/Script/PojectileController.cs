using UnityEngine;
using System.Collections.Generic;

public class PojectileController : MonoBehaviour
{
    [Header("Configuration")]
    public Transform target;
    public float damage; // Contient les dégâts de base (ex: 40) déjà multipliés par les cartes
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

    private int enemyLayerMask;

    void Start()
    {
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Default");
    }

    void Update()
    {
        lifeTime += Time.deltaTime;

        // --- MOUVEMENT STANDARD VERS CIBLE ---
        if (target != null && !isPatrol)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0; 

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                transform.position += direction.normalized * speed * Time.deltaTime;
            }

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
                
                float x = centerPointForPatrol.position.x + radiusForPatrol * Mathf.Cos(angleForPatrol);
                float z = centerPointForPatrol.position.z + radiusForPatrol * Mathf.Sin(angleForPatrol);
                
                transform.position = new Vector3(x, 0, z);
                
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
            Destroy(gameObject);
        }

        if (lifeTime > maxLifeTime && towerID != "TwinWind")
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // TwinWind traverse, les autres s'arrêtent
            if (towerID == "TwinWind") HitTarget(other.gameObject);
            else HitTarget(other.gameObject);
        }
    }

    void HitTarget(GameObject hitObject)
    {
        EnemyController enemyHealth = hitObject.GetComponent<EnemyController>();
        if (enemyHealth == null) return;

        // La variable 'damage' contient le BaseDamage (40) * Multiplicateurs (Carte 11, Carte 5...)
        
        switch (towerID)
        {
            // --- NOUVEAU : FORGE INFERNALE ---
            case "ForgeInfernale":
                // Consigne : "40 + 1% PV Max sur 1s" avec "1 tir / 0.2s" (donc 5 tirs/s)
                // On doit donc diviser les dégâts par 5 pour avoir la valeur par projectile.
                
                // 1. Partie Base (40)
                // 'damage' contient déjà 40 * Multiplicateurs. On divise par 5.
                float baseDmgPerShot = damage * 0.2f; 

                // 2. Partie % PV Max (1%)
                float hpPercentDamage = enemyHealth.maxHealth * 0.01f;

                // On divise par 5 (pour répartir le 1% sur la seconde entière)
                float hpDmgPerShot = hpPercentDamage * 0.2f;

                // 3. Total
                ApplyDamage(enemyHealth, baseDmgPerShot + hpDmgPerShot);
                Destroy(gameObject);
                break;
            // ---------------------------------

            case "ThorTotem":
                ApplyDamage(enemyHealth, damage);
                Destroy(gameObject);
                break;

            case "ThorHammer":
                bool isKill = (enemyHealth.health <= damage);
                ApplyDamage(enemyHealth, damage);
                if (isKill)
                {
                    int bounceCount = DrawingcardController.card1 ? 2 : 1;
                    Ricochet(bounceCount, hitObject.transform);
                }
                Destroy(gameObject);
                break;

            case "ThorPillar":
                Collider[] areaHits = Physics.OverlapSphere(transform.position, 1.5f, enemyLayerMask);
                foreach (var hit in areaHits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        EnemyController areaEnemy = hit.GetComponent<EnemyController>();
                        if (areaEnemy != null)
                        {
                            ApplyDamage(areaEnemy, damage);
                            float stunTime = 0.75f;
                            if (DrawingcardController.card4) stunTime += 0.75f; 
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
                    float execDmg = enemyHealth.health + 1f;
                    enemyHealth.TakeDamage(execDmg, mummyTower != null ? mummyTower.dmgType : DamageType.Base, mummyTower);
                    if (mummyTower) mummyTower.stockedDamage += enemyHealth.health * 0.2f;
                }
                Destroy(gameObject);
                break;

            case "TwinWind":
                Vector3 pushDir = (hitObject.transform.position - transform.position).normalized;
                pushDir.y = 0; 
                enemyHealth.getKnockBacked(1.5f, pushDir);
                ApplyDamage(enemyHealth, damage);
                break;
                
            default:
                ApplyDamage(enemyHealth, damage);
                Destroy(gameObject);
                break;
        }
    }

    void ApplyDamage(EnemyController enemy, float amount)
    {
        if (enemy == null) return;
        
        DamageType type = (mummyTower != null) ? mummyTower.dmgType : DamageType.Base;
        
        // On passe mummyTower pour les compteurs de kill (Carte 6)
        enemy.TakeDamage(amount, type, mummyTower);

        // --- CARTE 15 : FLAMME SOLITAIRE ---
        // Si la tour est isolée, chaque "Toucher" inflige 30 dégâts de FEU supplémentaires.
        // Comme ApplyDamage est appelé pour tout impact (Ricochet, AOE, Tir direct), cela couvre tout.
        if (mummyTower != null && mummyTower.hasSolitaryFlame)
        {
            enemy.TakeDamage(30f, DamageType.Feu, mummyTower);
        }
        // -----------------------------------
        
        if (mummyTower != null && mummyTower.electriqueCourantActivated)
        {
            enemy.TakeDamage(20, DamageType.Foudre, mummyTower);
        }
        
        if (mummyTower != null)
        {
            mummyTower.stockedDamage += amount * (1 + enemy.debuffDamage / 100f);
        }
    }

    void Ricochet(int targetsToFind, Transform deadTargetTransform)
    {
        if (mummyTower == null) return;

        Collider[] nearby = Physics.OverlapSphere(transform.position, mummyTower.attackRange, enemyLayerMask);
        int count = 0;
        foreach (Collider col in nearby)
        {
            if (count >= targetsToFind) break; 
            if (col.transform == deadTargetTransform || !col.CompareTag("Enemy")) continue;

            EnemyController chainEnemy = col.GetComponent<EnemyController>();
            if (chainEnemy != null && chainEnemy.health > 0)
            {
                ApplyDamage(chainEnemy, damage);
                count++;
            }
        }
    }
}