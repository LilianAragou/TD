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
    public LayerMask enemyLayer; 
    public string enemyTag = "Enemy";
    public DamageType dmgType; 
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
    public bool hasElectriqueCourant = false;
    public bool electriqueCourantActivated = false;

    [Header("Debug Visuel")]
    [SerializeField] private float rangeVisualMultiplier = 3.5f; 

    // État interne
    [HideInInspector] public float cooldownTime = 0f;
    [HideInInspector] public float stockedDamage = 0f; 
    public Transform target;
    private List<Transform> attackedTargets = new List<Transform>();
    private bool isFirstShot;
    private TileManager tileManager; // Référence pour la logique de grille (Carte 8)

    // --- Variables pour les Cartes & Buffs ---
    public bool isBuffedByOdinEye = false;
    
    // Carte 3 : Buff de zone
    private bool foudreBuffer = false; 
    public bool foudrebuffed = false; 

    // Carte 2 : Orage
    public bool orageActive = false;
    public float orageTickRate = 0.1f;
    private Coroutine orageCoroutine;

    // Carte 6 : Scaling
    [Header("Carte 6 - Scaling")]
    public bool hasKillScaling = false;
    public int killCount = 0;
    private int maxKillsForBonus = 50; 

    // Carte 8 : Synergie
    [Header("Carte 8 - Synergie")]
    public bool isThorPaired = false;
    private float adjacencyCheckTimer = 0f;
    private float adjacencyCheckInterval = 1.0f; // Vérification toutes les secondes
    
    // Carte 11 : Surcharge (AJOUT)
    [Header("Carte 11 - Surcharge")]
    public bool isSurcharged = false;

    // Aura Projectiles (TwinWind)
    private List<GameObject> auraProjectiles = new List<GameObject>();
    private float[] angles;
    [Header("Carte 12")]
    public bool hasDoubleRangeVolca = false;

    void Start()
    {
        isFirstShot = hasAFirstAttack;
        
        // Récupération du TileManager pour la logique de grille
        tileManager = FindObjectOfType<TileManager>();

        if (rangeaura) rangeaura.SetActive(false);

        if (hasProjectileAura)
            InitProjectileAura();
    }

    void Update()
    {
        cooldownTime -= Time.deltaTime;

        // --- LOGIQUE CARTE 3 (Buff de zone actif) ---
        if (foudreBuffer)
        {
            ApplyFoudreBufferToNeighbors();
        }

        // --- LOGIQUE CARTE 8 (Check Voisins via Grille) ---
    
        adjacencyCheckTimer -= Time.deltaTime;
        if (adjacencyCheckTimer <= 0f)
        {
            if (DrawingcardController.card8 && towerID == "ThorTotem")
            {
                CheckThorAdjacency();
            }
            if (hasElectriqueCourant)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                if (hits.Length >= 10)
                {
                    electriqueCourantActivated = true;
                }
                else
                {
                    electriqueCourantActivated = false;
                }
            }
            adjacencyCheckTimer = adjacencyCheckInterval;
        }
        // --------------------------------------------------

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
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                
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
        if (DrawingcardController.card12 && !hasDoubleRangeVolca && towerID == "VolcanoTower")
        {
            attackRange *= 2;
            hasDoubleRangeVolca = true;
        }
    }

    // --- SYSTÈME DE VISÉE OPTIMISÉ (3D) ---
    
    public void CheckForNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        
        Transform best = null;
        float bestDistToNexus = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag(enemyTag)) continue;

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
        PojectileController pc = bullet.GetComponent<PojectileController>(); 
        
        if (pc != null)
        {
            pc.target = _target;
            pc.towerID = towerID;
            pc.mummyTower = this;
            pc.speed = projectileSpeed;

            // --- CALCUL DES DÉGÂTS ---
            float finalDamage = attackDamage;

            // --- AJOUT CARTE 11 : SURCHARGE ---
            if (isSurcharged)
            {
                finalDamage *= 11f; // +1000% de dégâts (x11)
            }
            // ----------------------------------

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

            // CARTE 8 : +25% si Synergie Totem active
            if (isThorPaired && DrawingcardController.card8)
            {
                finalDamage *= 1.25f; 
            }

            pc.damage = finalDamage;
        }
    }

    // --- LOGIQUES SPÉCIFIQUES (Aura, Orage, Buffs) ---

    void HandleAuraTowerLogic()
    {
        // --- NOUVEAU : WIND TOTEM (Totem des Vents) CIBLE UNIQUE ---
        if (towerID == "WindTotem")
        {
            if (cooldownTime <= 0f)
            {
                Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                
                // 1. Trouver LA cible unique (la plus proche du Nexus)
                Transform bestTarget = null;
                float bestDistToNexus = Mathf.Infinity;

                foreach (Collider enemy in hitEnemies)
                {
                    if (!enemy.CompareTag(enemyTag)) continue;
                    
                    // On cherche celui qui est le plus proche de (0,0,0)
                    float distToNexus = Vector3.Distance(Vector3.zero, enemy.transform.position);
                    if (distToNexus < bestDistToNexus)
                    {
                        bestDistToNexus = distToNexus;
                        bestTarget = enemy.transform;
                    }
                }

                // 2. Si on a trouvé une cible, on frappe
                if (bestTarget != null)
                {
                    EnemyController enemyHealth = bestTarget.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                    {
                        float finalDamage = attackDamage; 
                        if (isSurcharged) finalDamage *= 11f; // Carte 11

                        // Dégâts
                        enemyHealth.TakeDamage(finalDamage, dmgType, this);
                        
                        // Recul (Direction : Tour -> Ennemi)
                        Vector3 pushDir = (bestTarget.position - transform.position).normalized;
                        pushDir.y = 0; 
                        
                        enemyHealth.getKnockBacked(3f, pushDir);

                        // On lance le cooldown seulement si on a tiré
                        cooldownTime = attackCooldown;
                    }
                }
            }
            return;
        }
        // -----------------------------------------------------------

        // --- Volcano Tower (Burn) ---
        if (towerID == "VolcanoTower")
        {
            // On applique les dégâts par "Tick" (intervalle régulier)
            // Pour simuler 30 damage/s, on peut tick tous les 0.5s et faire 15 dégâts.
            float tickRate = 0.5f;

            if (cooldownTime <= 0f)
            {
                Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                
                // Calcul des dégâts par tick
                // attackDamage = DPS souhaité (ex: 30 dans l'inspecteur)
                float damagePerTick = attackDamage * tickRate;

                // Application CARTE 11 (Surcharge) sur l'Aura
                if (isSurcharged) damagePerTick *= 11f;

                foreach (Collider enemy in hitEnemies)
                {
                    if (enemy.CompareTag(enemyTag))
                    {
                        EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.TakeDamage(damagePerTick, dmgType, this);
                        }
                    }
                }
                cooldownTime = tickRate; 
            }
            return; 
        }
        // -------------------------------

        // --- ThorPillar (Stun de zone) ---
        if (towerID == "ThorPillar")
        {
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
                            enemyHealth.TakeDamage(attackDamage, dmgType, this); 

                            // Carte 4 : Stun augmenté
                            float stunTime = 0.75f;
                            if (DrawingcardController.card4) stunTime += 0.75f;
                            enemyHealth.GetStunned(stunTime);
                        }
                    }
                }
                cooldownTime = attackCooldown; 
            }
            return; 
        }

        // --- Standard Auras ---
    
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
        // --- OdinEye (Buff Alliés) ---
        else if (towerID == "OdinEye")
        {
            Collider[] hitAllies = Physics.OverlapSphere(transform.position, attackRange);
            foreach (Collider ally in hitAllies)
            {
                if (ally.CompareTag("Tour"))
                {
                    TowerController allyTower = ally.GetComponent<TowerController>();
                    if (allyTower != null && allyTower != this && allyTower.towerID != "OdinEye" && !allyTower.isBuffedByOdinEye)
                    {
                        allyTower.attackDamage *= 1.2f; 
                        allyTower.attackCooldown *= 0.8f; 
                        allyTower.isBuffedByOdinEye = true;
                    }
                }
            }
        }
        // --- BaldrObelisk (Accumulation) ---
        else if (towerID == "BaldrObelisk")
        {
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
            
            if (cooldownTime <= 0f && stockedDamage > 0f)
            {
                Collider[] enemyHits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
                foreach (Collider hit in enemyHits)
                {
                    EnemyController enemy = hit.GetComponent<EnemyController>();
                    if (enemy != null) enemy.TakeDamage(stockedDamage, dmgType, this);
                }
                cooldownTime = attackCooldown;
                stockedDamage = 0f;
            }
        }
    }

    // --- CARTE 3 : Buff Foudre ---
    public void BuffFoudre()
    {
        foudreBuffer = true;
    }

    void ApplyFoudreBufferToNeighbors()
    {
        Collider[] towersInRange = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider col in towersInRange)
        {
            if (col.gameObject == this.gameObject) continue;

            if (col.CompareTag("Tour"))
            {
                TowerController otherTower = col.GetComponent<TowerController>();
                if (otherTower != null && !otherTower.foudrebuffed && otherTower.dmgType == DamageType.Foudre)
                {
                    otherTower.foudrebuffed = true;
                }
            }
        }
    }
    
    // --- CARTE 8 : Vérification Adjacence (Optimisée Grille) ---
    void CheckThorAdjacency()
    {
        if (tileManager == null) return;

        // 1. Trouver sur quelle tuile est posée cette tour
        Tile myTile = GetComponentInParent<Tile>();
        if (myTile == null) return; 

        int myX = myTile.x;
        int myY = myTile.y;
        bool foundNeighbor = false;

        // 2. Vérifier les 8 voisins (Diagonales incluses)
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                if (xOffset == 0 && yOffset == 0) continue; // On saute soi-même

                int checkX = myX + xOffset;
                int checkY = myY + yOffset;

                // Limites de la grille
                if (checkX >= 0 && checkX < tileManager.gridSize &&
                    checkY >= 0 && checkY < tileManager.gridSize)
                {
                    GameObject neighborTileObj = tileManager.tiles[checkX, checkY];
                    
                    if (neighborTileObj != null)
                    {
                        Tile neighborTile = neighborTileObj.GetComponent<Tile>();
                        // Si la tuile a un occupant (une tour)
                        if (neighborTile != null && neighborTile.HasOccupant)
                        {
                            TowerController neighborTower = neighborTileObj.GetComponentInChildren<TowerController>();
                            
                            if (neighborTower != null && neighborTower.towerID == "ThorTotem")
                            {
                                foundNeighbor = true;
                                break; 
                            }
                        }
                    }
                }
            }
            if (foundNeighbor) break;
        }

        isThorPaired = foundNeighbor;
    }
    // -----------------------------------------------------------

    // CARTE 2 : Déclenchement de l'Orage
    public void Orage(float damage, float duration)
    {
        if (orageCoroutine != null) StopCoroutine(orageCoroutine);
        orageCoroutine = StartCoroutine(OrageCoroutine(damage, duration));
    }

    private IEnumerator OrageCoroutine(float damage, float duration)
    {
        orageActive = true;
        float elapsed = 0f;
        Collider[] buffer = new Collider[50]; 

        while (elapsed < duration)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider enemy = buffer[i];
                if (enemy != null)
                {
                    EnemyController enemyHealth = enemy.GetComponent<EnemyController>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damage, DamageType.Foudre, this);
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

    // CARTE 6 : Kill Scaling
    public void ActivateKillScaling()
    {
        hasKillScaling = true;
        Debug.Log($"Carte 6 activée sur {towerID} : Les kills augmentent la vitesse !");
    }

    public void RegisterKill()
    {
        if (!hasKillScaling) return;

        if (killCount < maxKillsForBonus)
        {
            killCount++;
            attackCooldown = attackCooldown / 1.01f;
        }
    }
    
    // --- CARTE 11 : SURCHARGE (AJOUT) ---
    public void ActivateSurcharge(float duration)
    {
        // Si déjà activé, on reset (ou relance)
        if (isSurcharged) StopCoroutine("SurchargeRoutine");
        StartCoroutine(SurchargeRoutine(duration));
    }

    private IEnumerator SurchargeRoutine(float duration)
    {
        isSurcharged = true;
        // Optionnel : Ajouter ici un changement de couleur temporaire
        
        yield return new WaitForSeconds(duration);
        
        isSurcharged = false;
        // Optionnel : Revert de la couleur
    }
    // ------------------------------------

    void panickShot()
    {
        Vector2 rand = Random.insideUnitCircle * attackRange;
        Vector3 randomPoint = transform.position + new Vector3(rand.x, 0f, rand.y);

        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = randomPoint;
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
        
        Destroy(tempTarget, 2f); 
    }

    void InitProjectileAura()
    {
        angles = new float[(int)projectileCount];
        for (int i = 0; i < projectileCount; i++)
        {
            angles[i] = i * Mathf.PI * 2 / projectileCount;
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angles[i]), 0f, Mathf.Sin(angles[i])) * attackRange;
            
            GameObject proj = Instantiate(auraProjectilePrefab, spawnPos, Quaternion.identity);
            proj.tag = towerID; 
            
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

    public void UpdateRangeVisual()
    {
        if (rangeaura == null) return;
        float finalSize = attackRange * 2f * rangeVisualMultiplier;
        rangeaura.transform.localScale = new Vector3(finalSize, 0.1f, finalSize);
    }
}