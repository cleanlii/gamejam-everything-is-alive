using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public List<SpawnPoint> spawnPoints;

    private void Awake()
    {
        if (spawnPoints != null && spawnPoints.Count != 0) return;
        foreach (Transform child in transform)
        {
            if (!child.CompareTag("SpawnPoint")) continue;
            var sp = child.GetComponent<SpawnPoint>();
            if (sp == null) continue;
            spawnPoints?.Add(sp);
        }
    }

    public SpawnPoint GetSpawnPoint(string itemName)
    {
        if (spawnPoints == null || spawnPoints.Count == 0) throw new NotImplementedException("No spawn points!");
        return spawnPoints.Find(sp => sp.itemIndex == itemName);
    }
}
