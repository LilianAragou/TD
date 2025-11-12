using UnityEngine;

public class PojectileController : MonoBehaviour
{
    public Transform target;
    public float damage;
    public float speed;
    public string towerID;
    public TowerController mummyTower;
    public GameObject aoePrefab;

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
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            Destroy(gameObject);
                            return;
                        case "ThorHammer":
                            if (enemyHealth.health <= damage)
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                                Transform best = null;
                                Transform second = null;

                                float bestDistToMe = Mathf.Infinity;
                                float secondDistToMe = Mathf.Infinity;

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
                                        secondDistToMe = bestDistToMe;
                                        bestDistToMe = distToMe;
                                        second = best;
                                        best = e.transform;

                                    } else if (distToMe < secondDistToMe)
                                    {
                                        secondDistToMe = distToMe;
                                        second = e.transform;
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
                                if (second != null && best != null && DrawingcardController.card1)
                                {
                                    GameObject newProjectile = Instantiate(gameObject, transform.position, Quaternion.identity);
                                    PojectileController pc = newProjectile.GetComponent<PojectileController>();
                                    pc.target = second;
                                    pc.damage = damage;
                                    pc.speed = speed;
                                    pc.towerID = towerID;
                                    pc.mummyTower = mummyTower;
                                }
                            }
                            else
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                                Destroy(gameObject);
                            }
                            break;
                        case "ThorPillar":
                            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
                            foreach (var enemy in hitEnemies)
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                                EnemyController enemyCtrl = enemy.GetComponent<EnemyController>();
                                if (enemyCtrl != null)
                                {
                                    if (DrawingcardController.card4)
                                    {
                                        enemyCtrl.getStunned(1.5f);
                                    }
                                    else
                                    {
                                        enemyCtrl.getStunned(0.75f);
                                    }
                                }
                            }
                            Destroy(gameObject);
                            break;
                        case "SkadiTower":
                            enemyHealth.getSlowed(10f, 3f, towerID);
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            Destroy(gameObject);
                            break;
                        case "SkadiCryo":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            enemyHealth.SkadiShotCalculator += 1f;
                            Destroy(gameObject);
                            break;
                        case "NecroTower":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            enemyHealth.DebuffDamage();
                            Destroy(gameObject);
                            return;
                        case "HelheimSanctuary":
                            if (enemyHealth.health <= damage)
                            {
                                mummyTower.ReduceCooldown(0.1f);
                            }
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            enemyHealth.DebuffDamage();
                            Destroy(gameObject);
                            return;
                        case "VoidEye":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            if (enemyHealth.health <= enemyHealth.maxHealth * 0.2f)
                            {
                                enemyHealth.TakeDamage(enemyHealth.health + 1f, mummyTower.dmgType);
                            }
                            Destroy(gameObject);
                            return;
                        default:
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            Destroy(gameObject);
                            break;
                        case "RuneBrasero":
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                                Destroy(gameObject);
                                return;
                            }
                        case "VolcanoTower":
                            {
                                GameObject aoe = Instantiate(aoePrefab, transform.position, Quaternion.identity);
                                AOEManager aoeManager = aoe.GetComponent<AOEManager>();
                                if (aoeManager != null)
                                {
                                    aoeManager.damage = damage;
                                    aoeManager.dmgType = mummyTower.dmgType;
                                    aoeManager.radius = 0.8f;
                                    aoeManager.duration = 10f;
                                    aoeManager.tickInterval = 0.1f;
                                }
                                Destroy(gameObject);
                                return;
                            }


                    }

                }
                else if (towerID == "panickShot")
                {
                    GameObject aoe = Instantiate(aoePrefab, transform.position, Quaternion.identity);
                    AOEManager aoeManager = aoe.GetComponent<AOEManager>();
                    if (aoeManager != null)
                    {
                        aoeManager.damage = damage;
                        aoeManager.radius = 0.8f;
                        aoeManager.duration = 10f;
                        aoeManager.tickInterval = 0.1f;
                    }
                    Destroy(gameObject);
                    return;
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
