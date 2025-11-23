using UnityEngine;
using System.Collections.Generic;


 public enum DamageType { Feu, Snow, Terre, Air, Necrotique, Foudre, Base }

public class EnemyController : MonoBehaviour
{
    [Header("Stats de base")]
    public float baseHealth = 100f;
    public float speed = 1f;

    [HideInInspector] public float health;
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

    [Header("Diminishing Returns (Stun)")]
    public float stunResetTime = 5f;     // Temps sans stun pour revenir à la normale
    public float stunDecay = 0.8f;       // Facteur de réduction (0.8 = 80% de la durée précédente)
    public float minStunDuration = 0.1f; // Durée minimum d'un stun

    // Variables internes pour le Stun
    private float currentStunMultiplier = 1f;
    private float lastStunEndTime = -10f; 
    private float savedSpeedBeforeStun = -1f; // -1 signifie "pas sauvegardé"
    private Coroutine currentStunRoutine;

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
            currentSpeed *= (1f - activeSlows[i].amount / 100f);
        }

        secretSpeed = currentSpeed;

        // Mécanique Skadi (3 coups = stun)
        if (SkadiShotCalculator >= 3f) // >= par sécurité
        {
            getStunned(0.75f);
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
            if (health > maxHealth) health = maxHealth;
        }
        if (isBerserk)
        {
            var reduction = berserkDamage / 100f;
            // Attention boucle potentiellement lourde si berserkDamage est grand
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
            Die();
    }

    private void Die()
    {
        if (gameManager != null)
            gameManager.money += 10; // Valeur en dur, à changer par 'reward' si voulu

        Destroy(gameObject);
    }

    // --- SYSTÈME DE STUN (Diminishing Returns) ---
    public void getStunned(float duration)
    {
        // 1. Vérifier si on doit RESET la résistance (Si le dernier stun est fini depuis assez longtemps)
        // On utilise Time.time > lastStunEndTime + stunResetTime
        if (Time.time > lastStunEndTime + stunResetTime)
        {
            currentStunMultiplier = 1f; // Reset complet
        }

        // 2. Calculer la durée réelle
        float effectiveDuration = duration * currentStunMultiplier;

        // Cap minimal pour que le stun serve quand même à quelque chose (ex: interruption)
        if (effectiveDuration < minStunDuration) effectiveDuration = minStunDuration;

        // 3. Appliquer la réduction pour le PROCHAIN stun
        currentStunMultiplier *= stunDecay;

        // 4. Lancer la coroutine
        // Si on est déjà stun, on arrête l'ancien timer et on lance le nouveau (Refresh)
        if (currentStunRoutine != null) StopCoroutine(currentStunRoutine);
        currentStunRoutine = StartCoroutine(StunCoroutine(effectiveDuration));
        
        // Debug optionnel
        // Debug.Log($"Stun appliqué : {effectiveDuration}s (Prochain facteur : {currentStunMultiplier})");
    }

    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        // SAUVEGARDE SÉCURISÉE :
        // Si savedSpeedBeforeStun est à -1, c'est qu'on n'est pas encore stun. On sauvegarde la vitesse actuelle.
        // Si elle est différente de -1, c'est qu'on est DÉJÀ stun (vitesse 0), donc on ne touche pas à la sauvegarde originale.
        if (savedSpeedBeforeStun == -1f)
        {
            savedSpeedBeforeStun = speed;
            speed = 0f;
        }

        // On définit l'heure de fin pour le calcul du Reset
        lastStunEndTime = Time.time + duration;

        yield return new WaitForSeconds(duration);

        // FIN DU STUN
        // On rétablit la vitesse d'origine
        if (savedSpeedBeforeStun != -1f)
        {
            speed = savedSpeedBeforeStun;
            savedSpeedBeforeStun = -1f; // On reset le flag pour dire "je ne suis plus stun"
        }
        
        currentStunRoutine = null;
    }
    // ---------------------------------------------

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
        baseHealth = newHealth;
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
            TakeDamage(damage, damageType);
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