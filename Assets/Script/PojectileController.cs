using UnityEngine;

public class PojectileController : MonoBehaviour
{
    public Transform target;
    public float damage;
    public float speed;
    public string towerID;
    public TowerController mummyTower;
    public GameObject aoePrefab;
    public Transform centerPointForPatrol;
    public float radiusForPatrol = 2f;
    public bool isPatrol = false;
    public float angleForPatrol = 0f;
    public bool isPanickShot = false;
    private float lifeTime=0f;
    private float maxLifeTime = 5f;
    void Update()
    {
        lifeTime += Time.deltaTime;
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0;
            direction.Normalize();
            transform.position += direction * speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, target.position) < 0.2f)
            {
                EnemyController enemyHealth = target.GetComponent<EnemyController>();
                if (enemyHealth != null || isPanickShot)
                {
                    switch (towerID)
                    {
                        case "ThorTotem":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            Destroy(gameObject);
                            return;
                            break;
                        case "ThorHammer":
                            if (enemyHealth.health <= damage)
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
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
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                                Destroy(gameObject);
                            }
                            break;
                        case "ThorPillar":
                            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
                            foreach (var enemy in hitEnemies)
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
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
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            Destroy(gameObject);
                            break;
                        case "SkadiCryo":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            enemyHealth.SkadiShotCalculator += 1f;
                            Destroy(gameObject);
                            break;
                        case "NecroTower":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            enemyHealth.DebuffDamage();
                            Destroy(gameObject);
                            return;
                        case "HelheimSanctuary":
                            if (enemyHealth.health <= damage)
                            {
                                mummyTower.ReduceCooldown(0.1f);
                            }
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            enemyHealth.DebuffDamage();
                            Destroy(gameObject);
                            return;
                        case "VoidEye":
                            enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                            if (enemyHealth.health <= enemyHealth.maxHealth * 0.2f)
                            {
                                enemyHealth.TakeDamage(enemyHealth.health + 1f, mummyTower.dmgType);
                                mummyTower.stockedDamage+= (enemyHealth.health*0.2f);
                            }
                            Destroy(gameObject);
                            return;
                        case "RuneBrasero":
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                                
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                                Destroy(gameObject);
                                return;
                            }
                        case "VolcanoTower":
                            {
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
                                return;
                            }
                        case "InfernalForge":
                            {
                                enemyHealth.TakeDamage(damage + (enemyHealth.maxHealth * 0.2f), mummyTower.dmgType);
                                Destroy(gameObject);
                                return;
                            }
                        case "BaldrRune":
                            {
                                enemyHealth.TakeDamage(damage, mummyTower.dmgType);
                            mummyTower.stockedDamage+= damage*(1+enemyHealth.debuffDamage/100f);
                                enemyHealth.getStunned(1.0f);
                                Destroy(gameObject);
                                return;
                            }
                        case "BaldrHeart":
                            {
                                float lifeRatio = Mathf.Clamp01(lifeTime / maxLifeTime);
                                float extraDamage = Mathf.Lerp(1f, 0.1f, lifeRatio);
                                enemyHealth.TakeDamage(damage*(10f/(lifeTime-Time.deltaTime)), mummyTower.dmgType);
                                mummyTower.stockedDamage+= (damage*10f/(lifeTime-Time.deltaTime))*(1+enemyHealth.debuffDamage/100f);
                                Destroy(gameObject);
                                return;
                            }
                    }

                }
            }

        }
        if (target == null && towerID != "TwinWind")
        {
            Destroy(gameObject);
            return;
        }
        else if (towerID == "TwinWind")
        {
            if (centerPointForPatrol != null && isPatrol)
            {
                angleForPatrol += speed * Time.deltaTime;
                float x = centerPointForPatrol.position.x + radiusForPatrol * Mathf.Cos(angleForPatrol);
                float y = centerPointForPatrol.position.y + radiusForPatrol * Mathf.Sin(angleForPatrol);
                transform.position = new Vector3(x, 0, y);
                float angle = angleForPatrol * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                Destroy(gameObject);
            }

        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && towerID == "TwinWind")
        {
            EnemyController enemyHealth = collision.GetComponent<EnemyController>();
            if (enemyHealth != null)
            {
                enemyHealth.getKnockBacked(0.5f, (enemyHealth.transform.position - transform.position).normalized);
                enemyHealth.TakeDamage(damage, mummyTower.dmgType   );
            }
        }
    }
}
