using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnPair
{
    public int count;
    public GameObject prefab;
}

[System.Serializable]
public struct Wave
{
    public List<SpawnPair> pairs;
    public float delayBeforeWave; // délai avant le début de la vague
    public float duration;        // durée totale sur laquelle les ennemis apparaissent
}

public class CircleSpawner : MonoBehaviour
{
    [Header("Waves configuration")]
    public List<Wave> waves = new List<Wave>();

    [Header("Spawn settings")]
    public float radius = 5f;
    public Vector3 center = Vector3.zero;

    private int currentWaveIndex = 0;
    private float hpMultiplier = 1f;
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        while (true)
        {
            Wave wave = (currentWaveIndex < waves.Count)
                ? waves[currentWaveIndex]
                : waves[waves.Count - 1]; // rejoue la dernière vague

            // Attente avant la vague
            if (wave.delayBeforeWave > 0)
                yield return new WaitForSeconds(wave.delayBeforeWave);

            // Spawn de la vague
            yield return StartCoroutine(SpawnWave(wave));

            // Attendre que tous les ennemis soient morts
            yield return StartCoroutine(WaitUntilEnemiesDead());

            // Si on a fini toutes les vagues, augmenter la difficulté
            if (currentWaveIndex >= waves.Count - 1)
            {
                hpMultiplier *= 1.1f; // +10% PV
                Debug.Log($"🔁 Reprise de la dernière vague avec HP x{hpMultiplier:F2}");
            }
            else
            {
                currentWaveIndex++;
            }
        }
    }

    IEnumerator SpawnWave(Wave wave)
    {
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        foreach (var pair in wave.pairs)
        {
            if (pair.prefab == null || pair.count <= 0) continue;

            // Lance une coroutine séparée pour ce type d'ennemi
            Coroutine c = StartCoroutine(SpawnEnemyType(pair, wave.duration));
            activeCoroutines.Add(c);
        }

        // Attendre que toutes les sous-coroutines soient terminées
        foreach (Coroutine c in activeCoroutines)
        {
            yield return c;
        }
    }
    
    IEnumerator SpawnEnemyType(SpawnPair pair, float duration)
    {
        float interval = (pair.count > 0 && duration > 0) ? duration / pair.count : 0f;

        for (int i = 0; i < pair.count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle),0f , Mathf.Sin(angle)) * radius;

            GameObject enemy = Instantiate(pair.prefab, pos, Quaternion.identity);

            // 🔹 Ajustement PV si le prefab a un EnemyController
            EnemyController ctrl = enemy.GetComponent<EnemyController>();
            if (ctrl != null)
            {
                ctrl.SetHealth(ctrl.baseHealth * hpMultiplier);
            }

            activeEnemies.Add(enemy);

            // Attendre avant le prochain spawn de ce type
            if (interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }



    IEnumerator WaitUntilEnemiesDead()
    {
        activeEnemies.RemoveAll(e => e == null);
        while (activeEnemies.Count > 0)
        {
            activeEnemies.RemoveAll(e => e == null);
            yield return null;
        }
    }
}
