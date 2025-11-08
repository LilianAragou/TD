using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float health = 100f;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
