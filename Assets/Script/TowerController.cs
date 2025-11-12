using UnityEngine;
using System.Linq;
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
    

    void Start()
    {
        isFirstShot = hasAFirstAttack;
        rangeaura.SetActive(false);
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

        if (isProjectileTower)
        {
        if (cooldownTime>0f)
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
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.CompareTag(enemyTag))
                {
                    EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.getAuraEffect(towerID);
                    }
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

                for (int i = 0; i < numberOfTargets; i++)
                {
                    getNewTarget();
                    if (target == null)
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
    pojectileController.target = tempTarget.transform;
    pojectileController.damage = attackDamage;
    pojectileController.speed = projectileSpeed;
    pojectileController.towerID = "panickShot";
    pojectileController.mummyTower = this;
    Destroy(tempTarget, 2f);
}

}