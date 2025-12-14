using UnityEngine;
using System.Collections.Generic;

public enum DamageType { Feu, Snow, Terre, Air, Necrotique, Foudre, Base }

public class EnemyController : MonoBehaviour
{
    [Header("Stats de base")]
    public float speed = 1f;

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
    
    [Header("Card 10 - Unique")]
    public bool isLightningMarked = false; 

    public float traveled = 0f;
    private float berserkDamage = 0f;

    [Header("Diminishing Returns (Stun)")]
    public float stunResetTime = 5f;     
    public float stunDecay = 0.8f;       
    public float minStunDuration = 0.1f; 

    private float currentStunMultiplier = 1f;
    private float lastStunEndTime = -10f; 
    private float savedSpeedBeforeStun = -1f; 
    private Coroutine currentStunRoutine;
    public bool isStunned = false;

    [System.Serializable]
    public class SlowEffect
    {
        public float amount;
        public float timeLeft;
        public string towerID;
    }
    public List<SlowEffect> activeSlows = new List<SlowEffect>();
    
    [Header("Résistances et faiblesses")]
    public List<DamageType> resistances = new List<DamageType>();
    public List<DamageType> faiblesses = new List<DamageType>();

    private Dictionary<string, Coroutine> activeDots = new Dictionary<string, Coroutine>();

    // --- VARIABLES SYNERGIE FEU (Carte 14, 16, 19, 20) ---
    private float solarPlagueDurationLeft = 0f; 
    private bool isTakingDotDamage = false; 
    
    // --- Pour Card 20 ---
    public bool isBurning = false; 
    private float burnStatusTimer = 0f;
    // -------------------

