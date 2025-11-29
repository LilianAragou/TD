using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TowerController : MonoBehaviour
{
    [Header("Stats de base")]
    [SerializeField] public float attackRange = 5f;
    [SerializeField] public float attackDamage = 10f;
    [SerializeField] public float attackCooldown = 1f;
    [SerializeField] public float projectileSpeed = 10f;
    
    [Header("Configuration")]
    public LayerMask enemyLayer; // IMPORTANT : Mettre sur "Enemy" dans l'inspecteur
    public string enemyTag = "Enemy";
    public DamageType dmgType; // Assurez-vous que c'est 'Foudre' pour les tours électriques
    public string towerID;
    public Transform firePoint;
    
    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject rangeaura;
    public GameObject auraProjectilePrefab;

    [Header("Comportements")]
    public bool hasAFirstAttack = false;
    public bool isAuraTower = false;
    public bool isProjectileTower = true;
    public bool hasProjectileAura = false;
    public bool canAim = false;
    public bool hasPanickShot = false;
    
    [Header("Paramètres avancés")]
    public float rotationSpeed = 5f;
    public float projectileCount = 1f;
    public float numberOfTargets = 1f;
    public float firstShotCD;

    // État interne
    [HideInInspector] public float cooldownTime = 0f;
    [HideInInspector] public float stockedDamage = 0f; // Pour Baldr/Stats
    public Transform target;
    private List<Transform> attackedTargets = new List<Transform>();
    private bool isFirstShot;
    
    // --- Variables pour les Cartes & Buffs ---
    public bool isBuffedByOdinEye = false;
    
    // Carte 3 : Cette tour buff-t-elle les autres ?
    private bool foudreBuffer = false; 
    // Carte 3 : Cette tour est-elle buffée par une autre ?
    public bool foudrebuffed = false; 

    // Carte 2 : Orage
    public bool orageActive = false;
    public float orageTickRate = 0.1f;
    private Coroutine orageCoroutine;

    // Aura Projectiles (TwinWind)
    private List<GameObject> auraProjectiles = new List<GameObject>();
    private float[] angles;


    void Start()
    {
        isFirstShot = hasAFirstAttack;
        if (rangeaura) rangeaura.SetActive(false);

        if (hasProjectileAura)
            InitProjectileAura();
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

        // --- LOGIQUE CARTE 3 (Buff de zone actif) ---
        // Si on a reçu la carte "Se drop sur une tour...", on applique le buff aux voisins
        if (foudreBuffer)
        {
            ApplyFoudreBufferToNeighbors();
        }

        // --- LOGIQUE TOURELLE CLASSIQUE (PROJECTILES) ---
        if (isProjectileTower)
        {
            // Cas spécial BaldrObelisk (ne tire que si cooldown prêt)
            if (cooldownTime > 0f && towerID != "BaldrObelisk")
                return;

            if (target == null)
            {
                CheckForNearestEnemy();
            }
            else
            {
                // Vérification 3D de la portée
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                
                // Si la cible sort de la portée ou meurt (null check implicite unity)
                if (distanceToTarget > attackRange || target.gameObject == null)
                {
                    target = null;
                }
                else if (cooldownTime <= 0f)
                {
                    AttackTarget();
                    cooldownTime = attackCooldown;
                }
            }
        }

        // --- LOGIQUE AURA (ZONES D'EFFET PERMANENTES) ---
        if (isAuraTower)
        {
            HandleAuraTowerLogic();
        }
    }

    // --- SYSTÈME DE VISÉE OPTIMISÉ (3D) ---
    
    public void CheckForNearestEnemy()
    {
        // Optimisation : On cherche uniquement dans la sphère, sur le Layer Enemy
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // Double sécurité tag
            if (!hit.CompareTag(enemyTag)) continue;

            // On vise l'ennemi le plus proche du Nexus (0,0,0)
            float distToNexus = Vector3.Distance(Vector3.zero, hit.transform.position);
            
            if (distToNexus < bestDistToNexus)
            {
                bestDistToNexus = distToNexus;
                best = hit.transform;
            }
        }
        
        if (best != null)
        {
            target = best;
            isFirstShot = true;
            cooldownTime = firstShotCD;
        }
    }

    void getNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Transform t = hit.transform;
            
            // On évite de viser deux fois la même cible dans une salve multiple
            if (attackedTargets.Contains(t)) continue;
            if (!t.CompareTag(enemyTag)) continue;

            float distToNexus = Vector3.Distance(Vector3.zero, t.position);
            if (distToNexus < bestDistToNexus)
            {
                bestDistToNexus = distToNexus;
                best = t;
            }
        }

        if (best != null)
        {
            attackedTargets.Add(best);
            target = best;
        }
        else
        {
            target = null;
        }
    }

    // --- SYSTÈME D'ATTAQUE ---

    void AttackTarget()
    {
        if (target != null)
        {
            if (numberOfTargets > 1f)
            {
                attackedTargets.Clear();
                attackedTargets.Add(target); 

                FireProjectile(target);

                // Tirs multiples (Multishot)
                for (int i = 1; i < numberOfTargets; i++)
                {
                    getNewTarget();
                    if (target != null)
                    {
                         FireProjectile(target);
                    }
                    else if (hasPanickShot)
                    {
                        panickShot();
                    }
                }
                // Reset target principal pour la frame suivante
                target = attackedTargets.Count > 0 ? attackedTargets[0] : null; 
            }
            else
            {
                FireProjectile(target);
            }
        }
    }

    void FireProjectile(Transform _target)
    {
        if (projectilePrefab == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        PojectileController pc = bullet.GetComponent<PojectileController>(); // Attention à la typo "Pojectile"
        
        if (pc != null)
        {
            pc.target = _target;
            pc.towerID = towerID;
            pc.mummyTower = this;
            pc.speed = projectileSpeed;

            // --- CALCUL DES DÉGÂTS (CARTES 3 & 5) ---
            float finalDamage = attackDamage;

            // CARTE 5 : +15% sur tous les dégâts de foudre
            if (DrawingcardController.card5 && dmgType == DamageType.Foudre)
            {
                finalDamage *= 1.15f;
            }

            // CARTE 3 : +30% si buffé par une tour voisine
            if (foudrebuffed)
            {
                finalDamage *= 1.30f;
            }

            pc.damage = finalDamage;
        }
    }

    // --- LOGIQUES SPÉCIFIQUES (Aura, Orage, Buffs) ---

    void HandleAuraTowerLogic()
    {
         // --- NOUVEAU : ThorPillar en tour à aura ---
        if (towerID == "ThorPillar")
        {
            // La tour inflige périodiquement des dégâts et stun les ennemis
            if (cooldownTime <= 0f)
            {
                Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                foreach (Collider enemy in hitEnemies)
                {
                    if (enemy.CompareTag(enemyTag))
                    {
                        EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.TakeDamage(attackDamage, dmgType);

                            // --- Carte 4 : Stun augmenté ---
                            float stunTime = 0.75f;
                            if (DrawingcardController.card4) stunTime += 0.75f;
                            enemyHealth.GetStunned(stunTime);
                        }
                    }
                }

                cooldownTime = attackCooldown; // Reset du timer
            }
            return; // On sort ici, pas besoin d'appliquer les autres auras
        }
        if (towerID != "OdinEye" && towerID != "BaldrObelisk")
        {
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.CompareTag(enemyTag))
                {
                    EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                    if (enemyHealth != null) enemyHealth.getAuraEffect(towerID, this);
                }
            }
        }
        else if (towerID == "OdinEye")
        {
            // Odin buff les autres tours
            Collider[] hitAllies = Physics.OverlapSphere(transform.position, attackRange);
            foreach (Collider ally in hitAllies)
            {
                if (ally.CompareTag("Tour"))
                {
                    TowerController allyTower = ally.GetComponent<TowerController>();
                    if (allyTower != null && allyTower != this && allyTower.towerID != "OdinEye" && !allyTower.isBuffedByOdinEye)
                    {
                        allyTower.attackDamage *= 1.2f; // +20%
                        allyTower.attackCooldown *= 0.8f; // -20%
                        allyTower.isBuffedByOdinEye = true;
                    }
                }
            }
        }
        else if (towerID == "BaldrObelisk")
        {
            // Logique d'accumulation de dégâts
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Tour"))
                {
                    TowerController ally = hit.GetComponent<TowerController>();
                    if (ally != this && ally != null)
                    {
                        stockedDamage += ally.stockedDamage;
                        ally.stockedDamage = 0f;
                    }
                }
            }
            
            // Explosion stockée
            if (cooldownTime <= 0f && stockedDamage > 0f)
            {
                Collider[] enemyHits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                foreach (Collider hit in enemyHits)
                {
                    EnemyController enemy = hit.GetComponent<EnemyController>();
                    if (enemy != null) enemy.TakeDamage(stockedDamage, dmgType);
                }
                cooldownTime = attackCooldown;
                stockedDamage = 0f;
            }
        }
    }

    // Appelé si la Carte 3 est dropée sur cette tour
    public void BuffFoudre()
    {
        foudreBuffer = true;
    }

    // CARTE 3 : Logique cyclique pour buffer les voisins
    void ApplyFoudreBufferToNeighbors()
    {
        Collider[] towersInRange = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider col in towersInRange)
        {
            // On ignore soi-même
            if (col.gameObject == this.gameObject) continue;

            // On ne buffe que les TOURS
            if (col.CompareTag("Tour"))
            {
                TowerController otherTower = col.GetComponent<TowerController>();
                
                // Condition : C'est une tour de Foudre, et elle n'est pas encore buffée
                if (otherTower != null && !otherTower.foudrebuffed && otherTower.dmgType == DamageType.Foudre)
                {
                    otherTower.foudrebuffed = true;
                    // Ici on pourrait ajouter un petit effet visuel (particules)
                }
            }
        }
    }

    // CARTE 2 : Déclenchement de l'Orage
    public void Orage(float damage, float duration)
    {
        if (orageCoroutine != null)
        {
            StopCoroutine(orageCoroutine);
        }
        orageCoroutine = StartCoroutine(OrageCoroutine(damage, duration));
    }

    private IEnumerator OrageCoroutine(float damage, float duration)
    {
        orageActive = true;
        float elapsed = 0f;
        
        // Buffer pour éviter l'allocation mémoire à chaque frame
        Collider[] buffer = new Collider[50]; 

        while (elapsed < duration)
        {
            // Dégâts de zone périodiques (3D)
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider enemy = buffer[i];
                if (enemy != null)
                {
                    EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damage, DamageType.Foudre);
                        stockedDamage += damage * (1 + enemyHealth.debuffDamage / 100f);
                    }
                }
            }

            elapsed += orageTickRate;
            yield return new WaitForSeconds(orageTickRate);
        }

        orageActive = false;
        orageCoroutine = null;
    }

    void panickShot()
    {
        // Tir au hasard autour de la tour
        Vector2 rand = Random.insideUnitCircle * attackRange;
        Vector3 randomPoint = transform.position + new Vector3(rand.x, 0f, rand.y);

        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = randomPoint;
        // Important pour que le projectile guidé ne plante pas, on simule un ennemi
        tempTarget.tag = enemyTag; 
        
        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        PojectileController pc = bullet.GetComponent<PojectileController>();
        
        if (pc != null)
        {
            pc.isPanickShot = true;
            pc.target = tempTarget.transform;
            pc.damage = attackDamage;
            pc.speed = projectileSpeed;
            pc.towerID = towerID;
            pc.mummyTower = this;
        }
        
        Destroy(tempTarget, 2f); // Nettoyage de la cible temporaire
    }

    void InitProjectileAura()
    {
        angles = new float[(int)projectileCount];
        for (int i = 0; i < projectileCount; i++)
        {
            angles[i] = i * Mathf.PI * 2 / projectileCount;
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angles[i]), 0f, Mathf.Sin(angles[i])) * attackRange;
            
            GameObject proj = Instantiate(auraProjectilePrefab, spawnPos, Quaternion.identity);
            proj.tag = towerID; // Tag utilisé pour identifier l'effet dans le projectile controller
            
            PojectileController pc = proj.GetComponent<PojectileController>();
            if (pc != null)
            {
                pc.damage = attackDamage;
                pc.towerID = towerID;
                pc.mummyTower = this;
                pc.speed = projectileSpeed;
                pc.centerPointForPatrol = this.transform;
                pc.angleForPatrol = angles[i];
                pc.radiusForPatrol = attackRange;
                pc.isPatrol = true;
            }
            auraProjectiles.Add(proj);
        }
    }
    
    public void ReduceCooldown(float amount)
    {
        attackCooldown = Mathf.Max(0.1f, attackCooldown - amount);
    }

    // --- GIZMOS (DEBUG VISUEL) ---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    void OnMouseEnter()
    {
        if (rangeaura)
        {
            UpdateRangeVisual();
            rangeaura.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        if (rangeaura) rangeaura.SetActive(false);
    }

    // Nouvelle fonction utilitaire pour recalculer la taille proprement
    public void UpdateRangeVisual()
    {
        if (rangeaura == null) return;

        // Formule expliquée :
        // attackRange = Rayon (Radius)
        // Scale d'une primitive Unity (Sphere/Cylindre) = Diamètre
        // Diamètre = Rayon * 2
        // Si ton objet est encore 2x trop petit à cause de son mesh de base ou du parent -> on remultiplie par 2 via le multiplier.
        
        float finalSize = attackRange * 2f * 3.5f;
        
        // On applique la taille en X et Z (le sol), et on garde Y plat (0.1f ou moins)
        rangeaura.transform.localScale = new Vector3(finalSize, 0.1f, finalSize);
    }
}