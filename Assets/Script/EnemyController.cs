using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats de base")]
    public float baseHealth = 100f;
    public float speed = 1f;

    [HideInInspector] public float health;
    [HideInInspector] public GameManager gameManager;

    private Vector3 target;
    private Vector3 direction;
    private float angle;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        target = Vector3.zero;
        health = baseHealth;
    }

    void Update()
    {
        direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Nexus"))
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (gameManager != null)
            gameManager.money += 10;

        Destroy(gameObject);
    }

    // Appelée par le spawner pour appliquer le multiplicateur de PV
    public void SetHealth(float newHealth)
    {
        baseHealth = newHealth;
        health = newHealth;
    }
}
