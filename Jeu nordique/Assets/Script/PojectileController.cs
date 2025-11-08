using UnityEngine;

public class PojectileController : MonoBehaviour
{
    public Transform target;
    public float damage;
    public float speed;
    void Update()
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
                enemyHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
