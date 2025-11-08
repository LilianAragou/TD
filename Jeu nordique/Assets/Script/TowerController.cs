using UnityEngine;
using System.Linq;

public class TowerController : MonoBehaviour
{
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float projectileSpeed = 10f;
    public GameObject nexus;
    private float cooldownTime = 0f;
    private Transform target;
    public string enemyTag = "Enemy";

    public Transform firePoint;
    public GameObject projectilePrefab;
    public GameObject rangeaura;

    void Start()
    {
        rangeaura.SetActive(false);
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

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

    void OnMouseEnter()
    {
        rangeaura.transform.localScale = new Vector3(attackRange, attackRange, 1);
        rangeaura.SetActive(true);
    }
    

    void OnMouseExit()
    {
        rangeaura.SetActive(false);
    }

    void CheckForNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;
        foreach (GameObject e in enemies)
        {
            float distToMe = Vector3.Distance(transform.position, e.transform.position);
            if (distToMe > attackRange)
                continue;
            float distToNexus = Vector3.Distance(nexus.transform.position, e.transform.position);
            if (distToNexus < bestDistToNexus)
            {
                bestDistToNexus = distToNexus;
                best = e.transform;
            }
        }
        target = best;
    }
    
    void AttackTarget()
    {
        if (target != null)
        {
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            PojectileController pojectileController = bullet.GetComponent<PojectileController>();
            pojectileController.target=target;
            pojectileController.damage = attackDamage;
            pojectileController.speed = projectileSpeed;
        }
    }
}
