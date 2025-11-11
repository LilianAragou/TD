using UnityEngine;
using System.Linq;

public class TowerController : MonoBehaviour
{
    [SerializeField] public float attackRange = 5f;
    [SerializeField] public float attackDamage = 10f;
    [SerializeField] public float attackCooldown = 1f;
    [SerializeField] public float projectileSpeed = 10f;
    private float cooldownTime = 0f;
    public Transform target;
    public string enemyTag = "Enemy";
    public string dmgType;
    public string towerID;
    public Transform firePoint;
    public GameObject projectilePrefab;
    public GameObject rangeaura;
    public bool hasAFirstAttack = false;
    public bool isAuraTower = false;
    public bool isProjectileTower = true;
    private bool isFirstShot;
    public float firstShotCD;

    void Start()
    {
        isFirstShot = hasAFirstAttack;
        rangeaura.SetActive(false);
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

        if (target == null && isProjectileTower)
        {
            CheckForNearestEnemy();
        }
        else if (isProjectileTower)
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

    void AttackTarget()
    {
        if (target != null)
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
        public void ReduceCooldown(float amount)
        {
            attackCooldown = Mathf.Max(0.1f, attackCooldown - amount);
        }
}