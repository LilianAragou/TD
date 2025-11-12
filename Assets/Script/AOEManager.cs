using UnityEngine;

public class AOEManager : MonoBehaviour
{
    public float damage;
    public float radius;
    public float duration;
    public float tickInterval;
    private float tickTimer;
    private float timeLeft;
    public GameObject aoeEffect;
    void Start()
    {
        timeLeft = duration;
        tickTimer = tickInterval;
    }
    void Update()
    {
        aoeEffect.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        timeLeft -= Time.deltaTime;
        tickTimer -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            Destroy(gameObject);
        }
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            if (collider.CompareTag("Enemy"))
            {
                if (tickTimer <= 0f)
                {
                    EnemyController enemyHealth = collider.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damage);
                    }
                }
            }
        }
    }
}