    void Awake()
    {
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj) gameManager = gmObj.GetComponent<GameManager>();
        secretSpeed = speed;
    }

    void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        target = Vector3.zero;
        health = maxHealth;
    }

    void Update()
    {
        float currentSpeed = speed;

        // Variable pour cumuler les dégâts de froid de cette frame
        float skadiDotDamage = 0f;

        for (int i = activeSlows.Count - 1; i >= 0; i--)
        {
            activeSlows[i].timeLeft -= Time.deltaTime;
            if (activeSlows[i].timeLeft <= 0)
            {
                activeSlows.RemoveAt(i);
                continue;
            }
            
            // Application du ralentissement
            currentSpeed *= 1f - activeSlows[i].amount / 100f;
            if (currentSpeed < speed * 0.1f)
                currentSpeed = speed * 0.1f; 

            // --- CARTE 31 : BAISER ARCTIQUE ---
            // Si la carte est active
            if (DrawingcardController.card31)
            {
                // Si le ralentissement vient d'une tour "Skadi" (nom contient Skadi)
                if (activeSlows[i].towerID.Contains("Skadi"))
                {
                    // Calcul des dégâts : (% Slow / 2) par seconde
                    // Ex: 40% slow => 20 dégâts/s => 20 * deltaTime pour cette frame
                    float dmgPerSecond = activeSlows[i].amount / 2f;
                    skadiDotDamage += dmgPerSecond * Time.deltaTime;
                }
            }
            // ----------------------------------
        }

        // Appliquer les dégâts de Baiser Arctique
        if (skadiDotDamage > 0f)
        {
            // On utilise type Snow (Glace)
            TakeDamage(skadiDotDamage, DamageType.Snow, null);
        }

        secretSpeed = currentSpeed;
        if (isStunned)
        {
            secretSpeed = 0f;
        }

        if (SkadiShotCalculator >= 3f) 
        {
            GetStunned(0.75f);
            SkadiShotCalculator = 0f;
        }

        if (solarPlagueDurationLeft > 0f)
        {
            solarPlagueDurationLeft -= Time.deltaTime;
        }

        if (burnStatusTimer > 0f)
        {
            burnStatusTimer -= Time.deltaTime;
            if (burnStatusTimer <= 0f) isBurning = false;
        }

        target = Vector3.zero; 
        direction = (target - transform.position).normalized;
        transform.position += direction * secretSpeed * Time.deltaTime;
        
        if (direction != Vector3.zero)
        {
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Nexus"))
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float damage, DamageType type, TowerController attacker = null)
    {
        if (DrawingcardController.card5 && type == DamageType.Foudre) damage *= 1.15f;
        if (isLightningMarked && type == DamageType.Foudre) damage *= 4.0f; 

        if (type == DamageType.Feu)
        {
            isBurning = true;
            burnStatusTimer = 0.5f; 
        }

        if (DrawingcardController.card19 && type == DamageType.Feu && health < (maxHealth * 0.65f))
        {
            damage *= 1.30f;
        }

        if (type == DamageType.Feu && isPorteur)
        {
            speed *= 1.5f;
            isPorteur = false;
        }
        if (type == DamageType.Foudre && isFoudroye)
        {
            health += maxHealth * 0.5f;
            if (health > maxHealth) health = maxHealth;
        }
        if (isBerserk)
        {
            var reduction = berserkDamage / 100f;
            for (int i = 0; i < reduction; i += 1) damage *= 0.90f;
        }

        if (resistances.Contains(type)) damage *= 0.5f;
        else if (faiblesses.Contains(type)) damage *= 1.25f;

        health -= damage;
        berserkDamage += damage;

        if (type == DamageType.Feu && DrawingcardController.card14 && !isTakingDotDamage)
        {
            ApplySolarPlague(3f);
        }

        if (health <= 0f)
        {
            if (attacker != null) attacker.RegisterKill();

            if (DrawingcardController.card16 && type == DamageType.Feu)
            {
                if (solarPlagueDurationLeft > 0.1f) 
                {
                    TransferBurnsToNearest();
                }
            }

            Die();
            return;
        }

        if (type == DamageType.Foudre && DrawingcardController.card7)
        {
            if (Random.value < 0.5f) TakeDamage(damage * 0.5f, type, attacker);
        }
    }

    public bool TryExecuteBrasier()
    {
        bool sufferingBurn = isBurning || (activeDots.ContainsKey("SolarPlague"));
        
        if (sufferingBurn && health < (maxHealth * 0.10f))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 2.5f);
            foreach (var hit in hits)
            {
                if (hit.gameObject != this.gameObject && hit.CompareTag("Enemy"))
                {
                    EnemyController neighbor = hit.GetComponent<EnemyController>();
                    if (neighbor != null)
                    {
                        neighbor.TakeDamage(20f, DamageType.Feu, null);
                    }
                }
            }
            health = 0;
            Die();
            return true;
        }
        return false;
    }

    public void ApplyLightningMark()
    {
        isLightningMarked = true;
    }

    public void ApplySolarPlague(float duration)
    {
        solarPlagueDurationLeft = duration;

        if (activeDots.ContainsKey("SolarPlague"))
        {
            StopCoroutine(activeDots["SolarPlague"]);
        }
        activeDots["SolarPlague"] = StartCoroutine(SolarPlagueCoroutine());
    }

    private System.Collections.IEnumerator SolarPlagueCoroutine()
    {
        float tickTimer = 0f;

        while (solarPlagueDurationLeft > 0)
        {
            yield return null; 
            tickTimer += Time.deltaTime;

            if (tickTimer >= 1f)
            {
                tickTimer = 0f;

                float burnDamage = health * 0.01f;
                if (burnDamage < 1f) burnDamage = 1f;

                if (DrawingcardController.card19 && health < (maxHealth * 0.65f))
                {
                    burnDamage *= 1.30f;
                }

                isTakingDotDamage = true;
                TakeDamage(burnDamage, DamageType.Feu, null); 
                isTakingDotDamage = false;
            }
        }
        
        if (activeDots.ContainsKey("SolarPlague")) 
            activeDots.Remove("SolarPlague");
    }

    void TransferBurnsToNearest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f); 
        EnemyController bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue; 
            if (hit.CompareTag("Enemy"))
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = hit.GetComponent<EnemyController>();
                }
            }
        }

        if (bestTarget != null)
        {
            bestTarget.ApplySolarPlague(solarPlagueDurationLeft);
        }
    }

    private void Die()
    {
        if (gameManager != null)
            gameManager.money += 10; 

        Destroy(gameObject);
    }

    public void GetStunned(float duration)
    {
        if (Time.time > lastStunEndTime + stunResetTime)
        {
            currentStunMultiplier = 1f; 
        }

        float effectiveDuration = duration * currentStunMultiplier;
        if (effectiveDuration < minStunDuration) effectiveDuration = minStunDuration;
        float effectiveDurationCopy = effectiveDuration;
        
        while (effectiveDurationCopy > 1.0f)
        {
            currentStunMultiplier *= stunDecay;
            effectiveDurationCopy -= 1.0f;
        }
        
        if (isStunned)
        {
            if (lastStunEndTime < Time.time)
                lastStunEndTime = Time.time + effectiveDuration;
        }
        else
        {
            lastStunEndTime = Time.time + effectiveDuration;
            if (currentStunRoutine != null) StopCoroutine(currentStunRoutine);
            currentStunRoutine = StartCoroutine(StunCoroutine());
        }
    }

    private System.Collections.IEnumerator StunCoroutine()
    {
        isStunned = true;
        while (Time.time < lastStunEndTime)
        {
            yield return null; 
        }
        isStunned = false;
        currentStunRoutine = null;
    }

    public void getSlowed(float slowAmount, float duration, string towerID)
    {
        foreach (var s in activeSlows)
        {
            if (s.towerID == towerID) return; 
        }
        activeSlows.Add(new SlowEffect { amount = slowAmount, timeLeft = duration, towerID = towerID });
    }

    public void SetHealth(float newHealth)
    {
        maxHealth = newHealth;
        health = newHealth;
    }

    public void getAuraEffect(string towerID, TowerController tower)
    {
        switch (towerID)
        {
            case "SkadiAutel":
                getSlowed(55f, 1f, towerID);
                break;
            case "WindTotem":
                if (tower.cooldownTime <= 0f)
                {
                    getKnockBacked(1f, (transform.position - tower.transform.position).normalized);
                    tower.cooldownTime = tower.attackCooldown;
                }
                break;
        }
    }

    public void DebuffDamage()
    {
        if (debuffDamage == 0)
            debuffDamage = 10f;
        else
            debuffDamage += 5f;
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        health = maxHealth;
    }

    public void damageOverTime(float damage, float duration, float interval, string towerID, DamageType damageType)
    {
        if (activeDots.ContainsKey(towerID))
            StopCoroutine(activeDots[towerID]);

        Coroutine newDot = StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval, towerID, damageType));
        activeDots[towerID] = newDot;
    }

    private System.Collections.IEnumerator DamageOverTimeCoroutine(float damage, float duration, float interval, string towerID, DamageType damageType)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            TakeDamage(damage, damageType, null); 
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    public void getKnockBacked(float knockBackDistance, Vector3 knockBackDirection)
    {
        transform.position += knockBackDirection * knockBackDistance;

        // --- CARTE 23 : PEUR DES VENTS ---
        if (DrawingcardController.card23)
        {
            float knockbackDamage = knockBackDistance * 10f;
            TakeDamage(knockbackDamage, DamageType.Air, null);
        }
    }
    
    public void GetPulled(Vector3 pullDirection, float pullSpeed)
    {
        transform.position += pullDirection * pullSpeed * Time.deltaTime;
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