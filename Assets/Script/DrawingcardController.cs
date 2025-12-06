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

    // --- ZONE DE DEBUG ---
    [Header("DEBUG CHEAT")]
    public string debugCardID = "17";
    // ---------------------

    // --- VARIABLES STATIQUES ---
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

    // Carte 11 (Surcharge - Active sur Tour)
    public static bool card11 = false;
    public static float card11Cooldown = 90f; // 1min30
    public static float card11Timer = 0f;     // Timer actuel
    public static float card11Duration = 3f;  // Durée de l'effet

    // Carte 12 (Passif - Range Volcan)
    public static bool card12 = false;

    // Carte 13 (Active - Upgrade Forge)
    public static bool card13 = false;
    public static bool card13Used = false;

    // Carte 14 (Passif - Peste Solaire)
    public static bool card14 = false;

    // Carte 15 (Active - Flamme Solitaire)
    public static bool card15 = false;
    public static bool card15Used = false;

    // Carte 16 (Passif - Flamme Maudite)
    public static bool card16 = false;

    // --- AJOUT CARTE 17 (Active - Incandescence) ---
    public static bool card17 = false;
    public static bool card17Used = false;
    // -----------------------------------------------

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
        card17 = false;
        card17Used = false;
        // ----------------------

        card2Timer = 0f;
        card3Used = false;
    }

    void Update()
    {
        // --- INPUT DEBUG (Touche M) ---
        if (Input.GetKeyDown(KeyCode.M))
        {
            switch (debugCardID)
            {
                case "1":
                    if (!card1)
                    {
                        card1 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "2":
                    if (!card2)
                    {
                        card2 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "3":
                    if (!card3)
                    {
                        card3 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "4":
                    if (!card4)
                    {
                        card4 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "5":
                    if (!card5)
                    {
                        card5 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "6":
                    if (!card6)
                    {
                        card6 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "7":
                    if (!card7)
                    {
                        card7 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "8":
                    if (!card8)
                    {
                        card8 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "9":
                    if (!card9)
                    {
                        card9 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "10":
                    if (!card10)
                    {
                        card10 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "11":
                    if (!card11)
                    {
                        card11 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "12":
                    if (!card12)
                    {
                        card12 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "13":
                    if (!card13)
                    {
                        card13 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "14":
                    if (!card14)
                    {
                        card14 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "15":
                    if (!card15)
                    {
                        card15 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "16":
                    if (!card16)
                    {
                        card16 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                case "17":
                    if (!card17)
                    {
                        card17 = true;
                        AddToOwnedCards(debugCardID);
                    }
                    break;
                default: Debug.LogWarning("ID de carte inconnu : " + debugCardID); return;
            }

        }
        // ------------------------------

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
        if (card11Timer > 0f) card11Timer -= Time.deltaTime;
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
                display.SetCardData (data);
                temporaryCards.RemoveAt(randomIndex);
            }
            else
            {
                drawnCards.Add("EMPTY");
                CardSlotDisplay display = cardsObjects[i].GetComponent<CardSlotDisplay>();
                display.SetCardData(null);
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
            case "11": card11 = true; break;
            case "12": card12 = true; break;
            case "13": card13 = true; card13Used = false; break;
            case "14": card14 = true; break;
            case "15": card15 = true; card15Used = false; break;
            case "16": card16 = true; break;
            case "17": card17 = true; card17Used = false; break;
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
                text.text = cardName;

                // --- GESTION DES CARTES DRAGGABLES ---

                if (cardName == "2" || cardName == "3" || cardName == "6" || cardName == "9" ||
                    cardName == "10" || cardName == "11" || cardName == "13" || cardName == "15" ||
                    cardName == "17")
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
    CardManger GetCardByID(string id)
    {
    foreach (var c in cardDatas) 
    {
        if (c.cardID == id)
            return c;
    }
    Debug.LogWarning("Carte introuvable : " + id);
    return null;
    }

}