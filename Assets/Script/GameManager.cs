using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Instance unique
    public static GameManager Instance { get; private set; }

    public float money;

    void Awake()
    {
        // Si une autre instance existe, détruire ce GameObject
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Sinon, définir cette instance et la garder entre les scènes
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Initialisation si nécessaire
    }

    void Update()
    {
        // Logique du GameManager
    }
}
