using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float frequency = 3f;

    private void Start()
    {
        StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        yield return new WaitForSeconds(frequency);

        if (Enemy.Enemies.Count < Enemy.MaxEnemies)
        {
            NetworkManager.Instance.InstantiateEnemy(transform.position);
        }

        StartCoroutine(SpawnEnemy());
    }
}