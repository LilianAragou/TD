using UnityEngine;

public class PojectileController : MonoBehaviour
{
    public Transform target;
    public float damage;
    public float speed;
    public string towerID;
    public TowerController mummyTower;

    void Update()
    {
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.z = 0;
            direction.Normalize();
            transform.position += direction * speed * Time.deltaTime;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);


            if (Vector3.Distance(transform.position, target.position) < 0.2f)
            {
                EnemyController enemyHealth = target.GetComponent<EnemyController>();
                if (enemyHealth != null)
                {
                    switch (towerID)
                    {
                        case "ThorTotem":
                            enemyHealth.TakeDamage(damage);
                            Destroy(gameObject);
                            return;
                            break;
                        case "ThorHammer":
                            if (enemyHealth.health <= damage)
                            {
                                enemyHealth.TakeDamage(damage);
                                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                                Transform best = null;
                                float bestDistToMe = Mathf.Infinity;

                                foreach (GameObject e in enemies)
                                {
                                    if (e == null || e.transform == target)
                                        continue;

                                    float distToTower = Vector3.Distance(mummyTower.transform.position, e.transform.position);
                                    if (distToTower > mummyTower.attackRange)
                                        continue;

                                    float distToMe = Vector3.Distance(transform.position, e.transform.position);
                                    if (distToMe < bestDistToMe)
                                    {
                                        bestDistToMe = distToMe;
                                        best = e.transform;
                                    }
                                }

                                if (best != null)
                                {
                                    target = best;
                                }
                                else
                                {
                                    Destroy(gameObject);
                                }
                            }
                            else
                            {
                                enemyHealth.TakeDamage(damage);
                                Destroy(gameObject);
                            }
                            break;
                        case "ThorPillar":
                            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
                            foreach (var enemy in hitEnemies)
                            {
                                enemyHealth.TakeDamage(damage);
                                EnemyController enemyCtrl = enemy.GetComponent<EnemyController>();
                                if (enemyCtrl != null)
                                {
                                    enemyCtrl.getStunned(0.75f);
                                }
                            }
                            Destroy(gameObject);
                            break;
                        case "SkadiTower":
                            enemyHealth.getSlowed(10f, 3f, towerID);
                            enemyHealth.TakeDamage(damage);
                            Destroy(gameObject);
                            break;
                        case "SkadiCryo":
                            enemyHealth.TakeDamage(damage);
                            enemyHealth.SkadiShotCalculator+= 1f;
                            Destroy(gameObject);
                            break;
                        default:
                            enemyHealth.TakeDamage(damage);
                            Destroy(gameObject);
                            break;
                    }
                    
                }
            }

        }
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
    }
}
