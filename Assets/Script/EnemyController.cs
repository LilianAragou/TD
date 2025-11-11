using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;


public class EnemyController : MonoBehaviour
{
    [Header("Stats de base")]
    public float maxHealth = 100f;
    public float speed = 1f;

    [HideInInspector] public float health;
    [HideInInspector] public GameManager gameManager;
    [Header("Stats")]
    public Vector3 target;
    public Vector3 direction;
    public float angle;
    public float reward = 10f;
    public float secretSpeed;
    public float amount;
    public float timeLeft;
    public float SkadiShotCalculator = 0f;
    public float debuffDamage = 0;
    [System.Serializable]
    public class SlowEffect
    {
        public float amount;
        public float timeLeft;
        public string towerID;
    }
    public List<SlowEffect> activeSlows = new List<SlowEffect>();

    void Awake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        secretSpeed = speed;
    }    

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        target = Vector3.zero;
        health = maxHealth;
    }

    void Update()
    {
        float currentSpeed = speed;

        for (int i = activeSlows.Count - 1; i >= 0; i--)
        {
            activeSlows[i].timeLeft -= Time.deltaTime;
            if (activeSlows[i].timeLeft <= 0)
            {
                activeSlows.RemoveAt(i);
                continue;
            }
            currentSpeed *= (1f - activeSlows[i].amount / 100f);
        }

        secretSpeed = currentSpeed;

        if (SkadiShotCalculator == 3f)
        {
            getStunned(0.75f);
            SkadiShotCalculator = 0f;
        }

        target = Vector3.zero;
        direction = (target - transform.position).normalized;
        transform.position += direction * secretSpeed * Time.deltaTime;
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
        health -= damage * (1 + debuffDamage / 100f);
        if (health <= 0f)
        {
            gameManager.money += reward;
            Die();
        }
    }

    public void Die()
    {
        if (gameManager != null)
            gameManager.money += 10;

        Destroy(gameObject);
    }

public void getStunned(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }
    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        float originalSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
    }
    public void getSlowed(float slowAmount, float duration, string towerID)
    {
        foreach (var s in activeSlows)
        {
            if (s.towerID == towerID)
            {
                return;
            }
        }
        activeSlows.Add(new SlowEffect { amount = slowAmount, timeLeft = duration, towerID = towerID });
    }

    public void SetHealth(float newHealth)
    {
        maxHealth = newHealth;
        health = newHealth;
    }
    public void getAuraEffect(string towerID)
    {
        switch (towerID)
        {
            case "SkadiAutel":
                getSlowed(55f, 1f, towerID);
                break;
        }
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
}
