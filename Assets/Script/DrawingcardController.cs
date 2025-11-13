using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro; // si tu utilises TextMeshPro (sinon remplace par Text)

public class DrawingcardController : MonoBehaviour
{
    [SerializeField] List<string> cards = new List<string>();
    [SerializeField] List<string> temporaryCards = new List<string>();
    [SerializeField] List<string> drawnCards = new List<string>();

    public GameObject[] cardsObjects;         // UI des cartes tirées
    public GameObject[] ownedCardsObjects;    // UI des cartes possédées

    public int handSize = 5;
    public float rerollCost = 2f;
    public GameObject MenuUI;

    // États des cartes
    public static bool card1 = false;
    public static bool card2 = false;
    public static float card2Cooldown = 90f;
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

        // Initialise les slots des cartes possédées comme vides
        foreach (GameObject slot in ownedCardsObjects)
        {
            Image img = slot.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f); // gris vide

            TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = "";
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            drawCard(false);
        }
    }

    void drawCard(bool reroll)
    {
        if (!reroll)
        {
            temporaryCards = new List<string>(cards);
        }

        drawnCards.Clear();

        for (int i = 0; i < handSize; i++)
        {
            if (temporaryCards.Count == 0) break;

            int randomIndex = Random.Range(0, temporaryCards.Count);
            string drawnCard = temporaryCards[randomIndex];
            drawnCards.Add(drawnCard);

            // Change la couleur de la carte tirée
            cardsObjects[i].GetComponent<Image>().color = getColor(drawnCard);
            //cardsObjects[i].GetComponentInChildren<TMP_Text>().text = drawnCard;

            temporaryCards.RemoveAt(randomIndex);
        }

        MenuUI.SetActive(true);
        Debug.Log("Final drawn cards: " + string.Join(", ", drawnCards));
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
            "7" => new Color(1f, 0.5f, 0f, 1f),
            "8" => new Color(0.5f, 1f, 0f, 1f),
            "9" => new Color(0f, 0.5f, 1f, 1f),
            "10" => new Color(1f, 0f, 0.5f, 1f),
            "11" => new Color(0.5f, 0f, 1f, 1f),
            "12" => new Color(0f, 1f, 0.5f, 1f),
            _ => new Color(0.2f, 0.2f, 0.2f, 1f),
        };
    }

    public void rerollCard()
    {
        if (GameManager.Instance.money >= rerollCost)
        {
            GameManager.Instance.money -= rerollCost;
            drawCard(true);
        }
        else
        {
            Debug.Log("Not enough money to reroll cards.");
        }
    }

    public void IChooseCard(float cardIndex)
    {
        int index = (int)cardIndex;
        if (index < 0 || index >= drawnCards.Count) return;

        string chosen = drawnCards[index];
        Debug.Log("Player chose card: " + chosen);

        // Active les bools associés
        switch (chosen)
        {
            case "1": card1 = true; break;
            case "2": card2 = true; break;
            case "3": card3 = true; card3Used = false; break;
            case "4": card4 = true; break;
            case "5": card5 = true; break;
            default: Debug.Log("Unknown card selected."); break;
        }

        // Ajoute dans le premier slot vide de ownedCardsObjects
        AddToOwnedCards(chosen);

        MenuUI.SetActive(false);
        drawnCards.Clear();
    }

    void AddToOwnedCards(string cardName)
    {
        foreach (GameObject slot in ownedCardsObjects)
        {
            TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
            if (text != null && string.IsNullOrEmpty(text.text))
            {
                Image img = slot.GetComponent<Image>();
                if (img != null)
                    img.color = getColor(cardName);

                text.text = cardName;

                // Si la carte a un effet actif, ajoute le script DraggableCard
                if (cardName == "2" || cardName == "3")
                {
                    var draggable = slot.AddComponent<DraggableCard>();
                    draggable.cardID = cardName;
                    slot.AddComponent<CanvasGroup>();
                }

                Debug.Log($"Card {cardName} added to owned cards.");
                return;
            }
        }

        Debug.Log("No empty slot for new card!");
    }

}
