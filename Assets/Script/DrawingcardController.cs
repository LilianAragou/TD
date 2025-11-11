using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class DrawingcardController : MonoBehaviour
{
    [SerializeField] List<string> cards = new List<string>();

    [SerializeField] List<string> temporaryCards = new List<string>();
    [SerializeField] List<string> drawnCards = new List<string>();

    public GameObject[] cardsObjects;
    public int handSize = 5;
    public float rerollCost = 2f;
    public GameObject MenuUI;
    void Start()
    {
        MenuUI.SetActive(false);
    }

    // Update is called once per frame
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

        for (int i = 0; i < handSize; i++)
        {
            int randomIndex = Random.Range(0, temporaryCards.Count);
            string drawnCard = temporaryCards[randomIndex];
            Debug.Log("Drew card: " + drawnCard);
            cardsObjects[i].GetComponent<Image>().color = getColor(drawnCard);
            temporaryCards.RemoveAt(randomIndex);
            drawnCards.Add(drawnCard);
            MenuUI.SetActive(true);
        }
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
        drawCard(true);
        /*
        if (gameManager.money >= rerollCost)
        {
            gameManager.money -= rerollCost;
            drawCard(true);
        }
        else
        {
            Debug.Log("Not enough money to reroll cards.");
        }*/
    }
    public void IChooseCard(float cardIndex)
    {
        Debug.Log("Player chose card: " + drawnCards[(int)cardIndex]);
        MenuUI.SetActive(false);
        drawnCards.Clear();
    }
}