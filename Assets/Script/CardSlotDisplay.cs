using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CardSlotDisplay : MonoBehaviour
{
    public TMP_Text cardNameText;
    public Image cardImage;
    public TMP_Text cardDescriptionText;
    private CardManger cardData;
    public void SetCardData(CardManger data)
    {
        cardData = data;
        Refresh();
    }
    void Refresh()
    {
        if (cardData != null)
        {
            cardNameText.text = cardData.cardName;
            cardImage.sprite = cardData.lefondelacarte;
            cardDescriptionText.text = cardData.cardDescription;
        }
        else
        {
            cardNameText.text = "";
            cardImage.sprite = null;
            cardDescriptionText.text = "";
        }
    }
    public CardManger GetCardData()
    {
        return cardData;
    }
}
