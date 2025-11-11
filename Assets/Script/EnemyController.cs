using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float speed = 1f;
    public Vector3 target;
    public Vector3 direction;
    public float angle;
    public float reward = 10f;
    public float debuffDamage = 0;

    public GameManager gameManager;

void Awake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        health = maxHealth;
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
        health -= damage * (1 + debuffDamage / 100f);
        if (health <= 0f)
        {
            gameManager.money += reward;
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
    public void getStunned(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }
    public void DebuffDamage()
    {
        if (debuffDamage == 0)
        {
            debuffDamage = 10f;
        }
        else
        {
            debuffDamage += 5f;
        }
    }
    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        float originalSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
    }
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        health = maxHealth;
    }
}
