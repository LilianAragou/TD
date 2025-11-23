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

    // ATTENTION : Les variables statiques restent entre les scènes.
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
        card1 = false; card2 = false; card3 = false; card4 = false; card5 = false;
        card2Timer = 0f;
        card3Used = false;
    }

    void Update()
    {
        // MODIFICATION 1 : LOGIQUE DE TOGGLE (OUVRIR/FERMER)
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (MenuUI.activeSelf)
            {
                // Si ouvert, on ferme
                MenuUI.SetActive(false);
            }
            else
            {
                // Si fermé, on ouvre.
                // Si la main est vide (premier lancement), on pioche.
                if (drawnCards.Count == 0)
                {
                    drawCard(false);
                }
                else
                {
                    // Sinon on affiche juste la main existante (qui a été pré-rollée)
                    MenuUI.SetActive(true);
                }
            }
        }

        if (rerollbutton)
            rerollbutton.text = "Reroll = " + rerollCost + "$";

        if (card2Timer > 0f)
        {
            card2Timer -= Time.deltaTime;
        }
    }

    void drawCard(bool reroll)
    {
        // On remplit la liste temporaire avec TOUTES les cartes restantes
        temporaryCards = new List<string>(cards);

        drawnCards.Clear();

        for (int i = 0; i < handSize; i++)
        {
            Image slotImage = cardsObjects[i].GetComponent<Image>();

            if (temporaryCards.Count > 0)
            {
                // Cas normal : Il reste des cartes
                int randomIndex = Random.Range(0, temporaryCards.Count);
                string drawnCard = temporaryCards[randomIndex];

                drawnCards.Add(drawnCard);
                slotImage.color = getColor(drawnCard);

                // On retire de la liste temporaire pour éviter les doublons dans la main actuelle
                temporaryCards.RemoveAt(randomIndex);
            }
            else
            {
                // Cas vide : Cartes noires
                drawnCards.Add("EMPTY");
                slotImage.color = Color.black;
            }
        }

        // On affiche le menu à la fin du tirage
        MenuUI.SetActive(true);
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
            _ => new Color(0.2f, 0.2f, 0.2f, 1f),
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

        // Sécurité Clic sur case vide
        if (chosen == "EMPTY") return;

        // Suppression définitive du pool global
        if (cards.Contains(chosen))
        {
            cards.Remove(chosen);
        }

        // Activation
        switch (chosen)
        {
            case "1": card1 = true; break;
            case "2": card2 = true; break;
            case "3": card3 = true; card3Used = false; break;
            case "4": card4 = true; break;
            case "5": card5 = true; break;
        }

        AddToOwnedCards(chosen);

        // MODIFICATION 2 : AUTO-REROLL POUR LA PROCHAINE FOIS
        // 1. On génère immédiatement la nouvelle main avec les cartes restantes (sans payer)
        drawCard(false);
        
        // 2. Mais on ferme le menu tout de suite pour ne pas gêner le joueur.
        // La prochaine fois qu'il appuiera sur 'D', la main sera déjà prête.
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

                if (cardName == "2" || cardName == "3")
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
}