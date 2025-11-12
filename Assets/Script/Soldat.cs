using UnityEngine;

public class Soldat : MonoBehaviour
{
    public float range = 1.5f;
    public int soldierInRange = 0;
    public EnemyController me;

    void Update()
    {
        int count = 0;
        Soldat[] allSoldiers = FindObjectsByType<Soldat>(FindObjectsSortMode.None);

        foreach (Soldat s in allSoldiers)
        {
            if (s == this) continue;

            float dist = Vector3.Distance(transform.position, s.transform.position);
            if (dist <= range)
                count++;
        }

        soldierInRange = count;
        me.SetSoldierHeal(soldierInRange);
    }
}
