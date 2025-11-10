using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnPair
{
    public int count;
    public GameObject prefab;
}

public class CircleSpawner : MonoBehaviour
{
    public List<SpawnPair> pairs;
    public float duration = 3f;
    public float radius = 5f;
    public Vector3 center = Vector3.zero; // centre du cercle, relatif au monde

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        int total = 0;
        foreach (var p in pairs) total += Mathf.Max(0, p.count);
        if (total <= 0 || duration <= 0f)
        {
            yield break;
        }

        float dt = duration / total;

        foreach (var p in pairs)
        {
            if (p.prefab == null) continue;
            for (int i = 0; i < p.count; i++)
            {
                // angle aléatoire sur la circonférence (XZ plane)
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Instantiate(p.prefab, pos, Quaternion.identity);
                yield return new WaitForSeconds(dt);
            }
        }
    }
}