using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class DrawingcardController : MonoBehaviour
{
    public List<CardManger> cardDatas;

    [SerializeField] List<string> cards = new List<string>();
    [SerializeField] List<string> temporaryCards = new List<string>();
    [SerializeField] List<string> drawnCards = new List<string>();

    public GameObject[] cardsObjects;
    public GameObject[] ownedCardsObjects;

    public int handSize = 5;
    public float rerollCost = 2f;
    public GameObject MenuUI;
    public GameManager gameManager;
    public TextMeshProUGUI rerollbutton;

    // --- VARIABLES STATIQUES ---
    public static bool card1 = false; 
    public static bool card2 = false;
    public static float card2Cooldown = 15f; 
    public static float card2Timer = 0f;
    public static float card2Duration = 15f;
    public static float card2Damage = 20f;
    
    public static bool card3 = false;
    public static bool card3Used = false;
    public static bool card4 = false; 
    public static bool card5 = false; 
    public static bool card6 = false;
    public static bool card6Used = false;
    public static bool card7 = false; 
    public static bool card8 = false; 
    public static bool card9 = false;
    public static bool card9Used = false;
    public static bool card10 = false;
    public static bool card10Used = false;
    public static bool card11 = false;
    public static float card11Cooldown = 90f; 
    public static float card11Timer = 0f;     
    public static float card11Duration = 3f;  
    public static bool card12 = false;
    public static bool card13 = false;
    public static bool card13Used = false;
    public static bool card14 = false;
    public static bool card15 = false;
    public static bool card15Used = false;
    public static bool card16 = false;
    public static bool card17 = false;
    public static bool card17Used = false;
    public static bool card18 = false;
    public static bool card18ActiveEffect = false;
    public static float card18Cooldown = 60f;
    public static float card18Timer = 0f;
    public static float card18Duration = 10f;
    public static float card18DurationTimer = 0f;
    public static bool card19 = false;
    public static bool card20 = false;
    public static bool card20Used = false;
    public static bool card21 = false;
    public static bool card22 = false;
    public static bool card22Used = false;
    public static bool card23 = false;
    public static bool card24 = false;
    public static float card24Cooldown = 45f;
    public static float card24Timer = 0f;
    public static float card24Duration = 15f;
    
    public static bool card25 = false;
    public static bool card25Used = false;

    public static bool card26 = false;
    public static bool card27 = false;
    public static bool card27Used = false;
    
    public static bool card28 = false;
    public static bool card28Used = false;
    public static bool isCorbeauDuPereActive = false;
    private static int selectedTowersCount = 0;
    private static List<TowerController> selectedTowers = new List<TowerController>();

    public static bool card29 = false;
    public static bool card29ActiveEffect = false; 
    public static float card29Cooldown = 120f;     
    public static float card29Timer = 0f;          
    public static float card29Duration = 30f;      
    public static float card29DurationTimer = 0f;

    public static bool card30 = false;
    private float card30Timer = 0f;
    private float card30Interval = 7f;

    // --- AJOUT CARTE 31 : BAISER ARCTIQUE ---
    public static bool card31 = false;
    // ----------------------------------------

    void Start()
    {
        MenuUI.SetActive(false);
        ResetStatics(); 

        foreach (GameObject slot in ownedCardsObjects)
        {
            Image img = slot.GetComponent<Image>();
            if (img != null) img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = "";
        }
    }

    void ResetStatics()
    {
        card1 = false; card2 = false; card3 = false; card4 = false; card5 = false; card7 = false;
        card6 = false; card6Used = false;
        card8 = false;
        card9 = false; card9Used = false;
        card10 = false; card10Used = false;
        card11 = false; card11Timer = 0f;
        card12 = false;
        card13 = false; card13Used = false;
        card14 = false;
        card15 = false; card15Used = false;
        card16 = false;
        card17 = false; card17Used = false;
        card18 = false; card18Timer = 0f; card18ActiveEffect = false;
        card19 = false;
        card20 = false; card20Used = false;
        card21 = false;
        card22 = false; card22Used = false;
        card23 = false;
        card24 = false; card24Timer = 0f;
        card25 = false; card25Used = false;
        card26 = false;
        card27 = false; card27Used = false;
        card28 = false; card28Used = false;
        card29 = false; card29Timer = 0f; card29ActiveEffect = false;
        card30 = false; card30Timer = 0f;

        // --- RESET 31 ---
        card31 = false;
        // ----------------

        card2Timer = 0f;
        card3Used = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (MenuUI.activeSelf) MenuUI.SetActive(false);
            else
            {
                if (drawnCards.Count == 0) drawCard(false);
                else MenuUI.SetActive(true);
            }
        }

        if (rerollbutton) rerollbutton.text = "Reroll = " + rerollCost + "$";

        if (card2Timer > 0f) card2Timer -= Time.deltaTime;
        if (card11Timer > 0f) card11Timer -= Time.deltaTime;
        if (card18Timer > 0f) card18Timer -= Time.deltaTime;
        if (card18ActiveEffect)
        {
            card18DurationTimer -= Time.deltaTime;
            if (card18DurationTimer <= 0f) card18ActiveEffect = false;
        }

        if (card24Timer > 0f) card24Timer -= Time.deltaTime;
        if (isCorbeauDuPereActive && Input.GetMouseButtonDown(0))
        {
            DetectTowerClick();
        }

        if (card30)
        {
            card30Timer -= Time.deltaTime;
            if (card30Timer <= 0f)
            {
                ActivateOdinCyclone();
                card30Timer = card30Interval;
            }
        }

        if (card29Timer > 0f) card29Timer -= Time.deltaTime;
        if (card29ActiveEffect)
        {
            card29DurationTimer -= Time.deltaTime;
            if (card29DurationTimer <= 0f)
            {
                card29ActiveEffect = false;
                Debug.Log("Fin de Oeil et Vision (Carte 29)");
            }
        }
    }

    public static void ActivateEyeAndVision()
    {
        card29ActiveEffect = true;
        card29DurationTimer = card29Duration; 
        card29Timer = card29Cooldown;         
        Debug.Log("ACTIVATION OEIL ET VISION : Portée augmentée !");
    }

    void ActivateOdinCyclone()
    {
        GameObject nexus = GameObject.FindGameObjectWithTag("Nexus");
        if (nexus == null) return;
        float cycloneRadius = 6f;
        Collider[] hits = Physics.OverlapSphere(nexus.transform.position, cycloneRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    Vector3 pushDir = (enemy.transform.position - nexus.transform.position).normalized;
                    pushDir.y = 0;
                    enemy.getKnockBacked(6f, pushDir);
                }
            }
        }
    }

    public static void ActivateBrasierSeculaire()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        int execCount = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.TryExecuteBrasier()) execCount++;
        }
    }

    public static void ActivateFournaise()
    {
        card18ActiveEffect = true;
        card18DurationTimer = card18Duration;
        card18Timer = card18Cooldown;
        Debug.Log("ACTIVATION FOURNAISE : Toutes les tours brûlent les ennemis !");
    }

    // --- RESTAURATION DE LA MÉTHODE MANQUANTE ---
    public static void ActivateCorbeauDuPere()
    {
        if (isCorbeauDuPereActive) return; // éviter double activation
        isCorbeauDuPereActive = true;
        selectedTowersCount = 0;
        selectedTowers.Clear();

        Debug.Log("Corbeau du Père activé : cliquez sur 2 tours à lier !");
    }
    // --------------------------------------------
    void DetectTowerClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Tour"))
            {
                GameObject tower = hit.collider.gameObject;

                if (!selectedTowers.Contains(tower.GetComponent<TowerController>()))
                {
                    selectedTowers.Add(tower.GetComponent<TowerController>());
                    selectedTowersCount++;
                    Debug.Log("Tour sélectionnée (" + selectedTowersCount + "/2) : " + tower.name);

                    // Exemple : effet visuel temporaire
                    /*Renderer rend = tower.GetComponentInChildren<Renderer>();
                    if (rend != null)
                        rend.material.color = Color.cyan;*/
                }

                // Si deux tours sélectionnées, on arrête
                if (selectedTowersCount >= 2)
                {
                    isCorbeauDuPereActive = false;
                    Debug.Log("Deux tours sélectionnées. Corbeau du Père terminé !");
                    OnCorbeauDuPereFinished();
                }
            }
        }
    }
    void OnCorbeauDuPereFinished()
    {
        // Ici, tu peux faire ce que tu veux avec les deux tours choisies.
        // Exemple :
        if (selectedTowers.Count == 2)
        {
            TowerController towerA = selectedTowers[0];
            TowerController towerB = selectedTowers[1];
            float damageA = towerB.attackDamage * 0.05f;
            float damageB = towerA.attackDamage * 0.05f;


            towerA.attackDamage += damageA;
            towerB.attackDamage += damageB;
            float ABonus = 1 / towerB.attackCooldown * 0.05f;
            float BBonus = 1 / towerA.attackCooldown * 0.05f;
            float cooldownA = 1 / ((1 / towerA.attackCooldown) + ABonus);
            float cooldownB = 1 / ((1 / towerB.attackCooldown) + BBonus);
            towerA.attackCooldown = cooldownA;
            towerB.attackCooldown = cooldownB;
            if (towerA.attackCooldown < 0.1f)
            {
                towerA.attackCooldown = 0.1f;
            }
            if (towerB.attackCooldown < 0.1f)
            {
                towerB.attackCooldown = 0.1f;
            }
            Debug.Log("Tours liées : " + towerA.name + " et " + towerB.name);
            // Tu peux ici lancer un effet spécial, un buff partagé, etc.
        }

        selectedTowers.Clear();
    }


    void drawCard(bool reroll)
    {
        temporaryCards = new List<string>(cards);
        drawnCards.Clear();

        for (int i = 0; i < handSize; i++)
        {
            if (temporaryCards.Count > 0)
            {
                int randomIndex = Random.Range(0, temporaryCards.Count);
                string drawnCard = temporaryCards[randomIndex];
                drawnCards.Add(drawnCard);
                CardManger data = GetCardByID(drawnCard);
                CardSlotDisplay display = cardsObjects[i].GetComponent<CardSlotDisplay>();
                if(display) display.SetCardData(data);
                temporaryCards.RemoveAt(randomIndex);
            }
            else
            {
                drawnCards.Add("EMPTY");
                CardSlotDisplay display = cardsObjects[i].GetComponent<CardSlotDisplay>();
                if(display) display.SetCardData(null);
            }
        }
        MenuUI.SetActive(true);
    }

    public void rerollCard()
    {
        if (gameManager != null && gameManager.money >= rerollCost)
        {
            gameManager.money -= rerollCost;
            drawCard(true);
            rerollCost *= 2;
        }
    }

    public void IChooseCard(float cardIndex)
    {
        int index = (int)cardIndex;
        if (index < 0 || index >= drawnCards.Count) return;

        string chosen = drawnCards[index];
        if (chosen == "EMPTY") return;

        if (cards.Contains(chosen)) cards.Remove(chosen);

        switch (chosen)
        {
            case "1": card1 = true; break;
            case "2": card2 = true; break;
            case "3": card3 = true; card3Used = false; break;
            case "4": card4 = true; break;
            case "5": card5 = true; break;
            case "6": card6 = true; card6Used = false; break;
            case "7": card7 = true; break;
            case "8": card8 = true; break;
            case "9": card9 = true; card9Used = false; break;
            case "10": card10 = true; card10Used = false; break;
            case "11": card11 = true; break;
            case "12": card12 = true; break;
            case "13": card13 = true; card13Used = false; break;
            case "14": card14 = true; break;
            case "15": card15 = true; card15Used = false; break;
            case "16": card16 = true; break;
            case "17": card17 = true; card17Used = false; break;
            case "18": card18 = true; break;
            case "19": card19 = true; break;
            case "20": card20 = true; card20Used = false; break;
            case "21": card21 = true; break;
            case "22": card22 = true; card22Used = false; break;
            case "23": card23 = true; break;
            case "24": card24 = true; break;
            case "25": card25 = true; card25Used = false; break;
            case "26": card26 = true; break;
            case "27": card27 = true; card27Used = false; break;
            case "28": card28 = true; card28Used = false; break;
            case "29": card29 = true; break;
            case "30": card30 = true; break;
            // --- AJOUT 31 ---
            case "31": card31 = true; break;
            // ----------------
        }

        AddToOwnedCards(chosen);
        drawCard(false);
        MenuUI.SetActive(false);
    }

    void AddToOwnedCards(string cardName)
    {
        foreach (GameObject slot in ownedCardsObjects)
        {
            TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
            if (text != null && string.IsNullOrEmpty(text.text))
            {
                text.text = cardName;
                
                // La 31 est passive, non draggable
                if (cardName == "2" || cardName == "3" || cardName == "6" || cardName == "9" || 
                    cardName == "10" || cardName == "11" || cardName == "13" || cardName == "15" || 
                    cardName == "17" || cardName == "18" || cardName == "20" || cardName == "22" || 
                    cardName == "24" || cardName == "25" || cardName == "27" || cardName == "28" || 
                    cardName == "29")
                {
                    if (slot.GetComponent<CanvasGroup>() == null) 
                        slot.AddComponent<CanvasGroup>();

                    var existingDrag = slot.GetComponent<DraggableCard>();
                    if (existingDrag == null) existingDrag = slot.AddComponent<DraggableCard>();
                    existingDrag.cardID = cardName;
                }
                return;
            }
        }
    }

    public static bool TryActivateCard10()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        if (enemies.Length == 0) return false;
        EnemyController bestTarget = null;
        float highestMaxHP = -1f;
        foreach (EnemyController enemy in enemies)
        {
            if (enemy.maxHealth > highestMaxHP)
            {
                highestMaxHP = enemy.maxHealth;
                bestTarget = enemy;
            }
        }
        if (bestTarget != null)
        {
            bestTarget.ApplyLightningMark();
            return true; 
        }
        return false;
    }

    Color getColor(string cardName)
    {
        return cardName switch
        {
            "1" => new Color(1f, 0f, 0f, 1f),
            "2" => new Color(0f, 1f, 0f, 1f),
            "3" => new Color(0f, 0f, 1f, 1f),
            "4" => new Color(1f, 1f, 0f, 1f),
            "5" => new Color(1f, 0f, 1f, 1f),
            "6" => new Color(0f, 1f, 1f, 1f),
            "7" => new Color(0.5f, 0f, 0.5f, 1f),
            "8" => new Color(1f, 0.5f, 0f, 1f),
            "9" => new Color(0.8f, 0f, 0.8f, 1f),
            "10" => new Color(1f, 0.84f, 0f, 1f),
            "11" => new Color(1f, 0.2f, 0.2f, 1f),
            "12" => new Color(0.5f, 0.5f, 0.5f, 1f),
            "13" => new Color(1f, 0.5f, 0.2f, 1f),
            "14" => new Color(1f, 0.8f, 0.2f, 1f),
            "15" => new Color(0.6f, 0f, 0f, 1f),
            "16" => new Color(0.5f, 0f, 0.5f, 1f),
            "17" => new Color(1f, 1f, 0.4f, 1f),
            "18" => new Color(1f, 0.27f, 0f, 1f),
            "19" => new Color(0.7f, 0f, 0f, 1f),
            "20" => new Color(0.4f, 0f, 0f, 1f),
            "21" => new Color(0.6f, 0.8f, 1f, 1f),
            "22" => new Color(0f, 1f, 1f, 1f),
            "23" => new Color(0f, 1f, 1f, 1f),
            "24" => new Color(0.1f, 0.1f, 0.8f, 1f),
            "25" => new Color(0.1f, 0.1f, 0.8f, 1f),
            "26" => new Color(0.8f, 1f, 1f, 1f),
            "27" => new Color(0.6f, 1f, 0.8f, 1f),
            "28" => new Color(0.1f, 0.1f, 0.8f, 1f),
            "29" => new Color(0.5f, 0.8f, 1f, 1f),
            "30" => new Color(0.9f, 0.9f, 1f, 1f),
            // --- AJOUT 31 : Cyan Pâle ---
            "31" => new Color(0.7f, 1f, 1f, 1f),
            // ----------------------------
            _ => new Color(0.2f, 0.2f, 0.2f, 1f),
        };
    }

    CardManger GetCardByID(string id)
    {
        foreach (var c in cardDatas) 
        {
            if (c.cardID == id) return c;
        }
        Debug.LogWarning("Carte introuvable : " + id);
        return null;
    }
}