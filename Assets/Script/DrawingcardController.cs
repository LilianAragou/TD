using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class DrawingcardController : MonoBehaviour
{
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

    // --- VARIABLES STATIQUES (Sauvegardées entre les scènes) ---
    public static bool card1 = false; // Double Ricochet
    
    // Carte 2 (Orage - Active)
    public static bool card2 = false;
    public static float card2Cooldown = 15f; 
    public static float card2Timer = 0f;
    public static float card2Duration = 15f;
    public static float card2Damage = 20f;
    
    // Carte 3 (Buff Foudre - Active)
    public static bool card3 = false;
    public static bool card3Used = false;
    
    public static bool card4 = false; // Stun Boost
    public static bool card5 = false; // Dmg Foudre +15%
    
    // Carte 6 (Scaling Speed - Active)
    public static bool card6 = false;
    public static bool card6Used = false;

    public static bool card7 = false; // Re-proc 30%

    // Carte 8 (Synergie - Passive)
    public static bool card8 = false; 

    public static bool card9 = false;
    public static bool card9Used = false;

    // Carte 10 (Unique - Active Sort Global)
    public static bool card10 = false;
    public static bool card10Used = false;

    // --- AJOUT CARTE 11 (Surcharge - Active sur Tour) ---
    public static bool card11 = false;
    public static float card11Cooldown = 90f; // 1min30
    public static float card11Timer = 0f;     // Timer actuel
    public static float card11Duration = 30f;  // Durée de l'effet
    public static bool card12 = false;
    // ----------------------------------------------------

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

        card11 = false;
        card12 = false;
        card11Timer = 0f;

        card2Timer = 0f;
        card3Used = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (MenuUI.activeSelf)
            {
                MenuUI.SetActive(false);
            }
            else
            {
                if (drawnCards.Count == 0)
                {
                    drawCard(false);
                }
                else
                {
                    MenuUI.SetActive(true);
                }
            }
        }

        if (rerollbutton)
            rerollbutton.text = "Reroll = " + rerollCost + "$";

        // Gestion des timers
        if (card2Timer > 0f) card2Timer -= Time.deltaTime;

        // --- TIMER CARTE 11 ---
        if (card11Timer > 0f) card11Timer -= Time.deltaTime;
        // ----------------------
    }

    void drawCard(bool reroll)
    {
        temporaryCards = new List<string>(cards);
        drawnCards.Clear();

        for (int i = 0; i < handSize; i++)
        {
            Image slotImage = cardsObjects[i].GetComponent<Image>();

            if (temporaryCards.Count > 0)
            {
                int randomIndex = Random.Range(0, temporaryCards.Count);
                string drawnCard = temporaryCards[randomIndex];

                drawnCards.Add(drawnCard);
                slotImage.color = getColor(drawnCard);

                temporaryCards.RemoveAt(randomIndex);
            }
            else
            {
                drawnCards.Add("EMPTY");
                slotImage.color = Color.black;
            }
        }
        MenuUI.SetActive(true);
    }

    Color getColor(string cardName)
    {
        return cardName switch
        {
            "1" => new Color(1f, 0f, 0f, 1f),       // Rouge
            "2" => new Color(0f, 1f, 0f, 1f),       // Vert
            "3" => new Color(0f, 0f, 1f, 1f),       // Bleu
            "4" => new Color(1f, 1f, 0f, 1f),       // Jaune
            "5" => new Color(1f, 0f, 1f, 1f),       // Magenta
            "6" => new Color(0f, 1f, 1f, 1f),       // Cyan
            "7" => new Color(0.5f, 0f, 0.5f, 1f),   // Violet foncé
            "8" => new Color(1f, 0.5f, 0f, 1f),     // Orange
            "9" => new Color(0.8f, 0f, 0.8f, 1f),   // Violet clair
            "10" => new Color(1f, 0.84f, 0f, 1f),   // Gold
            // --- AJOUT : Rouge Vif "Danger" pour la 11 ---
            "11" => new Color(1f, 0.2f, 0.2f, 1f), 
            "12" => new Color(0.5f, 0.5f, 0.5f, 1f), 
            // ---------------------------------------------
            _ => new Color(0.2f, 0.2f, 0.2f, 1f),   // Gris
        };
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

        if (cards.Contains(chosen))
        {
            cards.Remove(chosen);
        }

        // Activation des effets
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
            // --- AJOUT CARTE 11 ---
            case "11": card11 = true; break;
            case "12": card12 = true; break;
            // ----------------------
        }

        AddToOwnedCards(chosen);

        // Auto-reroll
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
                slot.GetComponent<Image>().color = getColor(cardName);
                text.text = cardName;

                // --- GESTION DES CARTES DRAGGABLES ---
                // On ajoute "11" à la liste des cartes qui ont besoin du script DraggableCard
                if (cardName == "2" || cardName == "3" || cardName == "6" || cardName == "9" || cardName == "10" || cardName == "11")
                {
                    if (slot.GetComponent<CanvasGroup>() == null) 
                        slot.AddComponent<CanvasGroup>();

                    var existingDrag = slot.GetComponent<DraggableCard>();
                    if (existingDrag == null) 
                    {
                        existingDrag = slot.AddComponent<DraggableCard>();
                    }
                    existingDrag.cardID = cardName;
                }
                
                return;
            }
        }
    }

    public static bool TryActivateCard10()
    {
        // 1. Trouver tous les ennemis
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        
        if (enemies.Length == 0) return false;

        EnemyController bestTarget = null;
        float highestMaxHP = -1f;

        // 2. Chercher celui avec le plus de MaxHealth
        foreach (EnemyController enemy in enemies)
        {
            if (enemy.maxHealth > highestMaxHP)
            {
                highestMaxHP = enemy.maxHealth;
                bestTarget = enemy;
            }
        }

        // 3. Appliquer l'effet
        if (bestTarget != null)
        {
            bestTarget.ApplyLightningMark();
            return true; // Succès
        }

        return false;
    }
}