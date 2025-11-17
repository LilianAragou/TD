using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TowerController : MonoBehaviour
{
    [SerializeField] public float attackRange = 5f;
    [SerializeField] public float attackDamage = 10f;
    [SerializeField] public float attackCooldown = 1f;
    [SerializeField] public float projectileSpeed = 10f;
    [HideInInspector] public float cooldownTime = 0f;
    public Transform target;
    private List<Transform> attackedTargets = new List<Transform>();
    public string enemyTag = "Enemy";
    public DamageType dmgType;
    public string towerID;
    public Transform firePoint;
    public GameObject projectilePrefab;
    public GameObject rangeaura;
    public bool hasAFirstAttack = false;
    public bool isAuraTower = false;
    public bool isProjectileTower = true;
    public bool hasProjectileAura = false;
    public float rotationSpeed = 5f;
    public float projectileCount = 1f;
    public GameObject auraProjectilePrefab;
    private bool isFirstShot;
    public float firstShotCD;
    public float numberOfTargets = 1f;
    public bool hasPanickShot = false;
    public bool isBuffedByOdinEye = false;
    private List<GameObject> auraProjectiles = new List<GameObject>();
    private float[] angles;
    public bool canAim = false;
    public float stockedDamage = 0f;
    // --- Effets spéciaux ---
    private bool foudreBuffer = false;
    public bool foudrebuffed = false;
    public bool foudrebuffedapplied = false;

    // Orage
    public bool orageActive = false;
    public float orageDamage = 0f;
    public float orageTimer = 0f;
    public float orageTickRate = 0.1f;
    private Coroutine orageCoroutine;


    void Start()
    {
        isFirstShot = hasAFirstAttack;
        rangeaura.SetActive(false);

        if (hasProjectileAura)
            InitProjectileAura();
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

        if (isProjectileTower)
        {
            if (cooldownTime > 0f&&towerID!="BaldrObelisk")
                return;

            if (target == null)
            {
                CheckForNearestEnemy();
            }
            else
            {

                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget > attackRange)
                {
                    target = null;
                }
                else if (cooldownTime <= 0f)
                {
                    AttackTarget();
                    cooldownTime = attackCooldown;
                }
            }
        }
        if (isAuraTower)
        {
            if (towerID != "OdinEye" && towerID != "BaldrObelisk")
            {
                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
                foreach (Collider2D enemy in hitEnemies)
                {
                    if (enemy.CompareTag(enemyTag))
                    {
                        EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.getAuraEffect(towerID, this);
                        }
                    }
                }
            }
            else if (towerID == "OdinEye")
            {
                Collider2D[] hitAllies = Physics2D.OverlapCircleAll(transform.position, attackRange);
                foreach (Collider2D ally in hitAllies)
                {
                    if (ally.CompareTag("Tour"))
                    {
                        TowerController allyTower = ally.GetComponent<TowerController>();
                        if (allyTower != null && allyTower != this && allyTower.towerID != "OdinEye" && !allyTower.isBuffedByOdinEye)
                        {
                            allyTower.attackDamage += allyTower.attackDamage * 0.2f;
                            allyTower.attackCooldown -= allyTower.attackCooldown * 0.2f;
                            allyTower.isBuffedByOdinEye = true;
                        }
                    }
                }
            }
            else if (towerID == "BaldrObelisk")
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Tour"))
                    {
                        TowerController ally = hit.GetComponent<TowerController>();
                        if (ally != this)
                        {
                            stockedDamage += ally.stockedDamage;
                            ally.stockedDamage = 0f;
                        }
                    }
                }
                if (cooldownTime <= 0f && stockedDamage > 0f)
                {
                    foreach (Collider hit in hits)
                    {
                        if (hit.CompareTag(enemyTag))
                        {
                            EnemyController enemy = hit.GetComponent<EnemyController>();
                            if (enemy != null)
                            enemy.TakeDamage(stockedDamage, dmgType);
                        }
                    }

                    cooldownTime = attackCooldown;
                    stockedDamage=0f;
                }
            }
        }


    }
    void OnMouseEnter()
    {
        rangeaura.transform.localScale = new Vector3(attackRange * 4 / transform.localScale.x, attackRange * 4 / transform.localScale.y, 1);
        rangeaura.SetActive(true);
    }


    void OnMouseExit()
    {
        rangeaura.SetActive(false);
    }

    public void CheckForNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;
        foreach (GameObject e in enemies)
        {
            float distToMe = Vector3.Distance(transform.position, e.transform.position);
            if (distToMe > attackRange)
                continue;
            float distToNexus = Vector3.Distance(Vector3.zero, e.transform.position);
            if (distToNexus < bestDistToNexus)
            {
                bestDistToNexus = distToNexus;
                best = e.transform;
            }
        }
        target = best;
        isFirstShot = true;
        cooldownTime = firstShotCD;
    }

    void getNewTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            Transform t = e.transform;

            if (attackedTargets.Contains(t))
                continue;

            float distToMe = Vector3.Distance(transform.position, t.position);
            if (distToMe > attackRange)
                continue;

            float distToNexus = Vector3.Distance(Vector3.zero, t.position);
            if (distToNexus < bestDistToNexus)
            {
                bestDistToNexus = distToNexus;
                best = t;
            }
        }
        if (best != null)
        {
            attackedTargets.Add(best);
            target = best;
        }
        else
        {
            target = null;
        }
    }



    void AttackTarget()
    {
        if (target != null)
        {
            if (numberOfTargets > 1f)
            {
                attackedTargets.Clear();

                for (int i = 0; i <= numberOfTargets; i++)
                {
                    getNewTarget();
                    if (target == null && hasPanickShot)
                    {
                        panickShot();
                    }
                    else
                    {
                        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                        PojectileController pojectileController = bullet.GetComponent<PojectileController>();
                        pojectileController.target = target;
                        pojectileController.damage = attackDamage;
                        pojectileController.speed = projectileSpeed;
                        pojectileController.towerID = towerID;
                        pojectileController.mummyTower = this;
                    }
                }
            }
            else
            {
                GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                PojectileController pojectileController = bullet.GetComponent<PojectileController>();
                pojectileController.target = target;
                pojectileController.damage = attackDamage;
                pojectileController.speed = projectileSpeed;
                pojectileController.towerID = towerID;
                pojectileController.mummyTower = this;
            }
        }
    }
    public void ReduceCooldown(float amount)
    {
        attackCooldown = Mathf.Max(0.1f, attackCooldown - amount);
    }

    void panickShot()
    {
        Vector2 randomPoint = (Vector2)transform.position + Random.insideUnitCircle * attackRange;
        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = randomPoint;
        tempTarget.tag = enemyTag;
        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        PojectileController pojectileController = bullet.GetComponent<PojectileController>();
        pojectileController.isPanickShot = true;
        pojectileController.target = tempTarget.transform;
        pojectileController.damage = attackDamage;
        pojectileController.speed = projectileSpeed;
        pojectileController.towerID = towerID;
        pojectileController.mummyTower = this;
        Destroy(tempTarget, 2f);
    }

    void InitProjectileAura()
    {
        angles = new float[(int)projectileCount];
        for (int i = 0; i < projectileCount; i++)
        {
            angles[i] = i * Mathf.PI * 2 / projectileCount;
            Vector3 spawnPos = (Vector2)transform.position + new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i])) * attackRange;
            GameObject proj = Instantiate(auraProjectilePrefab, spawnPos, Quaternion.identity);
            proj.tag = towerID;
            proj.GetComponent<PojectileController>().damage = attackDamage;
            proj.GetComponent<PojectileController>().towerID = towerID;
            proj.GetComponent<PojectileController>().mummyTower = this;
            proj.GetComponent<PojectileController>().speed = projectileSpeed;
            proj.GetComponent<PojectileController>().centerPointForPatrol = this.transform;
            proj.GetComponent<PojectileController>().angleForPatrol = angles[i];
            proj.GetComponent<PojectileController>().radiusForPatrol = attackRange;
            proj.GetComponent<PojectileController>().isPatrol = true;

            auraProjectiles.Add(proj);
        }
    }
    
    // ⚡ --- FoudreBuffer : applique une aura de buff aux autres tours ---
    void ApplyFoudreBuffer()
    {
        Collider2D[] towersInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D col in towersInRange)
        {
            TowerController otherTower = col.GetComponent<TowerController>();
            if (otherTower != null && otherTower != this)
            {
                otherTower.foudrebuffed = true;
            }
        }
    }

    // 🌩️ --- Orage : coroutine pour infliger des dégâts constants ---
    private IEnumerator OrageCoroutine(float damage, float duration)
{
    orageActive = true;
    float elapsed = 0f;
    Collider2D[] buffer = new Collider2D[50];

    while (elapsed < duration)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, attackRange, buffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D enemy = buffer[i];
            if (enemy != null && enemy.CompareTag(enemyTag))
            {
                EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                if (enemyHealth != null)
                    enemyHealth.TakeDamage(damage, DamageType.Foudre);
            }
        }

        elapsed += orageTickRate;
        yield return new WaitForSeconds(orageTickRate);
    }

    orageActive = false;
    orageCoroutine = null;
}


    // --- Appelées par tes cartes ---
    public void BuffFoudre()
    {
        foudreBuffer = true;
    }

    public void Orage(float damage, float duration)
    {
        // Stoppe une orage précédente si encore active
        if (orageCoroutine != null)
        {
            StopCoroutine(orageCoroutine);
            orageCoroutine = null;
        }

       orageCoroutine = StartCoroutine(OrageCoroutine(damage, duration));

    }

}