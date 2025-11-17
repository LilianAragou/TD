using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float money;
    public TextMeshProUGUI text;
    void Start()
    {
        
    }

    void Update()
    {
        text.text = money + "$";
    }
}
