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
    private float cooldownTime = 0f;
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
    private bool isFirstShot;
    public float firstShotCD;
    public float numberOfTargets = 1f;

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
    }

    void Update()
    {
        if (foudrebuffed && !foudrebuffedapplied)
        {
            attackDamage *= 1.3f;
            foudrebuffedapplied = true;
        }

        cooldownTime -= Time.deltaTime;

        // --- Gestion du tir projectile ---
        if (isProjectileTower)
        {
            if (cooldownTime > 0f)
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

        // --- Gestion de l’aura ---
        if (isAuraTower)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.CompareTag(enemyTag))
                {
                    EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                        enemyHealth.getAuraEffect(towerID);
                }
            }
        }

        // --- Gestion du buff Foudre ---
        if (foudreBuffer)
            ApplyFoudreBuffer();
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

                for (int i = 0; i < numberOfTargets; i++)
                {
                    getNewTarget();
                    if (target == null)
                    {
                        panickShot();
                    }
                    else
                    {
                        FireProjectile(target);
                    }
                }
            }
            else
            {
                FireProjectile(target);
            }
        }
    }

    void FireProjectile(Transform target)
    {
        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        PojectileController pojectileController = bullet.GetComponent<PojectileController>();
        pojectileController.target = target;
        pojectileController.damage = attackDamage;
        pojectileController.speed = projectileSpeed;
        pojectileController.towerID = towerID;
        pojectileController.mummyTower = this;
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
        FireProjectile(tempTarget.transform);
        Destroy(tempTarget, 2f);
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

        // Utilisation d’un buffer statique pour éviter les allocations
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
