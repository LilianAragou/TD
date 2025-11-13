using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;

public enum DamageType { Feu, Snow, Terre, Air, Necrotique, Foudre, Base }
public class EnemyController : MonoBehaviour
{
    [Header("Stats de base")]
    public float speed = 1f;
    public float baseSpeed = 1f;

    public float health;
    [HideInInspector] public GameManager gameManager;

    [Header("Stats")]
    public float maxHealth = 100f;
    public Vector3 target;
    public Vector3 direction;
    public float angle;
    public float reward = 10f;
    public float secretSpeed;
    public float amount;
    public float timeLeft;
    public float SkadiShotCalculator = 0f;
    public float debuffDamage = 0f;
    [Header("Unit")]
    public bool slowImmune = false;
    public float slowImmuneTimer = 1.0f;
    public bool isBerserk = false;
    public bool isSanglier = false;
    public bool isFoudroye = false;
    public bool isPorteur = false;
    public bool isChef = false;
    public bool isClerc = false;
    public float chefRange = 1.5f;
    public float clercRange = 1.5f;

    public float traveled = 0f;
    private float berserkDamage = 0f;

    [System.Serializable]
    public class SlowEffect
    {
        public float amount;
        public float timeLeft;
        public string towerID;
    }
    

    [Header("Résistances et faiblesses")]
    public List<DamageType> resistances = new List<DamageType>();
    public List<DamageType> faiblesses = new List<DamageType>();

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
        baseSpeed = speed;
    }

    void Update()
    {
        float currentSpeed = speed;
        if (!slowImmune)
        {
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
        }
        else if (slowImmuneTimer > 0f && activeSlows.Count > 0)
        {
            slowImmuneTimer -= Time.deltaTime;
        }

        if (slowImmuneTimer <= 0f)
        {
            slowImmune = false;
        }
        if (isChef)
        {
            int count = 0;
            Meute[] allSoldiers = FindObjectsByType<Meute>(FindObjectsSortMode.None);

            foreach (Meute s in allSoldiers)
            {
                if (s == this) continue;

                float dist = Vector3.Distance(transform.position, s.transform.position);
                if (dist <= chefRange)
                    count++;
            }

            health += count * 15f * Time.deltaTime;
        }
        if (isClerc)
        {
            EnemyController[] allSoldiers = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

            foreach (EnemyController s in allSoldiers)
            {
                if (s == this) continue;

                float dist = Vector3.Distance(transform.position, s.transform.position);
                if (dist <= clercRange)
                    s.SetSoldierHeal(5);
            }

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
        traveled += secretSpeed * Time.deltaTime;
        if (isSanglier)
        {
            speed = (baseSpeed * Mathf.Exp(traveled / 3f));
        }
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Nexus"))
        {
            Destroy(gameObject);
        }
    }


    public void TakeDamage(float damage, DamageType type)
    {
        if (DrawingcardController.card5 && type == DamageType.Foudre)
        {
            damage *= 1.15f;
        }
        if (type == DamageType.Feu && isPorteur)
        {
            speed *= 1.5f;
            isPorteur = false;
        }
        if (type == DamageType.Foudre && isFoudroye)
        {
            health += maxHealth * 0.5f;
            if (health > maxHealth)
                health = maxHealth;
        }
        if (isBerserk)
        {
            var reduction = berserkDamage / 100f;
            for (int i = 0; i < reduction; i += 1)
            {
                damage *= 0.90f;
            }
        }
        
        if (resistances.Contains(type))
            damage *= 0.5f; // 50% dmg en moins
        else if (faiblesses.Contains(type))
            damage *= 1.25f; // 25% dmg en plus

        health -= damage;
        berserkDamage += damage;
        if (health <= 0f)
            Die();
    }


    private void Die()
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
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        health = maxHealth;
    }

    private Dictionary<string, Coroutine> activeDots = new Dictionary<string, Coroutine>();

    public void DamageOverTime(float damage, float duration, float interval, string towerID, DamageType type)
    {
        if (activeDots.ContainsKey(towerID))
            StopCoroutine(activeDots[towerID]);

        Coroutine newDot = StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval, towerID, type));
        activeDots[towerID] = newDot;
    }

    private System.Collections.IEnumerator DamageOverTimeCoroutine(float damage, float duration, float interval, string towerID, DamageType type)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            TakeDamage(damage, type);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }
    public void SetSoldierHeal(int soldierInRange)
    {
        float healAmount = soldierInRange * 10f * Time.deltaTime;
        health += healAmount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

}
