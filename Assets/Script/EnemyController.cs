using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float speed = 1f;
    public Vector3 target;
    public Vector3 direction;
    public float angle;
    public GameManager gameManager;
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        // Calcul de la direction vers le centre
        target = Vector3.zero;
        direction = (target - transform.position).normalized;

        // Déplacement continu vers le centre
        transform.position += direction * speed * Time.deltaTime;
        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    void OnTriggerEnter2D(Collider2D  collision)
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
        gameManager.money += 10;
        Destroy(gameObject);
    }
}
