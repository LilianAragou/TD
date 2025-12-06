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

    // --- VARIABLES SYNERGIE FEU (Carte 14 & 16) ---
    private float solarPlagueDurationLeft = 0f; 
    private bool isTakingDotDamage = false; // Sécurité pour éviter la boucle infinie
    // ----------------------------------------------

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

        for (int i = activeSlows.Count - 1; i >= 0; i--)
        {
            activeSlows[i].timeLeft -= Time.deltaTime;
            if (activeSlows[i].timeLeft <= 0)
            {
                activeSlows.RemoveAt(i);
                continue;
            }
            currentSpeed *= 1f - activeSlows[i].amount / 100f;
            if (currentSpeed < speed * 0.1f)
                currentSpeed = speed * 0.1f; 
        }

        secretSpeed = currentSpeed;
        if (isStunned) secretSpeed = 0f;

        if (SkadiShotCalculator >= 3f) 
        {
            GetStunned(0.75f);
            SkadiShotCalculator = 0f;
        }

        // --- TIMER PESTE SOLAIRE ---
        if (solarPlagueDurationLeft > 0f)
        {
            solarPlagueDurationLeft -= Time.deltaTime;
        }
        // ---------------------------

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

        // --- CARTE 14 : PESTE SOLAIRE ---
        // On déclenche SEULEMENT si ce n'est pas le DoT lui-même qui fait les dégâts
        if (type == DamageType.Feu && DrawingcardController.card14 && !isTakingDotDamage)
        {
            ApplySolarPlague(3f);
        }
        // --------------------------------

        // --- MORT ---
        if (health <= 0f)
        {
            if (attacker != null) attacker.RegisterKill();

            // --- CARTE 16 : FLAMME MAUDITE (Transfert) ---
            // Maintenant "type" sera bien "Feu" même si c'est le DoT qui tue
            if (DrawingcardController.card16 && type == DamageType.Feu)
            {
                // On transfère s'il reste du burn actif
                if (solarPlagueDurationLeft > 0.1f) 
                {
                    TransferBurnsToNearest();
                }
            }
            // ---------------------------------------------

            Die();
            return;
        }

        if (type == DamageType.Foudre && DrawingcardController.card7)
        {
            if (Random.value < 0.5f) TakeDamage(damage * 0.5f, type, attacker);
        }
    }

    public void ApplyLightningMark()
    {
        isLightningMarked = true;
    }

    // --- SYSTÈME PESTE SOLAIRE ---
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
        // On boucle tant qu'il reste du temps (géré dans Update)
        // Cela permet de ne pas avoir de décalage si la durée est refresh
        float tickTimer = 0f;

        while (solarPlagueDurationLeft > 0)
        {
            yield return null; // Attend la frame suivante
            tickTimer += Time.deltaTime;

            if (tickTimer >= 1f)
            {
                tickTimer = 0f; // Reset timer tick

                // Calcul dégâts (1% PV Max ou Actuel ? Consigne = Actuel)
                float burnDamage = health * 0.01f;
                if (burnDamage < 1f) burnDamage = 1f;

                // --- CRUCIAL : On met le flag à TRUE avant d'infliger les dégâts ---
                // Cela permet de passer "DamageType.Feu" à TakeDamage SANS re-déclencher ApplySolarPlague
                isTakingDotDamage = true;
                TakeDamage(burnDamage, DamageType.Feu, null); // On passe FEU pour activer la Carte 16 à la mort
                isTakingDotDamage = false;
                // ------------------------------------------------------------------
            }
        }
        
        if (activeDots.ContainsKey("SolarPlague")) 
            activeDots.Remove("SolarPlague");
    }

    void TransferBurnsToNearest()
    {
        // Recherche large (10 unités)
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f); 
        EnemyController bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue; // Pas soi-même
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
            // On transfère le temps restant à la cible
            Debug.Log($"Transfert Flamme Maudite ({solarPlagueDurationLeft.ToString("F1")}s) vers {bestTarget.name}");
            bestTarget.ApplySolarPlague(solarPlagueDurationLeft);
        }
    }
    // -----------------------------

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