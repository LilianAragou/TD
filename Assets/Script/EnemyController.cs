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
    
    // --- AJOUT CARTE 10 (Unique) ---
    [Header("Card 10 - Unique")]
    public bool isLightningMarked = false; // Cible prioritaire (+300% Foudre)
    // -------------------------------

    public float traveled = 0f;
    private float berserkDamage = 0f;

    [Header("Diminishing Returns (Stun)")]
    public float stunResetTime = 5f;     // Temps sans stun pour revenir à la normale
    public float stunDecay = 0.8f;       // Facteur de réduction (0.8 = 80% de la durée précédente)
    public float minStunDuration = 0.1f; // Durée minimum d'un stun

    // Variables internes pour le Stun
    private float currentStunMultiplier = 1f;
    private float lastStunEndTime = -10f; 
    private float savedSpeedBeforeStun = -1f; // -1 signifie "pas sauvegardé"
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

    void Awake()
    {
        // Recherche sécurisée du GameManager
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

        // Gestion des ralentissements (Slows)
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
                currentSpeed = speed * 0.1f; // Limite de ralentissement à 90%
        }

        secretSpeed = currentSpeed;
        if (isStunned)
        {
            secretSpeed = 0f;
        }

        // Mécanique Skadi (3 coups = stun)
        if (SkadiShotCalculator >= 3f) // >= par sécurité
        {
            GetStunned(0.75f);
            SkadiShotCalculator = 0f;
        }

        // Mouvement
        target = Vector3.zero; // Target semble inutilisé ou reset ici ?
        direction = (target - transform.position).normalized;
        
        // Note: Si 'speed' est mis à 0 par le stun, 'currentSpeed' et 'secretSpeed' vaudront 0 ici.
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
        // Carte 5
        if (DrawingcardController.card5 && type == DamageType.Foudre)
        {
            damage *= 1.15f;
        }

        // --- AJOUT CARTE 10 : Vulnérabilité Extrême ---
        // Si marqué ET dégâts de foudre = Dégâts x4 (+300%)
        if (isLightningMarked && type == DamageType.Foudre)
        {
            damage *= 4.0f; 
        }
        // ----------------------------------------------

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
            for (int i = 0; i < reduction; i += 1)
            {
                damage *= 0.90f;
            }
        }

        if (resistances.Contains(type))
            damage *= 0.5f;
        else if (faiblesses.Contains(type))
            damage *= 1.25f;

        health -= damage;
        berserkDamage += damage;

        if (health <= 0f)
        {
            // Carte 6 : Kill Scaling
            if (attacker != null)
            {
                attacker.RegisterKill();
            }

            Die();
            return;
        }

        // Carte 7 : 30% de chances de réappliquer
        if (type == DamageType.Foudre && DrawingcardController.card7)
        {
            if (Random.value < 0.5f)
            {
                TakeDamage(damage * 0.5f, type, attacker);
            }
        }
    }

    // --- CARTE 10 : Fonction d'application de la marque ---
    public void ApplyLightningMark()
    {
        isLightningMarked = true;
        // Effet visuel optionnel (Feedback pour le joueur)
        Debug.Log($"{gameObject.name} est marqué pour la mort ! (+300% Foudre)");
    }
    // -----------------------------------------------------

    private void Die()
    {
        if (gameManager != null)
            gameManager.money += 10; 

        Destroy(gameObject);
    }

    // --- SYSTÈME DE STUN (Diminishing Returns) ---
    public void GetStunned(float duration)
    {
        // 1. Vérifier si on doit RESET la résistance
        if (Time.time > lastStunEndTime + stunResetTime)
        {
            currentStunMultiplier = 1f; // Reset complet
        }

        // 2. Calculer la durée effective
        float effectiveDuration = duration * currentStunMultiplier;
        if (effectiveDuration < minStunDuration) effectiveDuration = minStunDuration;
        float effectiveDurationCopy = effectiveDuration;
        // 3. Réduction pour le prochain stun
        while (effectiveDurationCopy > 1.0f)
        {
            currentStunMultiplier *= stunDecay;
            effectiveDurationCopy -= 1.0f;
        }
        
        // 4. Si déjà stun → on ajoute le temps restant
        if (isStunned)
        {
            // Prolonge la fin du stun actuelle
            if (lastStunEndTime < Time.time)
                lastStunEndTime = Time.time + effectiveDuration;
        }
        else
        {
            // Nouveau stun
            lastStunEndTime = Time.time + effectiveDuration;
            if (currentStunRoutine != null) StopCoroutine(currentStunRoutine);
            currentStunRoutine = StartCoroutine(StunCoroutine());
        }
    }

    private System.Collections.IEnumerator StunCoroutine()
    {
        isStunned = true;

        // Tant que le stun n’est pas fini
        while (Time.time < lastStunEndTime)
        {
            yield return null; // Attente frame par frame
        }

        // Fin du stun
        isStunned = false;

        currentStunRoutine = null;
    }

    public void getSlowed(float slowAmount, float duration, string towerID)
    {
        foreach (var s in activeSlows)
        {
            if (s.towerID == towerID) return; // Pas de stack du même ID
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
            // Note : Ici on passe null pour l'attacker car DOT ne stocke pas la référence TowerController
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